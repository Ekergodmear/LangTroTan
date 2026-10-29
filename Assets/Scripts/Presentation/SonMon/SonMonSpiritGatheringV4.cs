using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LangTroTan.Presentation
{
    // Tàng Kinh Các Level 6 spirit VFX v4: Shuriken particle systems (prefabs in Resources/SonMon/VFX/L6) on Shader
    // Graph materials (SG_SpiritMist, SG_SpiritFormation, SG_ScriptureGlyph), orchestrated as one seamless 10 s
    // spirit-gathering cycle:
    //   0-3 s  ambient      soft jade mist circulates over the terrace, a few motes rise by the pillars
    //   3-6 s  gathering    the rune formation activates rune by rune; mist and motes drift towards the jade ornament
    //   6-8 s  convergence  the ornament glow swells (no flash)
    //   8-10 s release      a soft jade-gold pulse spreads across the terrace, motes disperse, back to ambient
    // Every driven quantity is a continuous periodic function of the cycle phase, so the loop has no visible reset; all
    // particles fade in and out over their lifetimes (no popping). Particles are emitted by script with reused
    // EmitParams (no per-frame allocations); formation properties go through a MaterialPropertyBlock.
    // Modes: Full; Reduced (faint static formation, steady pillar glow, a little ambient mist); Off (nothing built).
    // Distance LOD: near (zoom 10) everything; mid (zoom 21) larger, fewer mist shapes, formation, ornament and pillar
    // glow, release pulse; far (zoom 30) formation and glows only, every particle system stops emitting.
    // Presentation only: no colliders, Ignore Raycast layer, children of the building root.
    public sealed class SonMonSpiritGatheringV4 : MonoBehaviour
    {
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

        [SerializeField] Vector3[] pillars = new Vector3[0];
        [SerializeField] Vector3 ornament, hallMin, hallMax;
        [SerializeField] Vector4 tier1, tier2, allowed = new Vector4(-1e3f, 1e3f, -1e3f, 1e3f);
        [SerializeField] float tier1Top = .47f, tier2Top = .94f;

        public SonMonSpiritGathering.Mode ActiveMode { get; private set; } = SonMonSpiritGathering.Mode.Off;
        public SonMonSpiritGathering.Lod ActiveLod { get; private set; } = SonMonSpiritGathering.Lod.Near;
        public float Phase => (float)(time % Cycle);

        ParticleSystem mist, jade, gold, pillarGlow, ornamentGlow, pulse, glyphs;
        MeshRenderer band, seals;
        readonly List<Mesh> meshes = new List<Mesh>();
        readonly ParticleSystem.Particle[] glowBuf = new ParticleSystem.Particle[8];
        readonly Rect[] mistRects = new Rect[5];
        ParticleSystem.EmitParams ep;
        MaterialPropertyBlock block;
        System.Random rng;
        double time;
        float awakenT = -1, prevPhase, accMist, accGather, accJade, accGold, accGlyph, bandTiling;
        bool built;
        static Camera cam;
        static float camLookup;

        public void Configure(Vector3[] pillarCrystals, Vector3 ornamentLocal, Vector3 hallMinLocal, Vector3 hallMaxLocal,
            Vector4 tier1XZ, Vector4 tier2XZ, float tier1Height, float tier2Height, Vector4 allowedXZ)
        {
            pillars = pillarCrystals; ornament = ornamentLocal; hallMin = hallMinLocal; hallMax = hallMaxLocal;
            tier1 = tier1XZ; tier2 = tier2XZ; tier1Top = tier1Height; tier2Top = tier2Height; allowed = allowedXZ;
        }

        // ---------------------------------------------------------------- build
        // awaken: play the Level 5 -> 6 awakening, only right after a server-confirmed upgrade swap (never when a saved
        // game is loaded). The awakening fades everything in from zero and starts the cycle at the gathering phase, so the
        // formation lights rune by rune and the intro hands over to the loop without a seam.
        public bool Build(bool? awaken = null)
        {
            Clear();
            ActiveMode = CurrentMode;
            if (ActiveMode == SonMonSpiritGathering.Mode.Off) return false;
            rng = new System.Random(6006);
            block ??= new MaterialPropertyBlock();
            gameObject.layer = 2;
            bool full = ActiveMode == SonMonSpiritGathering.Mode.Full;
            var formMat = Resources.Load<Material>("SonMon/VFX/L6/M_SpiritFormation");
            band = Strip("Formation band", formMat, BuildBandMesh());
            seals = Strip("Formation seals", formMat, BuildSealMesh());
            mist = Spawn("PS_SpiritMist");
            pillarGlow = Spawn("PS_PillarGlow");
            for (int i = 0; i < pillars.Length; i++) EmitStatic(pillarGlow, pillars[i], .5f);
            if (full)
            {
                jade = Spawn("PS_JadeMotes");
                gold = Spawn("PS_GoldenMotes");
                ornamentGlow = Spawn("PS_OrnamentGlow");
                EmitStatic(ornamentGlow, ornament + new Vector3(0, 0, -.06f), .5f);
                pulse = Spawn("PS_ReleasePulse");
                pulse.GetComponent<ParticleSystemRenderer>().maxParticleSize = 4f;  // flat ring: must not be screen-clamped
                glyphs = Spawn("PS_ScriptureGlyphs");
                ConfigureGlyphs();
            }
            BuildMistRects();
            bool awakenNow = awaken ?? (transform.parent != null && SonMonUpgradeFx.IsPlaying(transform.parent, out var fx) && fx.ToLevel == 6 && fx.Swapped);
            awakenT = awakenNow ? 0 : -1;
            if (awakenNow) time = 3.0;
            prevPhase = Phase;
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
            mist = jade = gold = pillarGlow = ornamentGlow = pulse = glyphs = null;
            band = seals = null;
            built = false;
        }

        void OnDestroy() { foreach (var m in meshes) Kill(m); meshes.Clear(); }

        static void Kill(UnityEngine.Object o) { if (o == null) return; if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }

        ParticleSystem Spawn(string prefab)
        {
            var src = Resources.Load<GameObject>("SonMon/VFX/L6/" + prefab);
            var go = Instantiate(src, transform, false);
            go.name = prefab;
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
            float d = Vector3.Distance(cameraPos, transform.position);
            var lod = d < MidDistance ? SonMonSpiritGathering.Lod.Near : d < FarDistance ? SonMonSpiritGathering.Lod.Mid : SonMonSpiritGathering.Lod.Far;
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
        public const float AwakenDuration = 2.4f;

        void UpdateCycle(float dt, SonMonSpiritGathering.Lod lod)
        {
            float p = Phase;
            bool full = ActiveMode == SonMonSpiritGathering.Mode.Full;
            // awakening intro: 0 -> 1 over 2.4 s (1 when not awakening); pillars light one after another
            float aw = awakenT >= 0 ? (awakenT += dt) : AwakenDuration;
            if (awakenT >= AwakenDuration) awakenT = -1;
            float intro = Smooth(0, AwakenDuration, aw);
            bool near = lod == SonMonSpiritGathering.Lod.Near, mid = lod == SonMonSpiritGathering.Lod.Mid;

            // formation: activation front sweeps 3-6 s, lit level envelope hides the reset at the loop point
            float act = full ? Smooth(3f, 6.2f, p) * 1.05f : 0;
            float lit = full ? 1.3f * LitK(p) * (mid ? 1.35f : 1f) : 0;
            float baseL = full ? (lod == SonMonSpiritGathering.Lod.Far ? .3f : mid ? .34f : .22f) : .2f;
            SetFormation(band, act, lit, baseL * intro, bandTiling);
            SetFormation(seals, act, lit, baseL * intro, 1);

            // glows: pillars pulse gently (5 s, quarter-period stagger), the ornament swells in convergence
            float lodScale = lod == SonMonSpiritGathering.Lod.Far ? 1.7f : mid ? 1.5f : 1f;
            int n = pillarGlow.GetParticles(glowBuf);
            for (int i = 0; i < n; i++)
            {
                float k = full ? .72f + .28f * Mathf.Sin(((float)time / 5f + i * .25f) * Mathf.PI * 2) : .55f;
                glowBuf[i].startSize = (.5f + .1f * k) * lodScale;
                glowBuf[i].startColor = new Color(1, 1, 1, k * Smooth(.3f + i * .25f, .7f + i * .25f, aw));
            }
            pillarGlow.SetParticles(glowBuf, n);
            if (ornamentGlow != null)
            {
                n = ornamentGlow.GetParticles(glowBuf);
                float c = ConvergeK(p);
                for (int i = 0; i < n; i++)
                {
                    // swells softly; never an opaque ball or a flash
                    glowBuf[i].startSize = (.4f + .5f * c) * lodScale;
                    glowBuf[i].startColor = new Color(1, 1, 1, (.25f + .45f * c) * intro);
                }
                ornamentGlow.SetParticles(glowBuf, n);
            }

            if (lod == SonMonSpiritGathering.Lod.Far) { prevPhase = p; return; }
            float g = GatherK(p);
            // ambient mist over the open terrace (all modes; mid = fewer, larger shapes)
            float mistRate = (full ? (near ? 2.6f : 1.6f) : (near || mid ? .5f : 0)) * intro;
            accMist += mistRate * dt;
            while (accMist >= 1) { accMist -= 1; EmitMist(mid); }
            if (full)
            {
                // gathering mist and motes drifting towards the jade ornament
                accGather += (near ? 2.2f : 1.4f) * g * dt;
                while (accGather >= 1) { accGather -= 1; EmitGatherMist(mid); }
                if (near)
                {
                    accJade += (.8f + 3.5f * g) * dt;
                    while (accJade >= 1) { accJade -= 1; EmitJade(g); }
                    accGold += (.9f + .6f * g) * dt;
                    while (accGold >= 1) { accGold -= 1; EmitGold(); }
                    var ge = glyphs.emission;
                    ge.rateOverTimeMultiplier = .9f + .7f * g;
                }
                // release at 8 s: one soft pulse ring and an outward dispersal of motes
                if (prevPhase < 8f && p >= 8f)
                {
                    ep = default;
                    // centred on the terrace below the ornament; the ring never leaves the lot
                    float cz = (tier1.z + tier1.w) * .5f;
                    float reach = Mathf.Min(Mathf.Min(ornament.x - allowed.x, allowed.y - ornament.x), Mathf.Min(cz - allowed.z, allowed.w - cz));
                    ep.position = new Vector3(ornament.x, tier2Top + .05f, cz);  // over the upper terrace (most of the platform)
                    ep.velocity = Vector3.zero;
                    ep.startLifetime = 2f;  // ends exactly as the next cycle begins
                    ep.startSize = reach * 2f * .98f;
                    ep.startColor = new Color(1f, .95f, .8f, 1f);
                    pulse.Emit(ep, 1);
                    if (near)
                        for (int i = 0; i < 14; i++)
                        {
                            float a = Rand(0, Mathf.PI * 2);
                            var dir = new Vector3(Mathf.Cos(a), Rand(.1f, .5f), -Mathf.Abs(Mathf.Sin(a)) * .8f - .2f).normalized;
                            ep.position = ornament + dir * .2f;
                            ep.velocity = dir * Rand(.35f, .6f);
                            ep.startLifetime = Rand(2.2f, 3.2f);
                            ep.startSize = Rand(.04f, .07f);
                            ep.startColor = new Color(1f, .9f, .6f, .95f);
                            gold.Emit(ep, 1);
                        }
                }
            }
            prevPhase = p;
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

        void EmitMist(bool large)
        {
            int r = rng.Next(mistRects.Length);
            var rect = mistRects[r];
            ep = default;
            float x = Rand(rect.xMin, rect.xMax), z = Rand(rect.yMin, rect.yMax);
            ep.position = new Vector3(x, SurfaceY(x, z) + Rand(.3f, .45f), z);
            ep.velocity = new Vector3(Rand(-.03f, .03f), Rand(.02f, .05f), Rand(-.03f, .03f));
            ep.startLifetime = Rand(5.5f, 7f);
            ep.startSize = FitLot(ep.position, Rand(.75f, 1.15f) * (large ? 1.5f : 1f));
            ep.rotation = Rand(0, 360);
            ep.startColor = new Color(1, 1, 1, large ? .8f : .7f);
            mist.Emit(ep, 1);
        }

        // From a front arc in front of the hall (over the upper tier, never inside the building), drifting in.
        void EmitGatherMist(bool large)
        {
            float a = Rand(-Mathf.PI * .8f, -Mathf.PI * .2f);
            float rad = Rand(1.6f, 2.4f);
            var pos = new Vector3(ornament.x + Mathf.Cos(a) * rad * 1.2f, Rand(tier2Top + .15f, ornament.y + .3f), ornament.z + Mathf.Sin(a) * rad);
            pos.z = Mathf.Max(pos.z, allowed.z + .6f);
            float life = Rand(3.5f, 4.5f);
            ep = default;
            ep.position = pos;
            ep.velocity = (ornament - pos) * (.6f / life);
            ep.startLifetime = life;
            ep.startSize = FitLot(ep.position, Rand(.55f, .85f) * (large ? 1.5f : 1f));
            ep.rotation = Rand(0, 360);
            ep.startColor = new Color(.9f, 1f, 1f, .75f);
            mist.Emit(ep, 1);
        }

        void EmitJade(float g)
        {
            ep = default;
            if (rng.NextDouble() > g * .8f)
            {
                // ambient: rising beside a pillar crystal
                var pl = pillars[rng.Next(pillars.Length)];
                ep.position = pl + new Vector3(Rand(-.12f, .12f), Rand(-.4f, .05f), Rand(-.12f, .12f));
                ep.velocity = new Vector3(0, Rand(.12f, .2f), 0);
                ep.startLifetime = Rand(2f, 3f);
            }
            else
            {
                // gathering: in from the front arc towards the ornament, fading before it arrives
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
            main.startSize = new ParticleSystem.MinMaxCurve(.13f, .19f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .82f, .42f, 1f), new Color(.55f, 1f, .85f, .9f));
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
            e.rateOverTime = .9f;
        }

        // ---------------------------------------------------------------- LOD / modes
        void ApplyLod(SonMonSpiritGathering.Lod lod)
        {
            ActiveLod = lod;
            bool near = lod == SonMonSpiritGathering.Lod.Near;
            // systems that are not needed stop emitting; their live particles fade out over their lifetimes
            // (no popping), and a stopped system with no particles costs nothing
            Run(glyphs, near);
            Run(jade, near);
            Run(gold, near);
            Run(mist, lod != SonMonSpiritGathering.Lod.Far);
            Run(pulse, lod != SonMonSpiritGathering.Lod.Far);
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
            // open strips along the outer edges of the upper terrace (outside the wings), the back ledge, the upper front
            // court and the lower front landing; emission heights follow the surface below (SurfaceY)
            mistRects[0] = Rect.MinMaxRect(-x2 + .05f, tier2.z + .8f, -x2 + .35f, tier2.w - .3f);
            mistRects[1] = Rect.MinMaxRect(x2 - .35f, tier2.z + .8f, x2 - .05f, tier2.w - .3f);
            mistRects[2] = Rect.MinMaxRect(-x1 + .5f, Mathf.Max(tier2.w, hallMax.z) + .02f, x1 - .5f, Mathf.Max(tier1.w, hallMax.z + .1f));
            mistRects[3] = Rect.MinMaxRect(-1.7f, tier2.z + .35f, 1.7f, Mathf.Max(tier2.z + .5f, ornament.z - .6f));  // upper front court
            mistRects[4] = Rect.MinMaxRect(-1.5f, tier1.z + .4f, 1.5f, tier2.z - .35f);  // lower front landing
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
            // vertex colour: G = 1 (threshold = rune position along the band), R = 0, A = 1
            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var cols = new List<Color>(); var tris = new List<int>();
            float acc = 0;
            for (int i = 0; i < path.Count; i++)
            {
                if (i > 0) acc += Vector3.Distance(path[i - 1], path[i]);
                var tan = (path[Mathf.Min(i + 1, path.Count - 1)] - path[Mathf.Max(i - 1, 0)]).normalized;
                var sd = Vector3.Cross(Vector3.up, tan).normalized;
                float u = acc / length;
                float endFade = Mathf.Clamp01(Mathf.Min(u, 1 - u) / .03f);
                verts.Add(path[i] - sd * width * .5f + Vector3.up * .014f); uvs.Add(new Vector2(u, 0)); cols.Add(new Color(0, 1, 0, endFade));
                verts.Add(path[i] + sd * width * .5f + Vector3.up * .014f); uvs.Add(new Vector2(u, 1)); cols.Add(new Color(0, 1, 0, endFade));
            }
            for (int i = 0; i < path.Count - 1; i++) { int a = i * 2; tris.Add(a); tris.Add(a + 1); tris.Add(a + 3); tris.Add(a); tris.Add(a + 3); tris.Add(a + 2); }
            return NewMesh("Formation band", verts, uvs, cols, tris);
        }

        Mesh BuildSealMesh()
        {
            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var cols = new List<Color>(); var tris = new List<int>();
            // seals light as the activation front passes: front-left, front-right, back-left, back-right
            float[] threshold = { .15f, .35f, .6f, .85f };
            for (int c = 0; c < sealCenters.Length; c++)
            {
                var ctr = sealCenters[c];
                float rmax = Mathf.Min(Mathf.Min(allowed.y - Mathf.Abs(ctr.x), ctr.z - allowed.z), Mathf.Min(allowed.w - ctr.z, .55f)) - .02f;
                Annulus(verts, uvs, cols, tris, ctr + Vector3.up * .016f, rmax * .7f, rmax, 48, 8, threshold[c]);
                Annulus(verts, uvs, cols, tris, ctr + Vector3.up * .017f, rmax * .3f, rmax * .58f, 32, 5, threshold[c]);
            }
            return NewMesh("Formation seals", verts, uvs, cols, tris);
        }

        static void Annulus(List<Vector3> v, List<Vector2> uv, List<Color> col, List<int> tri, Vector3 c, float r0, float r1, int seg, int repeats, float threshold)
        {
            int b = v.Count;
            for (int i = 0; i <= seg; i++)
            {
                float t = i / (float)seg, a = t * Mathf.PI * 2;
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                v.Add(c + d * r0); uv.Add(new Vector2(t * repeats, 0)); col.Add(new Color(threshold, 0, 0, 1));
                v.Add(c + d * r1); uv.Add(new Vector2(t * repeats, 1)); col.Add(new Color(threshold, 0, 0, 1));
            }
            for (int i = 0; i < seg; i++) { int a = b + i * 2; tri.Add(a); tri.Add(a + 1); tri.Add(a + 3); tri.Add(a); tri.Add(a + 3); tri.Add(a + 2); }
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

        // Caps a mist shape so that, grown to its end size (x1.15) and allowing for drift, it stays inside the lot.
        float FitLot(Vector3 p, float size)
        {
            float edge = Mathf.Min(Mathf.Min(p.x - allowed.x, allowed.y - p.x), Mathf.Min(p.z - allowed.z, allowed.w - p.z));
            return Mathf.Clamp(size, .3f, Mathf.Max(.3f, (edge - .5f) * 2f / 1.15f));
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
