using UnityEngine;
using UnityEngine.Rendering;

namespace LangTroTan.Presentation
{
    // Tàng Kinh Các Level 5 ornamental jade crystal (separate prop on the second-floor front) and its subtle ambient
    // effects: a slow jade emission pulse, a soft halo behind the crystal and a few rising spirit motes. Presentation
    // only and gated: it does nothing unless -visual-tiers is on. With reduced effects the crystal keeps a steady,
    // dim glow and the halo pulse and motes are off.
    public sealed class SonMonJadeAura : MonoBehaviour
    {
        static readonly Color Jade = new Color(.12f, .66f, .55f);
        static readonly Color JadeGlow = new Color(.35f, 1f, .82f);
        // Full effects use a deeper tint and a lower pulse so the faceted turquoise-green core stays visible (QA review
        // 2026-10-29: the first pulse washed the crystal out to near white). Reduced effects keep their steady glow.
        static readonly Color FullTint = new Color(.10f, .72f, .56f);
        static readonly Color Gold = new Color(.74f, .56f, .26f);
        static readonly Color Lacquer = new Color(.45f, .09f, .07f);
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        const float PulsePeriod = 4.5f, SteadyGlow = .35f, PulseLow = .12f, PulseHigh = .32f;

        public bool Reduced { get; private set; }
        Material jade, halo;
        ParticleSystem motes;
        float phase;

        // Builds materials and effects for the prop's JadeCrystal and GoldFrame children. Returns false (and removes
        // itself) when the visual-tier gate is off.
        public bool Build()
        {
            if (!SonMonTiers.Enabled) { Kill(this); return false; }
            Reduced = SonMonUpgradeFx.ReducedEffects;
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            foreach (var r in GetComponentsInChildren<MeshRenderer>())
            {
                bool isJade = r.name.Contains("JadeCrystal"), isPlate = r.name.Contains("Backplate");
                var m = new Material(lit) { name = isJade ? "Jade crystal (runtime)" : isPlate ? "Lacquer backplate (runtime)" : "Carved gold frame (runtime)" };
                m.SetColor(BaseColor, isJade ? Jade : isPlate ? Lacquer : Gold);
                m.SetFloat("_Metallic", isJade || isPlate ? 0f : 1f);
                m.SetFloat("_Smoothness", isJade ? .85f : isPlate ? .45f : .55f);
                if (isJade) { m.EnableKeyword("_EMISSION"); jade = m; }
                r.sharedMaterial = m;
            }
            BuildHalo();
            if (!Reduced) BuildMotes();
            Apply(0);
            return true;
        }

        void BuildHalo()
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Kill(quad.GetComponent<Collider>());
            quad.name = "Jade halo";
            quad.transform.SetParent(transform, false);
            quad.transform.localPosition = new Vector3(0, 0, -.16f);  // soft additive glow just in front of the crystal
            quad.transform.localScale = new Vector3(.8f, 1f, 1);
            halo = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Jade halo (runtime)" };
            halo.SetTexture("_BaseMap", RadialTexture());
            halo.SetFloat("_Surface", 1);
            halo.SetFloat("_Blend", 2);  // additive
            halo.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            halo.SetInt("_DstBlend", (int)BlendMode.One);
            halo.SetInt("_ZWrite", 0);
            halo.SetFloat("_Cull", 0);
            halo.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            halo.renderQueue = 3000;
            var r = quad.GetComponent<MeshRenderer>();
            r.sharedMaterial = halo;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }

        void BuildMotes()
        {
            var go = new GameObject("Spirit motes");
            go.transform.SetParent(transform, false);
            motes = go.AddComponent<ParticleSystem>();
            motes.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = motes.main;
            main.loop = true;
            main.maxParticles = 18;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
            main.startSpeed = 0;
            main.startSize = new ParticleSystem.MinMaxCurve(.03f, .07f);
            main.startColor = new Color(JadeGlow.r, JadeGlow.g, JadeGlow.b, .55f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = motes.emission;
            emission.rateOverTime = 4;
            var shape = motes.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = .35f;
            var velocity = motes.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.y = new ParticleSystem.MinMaxCurve(.12f, .25f);
            velocity.x = new ParticleSystem.MinMaxCurve(-.03f, .03f);
            velocity.z = new ParticleSystem.MinMaxCurve(-.03f, .03f);
            var fade = motes.colorOverLifetime;
            fade.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(JadeGlow, 0), new GradientColorKey(JadeGlow, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(.6f, .3f), new GradientAlphaKey(0, 1) });
            fade.color = g;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = halo;
            motes.Play();
        }

        void Update()
        {
            phase += Time.deltaTime;
            Apply(phase);
        }

        // Deterministic for captures: the visual state at time t (seconds).
        public void Apply(float t)
        {
            float k = Reduced ? SteadyGlow : Mathf.Lerp(PulseLow, PulseHigh, .5f + .5f * Mathf.Sin(t * 2 * Mathf.PI / PulsePeriod));
            if (jade != null) jade.SetColor(EmissionColor, (Reduced ? JadeGlow : FullTint) * k);
            if (halo != null) halo.SetColor(BaseColor, new Color(JadeGlow.r, JadeGlow.g, JadeGlow.b, Reduced ? .18f : .08f + .14f * k));
        }

        static Texture2D RadialTexture()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Jade halo radial" };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f - .5f, n / 2f - .5f)) / (n / 2f);
                    float a = Mathf.Clamp01(1 - d);
                    tex.SetPixel(x, y, new Color(1, 1, 1, a * a));
                }
            tex.Apply();
            return tex;
        }

        // Destroy in play mode, DestroyImmediate in edit-mode captures.
        static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        void OnDestroy()
        {
            if (halo != null) Kill(halo.mainTexture);
            foreach (var r in GetComponentsInChildren<MeshRenderer>())
                if (r.sharedMaterial != null && r.sharedMaterial.name.EndsWith("(runtime)")) Kill(r.sharedMaterial);
        }
    }
}
