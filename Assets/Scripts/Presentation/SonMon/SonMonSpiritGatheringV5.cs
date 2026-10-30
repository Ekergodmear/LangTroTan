using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LangTroTan.Presentation
{
    // Tàng Kinh Các Level 6 spirit VFX v5 candidate (isolated QA, not integrated; v4 stays the baseline). Same 10 s
    // cycle, modes, distance LOD and allocation-free emission as SonMonSpiritGatheringV4, with these changes:
    //   release   two flat gold-jade waves (upper terrace and lower landing, elliptical to the tiers) and a warm bloom
    //             at the stair foot, alpha-blended so they read on pale stone; emitted at every distance
    //   distance  pillar glows, ornament halo and formation brightness scale with camera distance, so the effect still
    //             separates from Off at gameplay zoom 21 / 30 without adding particles
    //   seals     one brush-stroke seal quad per corner (open arcs and a cloud-scroll spiral) instead of tick-mark rings
    //   runes     curved seal-script-like characters (no plus / hash shapes, no edge hairlines)
    //   mist      smaller, more transparent horizontal wisps, emitted higher, away from the stairs, faded near the camera
    //   ornament  a small halo (brighter rim, dim centre) instead of a cyan column
    //   glyphs    bolder curved characters, slightly larger
    // Assets: Resources/SonMon/VFX/L6v5 (built by SpiritV5Builder); motes reuse the v4 prefabs.
    //
    // V6 release concept (ReleaseStyle.Plume, the default): keeps every v5 change above but replaces the flat waves,
    // which do not read at gameplay zoom, with a vertical release over the main pavilion:
    //   7.4-8.3 s  spirit seeds drift in from a ring round the main roof towards its ridge (gathering)
    //   8.0-9.3 s  a soft jade-gold plume of elongated, translucent wisps rises above the central roof, with a few
    //              motes spiralling up round it (orbital velocity, no per-particle updates)
    //   ~9.0 s     readable peak (plume ~4-5 units tall, scaled with camera distance), then it thins and dissolves
    // No laser core: every plume particle is a soft, noisy, alpha-blended wisp that fades over its whole life.
    // (V6 lives in the v5 files because the trusted QA overlay only accepts *V5* candidate paths.)
    public sealed class SonMonSpiritGatheringV5 : MonoBehaviour
    {
        public enum ReleaseStyle { Waves, Plume }  // Waves = v5 iteration 3, Plume = V6 concept
        public static ReleaseStyle? StyleOverride;  // QA only
        public ReleaseStyle Style => StyleOverride ?? ReleaseStyle.Plume;
        public const float Cycle = 10f;
        public const float MidDistance = 42f, FarDistance = 70f;
        public static SonMonSpiritGathering.Mode? ModeOverride;  // QA only
        static readonly bool NoAmbientFlag = Array.IndexOf(Environment.GetCommandLineArgs(), "-no-ambient-fx") >= 0;
        public static SonMonSpiritGathering.Mode CurrentMode => ModeOverride ?? (!SonMonTiers.Enabled || NoAmbientFlag
            ? SonMonSpiritGathering.Mode.Off : SonMonUpgradeFx.ReducedEffects ? SonMonSpiritGathering.Mode.Reduced : SonMonSpiritGathering.Mode.Full);

        static readonly int ActivationId = Shader.PropertyToID("_Activation");
        static readonly int LitId = Shader.PropertyToID("_LitLevel");
        static readonly int BaseId = Shader.PropertyToID("_BaseLevel");
        static readonly int TilingId = Shader.PropertyToID("_Tiling");
        static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
        const string V5 = "SonMon/VFX/L6v5/", V4 = "SonMon/VFX/L6/";

        [SerializeField] Vector3[] pillars = new Vector3[0];
        [SerializeField] Vector3 ornament, hallMin, hallMax;
        [SerializeField] Vector4 tier1, tier2, allowed = new Vector4(-1e3f, 1e3f, -1e3f, 1e3f);
        [SerializeField] float tier1Top = .47f, tier2Top = .94f, tier2FrontTop = .94f;

        public SonMonSpiritGathering.Mode ActiveMode { get; private set; } = SonMonSpiritGathering.Mode.Off;
        public SonMonSpiritGathering.Lod ActiveLod { get; private set; } = SonMonSpiritGathering.Lod.Near;
        public float Phase => (float)(time % Cycle);

        ParticleSystem mist, jade, gold, pillarGlow, ornamentGlow, wave, bloom, glyphs, plume, swirl;
        MeshRenderer band, seals;
        readonly List<Mesh> meshes = new List<Mesh>();
        readonly ParticleSystem.Particle[] glowBuf = new ParticleSystem.Particle[8];
        readonly Rect[] mistRects = new Rect[5];
        ParticleSystem.EmitParams ep;
        MaterialPropertyBlock block;
        System.Random rng;
        double time;
        float awakenT = -1, prevPhase, accMist, accGather, accJade, accGold, accSeed, accPlume, accSwirl, bandTiling, camDistance;
        Vector3 apex;
        Vector3 camLocal;
        bool built, lowerWavePending;
        static Camera cam;
        static float camLookup;

        public void Configure(Vector3[] pillarCrystals, Vector3 ornamentLocal, Vector3 hallMinLocal, Vector3 hallMaxLocal,
            Vector4 tier1XZ, Vector4 tier2XZ, float tier1Height, float tier2Height, Vector4 allowedXZ)
        {
            pillars = pillarCrystals; ornament = ornamentLocal; hallMin = hallMinLocal; hallMax = hallMaxLocal;
            tier1 = tier1XZ; tier2 = tier2XZ; tier1Top = tier1Height; tier2Top = tier2FrontTop = tier2Height; allowed = allowedXZ;
        }

        // Measured walkable surface heights (local), when the anchor tier heights sit below the real paving
        public void SetSurfaceHeights(float lowerTop, float upperTop, float upperFrontTop)
        {
            tier1Top = lowerTop; tier2Top = upperTop; tier2FrontTop = upperFrontTop;
        }

        // ---------------------------------------------------------------- build
        // awaken: as v4, only right after a server-confirmed upgrade swap (never when a saved game is loaded).
        public bool Build(bool? awaken = null)
        {
            Clear();
            ActiveMode = CurrentMode;
            if (ActiveMode == SonMonSpiritGathering.Mode.Off) return false;
            rng = new System.Random(6006);
            block ??= new MaterialPropertyBlock();
            gameObject.layer = 2;
            bool full = ActiveMode == SonMonSpiritGathering.Mode.Full;
            band = Strip("Formation band", Resources.Load<Material>(V5 + "M_FormationBandV5"), BuildBandMesh());
            seals = Strip("Formation seals", Resources.Load<Material>(V5 + "M_SealV5"), BuildSealMesh());
            mist = Spawn(V5 + "PS_SpiritMistV5");
            pillarGlow = Spawn(V5 + "PS_PillarGlowV5");
            for (int i = 0; i < pillars.Length; i++) EmitStatic(pillarGlow, pillars[i], .5f);
            if (full)
            {
                jade = Spawn(V4 + "PS_JadeMotes");
                gold = Spawn(V4 + "PS_GoldenMotes");
                ornamentGlow = Spawn(V5 + "PS_OrnamentHaloV5");
                EmitStatic(ornamentGlow, ornament + new Vector3(0, 0, -.08f), .3f);
                wave = Spawn(V5 + "PS_ReleaseWaveV5");
                bloom = Spawn(V5 + "PS_ReleaseGlowV5");
                glyphs = Spawn(V5 + "PS_ScriptureGlyphsV5");
                // plume systems sit on the main roof's vertical axis so orbital velocity spirals round it
                apex = new Vector3((hallMin.x + hallMax.x) * .5f, hallMax.y - .25f, (hallMin.z + hallMax.z) * .5f);
                plume = Spawn(V5 + "PS_PlumeV5");
                swirl = Spawn(V5 + "PS_SwirlV5");
                plume.transform.localPosition = apex;
                swirl.transform.localPosition = apex;
                ConfigureGlyphs();
            }
            BuildMistRects();
            bool awakenNow = awaken ?? (transform.parent != null && SonMonUpgradeFx.IsPlaying(transform.parent, out var fx) && fx.ToLevel == 6 && fx.Swapped);
            awakenT = awakenNow ? 0 : -1;
            if (awakenNow) time = 3.0;
            prevPhase = Phase;
            lowerWavePending = false;
            camDistance = 0;
            camLocal = new Vector3(0, 1e4f, 0);
            built = true;
            ApplyLod(SonMonSpiritGathering.Lod.Near);
            UpdateCycle(0, SonMonSpiritGathering.Lod.Near);
            return true;
        }

        public bool Awakening => awakenT >= 0;

        // World-space bounds of everything rendered (formation strips and live particles), for footprint QA.
        public Bounds RenderedBounds()
        {
            bool any = false;
            var b = new Bounds(transform.position, Vector3.zero);
            void Add(Bounds x) { if (!any) { b = x; any = true; } else b.Encapsulate(x); }
            foreach (var r in GetComponentsInChildren<MeshRenderer>()) if (r.enabled) Add(r.bounds);
            var buf = new ParticleSystem.Particle[64];
            foreach (var ps in GetComponentsInChildren<ParticleSystem>())
            {
                int n = ps.GetParticles(buf);
                for (int i = 0; i < n; i++)
                    Add(new Bounds(ps.transform.TransformPoint(buf[i].position), Vector3.one * buf[i].GetCurrentSize(ps)));
            }
            return b;
        }

        void Start() { if (!built && pillars.Length > 0) Build(); }

        public void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Kill(transform.GetChild(i).gameObject);
            foreach (var m in meshes) Kill(m);
            meshes.Clear();
            mist = jade = gold = pillarGlow = ornamentGlow = wave = bloom = glyphs = plume = swirl = null;
            band = seals = null;
            built = false;
        }

        void OnDestroy() { foreach (var m in meshes) Kill(m); meshes.Clear(); }

        static void Kill(UnityEngine.Object o) { if (o == null) return; if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }

        ParticleSystem Spawn(string resource)
        {
            var src = Resources.Load<GameObject>(resource);
            var go = Instantiate(src, transform, false);
            go.name = src.name;
            go.layer = 2;
            var ps = go.GetComponent<ParticleSystem>();
            ps.Play(true);
            return ps;
        }

        void EmitStatic(ParticleSystem ps, Vector3 at, float size)
        {
            ep = default;
            ep.position = at;
            ep.velocity = Vector3.zero;
            ep.startLifetime = 1e6f;
            ep.startSize = size;
            ep.startColor = Color.white;
            ps.Emit(ep, 1);
        }

        // ---------------------------------------------------------------- per frame
        void Update()
        {
            if (!built) return;
            if (cam == null || Time.unscaledTime > camLookup) { cam = Camera.main; camLookup = Time.unscaledTime + 1f; }
            Step(Time.deltaTime, cam != null ? cam.transform.position : transform.position);
        }

        // Advances the cycle by dt as seen from cameraPos (QA captures call this directly; in edit mode it also
        // advances the particle simulation).
        public void Step(float dt, Vector3 cameraPos)
        {
            if (!built) return;
            time += dt;
            camDistance = Vector3.Distance(cameraPos, transform.position);
            camLocal = transform.InverseTransformPoint(cameraPos);
            var lod = camDistance < MidDistance ? SonMonSpiritGathering.Lod.Near : camDistance < FarDistance ? SonMonSpiritGathering.Lod.Mid : SonMonSpiritGathering.Lod.Far;
            if (lod != ActiveLod) ApplyLod(lod);
            UpdateCycle(dt, lod);
            if (!Application.isPlaying && SonMonSpiritGathering.SimulateInEditMode)
                for (int i = 0; i < transform.childCount; i++)
                {
                    var ps = transform.GetChild(i).GetComponent<ParticleSystem>();
                    if (ps != null) ps.Simulate(dt, false, false, false);
                }
        }

        // Smooth periodic envelopes of the cycle phase p (0..10)
        static float Smooth(float a, float b, float x) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(a, b, x));
        public static float GatherK(float p) => Smooth(3f, 6f, p) * (1 - Smooth(8f, 9.3f, p));
        public static float ConvergeK(float p) => Smooth(4.8f, 7.2f, p) * (1 - Smooth(8.1f, 9.6f, p));
        public static float LitK(float p) => Smooth(3f, 4f, p) * (1 - Smooth(8.4f, 9.8f, p));
        public static float ReleaseK(float p) => Smooth(8f, 8.4f, p) * (1 - Smooth(8.8f, 9.8f, p));
        public const float AwakenDuration = 2.4f;
        public const float ReleaseAt = 8f, LowerWaveDelay = .18f, WaveLift = .06f;

        void UpdateCycle(float dt, SonMonSpiritGathering.Lod lod)
        {
            float p = Phase;
            bool full = ActiveMode == SonMonSpiritGathering.Mode.Full;
            float aw = awakenT >= 0 ? (awakenT += dt) : AwakenDuration;
            if (awakenT >= AwakenDuration) awakenT = -1;
            float intro = Smooth(0, AwakenDuration, aw);
            bool near = lod == SonMonSpiritGathering.Lod.Near, mid = lod == SonMonSpiritGathering.Lod.Mid;
            // 0 at close range .. 1 at far gameplay zoom: glows and formation brightness grow with distance so the
            // effect stays readable when the building is small on screen (no extra particles)
            float far01 = Mathf.InverseLerp(MidDistance * .6f, FarDistance * 1.15f, camDistance);

            // formation: activation front sweeps 3-6 s; the lit envelope hides the reset at the loop point
            float act = full ? Smooth(3f, 6.2f, p) * 1.05f : 0;
            float lit = full ? 1.3f * LitK(p) * (1f + 1f * far01) : 0;
            float baseL = (full ? .22f : .2f) + .4f * far01;
            SetFormation(band, act, lit, baseL * intro, bandTiling);
            SetFormation(seals, act, lit, baseL * intro, 1);

            // glows: pillars pulse gently (5 s, quarter-period stagger), the ornament halo swells in convergence and
            // flares softly at the release
            float glowScale = Mathf.Lerp(1f, 4.2f, far01);
            int n = pillarGlow.GetParticles(glowBuf);
            for (int i = 0; i < n; i++)
            {
                float k = full ? .72f + .28f * Mathf.Sin(((float)time / 5f + i * .25f) * Mathf.PI * 2) : .55f;
                glowBuf[i].startSize = (.5f + .1f * k) * glowScale;
                glowBuf[i].startColor = new Color(1, 1, 1, Mathf.Min(1f, k * (1f + .4f * far01)) * Smooth(.3f + i * .25f, .7f + i * .25f, aw));
            }
            pillarGlow.SetParticles(glowBuf, n);
            if (ornamentGlow != null)
            {
                n = ornamentGlow.GetParticles(glowBuf);
                float c = ConvergeK(p), rel = ReleaseK(p);
                for (int i = 0; i < n; i++)
                {
                    glowBuf[i].startSize = (.32f + .22f * c + .12f * rel) * (1f + 2.4f * far01);
                    glowBuf[i].startColor = new Color(1, 1, 1, Mathf.Min(1f, .18f + .32f * c + .25f * rel) * intro);
                }
                ornamentGlow.SetParticles(glowBuf, n);
            }

            // release (all distances)
            if (full && Style == ReleaseStyle.Waves)
            {
                // v5: upper-terrace waves and stair-foot bloom at 8 s, lower-landing wave just after
                if (prevPhase < ReleaseAt && p >= ReleaseAt) { EmitUpperWave(); lowerWavePending = true; }
                if (lowerWavePending && p >= ReleaseAt + LowerWaveDelay) { EmitLowerWave(); lowerWavePending = false; }
                if (p < ReleaseAt) lowerWavePending = false;
            }
            else if (full) UpdatePlume(dt, p, lod, far01);

            if (lod == SonMonSpiritGathering.Lod.Far) { prevPhase = p; return; }
            float g = GatherK(p);
            // ambient mist over the open terrace (all modes; mid = fewer, larger shapes)
            float mistRate = (full ? (near ? 2.4f : 1.5f) : (near || mid ? .5f : 0)) * intro;
            accMist += mistRate * dt;
            while (accMist >= 1) { accMist -= 1; EmitMist(mid); }
            if (full)
            {
                accGather += (near ? 2f : 1.3f) * g * dt;
                while (accGather >= 1) { accGather -= 1; EmitGatherMist(mid); }
                if (near)
                {
                    accJade += (.8f + 3.5f * g) * dt;
                    while (accJade >= 1) { accJade -= 1; EmitJade(g); }
                    accGold += (.9f + .6f * g) * dt;
                    while (accGold >= 1) { accGold -= 1; EmitGold(); }
                    var ge = glyphs.emission;
                    ge.rateOverTimeMultiplier = .8f + .6f * g;
                    if (prevPhase < ReleaseAt && p >= ReleaseAt) EmitGoldDispersal();
                }
            }
            prevPhase = p;
        }

        // V6: seeds gather to the roof ridge, then a soft plume and a few spiralling motes rise above it. Emission
        // windows are smooth in time and every particle fades in and out, so the start and end never pop.
        void UpdatePlume(float dt, float p, SonMonSpiritGathering.Lod lod, float far01)
        {
            float grow = 1f + 1.3f * far01;  // plume stays readable when the building is small on screen
            if (lod != SonMonSpiritGathering.Lod.Far)
            {
                accSeed += 11f * Smooth(7.3f, 7.7f, p) * (1 - Smooth(8f, 8.3f, p)) * dt;
                while (accSeed >= 1) { accSeed -= 1; EmitSeed(); }
                accSwirl += 15f * Smooth(8.05f, 8.3f, p) * (1 - Smooth(9f, 9.4f, p)) * dt;
                while (accSwirl >= 1) { accSwirl -= 1; EmitSwirl(grow); }
            }
            accPlume += 11f * Smooth(7.95f, 8.2f, p) * (1 - Smooth(8.9f, 9.3f, p)) * dt;
            while (accPlume >= 1) { accPlume -= 1; EmitPlume(grow); }
        }

        void EmitSeed()
        {
            float a = Rand(0, Mathf.PI * 2), rad = Rand(1.4f, 2f);
            var local = new Vector3(Mathf.Cos(a) * rad, Rand(-.9f, -.4f), Mathf.Sin(a) * rad);
            float life = Rand(.7f, .95f);
            ep = default;
            ep.position = local;
            ep.velocity = -local * (.95f / life);
            ep.startLifetime = life;
            ep.startSize = Rand(.06f, .1f);
            ep.startColor = rng.NextDouble() < .5 ? new Color(1f, .88f, .55f, .9f) : new Color(.6f, 1f, .85f, .9f);
            swirl.Emit(ep, 1);
        }

        void EmitPlume(float grow)
        {
            ep = default;
            ep.position = new Vector3(Rand(-.12f, .12f), Rand(0f, .15f), Rand(-.12f, .12f));
            ep.velocity = new Vector3(Rand(-.05f, .05f), Rand(1.5f, 2.1f) * Mathf.Sqrt(grow), Rand(-.05f, .05f));
            ep.startLifetime = Rand(1.5f, 1.9f);
            ep.startSize = Rand(.8f, 1.05f) * grow;
            ep.startColor = rng.NextDouble() < .6 ? new Color(.75f, 1f, .88f, .8f) : new Color(1f, .9f, .62f, .75f);
            plume.Emit(ep, 1);
        }

        void EmitSwirl(float grow)
        {
            float a = Rand(0, Mathf.PI * 2), rad = Rand(.45f, .7f) * grow;
            ep = default;
            ep.position = new Vector3(Mathf.Cos(a) * rad, Rand(0f, .3f), Mathf.Sin(a) * rad);
            ep.velocity = new Vector3(0, Rand(1.1f, 1.5f) * Mathf.Sqrt(grow), 0);
            ep.startLifetime = Rand(1.4f, 1.8f);
            ep.startSize = Rand(.07f, .11f) * grow;
            ep.startColor = rng.NextDouble() < .5 ? new Color(1f, .86f, .5f, .95f) : new Color(.62f, 1f, .86f, .95f);
            swirl.Emit(ep, 1);
        }

        // Flat waves over the upper tier, elliptical and kept inside the lot. The upper tier is not flat: its raised
        // middle/back terraces and its lower front strip each get a wave at their own height (each is hidden where the
        // other surface is higher), so no wave floats above the paving.
        void EmitUpperWave()
        {
            float halfX = Mathf.Min(Mathf.Max(Mathf.Abs(tier2.x), Mathf.Abs(tier2.y)), Mathf.Min(ornament.x - allowed.x, allowed.y - ornament.x));
            float midZ = (tier2.z + tier2.w) * .5f;
            float czBack = (midZ + tier2.w) * .5f;
            float halfBack = Mathf.Min((tier2.w - midZ) * .5f + .6f, Mathf.Min(czBack - allowed.z, allowed.w - czBack));
            EmitWave(new Vector3(ornament.x, tier2Top + WaveLift, czBack), halfX * 2f * .98f, halfBack * 2f * .98f, 2.2f);
            float czFront = tier2.z + (midZ - tier2.z) * .5f;
            float halfFront = Mathf.Min((midZ - tier2.z) * .5f + .3f, Mathf.Min(czFront - allowed.z, allowed.w - czFront));
            EmitWave(new Vector3(ornament.x, tier2FrontTop + WaveLift, czFront), halfX * 2f * .98f, halfFront * 2f * .98f, 2.2f);
            ep = default;
            ep.position = new Vector3(ornament.x, tier1Top + WaveLift, (tier1.z + tier2.z) * .5f);
            ep.velocity = Vector3.zero;
            ep.startLifetime = 1.6f;
            ep.startSize = Mathf.Min(2.4f, (tier2.z - tier1.z) * 2.2f);
            ep.startColor = Color.white;
            bloom.Emit(ep, 1);
        }

        // Flat wave over the lower landing (only its rim around the upper tier is visible)
        void EmitLowerWave()
        {
            float cz = (tier1.z + tier1.w) * .5f;
            float halfX = Mathf.Min(Mathf.Max(Mathf.Abs(tier1.x), Mathf.Abs(tier1.y)) + .15f, Mathf.Min(ornament.x - allowed.x, allowed.y - ornament.x));
            float halfZ = Mathf.Min((tier1.w - tier1.z) * .5f + .15f, Mathf.Min(cz - allowed.z, allowed.w - cz));
            EmitWave(new Vector3(ornament.x, tier1Top + WaveLift, cz), halfX * 2f * .98f, halfZ * 2f * .98f, 2f);
        }

        void EmitWave(Vector3 at, float sizeX, float sizeZ, float life)
        {
            ep = default;
            ep.position = at;
            ep.velocity = Vector3.zero;
            ep.startLifetime = life;  // ends before the next cycle's gathering
            ep.startSize3D = new Vector3(sizeX, sizeZ, 1f);
            ep.startColor = Color.white;
            wave.Emit(ep, 1);
        }

        void EmitGoldDispersal()
        {
            for (int i = 0; i < 14; i++)
            {
                float a = Rand(0, Mathf.PI * 2);
                var dir = new Vector3(Mathf.Cos(a), Rand(.1f, .5f), -Mathf.Abs(Mathf.Sin(a)) * .8f - .2f).normalized;
                ep = default;
                ep.position = ornament + dir * .2f;
                ep.velocity = dir * Rand(.35f, .6f);
                ep.startLifetime = Rand(2.2f, 3.2f);
                ep.startSize = Rand(.04f, .07f);
                ep.startColor = new Color(1f, .9f, .6f, .95f);
                gold.Emit(ep, 1);
            }
        }

        void SetFormation(MeshRenderer r, float act, float lit, float baseL, float tiling)
        {
            if (r == null) return;
            r.GetPropertyBlock(block);
            block.SetFloat(ActivationId, act);
            block.SetFloat(LitId, lit);
            block.SetFloat(BaseId, baseL);
            block.SetFloat(TilingId, tiling);
            block.SetFloat(DissolveId, 0);
            r.SetPropertyBlock(block);
        }

        // Mist fades out within a few units of the camera (close-ups) instead of filling the view with a blob
        float NearCameraFade(Vector3 localPos) => Smooth(2.5f, 6f, Vector3.Distance(localPos, camLocal));

        void EmitMist(bool large)
        {
            // the outer terrace strips, the back ledge and the lower landing; not the court in front of the stairs
            int r = rng.Next(4);
            if (r == 3) r = 4;
            var rect = mistRects[r];
            ep = default;
            float x = Rand(rect.xMin, rect.xMax), z = Rand(rect.yMin, rect.yMax);
            ep.position = new Vector3(x, SurfaceY(x, z) + Rand(.42f, .6f), z);
            ep.velocity = new Vector3(Rand(-.03f, .03f), Rand(.015f, .035f), Rand(-.03f, .03f));
            ep.startLifetime = Rand(5.5f, 7f);
            ep.startSize = FitLot(ep.position, Rand(.55f, .85f) * (large ? 1.4f : 1f));
            ep.rotation = Rand(-12f, 12f);  // horizontal wisps
            ep.startColor = new Color(1, 1, 1, (large ? .6f : .5f) * NearCameraFade(ep.position));
            mist.Emit(ep, 1);
        }

        // From a front arc in front of the hall (over the upper tier, never inside the building), drifting in.
        void EmitGatherMist(bool large)
        {
            float a = Rand(-Mathf.PI * .8f, -Mathf.PI * .2f);
            float rad = Rand(1.6f, 2.4f);
            var pos = new Vector3(ornament.x + Mathf.Cos(a) * rad * 1.2f, Rand(tier2Top + .3f, ornament.y + .4f), ornament.z + Mathf.Sin(a) * rad);
            pos.z = Mathf.Max(pos.z, allowed.z + .6f);
            float life = Rand(3.5f, 4.5f);
            ep = default;
            ep.position = pos;
            ep.velocity = (ornament - pos) * (.6f / life);
            ep.startLifetime = life;
            ep.startSize = FitLot(ep.position, Rand(.45f, .7f) * (large ? 1.4f : 1f));
            ep.rotation = Rand(-12f, 12f);
            ep.startColor = new Color(.9f, 1f, 1f, .55f * NearCameraFade(pos));
            mist.Emit(ep, 1);
        }

        void EmitJade(float g)
        {
            ep = default;
            if (rng.NextDouble() > g * .8f)
            {
                var pl = pillars[rng.Next(pillars.Length)];
                ep.position = pl + new Vector3(Rand(-.12f, .12f), Rand(-.4f, .05f), Rand(-.12f, .12f));
                ep.velocity = new Vector3(0, Rand(.12f, .2f), 0);
                ep.startLifetime = Rand(2f, 3f);
            }
            else
            {
                float a = Rand(-Mathf.PI * .85f, -Mathf.PI * .15f), rad = Rand(1.4f, 2.6f);
                var pos = new Vector3(ornament.x + Mathf.Cos(a) * rad * 1.3f, Rand(tier2Top + .2f, ornament.y + .6f), ornament.z + Mathf.Sin(a) * rad);
                pos.z = Mathf.Max(pos.z, allowed.z + .4f);
                float life = Rand(2.4f, 3.2f);
                ep.position = pos;
                ep.velocity = (ornament - pos) * (.85f / life);
                ep.startLifetime = life;
            }
            ep.startSize = Rand(.035f, .06f);
            ep.startColor = new Color(1, 1, 1, .9f);
            jade.Emit(ep, 1);
        }

        void EmitGold()
        {
            int r = rng.Next(mistRects.Length);
            var rect = mistRects[r];
            ep = default;
            float x = Rand(rect.xMin, rect.xMax), z = Rand(rect.yMin, rect.yMax);
            ep.position = new Vector3(x, SurfaceY(x, z) + Rand(.25f, .8f), z);
            ep.velocity = new Vector3(Rand(-.02f, .02f), Rand(.04f, .08f), Rand(-.02f, .02f));
            ep.startLifetime = Rand(4f, 6f);
            ep.startSize = Rand(.03f, .05f);
            ep.startColor = new Color(1, 1, 1, .85f);
            gold.Emit(ep, 1);
        }

        void ConfigureGlyphs()
        {
            var main = glyphs.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5.5f, 7.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(.17f, .24f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .82f, .42f, 1f), new Color(.6f, 1f, .86f, .95f));
            var shape = glyphs.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3((hallMax.x - hallMin.x) * 1.05f, .35f, .25f);
            shape.position = new Vector3((hallMin.x + hallMax.x) / 2, Mathf.Lerp(hallMin.y, hallMax.y, .5f), hallMin.z - .25f);
            var vel = glyphs.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(-.02f, .02f);
            vel.y = new ParticleSystem.MinMaxCurve(.07f, .11f);
            vel.z = new ParticleSystem.MinMaxCurve(-.03f, -.01f);
            var e = glyphs.emission;
            e.rateOverTime = .8f;
        }

        // ---------------------------------------------------------------- LOD / modes
        void ApplyLod(SonMonSpiritGathering.Lod lod)
        {
            ActiveLod = lod;
            bool near = lod == SonMonSpiritGathering.Lod.Near;
            Run(glyphs, near);
            Run(jade, near);
            Run(gold, near);
            Run(mist, lod != SonMonSpiritGathering.Lod.Far);
            Run(wave, true);
            Run(bloom, true);
            Run(plume, true);
            Run(swirl, lod != SonMonSpiritGathering.Lod.Far);
        }

        static void Run(ParticleSystem ps, bool on)
        {
            if (ps == null) return;
            if (on && !ps.isEmitting) ps.Play(true);
            else if (!on && ps.isEmitting) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        // ---------------------------------------------------------------- geometry
        void BuildMistRects()
        {
            float x1 = Mathf.Max(Mathf.Abs(tier1.x), Mathf.Abs(tier1.y)), x2 = Mathf.Max(Mathf.Abs(tier2.x), Mathf.Abs(tier2.y));
            // as v4, pulled further in from the outer walls and railings
            mistRects[0] = Rect.MinMaxRect(-x2 + .2f, tier2.z + .9f, -x2 + .45f, tier2.w - .4f);
            mistRects[1] = Rect.MinMaxRect(x2 - .45f, tier2.z + .9f, x2 - .2f, tier2.w - .4f);
            mistRects[2] = Rect.MinMaxRect(-x1 + .6f, Mathf.Max(tier2.w, hallMax.z) + .05f, x1 - .6f, Mathf.Max(tier1.w - .05f, hallMax.z + .12f));
            mistRects[3] = Rect.MinMaxRect(-1.7f, tier2.z + .35f, 1.7f, Mathf.Max(tier2.z + .5f, ornament.z - .6f));  // upper front court (gold motes only)
            mistRects[4] = Rect.MinMaxRect(-1.5f, tier1.z + .45f, 1.5f, tier2.z - .4f);  // lower front landing
        }

        Vector3[] sealCenters = new Vector3[4];

        Mesh BuildBandMesh()
        {
            float tx = Mathf.Max(Mathf.Abs(tier1.x), Mathf.Abs(tier1.y)), tzB = tier1.w, tzF = tier1.z;
            float side = allowed.y - tx, back = allowed.w - tzB;
            float gap = Mathf.Min(side, back) * .5f, width = Mathf.Min(.5f, Mathf.Min(side, back) * .7f);
            float x = tx + gap, zb = tzB + gap, rc = .7f, zf = Mathf.Max(tzF + .35f, allowed.z + .4f);
            var path = new List<Vector3>();
            void Line(Vector3 p0, Vector3 p1, int s) { for (int i = 0; i < s; i++) path.Add(Vector3.Lerp(p0, p1, i / (float)s)); }
            void Arc(Vector3 c, float a0, float a1, int s) { for (int i = 0; i < s; i++) { float t = Mathf.Lerp(a0, a1, i / (float)s); path.Add(c + new Vector3(Mathf.Cos(t), 0, Mathf.Sin(t)) * rc); } }
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
            bandTiling = Mathf.Round(length / (width * 1.2f * 8));  // eight different characters per texture repeat
            // the band starts and ends short of the front seals, so the first rune never sits on the terrace wall
            float trim = Mathf.Min(.6f, length * .05f);
            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var cols = new List<Color>(); var tris = new List<int>();
            float acc = 0;
            for (int i = 0; i < path.Count; i++)
            {
                if (i > 0) acc += Vector3.Distance(path[i - 1], path[i]);
                var tan = (path[Mathf.Min(i + 1, path.Count - 1)] - path[Mathf.Max(i - 1, 0)]).normalized;
                var sd = Vector3.Cross(Vector3.up, tan).normalized;
                float u = acc / length;
                float endFade = Mathf.Clamp01((Mathf.Min(acc, length - acc) - trim) / (length * .04f));
                verts.Add(path[i] - sd * width * .5f + Vector3.up * .014f); uvs.Add(new Vector2(u, 0)); cols.Add(new Color(0, 1, 0, endFade));
                verts.Add(path[i] + sd * width * .5f + Vector3.up * .014f); uvs.Add(new Vector2(u, 1)); cols.Add(new Color(0, 1, 0, endFade));
            }
            for (int i = 0; i < path.Count - 1; i++) { int a = i * 2; tris.Add(a); tris.Add(a + 1); tris.Add(a + 3); tris.Add(a); tris.Add(a + 3); tris.Add(a + 2); }
            return NewMesh("Formation band", verts, uvs, cols, tris);
        }

        // One flat brush-stroke seal quad per corner, each turned differently; lights as the activation front passes
        Mesh BuildSealMesh()
        {
            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var cols = new List<Color>(); var tris = new List<int>();
            float[] threshold = { .15f, .35f, .6f, .85f };
            float[] turn = { 20f, 110f, 200f, 290f };
            for (int c = 0; c < sealCenters.Length; c++)
            {
                var ctr = sealCenters[c] + Vector3.up * .016f;
                float rmax = Mathf.Min(Mathf.Min(allowed.y - Mathf.Abs(ctr.x), ctr.z - allowed.z), Mathf.Min(allowed.w - ctr.z, .6f)) - .02f;
                var q = Quaternion.Euler(0, turn[c], 0);
                int b = verts.Count;
                for (int k = 0; k < 4; k++)
                {
                    float ux = (k == 1 || k == 2) ? 1 : 0, uz = k >= 2 ? 1 : 0;
                    verts.Add(ctr + q * new Vector3((ux * 2 - 1) * rmax, 0, (uz * 2 - 1) * rmax));
                    uvs.Add(new Vector2(ux, uz));
                    cols.Add(new Color(threshold[c], 0, 0, 1));
                }
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 1); tris.Add(b); tris.Add(b + 3); tris.Add(b + 2);
            }
            return NewMesh("Formation seals", verts, uvs, cols, tris);
        }

        Mesh NewMesh(string name, List<Vector3> v, List<Vector2> uv, List<Color> c, List<int> t)
        {
            var m = new Mesh { name = name };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetColors(c); m.SetTriangles(t, 0);
            m.RecalculateBounds();
            meshes.Add(m);
            return m;
        }

        MeshRenderer Strip(string name, Material mat, Mesh mesh)
        {
            var go = new GameObject(name) { layer = 2 };
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return r;
        }

        // Height of the terrace surface under a local x/z point (upper tier, lower tier or ground).
        float SurfaceY(float x, float z)
        {
            if (x > tier2.x && x < tier2.y && z > tier2.z && z < tier2.w) return tier2Top;
            if (x > tier1.x && x < tier1.y && z > tier1.z && z < tier1.w) return tier1Top;
            return 0;
        }

        // Caps a mist shape so that, grown to its end size (x1.1) and allowing for drift, it stays inside the lot.
        float FitLot(Vector3 p, float size)
        {
            float edge = Mathf.Min(Mathf.Min(p.x - allowed.x, allowed.y - p.x), Mathf.Min(p.z - allowed.z, allowed.w - p.z));
            return Mathf.Clamp(size, .3f, Mathf.Max(.3f, (edge - .5f) * 2f / 1.1f));
        }

        float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        public int ParticleCount()
        {
            int n = 0;
            for (int i = 0; i < transform.childCount; i++)
            {
                var ps = transform.GetChild(i).GetComponent<ParticleSystem>();
                if (ps != null) n += ps.particleCount;
            }
            return n;
        }
    }
}
