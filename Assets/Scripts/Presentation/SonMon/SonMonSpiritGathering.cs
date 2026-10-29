using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LangTroTan.Presentation
{
    // Tàng Kinh Các Level 6 "Tụ Linh Kinh Các" ambient spirit-gathering effects, kept separate from the building mesh.
    //   1 Formation 聚灵阵: an ancient jade rune band embracing the terrace inside the permitted footprint, with a smooth
    //     rune-by-rune activation wave, and four corner seals of counter-rotating rune rings lit in sequence
    //   2 Four spirit pillars: jade cores and halos on the existing pillar caps with staggered pulses, rising motes, and
    //     thin noise-distorted spirit wisps (three per pillar) curling from the pillars into the central hall
    //   3 Scripture characters: small golden characters rising from the hall's eaves, fading in, floating and
    //     dissolving into specks
    //   4 Spirit mist: sparse jade wisps and motes drifting inwards and gathering around the hall, periodic gathering
    //     at the round jade ornament
    // Strip and particle shaders animate on the GPU from the global _SpiritTime. Every looping motion is a whole
    // number of repetitions per 10 s (LoopSeconds), so loops are seamless; particles flow continuously (no resets).
    // Modes: Full; Reduced (steady dim pillar glow, faint static formation; no streams, glyphs, mist or motes); Off
    // (nothing built). Gated by -visual-tiers; -no-ambient-fx turns it off. Distance LOD: near (zoom 10) everything;
    // mid (zoom 21) formation, seals, streams, mist and pillar glow, no glyphs or motes; far (zoom 30) formation and
    // pillar glow only. Presentation only: no colliders, Ignore Raycast layer, follows the building root, nothing
    // allocated per frame.
    public sealed class SonMonSpiritGathering : MonoBehaviour
    {
        public enum Mode { Off, Reduced, Full }
        public enum Lod { Near, Mid, Far }

        public static Mode? ModeOverride;  // QA only
        public static bool SimulateInEditMode = true;  // QA captures advance particles manually in edit mode
        static readonly bool NoAmbientFlag = Array.IndexOf(Environment.GetCommandLineArgs(), "-no-ambient-fx") >= 0;
        public static Mode CurrentMode =>
            ModeOverride ?? (!SonMonTiers.Enabled || NoAmbientFlag ? Mode.Off : SonMonUpgradeFx.ReducedEffects ? Mode.Reduced : Mode.Full);

        // Camera distances for the gameplay zooms (distance = zoom * 2.7): 10 -> 27, 21 -> 57, 30 -> 81.
        public const float MidDistance = 42f, FarDistance = 70f;
        public const float AwakenDuration = 2.4f;
        public const float LoopSeconds = 10f;
        static readonly int SpiritTimeId = Shader.PropertyToID("_SpiritTime");
        static int timeFrame = -1;

        // Sets the shared animation clock (seconds). Wrapped on 120 s, a multiple of every loop length.
        public static void SetSpiritTime(double seconds) => Shader.SetGlobalFloat(SpiritTimeId, (float)(seconds % 120.0));
        static readonly Color Jade = new Color(.35f, 1f, .85f);
        static readonly Color Cyan = new Color(.45f, .95f, 1f);
        static readonly Color Gold = new Color(1f, .84f, .45f);
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int AnimateId = Shader.PropertyToID("_Animate");

        public Mode ActiveMode { get; private set; } = Mode.Off;
        public Lod ActiveLod { get; private set; } = Lod.Near;

        // Anchors in this transform's local space (the building's bottom-centre, entrance towards -Z).
        [SerializeField] Vector3[] pillars = new Vector3[0];
        [SerializeField] Vector3 ornament, hallMin, hallMax;
        [SerializeField] Vector4 tier1, tier2;  // x0, x1, z0, z1
        [SerializeField] float tier1Top = .47f, tier2Top = .94f;
        [SerializeField] Vector4 allowed = new Vector4(-1e3f, 1e3f, -1e3f, 1e3f);  // permitted footprint x0, x1, z0, z1

        MeshRenderer band, seals, streams, pulse;
        readonly List<Mesh> meshes = new List<Mesh>();
        ParticleSystem halos, cores, pillarMotes, runes, mist, motes, gather;
        readonly ParticleSystem.Particle[] haloBuffer = new ParticleSystem.Particle[8];
        readonly ParticleSystem.Particle[] coreBuffer = new ParticleSystem.Particle[8];
        readonly Rect[] mistRects = new Rect[6];
        readonly float[] mistHeights = new float[6];
        ParticleSystem.EmitParams emit;
        MaterialPropertyBlock block;
        float clock, awakenT = -1, nextGather, nextMote, nextMist, nextGatherMote;
        Vector2 gatherRadius = new Vector2(2.6f, 2.2f);
        Bounds wispRawBounds;

        Vector3 HallCentre(float y) => new Vector3((hallMin.x + hallMax.x) / 2, y, (hallMin.z + hallMax.z) / 2);
        float[] pillarPhase;
        float bandIntensity, sealIntensity, streamIntensity;
        Vector3[] sealCenters = new Vector3[4];
        System.Random rng;
        bool built;
        static Camera cam;
        static float camLookup;

        // ---------------------------------------------------------------- setup
        public void Configure(Vector3[] pillarCrystals, Vector3 ornamentLocal, Vector3 hallMinLocal, Vector3 hallMaxLocal,
            Vector4 tier1XZ, Vector4 tier2XZ, float tier1Height, float tier2Height, Vector4 allowedXZ)
        {
            pillars = pillarCrystals;
            ornament = ornamentLocal;
            hallMin = hallMinLocal;
            hallMax = hallMaxLocal;
            tier1 = tier1XZ;
            tier2 = tier2XZ;
            tier1Top = tier1Height;
            tier2Top = tier2Height;
            allowed = allowedXZ;
        }

        // Builds for the current mode. awaken: play the Level 5 -> 6 awakening (only right after a server-confirmed
        // upgrade swap; never when a saved game is loaded).
        public bool Build(bool? awaken = null)
        {
            Clear();
            ActiveMode = CurrentMode;
            if (ActiveMode == Mode.Off) return false;
            rng = new System.Random(6006);
            block ??= new MaterialPropertyBlock();
            gameObject.layer = 2;  // Ignore Raycast: never interferes with selection
            bool full = ActiveMode == Mode.Full;
            band = Strip("Formation bands", Shared.BandMaterial, BuildBandMesh());
            seals = Strip("Formation seals", Shared.SealMaterial, BuildSealMesh());
            if (full) streams = Strip("Spirit streams", Shared.StreamMaterial, BuildStreamMesh());

            pillarPhase = new float[pillars.Length];
            for (int i = 0; i < pillars.Length; i++) pillarPhase[i] = i * Mathf.PI * .5f;  // quarter-period stagger
            // Pillar glow = an alpha-blended jade core that visibly brightens each crystal (additive light alone
            // washes out to white on pale stone and sky) plus a soft additive halo around it.
            halos = System("Pillar glow", Shared.GlowMaterial, 8, false);
            cores = System("Pillar crystal cores", Shared.CoreMaterial, 8, false);
            for (int i = 0; i < pillars.Length; i++)
            {
                emit.position = pillars[i];
                emit.velocity = Vector3.zero;
                emit.startLifetime = 1e6f;
                emit.startSize = .5f;
                emit.startColor = Jade;
                halos.Emit(emit, 1);
                cores.Emit(emit, 1);
            }
            if (full)
            {
                pillarMotes = System("Pillar motes", Shared.GlowMaterial, 16, false);
                runes = GlyphSystem();
                mist = System("Spirit mist", Shared.MistMaterial, 16, false);
                motes = System("Gathering motes", Shared.GlowMaterial, 24, false);
                gather = System("Ornament gathering", Shared.GlowMaterial, 12, false);
                BuildMistRects();
                gatherRadius = new Vector2(Mathf.Max(Mathf.Abs(tier1.x), Mathf.Abs(tier1.y)) - .3f, (tier1.w - tier1.z) * .5f - .3f);
                nextGather = 3f;
            }
            bool awakenNow = awaken ?? (transform.parent != null && SonMonUpgradeFx.IsPlaying(transform.parent, out var fx) && fx.ToLevel == 6 && fx.Swapped);
            awakenT = awakenNow ? 0 : -1;
            if (awakenNow && full)
                pulse = Quad("Awakening pulse", Shared.PulseMaterial, new Vector3(0, .02f, (tier1.z + tier1.w) / 2), Vector2.one);
            clock = 0;
            built = true;
            ApplyLod(Lod.Near);
            Step(0, transform.position + new Vector3(0, 10, -20));
            return true;
        }

        void Start()
        {
            if (!built && pillars.Length > 0) Build();
        }

        public void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Kill(transform.GetChild(i).gameObject);
            foreach (var m in meshes) Kill(m);
            meshes.Clear();
            band = seals = streams = pulse = null;
            halos = cores = pillarMotes = runes = mist = motes = gather = null;
            built = false;
        }

        void OnDestroy()
        {
            foreach (var m in meshes) Kill(m);
            meshes.Clear();
        }

        static void Kill(UnityEngine.Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        // ---------------------------------------------------------------- per frame
        void Update()
        {
            if (!built) return;
            if (timeFrame != Time.frameCount)  // once per frame for every instance
            {
                timeFrame = Time.frameCount;
                SetSpiritTime(Time.timeAsDouble);
            }
            if (cam == null || Time.unscaledTime > camLookup)
            {
                cam = Camera.main;
                camLookup = Time.unscaledTime + 1f;
            }
            Step(Time.deltaTime, cam != null ? cam.transform.position : transform.position);
        }

        // Advances the effects by dt as seen from cameraPos (used by Update and by deterministic QA captures; in edit
        // mode it also advances the particle systems, since the strips animate on the GPU from _Time).
        public void Step(float dt, Vector3 cameraPos)
        {
            if (!built) return;
            clock += dt;
            float d = Vector3.Distance(cameraPos, transform.position);
            var lod = d < MidDistance ? Lod.Near : d < FarDistance ? Lod.Mid : Lod.Far;
            if (lod != ActiveLod) ApplyLod(lod);
            bool full = ActiveMode == Mode.Full;
            float a = awakenT >= 0 ? (awakenT += dt) : AwakenDuration;
            bool awakening = a < AwakenDuration;

            // 1 formation and streams: intensities change only on LOD / mode changes, except during the awakening
            if (awakening)
            {
                float flash = Mathf.Clamp01(1 - Mathf.Abs(a - .5f) / .6f) * 1.2f;
                float fade = Mathf.SmoothStep(0, 1, a / .5f);
                SetIntensity(band, bandIntensity * fade + flash, full);
                SetIntensity(seals, sealIntensity * fade + flash, full);
                SetIntensity(streams, streamIntensity * Mathf.SmoothStep(0, 1, (a - 1.3f) / .8f));
            }
            else if (awakenT >= 0)
            {
                awakenT = -1;
                SetIntensity(band, bandIntensity, full);
                SetIntensity(seals, sealIntensity, full);
                SetIntensity(streams, streamIntensity);
            }
            if (pulse != null)
            {
                float u = Mathf.Clamp01((a - 1.25f) / 1.0f);
                float s = Mathf.Lerp(1.5f, (allowed.y - allowed.x) * 1.0f, u);
                pulse.transform.localScale = new Vector3(s, s, 1);
                pulse.GetPropertyBlock(block);
                block.SetColor(BaseColor, new Color(Jade.r, Jade.g, Jade.b, u > 0 && u < 1 ? (1 - u) * .7f : 0));
                pulse.SetPropertyBlock(block);
                pulse.enabled = u > 0 && u < 1;
                if (u >= 1) { Kill(pulse.gameObject); pulse = null; }
            }

            // 2 pillar glow: asynchronous pulses; during the awakening the pillars light one after another
            float lodScale = lod == Lod.Far ? 1.6f : lod == Lod.Mid ? 1.3f : 1f;  // keep the caps readable from afar
            int n = halos.GetParticles(haloBuffer);
            for (int i = 0; i < n && i < pillars.Length; i++)
            {
                float k = full ? .7f + .3f * Mathf.Sin(clock * 2 * Mathf.PI / (LoopSeconds * .5f) + pillarPhase[i]) : .55f;
                float lit = awakening ? Mathf.SmoothStep(0, 1, (a - (.35f + i * .25f)) / .3f) : 1;
                float pop = awakening ? Mathf.Clamp01(1 - Mathf.Abs(a - (.5f + i * .25f)) / .25f) * .6f : 0;
                haloBuffer[i].startSize = (.42f + .14f * k + pop * .3f) * lodScale;
                haloBuffer[i].startColor = new Color(Jade.r, Jade.g, Jade.b, Mathf.Clamp01((full ? .9f : .55f) * k * lit + pop));
                haloBuffer[i].position = pillars[i];
            }
            halos.SetParticles(haloBuffer, n);
            int nc = cores.GetParticles(coreBuffer);
            for (int i = 0; i < nc && i < pillars.Length; i++)
            {
                float k = full ? .75f + .25f * Mathf.Sin(clock * 2 * Mathf.PI / (LoopSeconds * .5f) + pillarPhase[i]) : .6f;
                float lit = awakening ? Mathf.SmoothStep(0, 1, (a - (.35f + i * .25f)) / .3f) : 1;
                coreBuffer[i].startSize = (.24f + .05f * k) * lodScale;
                coreBuffer[i].startColor = new Color(.42f, 1f, .86f, Mathf.Clamp01((full ? .85f : .6f) * k * lit));
                coreBuffer[i].position = pillars[i];
            }
            cores.SetParticles(coreBuffer, nc);

            if (full && a >= 1.6f && lod == Lod.Near)
            {
                if (clock >= nextMote)
                {
                    nextMote = clock + .35f;
                    var p = pillars[rng.Next(pillars.Length)];
                    emit.position = p + new Vector3(Rand(-.06f, .06f), .12f, Rand(-.06f, .06f));
                    emit.velocity = new Vector3(0, Rand(.12f, .2f), 0);
                    emit.startLifetime = 2.2f;
                    emit.startSize = Rand(.03f, .05f);
                    emit.rotation = 0;
                    emit.startColor = new Color(Jade.r, Jade.g, Jade.b, .8f);
                    pillarMotes.Emit(emit, 1);
                }
                // ornament gathering: a short inward burst every ~7 s
                if (clock >= nextGather)
                {
                    nextGather = clock + Rand(6f, 8f);
                    for (int k = 0; k < 8; k++)
                    {
                        float ang = Rand(0, Mathf.PI * 2), el = Rand(-.6f, .6f);
                        var off = new Vector3(Mathf.Cos(ang), el, -Mathf.Abs(Mathf.Sin(ang)) - .2f).normalized;  // in front of the facade
                        emit.position = ornament + off * .55f;
                        emit.velocity = -off * .2f;
                        emit.startLifetime = 2.4f;
                        emit.startSize = .035f;
                        emit.startColor = new Color(Jade.r, Jade.g, Jade.b, .85f);
                        gather.Emit(emit, 1);
                    }
                }
            }
            // 4 mist and motes gathering around the hall (mist near and mid at half rate, motes near only)
            if (full && a >= 1.0f && lod != Lod.Far && clock >= nextMist)
            {
                nextMist = clock + (lod == Lod.Near ? .5f : 1.0f);
                int r = rng.Next(mistRects.Length);
                var rect = mistRects[r];
                var pos = new Vector3(Rand(rect.xMin, rect.xMax), mistHeights[r] + .05f, Rand(rect.yMin, rect.yMax));
                var inward = HallCentre(pos.y) - pos;
                inward.y = 0;
                emit.position = pos;
                emit.velocity = inward.normalized * .012f + new Vector3(0, Rand(.06f, .08f), 0);  // rises clear of the upper tier
                emit.startLifetime = Rand(7f, 9f);
                emit.startSize = Rand(.34f, .55f);
                emit.rotation = Rand(0, 360);
                emit.startColor = new Color(.72f, 1f, .92f, .13f);
                mist.Emit(emit, 1);
                emit.rotation = 0;
            }
            if (full && a >= 1.0f && lod == Lod.Near && clock >= nextGatherMote)
            {
                nextGatherMote = clock + .3f;
                // from a ring over the terrace, curving in towards the hall walls; they fade before reaching them
                float ang = Rand(0, Mathf.PI * 2);
                var c0 = HallCentre(Rand(tier2Top + .15f, tier2Top + .95f));  // above the terrace solids
                var pos = c0 + new Vector3(Mathf.Cos(ang) * gatherRadius.x, 0, Mathf.Sin(ang) * gatherRadius.y);
                float life = Rand(4.5f, 6f);
                var toHall = c0 - pos;
                var tangent = new Vector3(-toHall.z, 0, toHall.x).normalized;
                emit.position = pos;
                emit.velocity = new Vector3(toHall.x, 0, toHall.z) * (.55f / life) + tangent * .05f + Vector3.up * .03f;
                emit.startLifetime = life;
                emit.startSize = Rand(.025f, .045f);
                emit.startColor = new Color(Jade.r, Jade.g, Jade.b, .8f);
                motes.Emit(emit, 1);
            }

            if (!Application.isPlaying && SimulateInEditMode)
                foreach (var ps in GetComponentsInChildren<ParticleSystem>())
                    ps.Simulate(dt, false, false, false);
        }

        // ---------------------------------------------------------------- distance LOD / mode intensities
        void ApplyLod(Lod lod)
        {
            ActiveLod = lod;
            if (ActiveMode != Mode.Full)
            {
                // Reduced: steady and faint, no motion
                bandIntensity = .35f; sealIntensity = .3f; streamIntensity = 0;
                SetIntensity(band, bandIntensity, false);
                SetIntensity(seals, sealIntensity, false);
                return;
            }
            // From zoom 21 the inscription's fine pattern averages out under mip-mapping, so the band and seals are
            // driven harder to stay identifiable; at zoom 30 they remain as a low-cost signature.
            bandIntensity = lod == Lod.Near ? 1f : lod == Lod.Mid ? 2.2f : 1.6f;
            sealIntensity = lod == Lod.Near ? 1f : lod == Lod.Mid ? 1.9f : 1.3f;
            streamIntensity = lod == Lod.Near ? 1f : lod == Lod.Mid ? 1.8f : 0;
            SetIntensity(band, bandIntensity);
            SetIntensity(seals, sealIntensity);
            SetIntensity(streams, streamIntensity);
            Toggle(runes, lod == Lod.Near, 1.6f);
            if (lod != Lod.Near && motes != null) motes.Clear();
            if (lod != Lod.Near)
            {
                if (pillarMotes != null) pillarMotes.Clear();
                if (gather != null) gather.Clear();
            }
            if (lod == Lod.Far && mist != null) mist.Clear();
        }

        void SetIntensity(MeshRenderer r, float v, bool animate = true)
        {
            if (r == null) return;
            r.GetPropertyBlock(block);
            block.SetFloat(IntensityId, v);
            block.SetFloat(AnimateId, animate ? 1 : 0);
            r.SetPropertyBlock(block);
            r.enabled = v > .002f;
        }

        static void Toggle(ParticleSystem ps, bool on, float rate)
        {
            if (ps == null) return;
            var e = ps.emission;
            e.rateOverTimeMultiplier = on ? rate : 0;
            if (on && !ps.isPlaying) ps.Play(true);
            if (!on)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Clear(true);  // no simulation or rendering cost while hidden at this distance
            }
        }

        // ---------------------------------------------------------------- strip meshes
        // Formation band: one U-shaped rounded strip around the sides and back of the terrace (the front stays open
        // for the entrance), centred between the terrace and the permitted footprint edge.
        Mesh BuildBandMesh()
        {
            float tx = Mathf.Max(Mathf.Abs(tier1.x), Mathf.Abs(tier1.y)), tzB = tier1.w, tzF = tier1.z;
            float side = allowed.y - tx, back = allowed.w - tzB;
            float gap = Mathf.Min(side, back) * .5f;
            float width = Mathf.Min(.5f, Mathf.Min(side, back) * .7f);
            float x = tx + gap, zb = tzB + gap, rc = .7f;
            float zf = Mathf.Max(tzF + .35f, allowed.z + .4f);
            var path = new List<Vector3>();
            void Line(Vector3 p0, Vector3 p1, int steps) { for (int i = 0; i < steps; i++) path.Add(Vector3.Lerp(p0, p1, i / (float)steps)); }
            void Arc(Vector3 c, float a0, float a1, int steps) { for (int i = 0; i < steps; i++) { float t = Mathf.Lerp(a0, a1, i / (float)steps); path.Add(c + new Vector3(Mathf.Cos(t), 0, Mathf.Sin(t)) * rc); } }
            Line(new Vector3(-x, 0, zf), new Vector3(-x, 0, zb - rc), 24);
            Arc(new Vector3(-x + rc, 0, zb - rc), Mathf.PI, Mathf.PI * .5f, 10);
            Line(new Vector3(-x + rc, 0, zb), new Vector3(x - rc, 0, zb), 32);
            Arc(new Vector3(x - rc, 0, zb - rc), Mathf.PI * .5f, 0, 10);
            Line(new Vector3(x, 0, zb - rc), new Vector3(x, 0, zf), 24);
            path.Add(new Vector3(x, 0, zf));
            sealCenters[0] = new Vector3(-x, 0, zf); sealCenters[1] = new Vector3(x, 0, zf);
            sealCenters[2] = new Vector3(-x, 0, zb); sealCenters[3] = new Vector3(x, 0, zb);
            float length = 0;
            for (int i = 1; i < path.Count; i++) length += Vector3.Distance(path[i - 1], path[i]);
            Shared.BandMaterial.SetFloat("_Tiling", Mathf.Round(length / (width * 1.25f)));
            return FlatStrip("Formation bands", path, width, .014f, 0);
        }

        // Corner seals: three concentric annuli per corner, alternating rotation; radius limited by the footprint.
        Mesh BuildSealMesh()
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var cols = new List<Color>();
            var tris = new List<int>();
            for (int c = 0; c < sealCenters.Length; c++)
            {
                var ctr = sealCenters[c];
                float rmax = Mathf.Min(Mathf.Min(allowed.y - Mathf.Abs(ctr.x), ctr.z - allowed.z), Mathf.Min(allowed.w - ctr.z, .55f)) - .02f;
                float phase = new[] { 0f, .25f, .75f, .5f }[c];  // front-left, front-right, back-left, back-right
                // outer rune ring and inner rune ring turning the other way; the corners light in sequence
                Annulus(verts, uvs, cols, tris, ctr + Vector3.up * .016f, rmax * .70f, rmax, 48, 8, false, phase);
                Annulus(verts, uvs, cols, tris, ctr + Vector3.up * .017f, rmax * .30f, rmax * .58f, 32, 5, true, phase);
            }
            return NewMesh("Formation seals", verts, uvs, cols, tris);
        }

        static void Annulus(List<Vector3> v, List<Vector2> uv, List<Color> col, List<int> tri, Vector3 c, float r0, float r1,
            int seg, int repeats, bool reverse, float phase)
        {
            int baseIndex = v.Count;
            for (int i = 0; i <= seg; i++)
            {
                float t = i / (float)seg, ang = t * Mathf.PI * 2;
                var dir = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
                float u = (reverse ? 1 - t : t) * repeats;
                v.Add(c + dir * r0); uv.Add(new Vector2(u, 0));
                v.Add(c + dir * r1); uv.Add(new Vector2(u, 1));
                col.Add(new Color(1, 1, 1, phase)); col.Add(new Color(1, 1, 1, phase));
            }
            for (int i = 0; i < seg; i++)
            {
                int a = baseIndex + i * 2;
                tri.Add(a); tri.Add(a + 1); tri.Add(a + 3);
                tri.Add(a); tri.Add(a + 3); tri.Add(a + 2);
            }
        }

        // Spirit wisps: three thin ribbons per pillar on slightly different curved paths into the hall; the SpiritWisp
        // shader displaces them with smooth periodic noise (vertex NORMAL = ribbon side, colour A = per-wisp seed).
        Mesh BuildStreamMesh()
        {
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var cols = new List<Color>();
            var tris = new List<int>();
            float midY = Mathf.Lerp(hallMin.y, hallMax.y, .5f);
            float hallZ = (hallMin.z + hallMax.z) / 2;
            int wisp = 0;
            for (int i = 0; i < pillars.Length; i++)
            {
                bool frontPillar = pillars[i].z < hallZ;
                float sx = Mathf.Sign(pillars[i].x);
                for (int w = 0; w < 2; w++, wisp++)
                {
                    float j = (w - .5f) * .3f;
                    var p0 = pillars[i] + Vector3.up * (.22f + w * .05f);
                    var p3 = new Vector3(sx * (.55f + w * .25f), midY + j * .8f, frontPillar ? hallMin.z - .1f : hallMax.z + .1f);
                    var away = new Vector3(0, 0, frontPillar ? -1 : 1);
                    var p1 = p0 + new Vector3(-sx * (.25f + w * .15f), 1.1f + j, (frontPillar ? 1 : -1) * Mathf.Abs(j) * .6f);  // never outward past the lot
                    var p2 = p3 + away * (.7f + w * .2f) + Vector3.up * (.8f - j);
                    var pts = new List<Vector3>();
                    const int steps = 36;
                    for (int s = 0; s <= steps; s++)
                    {
                        float t = s / (float)steps, u = 1 - t;
                        pts.Add(u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3);
                    }
                    float seed = (wisp * .618f) % 1f;
                    AppendRibbon(verts, norms, uvs, cols, tris, pts, .07f - w * .012f, false, seed);
                    AppendRibbon(verts, norms, uvs, cols, tris, pts, .045f, true, seed);
                }
            }
            var m = NewMesh("Spirit wisps", verts, uvs, cols, tris);
            m.SetNormals(norms);
            wispRawBounds = m.bounds;
            m.bounds = new Bounds(m.bounds.center, m.bounds.size + Vector3.one * .3f);  // culling room for the shader sway
            return m;
        }

        static void AppendRibbon(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<Color> col, List<int> tri, List<Vector3> pts, float width, bool vertical, float seed)
        {
            int baseIndex = v.Count;
            float total = 0;
            var acc = new float[pts.Count];
            for (int i = 1; i < pts.Count; i++) acc[i] = total += Vector3.Distance(pts[i - 1], pts[i]);
            for (int i = 0; i < pts.Count; i++)
            {
                var tan = (pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[Mathf.Max(i - 1, 0)]).normalized;
                var flat = Vector3.Cross(Vector3.up, tan);
                if (flat.sqrMagnitude < 1e-4f) flat = Vector3.right;
                flat.Normalize();
                var side = vertical ? Vector3.Cross(tan, flat).normalized : flat;
                float w = width * (.35f + .65f * Mathf.Sin(acc[i] / total * Mathf.PI));  // slender ends
                float u = acc[i] / total;
                v.Add(pts[i] - side * w * .5f); uv.Add(new Vector2(u, 0)); n.Add(flat);
                v.Add(pts[i] + side * w * .5f); uv.Add(new Vector2(u, 1)); n.Add(flat);
                col.Add(new Color(1, 1, 1, seed)); col.Add(new Color(1, 1, 1, seed));
            }
            for (int i = 0; i < pts.Count - 1; i++)
            {
                int a = baseIndex + i * 2;
                tri.Add(a); tri.Add(a + 1); tri.Add(a + 3);
                tri.Add(a); tri.Add(a + 3); tri.Add(a + 2);
            }
        }

        Mesh FlatStrip(string name, List<Vector3> path, float width, float y, float phase)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var cols = new List<Color>();
            var tris = new List<int>();
            float total = 0;
            var acc = new float[path.Count];
            for (int i = 1; i < path.Count; i++) acc[i] = total += Vector3.Distance(path[i - 1], path[i]);
            for (int i = 0; i < path.Count; i++)
            {
                var tan = (path[Mathf.Min(i + 1, path.Count - 1)] - path[Mathf.Max(i - 1, 0)]).normalized;
                var side = Vector3.Cross(Vector3.up, tan).normalized;
                float u = acc[i] / total;
                verts.Add(path[i] - side * width * .5f + Vector3.up * y); uvs.Add(new Vector2(u, 0));
                verts.Add(path[i] + side * width * .5f + Vector3.up * y); uvs.Add(new Vector2(u, 1));
                cols.Add(new Color(1, 1, 1, phase)); cols.Add(new Color(1, 1, 1, phase));
            }
            for (int i = 0; i < path.Count - 1; i++)
            {
                int a = i * 2;
                tris.Add(a); tris.Add(a + 1); tris.Add(a + 3);
                tris.Add(a); tris.Add(a + 3); tris.Add(a + 2);
            }
            return NewMesh(name, verts, uvs, cols, tris);
        }

        Mesh NewMesh(string name, List<Vector3> verts, List<Vector2> uvs, List<Color> cols, List<int> tris)
        {
            var m = new Mesh { name = name };
            m.SetVertices(verts); m.SetUVs(0, uvs); m.SetColors(cols); m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            meshes.Add(m);
            return m;
        }

        void BuildMistRects()
        {
            float x1 = Mathf.Max(Mathf.Abs(tier1.x), Mathf.Abs(tier1.y)), x2 = Mathf.Max(Mathf.Abs(tier2.x), Mathf.Abs(tier2.y));
            // tier-1 top strips outside tier 2 (sides and back), tier-2 side strips beside the wings, front landing
            mistRects[0] = Rect.MinMaxRect(-x1 + .05f, tier1.z + 1.2f, -x2 - .2f, tier1.w - .1f); mistHeights[0] = tier1Top;
            mistRects[1] = Rect.MinMaxRect(x2 + .2f, tier1.z + 1.2f, x1 - .05f, tier1.w - .1f); mistHeights[1] = tier1Top;
            mistRects[2] = Rect.MinMaxRect(-x1 + .3f, tier2.w + .02f, x1 - .3f, tier1.w - .03f); mistHeights[2] = tier1Top;
            mistRects[3] = Rect.MinMaxRect(-x2 + .05f, tier2.z + .6f, -x2 + .3f, tier2.w - .2f); mistHeights[3] = tier2Top;
            mistRects[4] = Rect.MinMaxRect(x2 - .3f, tier2.z + .6f, x2 - .05f, tier2.w - .2f); mistHeights[4] = tier2Top;
            mistRects[5] = Rect.MinMaxRect(-1.6f, tier1.z + .35f, 1.6f, tier2.z - .3f); mistHeights[5] = tier1Top;
        }

        // ---------------------------------------------------------------- builders
        MeshRenderer Strip(string name, Material material, Mesh mesh)
        {
            var go = new GameObject(name) { layer = 2 };
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return r;
        }

        MeshRenderer Quad(string name, Material material, Vector3 localPos, Vector2 size)
        {
            var go = new GameObject(name) { layer = 2 };
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(90, 0, 0);
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            go.AddComponent<MeshFilter>().sharedMesh = Shared.QuadMesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }

        ParticleSystem System(string name, Material material, int max, bool loop)
        {
            var go = new GameObject(name) { layer = 2 };
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = loop;
            main.playOnAwake = false;
            main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startSpeed = 0;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var e = ps.emission;
            e.rateOverTime = 0;
            var shape = ps.shape;
            shape.enabled = false;
            var col = ps.colorOverLifetime;
            col.enabled = name != "Pillar glow" && name != "Pillar crystal cores";
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .2f), new GradientAlphaKey(1, .7f), new GradientAlphaKey(0, 1) });
            col.color = g;
            if (name == "Spirit mist")
            {
                var rot = ps.rotationOverLifetime;
                rot.enabled = true;
                rot.z = new ParticleSystem.MinMaxCurve(-.08f, .08f);
                var size = ps.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, .7f, 1, 1.3f));
            }
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play(true);
            return ps;
        }

        // Small golden scripture characters rising from the hall's second-roof eaves: fade in, float with a gentle sway,
        // then dissolve into specks (SonMon/SpiritGlyph reads the particle age through a custom vertex stream).
        ParticleSystem GlyphSystem()
        {
            var ps = System("Scripture characters", Shared.GlyphMaterial, 16, true);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5.5f, 7.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(.13f, .19f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .82f, .4f, 1f), new Color(1f, .9f, .6f, 1f));
            var col = ps.colorOverLifetime;
            col.enabled = false;  // the shader fades and dissolves by age
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            float hw = (hallMax.x - hallMin.x) * .5f;
            shape.scale = new Vector3(hw * 2.1f, .35f, .25f);
            // just in front of the hall facade, between the first and second roofs
            shape.position = new Vector3((hallMin.x + hallMax.x) / 2, Mathf.Lerp(hallMin.y, hallMax.y, .5f), hallMin.z - .25f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(-.02f, .02f);
            vel.y = new ParticleSystem.MinMaxCurve(.07f, .11f);
            vel.z = new ParticleSystem.MinMaxCurve(-.03f, -.01f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = .06f;
            noise.frequency = .35f;
            noise.scrollSpeed = .1f;
            noise.quality = ParticleSystemNoiseQuality.Low;
            var sheet = ps.textureSheetAnimation;
            sheet.enabled = true;
            sheet.numTilesX = 4;
            sheet.numTilesY = 2;
            sheet.animation = ParticleSystemAnimationType.WholeSheet;
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
            sheet.startFrame = new ParticleSystem.MinMaxCurve(0f, 7.99f);
            var e = ps.emission;
            e.rateOverTime = 1.6f;
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.SetActiveVertexStreams(new List<ParticleSystemVertexStream>
            {
                ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Color,
                ParticleSystemVertexStream.UV, ParticleSystemVertexStream.AgePercent, ParticleSystemVertexStream.StableRandomX,
            });
            return ps;
        }

        float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        public int ParticleCount()
        {
            int n = 0;
            foreach (var ps in GetComponentsInChildren<ParticleSystem>()) n += ps.particleCount;
            return n;
        }

        // World-space bounds of everything this component renders (strips + live particles), for footprint QA.
        public Bounds RenderedBounds()
        {
            bool any = false;
            var b = new Bounds(transform.position, Vector3.zero);
            void Add(Bounds x) { if (!any) { b = x; any = true; } else b.Encapsulate(x); }
            foreach (var r in GetComponentsInChildren<MeshRenderer>())
            {
                if (!r.enabled) continue;
                if (r == streams)
                {
                    // actual geometry plus the shader sway (horizontal up to the full amplitude; ends are anchored)
                    var wb = wispRawBounds;
                    wb.Expand(new Vector3(.14f, .1f, .14f) * 2);
                    var c = transform.TransformPoint(wb.center);
                    var fb = new Bounds(c, wb.size);
                    // the pillar ends do not sway: clamp the front/back growth to the raw geometry there
                    var raw = new Bounds(transform.TransformPoint(wispRawBounds.center), wispRawBounds.size);
                    fb.SetMinMax(new Vector3(fb.min.x, fb.min.y, raw.min.z), new Vector3(fb.max.x, fb.max.y, raw.max.z));
                    Add(fb);
                }
                else Add(r.bounds);
            }
            var buf = new ParticleSystem.Particle[32];
            foreach (var ps in GetComponentsInChildren<ParticleSystem>())
            {
                int n = ps.GetParticles(buf);
                for (int i = 0; i < n; i++)
                {
                    var p = ps.transform.TransformPoint(buf[i].position);
                    Add(new Bounds(p, Vector3.one * buf[i].GetCurrentSize(ps)));
                }
            }
            return b;
        }

        // ---------------------------------------------------------------- shared resources (created once)
        static class Shared
        {
            static Material band, seal, stream, glow, glyph, mist, pulseMat, core;
            static Mesh quad;
            static Texture2D runeStrip;

            public static Mesh QuadMesh => quad != null ? quad : quad = MakeQuad();
            public static Material BandMaterial => band != null ? band : band = FlowMat("Formation band", RuneStripTexture(),
                new Color(.32f, .92f, .78f, .55f), scrollRepeats: 1, waves: 1, waveWidth: .1f, waveGain: 1.5f, breath: .3f);
            public static Material SealMaterial => seal != null ? seal : seal = FlowMat("Formation seals", RuneStripTexture(),
                new Color(.32f, .92f, .78f, .6f), scrollRepeats: 1, waves: 1, waveWidth: .2f, waveGain: 1.2f, breath: .45f);
            public static Material StreamMaterial => stream != null ? stream : stream = WispMat();
            public static Material GlowMaterial => glow != null ? glow : glow = Additive("Spirit glow", RadialTexture(64, 2f));
            public static Material GlyphMaterial => glyph != null ? glyph : glyph = GlyphMat();
            public static Material PulseMaterial => pulseMat != null ? pulseMat : pulseMat = Additive("Awakening pulse", RingTexture());
            public static Material MistMaterial => mist != null ? mist : mist = AlphaBlend("Spirit mist", WispTexture());
            public static Material CoreMaterial => core != null ? core : core = AlphaBlend("Pillar crystal core", RadialTexture(64, 1.3f));

            static Material FlowMat(string name, Texture2D tex, Color color, float scrollRepeats, float waves, float waveWidth, float waveGain, float breath)
            {
                var m = new Material(Shader.Find("SonMon/SpiritFlow")) { name = name };
                m.SetTexture("_MainTex", tex);
                m.SetColor("_Color", color);
                m.SetColor("_LitColor", new Color(.78f, 1f, .92f, 1));
                m.SetFloat("_Tiling", 1);
                m.SetFloat("_Cycle", LoopSeconds);
                m.SetFloat("_ScrollRepeats", scrollRepeats);
                m.SetFloat("_WavesPerCycle", waves);
                m.SetFloat("_WaveWidth", waveWidth);
                m.SetFloat("_WaveGain", waveGain);
                m.SetFloat("_Breath", breath);
                return m;
            }

            static Material WispMat()
            {
                var m = new Material(Shader.Find("SonMon/SpiritWisp")) { name = "Spirit wisps" };
                m.SetTexture("_MainTex", StreamTexture());
                m.SetColor("_Color", new Color(.5f, 1f, .9f, .34f));
                m.SetColor("_PulseColor", new Color(.92f, 1f, .96f, 1));
                m.SetFloat("_Cycle", LoopSeconds);
                m.SetFloat("_Sway", .11f);
                m.SetFloat("_FlowRepeats", 2);
                m.SetFloat("_Tiling", 2);
                m.SetFloat("_PulseGain", 1.4f);
                m.SetFloat("_EndFade", .2f);
                return m;
            }

            static Material GlyphMat()
            {
                var m = new Material(Shader.Find("SonMon/SpiritGlyph")) { name = "Scripture characters" };
                m.SetTexture("_MainTex", GlyphAtlas());
                m.SetColor("_EdgeColor", new Color(1f, .96f, .78f, 1));
                m.SetFloat("_Intensity", 1.25f);
                return m;
            }

            static Mesh MakeQuad()
            {
                var m = new Mesh { name = "Spirit quad" };
                m.vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(.5f, .5f, 0), new Vector3(-.5f, .5f, 0) };
                m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
                m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                m.RecalculateBounds();
                return m;
            }

            static Material Additive(string name, Texture2D tex)
            {
                var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
                m.SetTexture("_BaseMap", tex);
                m.SetColor(BaseColor, Color.white);
                m.SetFloat("_Surface", 1);
                m.SetFloat("_Blend", 2);
                m.SetFloat("_Cull", 0);
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)BlendMode.One);
                m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = 3000;
                return m;
            }

            static Material AlphaBlend(string name, Texture2D tex)
            {
                var m = Additive(name, tex);
                m.SetFloat("_Blend", 0);
                m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                return m;
            }

            static Texture2D NewTex(int w, int h, string name, bool mips, TextureWrapMode wrap)
                => new Texture2D(w, h, TextureFormat.RGBA32, mips) { wrapMode = wrap, name = name, anisoLevel = 4 };

            static Texture2D Finish(Texture2D t, Color32[] px, bool mips)
            {
                t.SetPixels32(px);
                t.Apply(mips, true);  // GPU only: no CPU copy kept
                return t;
            }

            public static Texture2D RadialTexture(int n, float falloff)
            {
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f - .5f, n / 2f - .5f)) / (n / 2f);
                        px[y * n + x] = new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - d), falloff));
                    }
                return Finish(NewTex(n, n, "Spirit radial", false, TextureWrapMode.Clamp), px, false);
            }

            static Texture2D RingTexture()
            {
                const int n = 256;
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                        float ring = Mathf.Exp(-Mathf.Pow((d - .9f) / .035f, 2)) + .35f * Mathf.Exp(-Mathf.Pow((d - .8f) / .02f, 2));
                        px[y * n + x] = new Color(1, 1, 1, Mathf.Clamp01(ring));
                    }
                return Finish(NewTex(n, n, "Awakening ring", false, TextureWrapMode.Clamp), px, false);
            }

            static Texture2D WispTexture()
            {
                const int n = 128;
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float u = x / (float)n - .5f, v = y / (float)n - .5f;
                        float d = Mathf.Sqrt(u * u * 2.6f + v * v * .9f) * 2;  // tall, thin wisp
                        float noise = Mathf.PerlinNoise(x * .06f, y * .03f) * .7f + Mathf.PerlinNoise(x * .15f + 7, y * .08f) * .3f;
                        px[y * n + x] = new Color(1, 1, 1, Mathf.Clamp01((1 - d) * 1.4f) * noise);
                    }
                return Finish(NewTex(n, n, "Spirit wisp", false, TextureWrapMode.Clamp), px, false);
            }

            // One repeat of the formation inscription along u: a scripture character with a soft glow between two faint
            // carved hairlines. Tiles seamlessly along u. 128 x 128.
            static Texture2D RuneStripTexture()
            {
                if (runeStrip != null) return runeStrip;
                const int w = 128, h = 128;
                var px = new Color32[w * h];
                var glyph = Character(11);
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float u = (x + .5f) / w, v = (y + .5f) / h;
                        float a = Mathf.Max(Line(v - .1f, .012f), Line(v - .9f, .012f)) * .3f;  // faint carved hairlines
                        float gy = (v - .2f) / .6f;
                        if (gy > 0 && gy < 1)
                        {
                            var q = new Vector2(.15f + u * .7f, gy);
                            float d = glyph(q);
                            a = Mathf.Max(a, Mathf.Max(Mathf.Clamp01(1 - d / .055f) * .9f, Mathf.Clamp01(1 - d / .17f) * .3f));
                        }
                        px[y * w + x] = new Color(1, 1, 1, a);
                    }
                return runeStrip = Finish(NewTex(w, h, "Formation rune strip", true, TextureWrapMode.Repeat), px, true);
            }

            // Soft energy streaks for the streams: tileable along u, Gaussian across v. 256 x 64.
            static Texture2D StreamTexture()
            {
                const int w = 256, h = 64;
                var px = new Color32[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float u = x / (float)w, v = (y + .5f) / h;
                        float across = Mathf.Exp(-Mathf.Pow((v - .5f) / .22f, 2));
                        float coreLine = Mathf.Exp(-Mathf.Pow((v - .5f) / .07f, 2));
                        float streak = .55f + .45f * (.5f + .5f * Mathf.Sin(u * Mathf.PI * 2 * 3 + Mathf.Sin(u * Mathf.PI * 2) * 1.5f));
                        float wisp = .6f + .4f * (.5f + .5f * Mathf.Sin(u * Mathf.PI * 2 * 7 + v * 5));
                        px[y * w + x] = new Color(1, 1, 1, Mathf.Clamp01(across * .55f * streak * wisp + coreLine * .55f));
                    }
                return Finish(NewTex(w, h, "Spirit stream", true, TextureWrapMode.Repeat), px, true);
            }

            static float Line(float d, float width) => Mathf.Clamp01(1 - Mathf.Abs(d) / width);

            static float[][] GlyphStrokes() => new[]
            {
                new[] { .2f, .78f, .8f, .78f, .5f, .78f, .5f, .2f, .25f, .45f, .75f, .45f, .3f, .2f, .7f, .2f },
                new[] { .25f, .25f, .25f, .8f, .25f, .8f, .75f, .8f, .75f, .8f, .75f, .25f, .4f, .55f, .6f, .55f },
                new[] { .5f, .85f, .5f, .15f, .2f, .6f, .8f, .6f, .3f, .35f, .45f, .25f, .7f, .35f, .55f, .25f },
                new[] { .2f, .2f, .8f, .8f, .8f, .2f, .2f, .8f, .5f, .15f, .5f, .35f, .5f, .65f, .5f, .85f },
                new[] { .3f, .8f, .7f, .8f, .5f, .8f, .5f, .5f, .2f, .5f, .8f, .5f, .35f, .2f, .65f, .35f },
                new[] { .2f, .7f, .8f, .7f, .2f, .45f, .8f, .45f, .35f, .2f, .65f, .2f, .5f, .85f, .5f, .2f },
                new[] { .25f, .8f, .5f, .55f, .75f, .8f, .5f, .55f, .5f, .55f, .5f, .2f, .3f, .3f, .7f, .3f },
                new[] { .2f, .5f, .8f, .5f, .5f, .2f, .5f, .8f, .3f, .3f, .7f, .7f, .7f, .3f, .3f, .7f },
            };

            // Eight procedurally composed scripture characters (horizontal, vertical, sweeping and dot strokes on a
            // character grid) in a 4 x 2 atlas; crisp cores with a soft halo, no frame.
            static Texture2D GlyphAtlas()
            {
                const int cell = 96, w = cell * 4, h = cell * 2;
                var px = new Color32[w * h];
                for (int g = 0; g < 8; g++)
                {
                    int ox = (g % 4) * cell, oy = (1 - g / 4) * cell;
                    var glyph = Character(g + 3);
                    for (int y = 0; y < cell; y++)
                        for (int x = 0; x < cell; x++)
                        {
                            var p = new Vector2((x + .5f) / cell, (y + .5f) / cell);
                            float d = glyph(p);
                            float coreA = Mathf.Clamp01(1 - d / .05f), halo = Mathf.Clamp01(1 - d / .14f) * .35f;
                            float edge = Mathf.Clamp01(Mathf.Min(Mathf.Min(p.x, 1 - p.x), Mathf.Min(p.y, 1 - p.y)) / .06f);
                            px[(oy + y) * w + ox + x] = new Color(1, 1, 1, Mathf.Max(coreA, halo) * edge);
                        }
                }
                return Finish(NewTex(w, h, "Scripture characters", true, TextureWrapMode.Clamp), px, true);
            }

            // Distance function of a deterministic character: 5-8 strokes on a 4 x 4 grid inside [0.18, 0.82]^2.
            static Func<Vector2, float> Character(int seed)
            {
                var r = new System.Random(seed * 7919);
                var segs = new List<(Vector2, Vector2)>();
                float G(int k) => .18f + k * .16f;
                int n = 5 + r.Next(4);
                for (int i = 0; i < n; i++)
                {
                    int kind = r.Next(10);
                    int a = r.Next(4), b = r.Next(4), len = 1 + r.Next(3);
                    if (kind < 4) segs.Add((new Vector2(G(a), G(b)), new Vector2(G(Mathf.Min(a + len, 4)), G(b))));           // horizontal
                    else if (kind < 7) segs.Add((new Vector2(G(a), G(b)), new Vector2(G(a), G(Mathf.Min(b + len, 4)))));     // vertical
                    else if (kind < 9) segs.Add((new Vector2(G(a), G(b + 1 > 4 ? 4 : b + 1)), new Vector2(G(a) - .14f, G(b) - .05f)));  // sweep
                    else segs.Add((new Vector2(G(a) + .03f, G(b) + .03f), new Vector2(G(a) + .07f, G(b) - .02f)));        // dot
                }
                segs.Add((new Vector2(G(0), G(2)), new Vector2(G(4), G(2))));  // a long horizontal anchors every character
                return p =>
                {
                    float d = 1;
                    foreach (var (s0, s1) in segs) d = Mathf.Min(d, Segment(p, s0, s1));
                    return d;
                };
            }

            static float Segment(Vector2 p, Vector2 a, Vector2 b)
            {
                var ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                return Vector2.Distance(p, a + ab * t);
            }
        }
    }
}
