using System;
using System.Collections.Generic;
using UnityEngine;

namespace LangTroTan.Presentation
{
    // Building upgrade transition, played after the authoritative server has confirmed a higher level:
    // glow + gathering energy -> rune formation -> controlled model change -> burst of light -> the UI is
    // told the new level. Presentation only and non-blocking (input, camera and selection stay live). It
    // never runs for a level that was loaded from a save; that path builds the right model directly.
    public sealed class SonMonUpgradeFx : MonoBehaviour
    {
        // Full sequence 3.6 s (inside the 3-5 s design window); reduced effects 1.2 s, no particles.
        public const float FullDuration = 3.6f, ReducedDuration = 1.2f;
        const float FullSwapAt = 2.2f, ReducedSwapAt = .5f;
        const string ReducedKey = "sonmon.reducedEffects";
        static readonly Color GlowColor = new Color(.55f, 1f, .82f);
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        static bool? reduced;
        public static bool? SessionOverride;  // QA: test the reduced path without touching saved preferences

        // Player preference (persisted; also forced by the -reduced-effects launch flag).
        public static bool ReducedEffects
        {
            get
            {
                if (SessionOverride.HasValue) return SessionOverride.Value;
                if (reduced == null)
                {
                    bool flag = Array.IndexOf(Environment.GetCommandLineArgs(), "-reduced-effects") >= 0;
                    try { reduced = flag || PlayerPrefs.GetInt(ReducedKey, 0) == 1; } catch { reduced = flag; }
                }
                return reduced.Value;
            }
            set
            {
                reduced = value;
                try { PlayerPrefs.SetInt(ReducedKey, value ? 1 : 0); PlayerPrefs.Save(); } catch { }
            }
        }

        public static event Action<Transform, int, int> Started;
        public static event Action<Transform, int, int, bool> Finished;  // root, from, to, cancelled

        public string Type { get; private set; }
        public int FromLevel { get; private set; }
        public int ToLevel { get; private set; }
        public bool Playing { get; private set; }
        public bool Swapped { get; private set; }
        public bool Reduced { get; private set; }
        public float Elapsed => t;
        public float Duration => Reduced ? ReducedDuration : FullDuration;
        float SwapAt => Reduced ? ReducedSwapAt : FullSwapAt;

        float t;
        GameObject fx;
        ParticleSystem gather, burst;
        readonly List<(Renderer renderer, Color color)> formations = new List<(Renderer, Color)>();
        readonly List<(Renderer renderer, Color color)> pillars = new List<(Renderer, Color)>();
        readonly List<(Renderer renderer, Material[] originals, Material[] instances, Color[] baseEmission)> glow =
            new List<(Renderer, Material[], Material[], Color[])>();
        MaterialPropertyBlock block;
        Transform visual;

        public static bool IsPlaying(Transform root, out SonMonUpgradeFx fx)
        {
            fx = root != null ? root.GetComponent<SonMonUpgradeFx>() : null;
            return fx != null && fx.Playing;
        }

        public static SonMonUpgradeFx Play(Transform root, string type, int from, int to)
        {
            var fx = root.GetComponent<SonMonUpgradeFx>();
            if (fx != null && fx.Playing) { fx.Retarget(to); return fx; }
            if (fx == null) fx = root.gameObject.AddComponent<SonMonUpgradeFx>();
            fx.Begin(type, from, to);
            return fx;
        }

        void Begin(string type, int from, int to)
        {
            Type = type;
            FromLevel = from;
            ToLevel = to;
            Reduced = ReducedEffects;
            Playing = true;
            Swapped = false;
            t = 0;
            block ??= new MaterialPropertyBlock();
            visual = transform.Find("SonMonVisual");
            ApplyGlow(visual);
            var kit = SonMonKit.Instance;
            if (!Reduced && kit != null && kit.upgradeFx != null)
            {
                fx = Instantiate(kit.upgradeFx, transform, false);
                fx.name = kit.upgradeFx.name;
                // Scale the effect to the building's footprint (authored for a 3x3 building).
                if (visual != null && SonMonArt.LocalVisualBounds(transform, visual, out var b))
                    fx.transform.localScale = Vector3.one * Mathf.Clamp(Mathf.Max(b.size.x, b.size.z) / 3.2f, .6f, 2.5f);
                foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>())
                {
                    if (ps.name == "Gather") gather = ps;
                    else if (ps.name == "Burst") burst = ps;
                }
                foreach (var r in fx.GetComponentsInChildren<MeshRenderer>())
                {
                    var color = r.sharedMaterial != null && r.sharedMaterial.HasProperty(BaseColor) ? r.sharedMaterial.GetColor(BaseColor) : Color.white;
                    (r.name.StartsWith("Pillar") ? pillars : formations).Add((r, color));
                    Fade(r, color, 0);
                }
                if (gather != null) gather.Play();
            }
            Debug.Log($"SONMON_FX begin {name} {FromLevel}->{ToLevel} reduced={Reduced} frame={Time.frameCount} t={Time.realtimeSinceStartup:0.00}");
            Started?.Invoke(transform, FromLevel, ToLevel);
        }

        // A newer server level arrived while playing. Before the model change the change simply targets
        // the newer level; after it, the visual is swapped again at once and the burst replays.
        public void Retarget(int level)
        {
            if (!Playing || level <= ToLevel) return;
            ToLevel = level;
            if (!Swapped) return;
            Swap();
            t = SwapAt;
        }

        // Stop now and leave the building in a clean state (shared materials restored, effect removed).
        // The visual stays whatever it currently is; the caller decides the final model.
        public void Cancel() => End(true);

        void Update()
        {
            if (!Playing) return;
            t += Time.unscaledDeltaTime;
            float swapAt = SwapAt, duration = Duration;
            if (!Swapped && t >= swapAt) Swap();
            if (visual == null && !Swapped) { End(true); return; }  // visual removed by something else

            // Glow: rises to the change, peaks at it, decays afterwards.
            float k = t < swapAt ? Mathf.SmoothStep(0, 1.15f, t / swapAt) : Mathf.Lerp(1.15f, 0, Mathf.Clamp01((t - swapAt) / (duration - swapAt - .2f)));
            SetGlow(k);
            if (visual != null)
            {
                // Controlled transition: the old model settles slightly before the change, the new one
                // rises from the same scale with a small overshoot.
                float s = 1;
                if (!Swapped) s = 1 - .08f * Mathf.Clamp01((t - (swapAt - .3f)) / .3f);
                else { float u = Mathf.Clamp01((t - swapAt) / .45f); s = .92f + .08f * u + Mathf.Sin(u * Mathf.PI) * .04f; }
                visual.localScale = new Vector3(1, s, 1);
            }
            if (fx != null)
            {
                float formation = t < swapAt ? Mathf.Clamp01((t - .45f) / .8f) : 1 - Mathf.Clamp01((t - (swapAt + .3f)) / 1f);
                float spin = 30 + 260 * Mathf.Clamp01(t / swapAt);
                foreach (var (r, color) in formations)
                {
                    if (r == null) continue;
                    Fade(r, color, formation * (r.name.EndsWith("outer") ? .7f : 1f));
                    float dir = r.name.EndsWith("outer") ? -.5f : 1f;
                    r.transform.Rotate(0, 0, spin * dir * Time.unscaledDeltaTime, Space.Self);
                    float grow = .55f + .45f * Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - .45f) / 1.1f));
                    r.transform.localScale = new Vector3(grow, grow, 1) * (r.name.EndsWith("outer") ? 6.4f : 4.2f);
                }
                float pillar = t < swapAt ? Mathf.Clamp01((t - 1.3f) / .9f) * .35f : Mathf.Lerp(1, 0, Mathf.Clamp01((t - swapAt) / .8f));
                foreach (var (r, color) in pillars) if (r != null) Fade(r, color, pillar);
                if (gather != null && t >= swapAt - .2f && gather.isEmitting) gather.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            if (t >= duration) End(false);
        }

        void Swap()
        {
            Swapped = true;
            RestoreGlow();
            var replaced = SonMonArt.SwapVisual(transform, Type, ToLevel);
            visual = replaced;
            if (visual != null)
            {
                visual.localScale = new Vector3(1, .92f, 1);
                ApplyGlow(visual);
            }
            if (burst != null) burst.Emit(Reduced ? 0 : 160);
        }

        void End(bool cancelled)
        {
            if (!Playing) return;
            Playing = false;
            RestoreGlow();
            if (visual != null) visual.localScale = Vector3.one;
            if (fx != null) Destroy(fx);
            fx = null;
            gather = burst = null;
            formations.Clear();
            pillars.Clear();
            Debug.Log($"SONMON_FX end {FromLevel}->{ToLevel} cancelled={cancelled} elapsed={t:0.00} frame={Time.frameCount}");
            Finished?.Invoke(transform, FromLevel, ToLevel, cancelled);
            Destroy(this);
        }

        void OnDestroy()
        {
            // Building removed mid-transition (demolished, scene change): never leak material instances.
            RestoreGlow();
            Playing = false;
        }

        void Fade(Renderer r, Color color, float alpha)
        {
            r.GetPropertyBlock(block);
            block.SetColor(BaseColor, color * alpha);
            r.SetPropertyBlock(block);
            r.enabled = alpha > .001f;
        }

        // Temporary emissive instances on the building's own renderers (restored after the transition).
        void ApplyGlow(Transform target)
        {
            if (target == null) return;
            foreach (var r in target.GetComponentsInChildren<MeshRenderer>())
            {
                if (r.GetComponent<TextMesh>() != null) continue;
                var originals = r.sharedMaterials;
                var instances = new Material[originals.Length];
                var baseEmission = new Color[originals.Length];
                bool any = false;
                for (int i = 0; i < originals.Length; i++)
                {
                    var m = originals[i];
                    if (m == null || !m.HasProperty(EmissionColor)) { instances[i] = m; continue; }
                    baseEmission[i] = m.IsKeywordEnabled("_EMISSION") ? m.GetColor(EmissionColor) : Color.black;
                    instances[i] = new Material(m) { name = m.name + " (upgrade glow)" };
                    instances[i].EnableKeyword("_EMISSION");
                    any = true;
                }
                if (!any) continue;
                r.sharedMaterials = instances;
                glow.Add((r, originals, instances, baseEmission));
            }
        }

        void SetGlow(float k)
        {
            var add = GlowColor * (k * k * 1.6f);
            foreach (var (_, originals, instances, baseEmission) in glow)
                for (int i = 0; i < instances.Length; i++)
                    if (instances[i] != originals[i]) instances[i].SetColor(EmissionColor, baseEmission[i] + add);
        }

        void RestoreGlow()
        {
            foreach (var (r, originals, instances, _) in glow)
            {
                if (r != null) r.sharedMaterials = originals;
                for (int i = 0; i < instances.Length; i++)
                    if (instances[i] != null && instances[i] != originals[i]) Destroy(instances[i]);
            }
            glow.Clear();
        }
    }
}
