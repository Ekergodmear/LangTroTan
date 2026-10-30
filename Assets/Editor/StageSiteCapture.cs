using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LangTroTan.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Isolated three-stage site check for Tàng Kinh Các (visual QA only). Builds a temporary, unsaved scene from the
// fresh-world server layout (Reports/WorldScale/site_plan/fresh_world_snapshot.json): every core lot, the store,
// tree/stone/deer nodes, the gate, and the proposed Tàng Kinh Các stage lots. The candidate model is placed on the
// current lot position at the approved door height (0.52) with 0.85 disciples, next to the previous level under
// identical lights and cameras. Candidate models are loaded from their converted folders with in-memory materials;
// nothing is assigned to the kit, and no project scene, gameplay data, server or save is touched.
// Run: Unity -batchmode -projectPath <p> -executeMethod StageSiteCapture.Run -quit   (needs graphics: no -nographics)
public static partial class StageSiteCapture
{
    static string Out = "Reports/ForgeGUI/scripture_L03/unity";
    const string Snapshot = "Reports/WorldScale/site_plan/fresh_world_snapshot.json";
    const float Door = .52f, Character = .85f;

    // dir, base name, measured door (source units), source height (source units); see MEASUREMENTS.json in each dir.
    const string Candidates = "Assets/ThirdParty/ForgeGUI/_Candidates";
    static readonly (string label, string dir, string baseName) L2 = ("L2", $"{Candidates}/ScripturePavilion_L02_attempt2", "ForgeGUI_ScripturePavilion_L02a2");
    static readonly (string label, string dir, string baseName) L3 = ("L3", $"{Candidates}/ScripturePavilion_L03", "ForgeGUI_ScripturePavilion_L03");
    static readonly (string label, string dir, string baseName) L4 = ("L4", $"{Candidates}/ScripturePavilion_L04", "ForgeGUI_ScripturePavilion_L04");
    static readonly (string label, string dir, string baseName) L5 = ("L5", $"{Candidates}/ScripturePavilion_L05_attempt2", "ForgeGUI_ScripturePavilion_L05a2");
    static readonly (string label, string dir, string baseName) L6 = ("L6", $"{Candidates}/ScripturePavilion_L06", "ForgeGUI_ScripturePavilion_L06");
    static readonly (string label, string dir, string baseName) L6R = ("L6R", $"{Candidates}/ScripturePavilion_L06_rework_prototype", "ForgeGUI_ScripturePavilion_L06R");
    static readonly (string label, string dir, string baseName) L6F = ("L6F", $"{Candidates}/ScripturePavilion_L06_refined", "ForgeGUI_ScripturePavilion_L06F");
    static readonly (string label, string dir, string baseName) L6V2 = ("L6V2", $"{Candidates}/ScripturePavilion_L06_v2", "ForgeGUI_ScripturePavilion_L06F");
    static (string label, string dir, string baseName)[] Models = { L2, L3 };
    static Action<List<GameObject>, List<(string name, Vector3 from, Vector3 at, float fov, bool ortho, float size)>, List<string>> customShots;

    // Proposed stage lots in server cells [x0, x1) x [z0, z1), as in docs/SCRIPTURE_THREE_STAGE_POLICY.md §3.
    static readonly (string name, int x0, int x1, int z0, int z1, Color color)[] Lots =
    {
        ("current 3x3", -10, -7, 4, 7, new Color(.15f, .15f, .15f)),
        ("Stage I 7x5", -12, -5, 4, 9, new Color(.18f, .5f, .47f)),
        ("Stage II 9x7", -13, -4, 4, 11, new Color(.85f, .65f, .2f)),
        ("Stage III 13x10", -15, -2, 4, 14, new Color(.75f, .2f, .15f)),
    };

    [Serializable] class Snap { public SnapBuilding[] buildings; public SnapNode[] nodes; }
    [Serializable] class SnapBuilding { public string id, type; public int x, z, w, d; }
    [Serializable] class SnapNode { public string kind; public float x, z; }
    [Serializable] class DoorMeasurement { public float doorHeightSource; public float sourceHeight; }

    public static void Run() => Capture(new[] { L2, L3 }, "Reports/ForgeGUI/scripture_L03/unity");

    // Run: Unity -batchmode -projectPath <p> -executeMethod StageSiteCapture.RunL4 -quit
    public static void RunL4() => Capture(new[] { L3, L4 }, "Reports/ForgeGUI/scripture_L04/unity");

    // Run: Unity -batchmode -projectPath <p> -executeMethod StageSiteCapture.RunL5 -quit
    public static void RunL5() => Capture(new[] { L3, L4, L5 }, "Reports/ForgeGUI/scripture_L05/unity");

    // Run: Unity -batchmode -projectPath <p> -executeMethod StageSiteCapture.RunL6 -quit
    public static void RunL6() => Capture(new[] { L4, L5, L6 }, "Reports/ForgeGUI/scripture_L06/unity");

    // Level 6 before/after the non-destructive Blender rework prototype (Tools/ForgeGUI/l6_rework_prototype.py).
    // Run: Unity -batchmode -projectPath <p> -executeMethod StageSiteCapture.RunL6Rework -quit
    public static void RunL6Rework() => Capture(new[] { L5, L6, L6R }, "Reports/ForgeGUI/scripture_L06/rework/unity");

    // Level 6 original vs the refined rework (terrace detail pass, Tools/ForgeGUI/l6_terrace_detail.py).
    // Run: Unity -batchmode -projectPath <p> -executeMethod StageSiteCapture.RunL6Refined -quit
    public static void RunL6Refined() => Capture(new[] { L5, L6, L6F }, "Reports/ForgeGUI/scripture_L06/refined/unity");

    // Level 6 refined + SonMonSpiritGathering (spirit-gathering ambient effects): L5 vs L6, effects off / full /
    // reduced, distance LOD at gameplay zoom 10/21/30, motion frames, the L5 -> L6 awakening, and measurements.
    // The visual-tier gate is switched on for this unsaved scene only.
    // Run: Unity -batchmode -projectPath <p> -executeMethod StageSiteCapture.RunL6Spirit -quit
    public static void RunL6Spirit()
    {
        bool gate = SonMonTiers.Enabled;
        var reducedOverride = SonMonUpgradeFx.SessionOverride;
        var modeOverride = SonMonSpiritGathering.ModeOverride;
        try
        {
            SonMonTiers.Enabled = true;
            spiritTarget = L6F;
            customShots = SpiritShots;
            Capture(new[] { L5, L6F }, "Reports/ForgeGUI/scripture_L06/spirit/unity");
        }
        finally
        {
            SonMonTiers.Enabled = gate;
            SonMonUpgradeFx.SessionOverride = reducedOverride;
            SonMonSpiritGathering.ModeOverride = modeOverride;
        }
    }

    // Level 6 v2: compressed two-material model (original 2048 textures + small terrace set) and the upgraded
    // formation / streams / glyphs / mist. Run: Unity -batchmode -projectPath <p> -executeMethod StageSiteCapture.RunL6SpiritV2 -quit
    public static void RunL6SpiritV2()
    {
        bool gate = SonMonTiers.Enabled;
        var reducedOverride = SonMonUpgradeFx.SessionOverride;
        var modeOverride = SonMonSpiritGathering.ModeOverride;
        try
        {
            SonMonTiers.Enabled = true;
            spiritTarget = L6V2;
            customShots = SpiritShots;
            Capture(new[] { L5, L6V2 }, "Reports/ForgeGUI/scripture_L06/spirit_v2/unity");
        }
        finally
        {
            SonMonTiers.Enabled = gate;
            SonMonUpgradeFx.SessionOverride = reducedOverride;
            SonMonSpiritGathering.ModeOverride = modeOverride;
        }
    }

    // Level 6 v3: organic wisps, jade rune formation, dissolving golden characters, gathering mist; one full 10 s loop
    // captured frame by frame. Run: Unity -batchmode -projectPath <p> -executeMethod StageSiteCapture.RunL6SpiritV3 -quit
    public static void RunL6SpiritV3()
    {
        bool gate = SonMonTiers.Enabled;
        var reducedOverride = SonMonUpgradeFx.SessionOverride;
        var modeOverride = SonMonSpiritGathering.ModeOverride;
        try
        {
            SonMonTiers.Enabled = true;
            spiritTarget = L6V2;
            captureCycle = true;
            customShots = SpiritShots;
            Capture(new[] { L5, L6V2 }, "Reports/ForgeGUI/scripture_L06/spirit_v3/unity");
        }
        finally
        {
            captureCycle = false;
            SonMonTiers.Enabled = gate;
            SonMonUpgradeFx.SessionOverride = reducedOverride;
            SonMonSpiritGathering.ModeOverride = modeOverride;
        }
    }

    // Level 6 v4: Shuriken + Shader Graph spirit gathering (SonMonSpiritGatheringV4). v3 vs v4 from identical cameras,
    // Full / Reduced / Off at gameplay zoom 10 / 21 / 30, close-ups of mist, formation and glyphs, one full 10 s cycle,
    // footprint, clipping, allocation and particle-overdraw measurements.
    // Run: Unity -batchmode -projectPath <p> -executeMethod StageSiteCapture.RunL6SpiritV4 -quit
    public static void RunL6SpiritV4()
    {
        bool gate = SonMonTiers.Enabled;
        var reducedOverride = SonMonUpgradeFx.SessionOverride;
        var modeOverride = SonMonSpiritGathering.ModeOverride;
        var modeOverride4 = SonMonSpiritGatheringV4.ModeOverride;
        try
        {
            SonMonTiers.Enabled = true;
            spiritTarget = L6V2;
            customShots = SpiritShotsV4;
            Capture(new[] { L5, L6V2 }, "Reports/ForgeGUI/scripture_L06/spirit_v4/unity");
        }
        finally
        {
            SonMonTiers.Enabled = gate;
            SonMonUpgradeFx.SessionOverride = reducedOverride;
            SonMonSpiritGathering.ModeOverride = modeOverride;
            SonMonSpiritGatheringV4.ModeOverride = modeOverride4;
        }
    }

    static void SpiritShotsV4(List<GameObject> built, List<(string name, Vector3 from, Vector3 at, float fov, bool ortho, float size)> views, List<string> report)
    {
        foreach (var root in built) root.SetActive(root.name == spiritTarget.label);
        var l6 = built.Find(r => r.name == spiritTarget.label);
        var model = l6.transform.GetChild(0).gameObject;
        var b = Bounds(model);
        var fxGo = new GameObject("SpiritGathering");
        fxGo.transform.SetParent(l6.transform, true);
        fxGo.transform.position = new Vector3(b.center.x, b.min.y, b.center.z);
        var a = JsonUtility.FromJson<SpiritAnchors>(File.ReadAllText($"{spiritTarget.dir}/VFX_ANCHORS_UNITY.json"));
        float k = model.transform.localScale.x;
        Vector3 V(float[] arr, int i) => new Vector3(arr[i], arr[i + 1], arr[i + 2]) * k;
        var pillars = new Vector3[a.pillars.Length / 3];
        for (int i = 0; i < pillars.Length; i++) pillars[i] = V(a.pillars, i * 3);
        var o = fxGo.transform.position;
        var allowedLocal = new Vector4(StageIILotWorld.x - o.x, StageIILotWorld.y - o.x, StageIILotWorld.z - o.z, StageIILotWorld.w - o.z);
        Vector4 t1 = new Vector4(a.tier1[0], a.tier1[1], a.tier1[2], a.tier1[3]) * k, t2 = new Vector4(a.tier2[0], a.tier2[1], a.tier2[2], a.tier2[3]) * k;
        var orn = V(a.ornament, 0);
        var pillarFR = o + new Vector3(Mathf.Abs(pillars[0].x), pillars[0].y, -Mathf.Abs(pillars[0].z));
        var cams = new List<(string name, Vector3 from, Vector3 at, float fov, bool ortho, float size)>
        {
            views.Find(v => v.name == "45"),
            views.Find(v => v.name == "front"),
            views.Find(v => v.name == "gameplay_zoom10"),
            views.Find(v => v.name == "gameplay_zoom21_default"),
            views.Find(v => v.name == "gameplay_zoom30"),
            ("hero", o + new Vector3(5.6f, 4.4f, -7.4f), o + new Vector3(0, 1.7f, -.4f), 34, false, 0),
            ("close_mist", o + new Vector3(-5.4f, 2.0f, -4.6f), o + new Vector3(-2.6f, .7f, -.6f), 34, false, 0),
            ("close_formation", o + new Vector3(-6.0f, 2.6f, -5.6f), o + new Vector3(-3.4f, .2f, -2.9f), 34, false, 0),
            ("close_pillar_formation", pillarFR + new Vector3(2.2f, .9f, -2.8f), pillarFR + new Vector3(-.2f, -.9f, 0), 34, false, 0),
            ("close_glyphs", o + new Vector3(.8f, 2.4f, -5.2f), o + new Vector3(0, 2.0f, -1.2f), 30, false, 0),
            ("close_ornament", o + orn + new Vector3(1.2f, .5f, -3.4f), o + orn, 30, false, 0),
        };
        const float dt = 1f / 30f;
        var near = cams.Find(v => v.name == "gameplay_zoom10").from;

        // 1. v3 (current approved-for-review version) from the same cameras, full effects
        var v3 = fxGo.AddComponent<SonMonSpiritGathering>();
        v3.Configure(pillars, orn, V(a.hallMin, 0), V(a.hallMax, 0), t1, t2, a.tier1[4] * k, a.tier2[4] * k, allowedLocal);
        SonMonSpiritGathering.ModeOverride = SonMonSpiritGathering.Mode.Full;
        SonMonUpgradeFx.SessionOverride = false;
        v3.Build(false);
        void Run3(float seconds, Vector3 camPos)
        {
            for (float t = 0; t < seconds; t += dt) { qaTime += dt; SonMonSpiritGathering.SetSpiritTime(qaTime); v3.Step(dt, camPos); }
        }
        Run3(7f, near);
        foreach (var v in cams)
        {
            Run3(2.5f, v.from);
            Capture($"v3_{v.name}", v.from, v.at, v.fov, v.ortho, v.size);
        }
        v3.Clear();
        UnityEngine.Object.DestroyImmediate(v3);

        // 2. v4
        var sg = fxGo.AddComponent<SonMonSpiritGatheringV4>();
        sg.Configure(pillars, orn, V(a.hallMin, 0), V(a.hallMax, 0), t1, t2, a.tier1[4] * k, a.tier2[4] * k, allowedLocal);
        void Run(float seconds, Vector3 camPos) { for (float t = 0; t < seconds; t += dt) sg.Step(dt, camPos); }
        // advance to a given cycle phase (captures compare like with like: v4 shots at the convergence peak 7.0 s
        // unless stated otherwise)
        void RunTo(float phase, Vector3 camPos) { float wait = (phase - sg.Phase + SonMonSpiritGatheringV4.Cycle) % SonMonSpiritGatheringV4.Cycle; Run(wait < 2.5f ? wait + 10f : wait, camPos); }
        foreach (var mode in new[] { SonMonSpiritGathering.Mode.Off, SonMonSpiritGathering.Mode.Full, SonMonSpiritGathering.Mode.Reduced })
        {
            SonMonSpiritGatheringV4.ModeOverride = mode;
            SonMonUpgradeFx.SessionOverride = mode == SonMonSpiritGathering.Mode.Reduced;
            bool builtOk = sg.Build(false);
            Run(4f, near);
            string tag = mode.ToString().ToLower();
            foreach (var v in cams)
            {
                if (mode != SonMonSpiritGathering.Mode.Full && v.name.StartsWith("close_") && v.name != "close_formation") continue;
                RunTo(7f, v.from);  // includes settling at this camera distance (LOD)
                if (v.name.StartsWith("gameplay"))
                    report.Add($"v4,{tag},{v.name},lod,{sg.ActiveLod},particles,{sg.ParticleCount()},activeRenderers,{ActiveRenderers(fxGo)},particleOverdraw,{ParticleOverdraw(fxGo, v.from, v.at, v.fov)}");
                Capture($"v4_{tag}_{v.name}", v.from, v.at, v.fov, v.ortho, v.size);
            }
            report.Add($"v4,{tag},built,{builtOk},colliders,{fxGo.GetComponentsInChildren<Collider>(true).Length},layers,{string.Join("/", LayersOf(fxGo))}");
            if (mode != SonMonSpiritGathering.Mode.Full) continue;

            // phases at zoom 10 and zoom 21: ambient 1.5, gathering 4.5, convergence 7.0, release 8.8
            foreach (var zv in new[] { cams.Find(v => v.name == "gameplay_zoom10"), cams.Find(v => v.name == "gameplay_zoom21_default"), cams.Find(v => v.name == "hero") })
                foreach (var ph in new[] { 1.5f, 4.5f, 7.0f, 8.8f })
                {
                    RunTo(ph, zv.from);
                    Capture($"v4_phase_{zv.name}_{ph:0.0}", zv.from, zv.at, zv.fov, false, 0);
                }
            // one complete 10 s loop at 10 fps from the hero camera (frame 100 = frame 0 + 10 s, to check the seam)
            var hero = cams.Find(v => v.name == "hero");
            var z21 = cams.Find(v => v.name == "gameplay_zoom21_default");
            RunTo(0f, hero.from);
            Directory.CreateDirectory(Path.Combine(Out, "cycle"));
            Directory.CreateDirectory(Path.Combine(Out, "cycle_zoom21"));
            for (int f = 0; f <= 100; f++)
            {
                Capture($"cycle/frame_{f:000}", hero.from, hero.at, hero.fov, false, 0, 960, 540);
                Run(.1f, hero.from);
            }
            RunTo(0f, z21.from);
            for (int f = 0; f <= 100; f += 2)
            {
                Capture($"cycle_zoom21/frame_{f:000}", z21.from, z21.at, z21.fov, false, 0, 960, 540);
                Run(.2f, z21.from);
            }
            report.Add($"v4,cycle,heroFrames,101,fps,10,zoom21Frames,51,fps,5,loopSeconds,{SonMonSpiritGatheringV4.Cycle}");
            // footprint and clipping over a full cycle (sampled every 0.5 s)
            Bounds all = default; bool any = false; int insideTerrace = 0, total = 0, maxParticles = 0;
            var perSystem = new Dictionary<string, (Bounds b, int inside, int total)>();
            var buf = new ParticleSystem.Particle[64];
            for (int s = 0; s < 20; s++)
            {
                Run(.5f, near);
                var rb = sg.RenderedBounds();
                if (!any) { all = rb; any = true; } else all.Encapsulate(rb);
                maxParticles = Mathf.Max(maxParticles, sg.ParticleCount());
                foreach (var ps in fxGo.GetComponentsInChildren<ParticleSystem>())
                {
                    bool skipClip = ps.name == "PS_PillarGlow" || ps.name == "PS_OrnamentGlow" || ps.name == "PS_ReleasePulse";
                    int n = ps.GetParticles(buf);
                    for (int i = 0; i < n; i++)
                    {
                        var wp = ps.transform.TransformPoint(buf[i].position);
                        var pb = new Bounds(wp, Vector3.one * buf[i].GetCurrentSize(ps));
                        var e = perSystem.TryGetValue(ps.name, out var cur) ? cur : (b: pb, inside: 0, total: 0);
                        e.b.Encapsulate(pb); e.total++;
                        if (!skipClip) total++;
                        var p = buf[i].position / k;  // anchors are in model units
                        bool in1 = p.x > a.tier1[0] && p.x < a.tier1[1] && p.z > a.tier1[2] && p.z < a.tier1[3] && p.y < a.tier1[4] - .02f;
                        bool in2 = p.x > a.tier2[0] && p.x < a.tier2[1] && p.z > a.tier2[2] && p.z < a.tier2[3] && p.y < a.tier2[4] - .02f;
                        // hall solid: behind the facade plane (the ornament sits on it); the hall box also spans the open porch
                        bool inHall = p.x > a.hallMin[0] && p.x < a.hallMax[0] && p.z > a.ornament[2] + .05f && p.z < a.hallMax[2] && p.y < a.hallMax[1];
                        if (!skipClip && (in1 || in2 || inHall)) { insideTerrace++; e.inside++; }
                        perSystem[ps.name] = e;
                    }
                }
            }
            bool inside = all.min.x >= StageIILotWorld.x - .001f && all.max.x <= StageIILotWorld.y + .001f && all.min.z >= StageIILotWorld.z - .001f && all.max.z <= StageIILotWorld.w + .001f;
            report.Add($"v4,footprint,insideStageIILot,{inside},vfxBoundsX,{all.min.x:0.00},{all.max.x:0.00},Z,{all.min.z:0.00},{all.max.z:0.00},Y,{all.min.y:0.00},{all.max.y:0.00},lotX,-13,-4,Z,4,11");
            report.Add($"v4,clipping,particleSamplesInsideTerraceOrHall,{insideTerrace},of,{total},maxParticlesNear,{maxParticles}");
            foreach (var kv in perSystem)
                report.Add($"v4,system,{kv.Key},X,{kv.Value.b.min.x:0.00},{kv.Value.b.max.x:0.00},Y,{kv.Value.b.min.y:0.00},{kv.Value.b.max.y:0.00},Z,{kv.Value.b.min.z:0.00},{kv.Value.b.max.z:0.00},insideSolids,{kv.Value.inside},of,{kv.Value.total}");
            foreach (var r in fxGo.GetComponentsInChildren<MeshRenderer>())
                report.Add($"v4,strip,{r.name},X,{r.bounds.min.x:0.00},{r.bounds.max.x:0.00},Z,{r.bounds.min.z:0.00},{r.bounds.max.z:0.00}");
            report.Add($"v4,anchors,tier1,{string.Join("/", a.tier1)},tier2,{string.Join("/", a.tier2)},hallMin,{string.Join("/", a.hallMin)},hallMax,{string.Join("/", a.hallMax)},ornament,{string.Join("/", a.ornament)},fxOrigin,{o.x:0.00}/{o.y:0.00}/{o.z:0.00}");
            // allocations: runtime path (edit-mode manual particle simulation switched off for the measurement)
            SonMonSpiritGathering.SimulateInEditMode = false;
            foreach (var cv in new[] { near, z21.from, cams.Find(v => v.name == "gameplay_zoom30").from })
            {
                Run(1f, cv);
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 600; i++) sg.Step(dt, cv);  // 20 s: two full cycles including two releases
                report.Add($"v4,gc,bytesPer600Steps,lod,{sg.ActiveLod},{GC.GetAllocatedBytesForCurrentThread() - before}");
            }
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 600; i++) sg.Step(dt, near);
            sw.Stop();
            report.Add($"v4,cpu,msPerStepEditorScriptOnly,{sw.Elapsed.TotalMilliseconds / 600:0.0000}");
            SonMonSpiritGathering.SimulateInEditMode = true;
        }
        // L5 -> L6 awakening (forced as after a server-confirmed swap): frames through the intro into the cycle
        SonMonSpiritGatheringV4.ModeOverride = SonMonSpiritGathering.Mode.Full;
        SonMonUpgradeFx.SessionOverride = false;
        sg.Build(true);
        var aw = cams.Find(v => v.name == "45");
        for (int f = 0; f <= 10; f++)
        {
            Capture($"v4_awaken_{f:00}", aw.from, aw.at, aw.fov, false, 0, 960, 540);
            Run(.4f, aw.from);
        }
        // no awakening on load: Build() without the flag and without a playing upgrade
        sg.Build();
        report.Add($"v4,awakenOnPlainBuild,{sg.Awakening}");
        // VFX texture memory (unique textures on the v4 materials)
        long vfxTex = 0;
        var seen = new HashSet<Texture>();
        foreach (var r in fxGo.GetComponentsInChildren<Renderer>(true))
        {
            var m = r.sharedMaterial;
            if (m == null) continue;
            foreach (var prop in m.GetTexturePropertyNames())
            {
                var t = m.GetTexture(prop);
                if (t != null && seen.Add(t)) { vfxTex += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t); report.Add($"v4,texture,{t.name},{t.width}x{t.height},{(t as Texture2D)?.format},{UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t)}"); }
            }
        }
        var mats = new HashSet<Material>();
        foreach (var r in fxGo.GetComponentsInChildren<Renderer>(true)) if (r.sharedMaterial != null) mats.Add(r.sharedMaterial);
        report.Add($"v4,memory,vfxTextures,{vfxTex},count,{seen.Count},materials,{mats.Count},shaders,{string.Join("/", mats.Select(m => m.shader.name).Distinct())}");
    }

    static IEnumerable<int> LayersOf(GameObject root) => root.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject.layer).Distinct();

    // Rough particle overdraw: summed screen area of all live particle quads (clamped by maxParticleSize) divided by the
    // screen area, i.e. the average number of transparent particle layers per pixel over the whole frame.
    static string ParticleOverdraw(GameObject root, Vector3 from, Vector3 at, float fov)
    {
        var buf = new ParticleSystem.Particle[64];
        double sum = 0, maxFrac = 0;
        float tanH = Mathf.Tan(fov * .5f * Mathf.Deg2Rad);
        var fwd = (at - from).normalized;
        foreach (var ps in root.GetComponentsInChildren<ParticleSystem>())
        {
            var pr = ps.GetComponent<ParticleSystemRenderer>();
            int n = ps.GetParticles(buf);
            for (int i = 0; i < n; i++)
            {
                var p = ps.transform.TransformPoint(buf[i].position);
                float d = Mathf.Max(.1f, Vector3.Dot(p - from, fwd));
                float h = buf[i].GetCurrentSize(ps) / (2 * d * tanH);  // fraction of screen height
                h = Mathf.Min(h, pr.maxParticleSize);
                if (pr.renderMode == ParticleSystemRenderMode.HorizontalBillboard) h *= Mathf.Abs(fwd.y);
                double frac = h * h * (9.0 / 16.0);  // quad area / screen area (16:9)
                sum += frac;
                maxFrac = Math.Max(maxFrac, frac);
            }
        }
        return $"{sum:0.000},largestQuadScreenFraction,{maxFrac:0.000}";
    }

    // Isolated QA player for frame pacing and performance: builds Assets/QA/L6SpiritQA/L6SpiritQA.unity (L6 v2 model,
    // ground, sun, configured SonMonSpiritGathering, L6SpiritBenchmark) and a development Windows player outside the
    // project. EditorBuildSettings and production scenes are not touched.
    // Run: Unity -batchmode -projectPath <p> -executeMethod StageSiteCapture.BuildL6SpiritQAPlayer -quit -qabuild <dir>
    public static void BuildL6SpiritQAPlayer()
    {
        const string dir = "Assets/QA/L6SpiritQA", scenePath = dir + "/L6SpiritQA.unity";
        var args = Environment.GetCommandLineArgs();
        int ai = Array.IndexOf(args, "-qabuild");
        string outDir = ai >= 0 ? args[ai + 1] : "../../work/qa_builds/L6SpiritQA";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var kit = AssetDatabase.LoadAssetAtPath<SonMonKit>("Assets/Resources/SonMon/SonMonKit.asset");
        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional; sun.intensity = 1.35f; sun.color = new Color(1, .93f, .82f); sun.shadows = LightShadows.Soft;
        sun.transform.eulerAngles = new Vector3(48, -35, 0);
        if (kit != null && kit.daySky != null) RenderSettings.skybox = kit.daySky;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.66f, .77f, .8f);
        RenderSettings.ambientEquatorColor = new Color(.58f, .64f, .58f);
        RenderSettings.ambientGroundColor = new Color(.3f, .36f, .32f);
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.localScale = new Vector3(10, 1, 10);
        var meadow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SonMon/Materials/SonMon Meadow.mat");
        if (meadow != null) ground.GetComponent<Renderer>().sharedMaterial = meadow;
        // building (door 0.52 already in the v2 export), entrance to -Z, front edge at z = 0, centred on x = 0
        var model = Load(L6V2.dir, L6V2.baseName);
        model.transform.localRotation = Quaternion.Euler(0, 180, 0);
        var b = Bounds(model);
        model.transform.position = new Vector3(-b.center.x, -b.min.y, -b.min.z);
        b = Bounds(model);
        // persist the in-memory materials so the saved QA scene keeps them
        var saved = new Dictionary<Material, Material>();
        foreach (var r in model.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (!saved.TryGetValue(mats[i], out var asset))
                {
                    asset = new Material(mats[i]);
                    AssetDatabase.CreateAsset(asset, $"{dir}/{L6V2.baseName}_{saved.Count}.mat");
                    saved[mats[i]] = asset;
                }
                mats[i] = asset;
            }
            r.sharedMaterials = mats;
        }
        var fxGo = new GameObject("SpiritGathering");
        fxGo.transform.position = new Vector3(b.center.x, b.min.y, b.center.z);
        var a = JsonUtility.FromJson<SpiritAnchors>(File.ReadAllText($"{L6V2.dir}/VFX_ANCHORS_UNITY.json"));
        Vector3 V(float[] arr, int i) => new Vector3(arr[i], arr[i + 1], arr[i + 2]);
        var pillars = new Vector3[a.pillars.Length / 3];
        for (int i = 0; i < pillars.Length; i++) pillars[i] = V(a.pillars, i * 3);
        var o = fxGo.transform.position;
        // proposed Stage II lot relative to this layout: same offsets as the site scene (front edge on the lot line)
        var allowedLocal = new Vector4(-4.5f, 4.5f, -(o.z - 0f), 7f - (o.z - 0f));
        var sg = fxGo.AddComponent<SonMonSpiritGathering>();
        sg.Configure(pillars, V(a.ornament, 0), V(a.hallMin, 0), V(a.hallMax, 0),
            new Vector4(a.tier1[0], a.tier1[1], a.tier1[2], a.tier1[3]), new Vector4(a.tier2[0], a.tier2[1], a.tier2[2], a.tier2[3]),
            a.tier1[4], a.tier2[4], allowedLocal);
        // v4 on its own sibling object (same origin), so the two versions never clear each other's children
        var fx4Go = new GameObject("SpiritGatheringV4");
        fx4Go.transform.position = fxGo.transform.position;
        var sg4 = fx4Go.AddComponent<SonMonSpiritGatheringV4>();
        sg4.Configure(pillars, V(a.ornament, 0), V(a.hallMin, 0), V(a.hallMax, 0),
            new Vector4(a.tier1[0], a.tier1[1], a.tier1[2], a.tier1[3]), new Vector4(a.tier2[0], a.tier2[1], a.tier2[2], a.tier2[3]),
            a.tier1[4], a.tier2[4], allowedLocal);
        // The game's Game.Boot (AfterSceneLoad) starts the full port UI unless a FullPortView already exists. A disabled
        // placeholder keeps the QA player isolated: no game UI, camera or server connection attempt, and no code change.
        new GameObject("QA boot guard").AddComponent<FullPortView>().enabled = false;
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 42; cam.nearClipPlane = .1f; cam.farClipPlane = 400; cam.allowHDR = true;
        var benchType = Type.GetType("L6SpiritBenchmark, Assembly-CSharp");
        var bench = camGo.AddComponent(benchType);
        var so = new SerializedObject(bench);
        so.FindProperty("effects").objectReferenceValue = sg;
        so.FindProperty("effectsV4").objectReferenceValue = sg4;
        so.FindProperty("focus").objectReferenceValue = fxGo.transform;
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene, scenePath);
        Directory.CreateDirectory(outDir);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { scenePath },
            locationPathName = Path.Combine(outDir, "L6SpiritQA.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development,
        });
        Debug.Log($"L6_QA_BUILD {report.summary.result} size={report.summary.totalSize} time={report.summary.totalTime}");
    }

    static bool captureCycle;
    static double qaTime;
    static (string label, string dir, string baseName) spiritTarget;
    static readonly Vector4 StageIILotWorld = new Vector4(-13, -4, 4, 11);  // proposed Stage II lot: x0, x1, z0, z1

    [Serializable] class SpiritAnchors { public float[] pillars, ornament, hallMin, hallMax, tier1, tier2; }

    static void SpiritShots(List<GameObject> built, List<(string name, Vector3 from, Vector3 at, float fov, bool ortho, float size)> views, List<string> report)
    {
        var baseViews = views.FindAll(v => v.name != "site_top" && v.name != "terrace_closeup");
        // 1. Identical camera and lighting: L5 and L6 (no effects)
        foreach (var root in built)
        {
            root.SetActive(true);
            foreach (var v in baseViews) Capture($"{(root.name == spiritTarget.label ? "L6_off" : "L5")}_{v.name}", v.from, v.at, v.fov, v.ortho, v.size);
            root.SetActive(false);
        }
        var l6 = built.Find(r => r.name == spiritTarget.label);
        l6.SetActive(true);
        var model = l6.transform.GetChild(0).gameObject;
        var b = Bounds(model);
        var fxGo = new GameObject("SpiritGathering");
        fxGo.transform.SetParent(l6.transform, true);
        fxGo.transform.position = new Vector3(b.center.x, b.min.y, b.center.z);
        var a = JsonUtility.FromJson<SpiritAnchors>(File.ReadAllText($"{spiritTarget.dir}/VFX_ANCHORS_UNITY.json"));
        float k = model.transform.localScale.x;
        Vector3 V(float[] arr, int i) => new Vector3(arr[i], arr[i + 1], arr[i + 2]) * k;
        var pillars = new Vector3[a.pillars.Length / 3];
        for (int i = 0; i < pillars.Length; i++) pillars[i] = V(a.pillars, i * 3);
        var sg = fxGo.AddComponent<SonMonSpiritGathering>();
        var o = fxGo.transform.position;
        var allowedLocal = new Vector4(StageIILotWorld.x - o.x, StageIILotWorld.y - o.x, StageIILotWorld.z - o.z, StageIILotWorld.w - o.z);
        sg.Configure(pillars, V(a.ornament, 0), V(a.hallMin, 0), V(a.hallMax, 0),
            new Vector4(a.tier1[0], a.tier1[1], a.tier1[2], a.tier1[3]) * k, new Vector4(a.tier2[0], a.tier2[1], a.tier2[2], a.tier2[3]) * k,
            a.tier1[4] * k, a.tier2[4] * k, allowedLocal);
        var pillarFR = o + pillars[0].x * Vector3.zero + new Vector3(Mathf.Abs(pillars[0].x), pillars[0].y, -Mathf.Abs(pillars[0].z));
        var extra = new List<(string name, Vector3 from, Vector3 at, float fov, bool ortho, float size)>
        {
            ("close_stairs", o + new Vector3(0, 1.7f, -6.2f), o + new Vector3(0, .65f, -2.4f), 30, false, 0),
            ("close_pillar_formation", pillarFR + new Vector3(2.2f, .9f, -2.8f), pillarFR + new Vector3(-.2f, -.9f, 0), 34, false, 0),
            ("close_formation_corner", o + new Vector3(-6.0f, 2.6f, -5.6f), o + new Vector3(-3.4f, .2f, -2.9f), 34, false, 0),
            ("close_roof_runes", o + new Vector3(3.5f, 4.2f, -7.5f), o + new Vector3(0, 3.2f, 0), 32, false, 0),
            ("close_streams", o + new Vector3(5.5f, 5.5f, -7.0f), o + new Vector3(0, 2.6f, -.6f), 34, false, 0),
        };
        var all = new List<(string name, Vector3 from, Vector3 at, float fov, bool ortho, float size)>(baseViews);
        all.AddRange(extra);
        const float dt = 1f / 30f;
        void Run(float seconds, Vector3 camPos)
        {
            for (float t = 0; t < seconds; t += dt)
            {
                qaTime += dt;
                SonMonSpiritGathering.SetSpiritTime(qaTime);
                sg.Step(dt, camPos);
            }
        }
        var near = all.Find(v => v.name == "gameplay_zoom10").from;

        foreach (var mode in new[] { SonMonSpiritGathering.Mode.Off, SonMonSpiritGathering.Mode.Full, SonMonSpiritGathering.Mode.Reduced })
        {
            SonMonSpiritGathering.ModeOverride = mode;
            SonMonUpgradeFx.SessionOverride = mode == SonMonSpiritGathering.Mode.Reduced;
            bool builtOk = sg.Build(false);
            Run(7f, near);
            string tag = mode.ToString().ToLower();
            foreach (var v in all)
            {
                Run(2.5f, v.from);  // settle at this camera distance (LOD) before capturing
                if (v.name.StartsWith("gameplay")) report.Add($"spirit,{tag},{v.name},lod,{sg.ActiveLod},particles,{sg.ParticleCount()},activeRenderers,{ActiveRenderers(fxGo)}");
                Capture($"L6_{tag}_{v.name}", v.from, v.at, v.fov, v.ortho, v.size);
            }
            report.Add($"spirit,{tag},built,{builtOk},colliders,{fxGo.GetComponentsInChildren<Collider>(true).Length},layer,{fxGo.layer}");
            if (mode == SonMonSpiritGathering.Mode.Full)
            {
                // motion frames (zoom 10 and pillar / formation close-up), 0.75 s apart
                var z10 = all.Find(v => v.name == "gameplay_zoom10");
                var pc = all.Find(v => v.name == "close_pillar_formation");
                var rr = all.Find(v => v.name == "close_roof_runes");
                var cs = all.Find(v => v.name == "close_streams");
                for (int f = 0; f < 6; f++)
                {
                    Run(.75f, z10.from);
                    Capture($"motion_zoom10_{f}", z10.from, z10.at, z10.fov, false, 0);
                    Capture($"motion_pillar_{f}", pc.from, pc.at, pc.fov, false, 0);
                    Capture($"motion_runes_{f}", rr.from, rr.at, rr.fov, false, 0);
                    Capture($"motion_streams_{f}", cs.from, cs.at, cs.fov, false, 0);
                }
                if (captureCycle)
                {
                    // one complete 10 s loop at 10 fps from a hero camera (plus frame 100 = t + 10 s to check the seam)
                    var hero = (from: o + new Vector3(5.6f, 4.4f, -7.4f), at: o + new Vector3(0, 1.7f, -.4f));
                    double start = Math.Ceiling(qaTime / SonMonSpiritGathering.LoopSeconds) * SonMonSpiritGathering.LoopSeconds;
                    Run((float)(start - qaTime), hero.from);
                    Directory.CreateDirectory(Path.Combine(Out, "cycle"));
                    for (int f = 0; f <= 100; f++)
                    {
                        Capture($"cycle/frame_{f:000}", hero.from, hero.at, 34, false, 0, 960, 540);
                        Run(.1f, hero.from);
                    }
                    report.Add($"spirit,cycle,frames,101,fps,10,loopSeconds,{SonMonSpiritGathering.LoopSeconds}");
                }
                // footprint: everything rendered (strips + live particles) against the proposed Stage II lot
                Run(3f, z10.from);
                var rb = sg.RenderedBounds();
                bool inside = rb.min.x >= StageIILotWorld.x - .001f && rb.max.x <= StageIILotWorld.y + .001f && rb.min.z >= StageIILotWorld.z - .001f && rb.max.z <= StageIILotWorld.w + .001f;
                report.Add($"spirit,footprint,insideStageIILot,{inside},vfxBoundsX,{rb.min.x:0.00},{rb.max.x:0.00},Z,{rb.min.z:0.00},{rb.max.z:0.00},lotX,-13,-4,Z,4,11");
                // the same without the pillar-crystal glow billboards (they sit on the crystals at ~2.2 m height)
                var glowGos = new List<GameObject>();
                foreach (var ps in fxGo.GetComponentsInChildren<ParticleSystem>())
                    if (ps.name == "Pillar glow" || ps.name == "Pillar crystal cores") { glowGos.Add(ps.gameObject); ps.gameObject.SetActive(false); }
                var rb2 = sg.RenderedBounds();
                foreach (var g in glowGos) g.SetActive(true);
                bool inside2 = rb2.min.x >= StageIILotWorld.x - .001f && rb2.max.x <= StageIILotWorld.y + .001f && rb2.min.z >= StageIILotWorld.z - .001f && rb2.max.z <= StageIILotWorld.w + .001f;
                report.Add($"spirit,footprint,excludingPillarGlow,insideStageIILot,{inside2},vfxBoundsX,{rb2.min.x:0.00},{rb2.max.x:0.00},Z,{rb2.min.z:0.00},{rb2.max.z:0.00}");
                // clipping: particles inside the terrace solids (tier 1 below its top, tier 2 below its top)
                int insideTerrace = 0, total = 0;
                var buf = new ParticleSystem.Particle[64];
                foreach (var ps in fxGo.GetComponentsInChildren<ParticleSystem>())
                {
                    if (ps.name == "Pillar glow" || ps.name == "Pillar crystal cores") continue;
                    int n = ps.GetParticles(buf);
                    for (int i = 0; i < n; i++)
                    {
                        var p = buf[i].position;  // local to the fx root (local simulation space, child at origin)
                        total++;
                        bool in1 = p.x > a.tier1[0] && p.x < a.tier1[1] && p.z > a.tier1[2] && p.z < a.tier1[3] && p.y < a.tier1[4] - .02f;
                        bool in2 = p.x > a.tier2[0] && p.x < a.tier2[1] && p.z > a.tier2[2] && p.z < a.tier2[3] && p.y < a.tier2[4] - .02f;
                        if (in1 || in2) insideTerrace++;
                    }
                }
                report.Add($"spirit,clipping,particlesInsideTerrace,{insideTerrace},of,{total}");
                // CPU cost of Step (editor, edit mode, includes manual particle simulation: an upper bound)
                var sw = System.Diagnostics.Stopwatch.StartNew();
                for (int i = 0; i < 600; i++) sg.Step(dt, near);
                sw.Stop();
                report.Add($"spirit,cpu,msPerStepEditor,{sw.Elapsed.TotalMilliseconds / 600:0.000}");
                // allocations per step: GC.GetAllocatedBytesForCurrentThread over 300 play-mode-equivalent steps
                // (edit-mode simulate path excluded by measuring the gameplay-zoom-30 far LOD, where nothing simulates)
                var far = all.Find(v => v.name == "gameplay_zoom30").from;
                Run(1f, far);
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 300; i++) sg.Step(dt, far);
                report.Add($"spirit,gc,bytesPer300StepsFarLod,{GC.GetAllocatedBytesForCurrentThread() - before}");
                // near LOD, runtime path only (edit-mode manual particle simulation switched off for the measurement)
                SonMonSpiritGathering.SimulateInEditMode = false;
                Run(1f, near);
                before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 300; i++) sg.Step(dt, near);
                report.Add($"spirit,gc,bytesPer300StepsNearLodRuntimePath,{GC.GetAllocatedBytesForCurrentThread() - before}");
                SonMonSpiritGathering.SimulateInEditMode = true;
            }
        }
        // L5 -> L6 awakening (forced, full effects): frames through the 2.4 s intro
        SonMonSpiritGathering.ModeOverride = SonMonSpiritGathering.Mode.Full;
        SonMonUpgradeFx.SessionOverride = false;
        sg.Build(true);
        var aw = all.Find(v => v.name == "45");
        for (int f = 0; f <= 8; f++)
        {
            Capture($"awaken_{f}", aw.from, aw.at, aw.fov, false, 0);
            Run(.3f, aw.from);
        }
        // texture memory and triangles (measured in the editor)
        long Tex(string path) { var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path); return t != null ? UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t) : -1; }
        string bn = $"{spiritTarget.dir}/{spiritTarget.baseName}";
        report.Add($"memory,model,albedo,{Tex(bn + "_albedo.png")},normal,{Tex(bn + "_normal.png")},mask,{Tex(bn + "_mask.png")},terraceAlbedo,{Tex(bn + "_terrace_albedo.png")},terraceMask,{Tex(bn + "_terrace_mask.png")}");
        foreach (var path in new[] { bn + "_albedo.png", bn + "_normal.png", bn + "_mask.png", bn + "_terrace_albedo.png" })
        {
            var tx = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tx != null) report.Add($"memory,format,{Path.GetFileName(path)},{tx.width}x{tx.height},{tx.format},mips,{tx.mipmapCount}");
        }
        long vfxTex = 0;
        var seen = new HashSet<Texture>();
        foreach (var r in fxGo.GetComponentsInChildren<Renderer>(true))
        {
            var mats = new List<Material> { r.sharedMaterial };
            if (r is ParticleSystemRenderer pr && pr.trailMaterial != null) mats.Add(pr.trailMaterial);
            foreach (var m in mats)
                if (m != null && m.mainTexture != null && seen.Add(m.mainTexture)) vfxTex += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(m.mainTexture);
        }
        report.Add($"memory,vfxTextures,{vfxTex},count,{seen.Count}");
        int tris = 0;
        foreach (var f in model.GetComponentsInChildren<MeshFilter>()) tris += f.sharedMesh.triangles.Length / 3;
        report.Add($"geometry,{spiritTarget.label},triangles,{tris}");
    }

    static int ActiveRenderers(GameObject root)
    {
        int n = 0;
        foreach (var r in root.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
            if (r is ParticleSystemRenderer && r.GetComponent<ParticleSystem>().particleCount == 0) continue;
            n++;
        }
        return n;
    }

    // Level 5 (approved art) with the separate jade crystal prop and its SonMonJadeAura effects, rendered without the
    // prop, with full effects and with reduced effects. The visual-tier gate is switched on for this unsaved scene only.
    // Run: Unity -batchmode -projectPath <p> -executeMethod StageSiteCapture.RunL5Jade -quit
    public static void RunL5Jade()
    {
        bool gate = SonMonTiers.Enabled;
        var reducedOverride = SonMonUpgradeFx.SessionOverride;
        try
        {
            SonMonTiers.Enabled = true;
            Capture(new[] { L5 }, "Reports/ForgeGUI/scripture_L05/unity_jade", jade: true);
        }
        finally
        {
            SonMonTiers.Enabled = gate;
            SonMonUpgradeFx.SessionOverride = reducedOverride;
        }
    }

    const string JadeProp = "Assets/Art/SonMon/Props/JadeCrystal_L05/SonMon_JadeCrystal_L05.fbx";
    const float JadeHeight = 1.79f, JadeForward = .35f;  // second-floor band at door 0.52; forward of the wall (balcony line)

    static void Capture((string label, string dir, string baseName)[] models, string outDir, bool jade = false)
    {
        Models = models;
        Out = outDir;
        Directory.CreateDirectory(Out);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);  // temporary, never saved
        var snap = JsonUtility.FromJson<Snap>(File.ReadAllText(Snapshot));
        var kit = AssetDatabase.LoadAssetAtPath<SonMonKit>("Assets/Resources/SonMon/SonMonKit.asset");
        var disciple = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/SonMon/Prefabs/Character_disciple.prefab");
        var tree = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/SonMon/Prefabs/Nature_CommonTree_3_50.prefab");
        var hall = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/SonMon/Prefabs/Building_hall.prefab");
        var meadow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SonMon/Materials/SonMon Meadow.mat");
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        Material Flat(Color c) { var m = new Material(unlit); m.SetColor("_BaseColor", c); return m; }

        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.35f;
        sun.color = new Color(1, .93f, .82f);
        sun.shadows = LightShadows.Soft;
        sun.transform.eulerAngles = new Vector3(48, -35, 0);
        if (kit != null && kit.daySky != null) RenderSettings.skybox = kit.daySky;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.66f, .77f, .8f);
        RenderSettings.ambientEquatorColor = new Color(.58f, .64f, .58f);
        RenderSettings.ambientGroundColor = new Color(.3f, .36f, .32f);
        RenderSettings.fog = false;
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.localScale = new Vector3(10, 1, 10);
        if (meadow != null) ground.GetComponent<Renderer>().sharedMaterial = meadow;

        // Server layout: server cell (x, z) covers world [x, x+1) x [z, z+1); Unity +Z = server +z.
        var coreMat = Flat(new Color(.55f, .55f, .55f));
        foreach (var b in snap.buildings)
        {
            if (b.id == "core:tang_kinh_cac") continue;
            Outline(b.x, b.x + b.w, b.z, b.z + b.d, coreMat, .05f);
            if (b.id == "core:chanh_dien" && hall != null)  // the kit hall, as today's production art (not door-normalised)
            {
                var h = (GameObject)PrefabUtility.InstantiatePrefab(hall);
                h.transform.position = new Vector3(b.x + b.w / 2f, 0, b.z + b.d / 2f);
            }
        }
        foreach (var n in snap.nodes)
        {
            if (n.kind == "tree" && tree != null)
            {
                var t = (GameObject)PrefabUtility.InstantiatePrefab(tree);
                t.transform.position = new Vector3(n.x, 0, n.z);
            }
            else Bar(new Vector3(n.x, .15f, n.z), Vector3.one * .3f, Flat(n.kind == "stone" ? Color.gray : new Color(.7f, .45f, .25f)));
        }
        foreach (var lot in Lots) Outline(lot.x0, lot.x1, lot.z0, lot.z1, Flat(lot.color), .07f);
        Bar(new Vector3(.5f, .02f, -3.5f), new Vector3(2, .02f, .4f), Flat(Color.white));  // gate marker (0,-4)

        // Candidates side by side on the same stage-lot anchor: L2 on the real lot, L3 shifted by a fixed offset so
        // both can be framed by identical cameras (each camera is rendered once per model, offset by the same vector).
        var report = new List<string> { "model,doorSource,sourceHeight,scale,worldWidth,worldDepth,worldHeight,frontEdgeZ,insideStageI(7x5),insideStageII(9x7)" };
        var lotCentre = new Vector3(-8.5f, 0, 5.5f);
        var built = new List<GameObject>();
        foreach (var (label, dir, baseName) in Models)
        {
            var inst = Load(dir, baseName);
            if (inst == null) { report.Add($"{label},missing"); continue; }
            var m = JsonUtility.FromJson<DoorMeasurement>(File.ReadAllText($"{dir}/MEASUREMENTS.json"));
            var root = new GameObject(label);
            inst.transform.SetParent(root.transform, false);
            inst.transform.localRotation = Quaternion.Euler(0, 180, 0);  // entrance to -Z, as the kit builder does
            float scale = Door / m.doorHeightSource * m.sourceHeight / Bounds(inst).size.y;
            inst.transform.localScale = Vector3.one * scale;
            var b = Bounds(inst);
            // Front edge (min z) on the lot's front edge z = 4, centred on x = -8.5 (growth backwards and sideways).
            inst.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.min.z);
            root.transform.position = new Vector3(lotCentre.x, 0, 4f);
            root.SetActive(false);
            built.Add(root);
            b = Bounds(inst);
            bool inside = b.min.x >= -12 && b.max.x <= -5 && b.min.z >= 4 - .01f && b.max.z <= 9;
            bool insideII = b.min.x >= -13 && b.max.x <= -4 && b.min.z >= 4 - .01f && b.max.z <= 11;
            report.Add($"{label},{m.doorHeightSource:0.000},{m.sourceHeight:0.0000},{scale:0.000},{b.size.x:0.00},{b.size.z:0.00},{b.size.y:0.00},{b.min.z:0.00},{inside},{insideII}");
        }
        // Disciples (0.85 display height, visual child only) at the entrance and beside the lot.
        foreach (var at in new[] { new Vector3(-8.5f, 0, 3.3f), new Vector3(-9.1f, 0, 3.0f), new Vector3(-4.6f, 0, 5.5f) })
        {
            var p = (GameObject)PrefabUtility.InstantiatePrefab(disciple);
            p.transform.position = at;
            p.transform.GetChild(0).localScale = Vector3.one * (Character / 1.41f);
        }

        var focus = lotCentre + new Vector3(0, 1.4f, 0);
        var gfocus = lotCentre;
        Vector3 Game(float zoom) => gfocus + new Vector3(31, 29, -38).normalized * (zoom * 2.7f);
        var views = new List<(string name, Vector3 from, Vector3 at, float fov, bool ortho, float size)>
        {
            ("front", focus + new Vector3(0, .4f, -16f), focus, 30, false, 0),
            ("45", focus + new Vector3(10f, 8f, -12f), focus, 34, false, 0),
            ("side", focus + new Vector3(16f, .4f, 0), focus, 30, false, 0),
            ("top", lotCentre + new Vector3(0, 40, 0), lotCentre, 0, true, 8.5f),
            ("site_top", new Vector3(-2, 60, 2), new Vector3(-2, 0, 2.001f), 0, true, 18f),
            ("gameplay_zoom10", Game(10), gfocus, 42, false, 0),
            ("gameplay_zoom21_default", Game(21), gfocus, 42, false, 0),
            ("gameplay_zoom30", Game(30), gfocus, 42, false, 0),
            ("terrace_closeup", lotCentre + new Vector3(4.2f, 2.4f, -6.0f), lotCentre + new Vector3(1.6f, .6f, -1.4f), 30, false, 0),
        };
        Capture("warmup", views[0].from, views[0].at, 30, false, 0);  // first batch frame compiles shaders
        File.Delete(Path.Combine(Out, "warmup.png"));
        if (customShots != null)
        {
            var shots = customShots;
            customShots = null;
            shots(built, views, report);
            File.WriteAllLines(Path.Combine(Out, "measurements.csv"), report);
            Debug.Log("STAGE_SITE_CUSTOM_DONE " + string.Join(" | ", report));
            return;
        }
        if (jade)
        {
            CaptureJade(built[0], views, report);
            File.WriteAllLines(Path.Combine(Out, "measurements.csv"), report);
            Debug.Log("STAGE_SITE_JADE_DONE " + string.Join(" | ", report));
            return;
        }
        foreach (var root in built)
        {
            root.SetActive(true);
            foreach (var v in views) Capture($"{root.name}_{v.name}", v.from, v.at, v.fov, v.ortho, v.size);
            root.SetActive(false);
        }
        File.WriteAllLines(Path.Combine(Out, "measurements.csv"), report);
        Debug.Log("STAGE_SITE_DONE " + string.Join(" | ", report));
    }

    static void CaptureJade(GameObject root, List<(string name, Vector3 from, Vector3 at, float fov, bool ortho, float size)> views,
        List<string> report)
    {
        root.SetActive(true);
        var model = root.transform.GetChild(0).gameObject;
        // Facade depth at the second-floor band: ray-cast against temporary mesh colliders on the building.
        var colliders = new List<MeshCollider>();
        foreach (var f in model.GetComponentsInChildren<MeshFilter>()) { var c = f.gameObject.AddComponent<MeshCollider>(); c.sharedMesh = f.sharedMesh; colliders.Add(c); }
        var b = Bounds(model);
        var origin = new Vector3(b.center.x, JadeHeight, b.min.z - 5);
        bool hit = Physics.Raycast(origin, Vector3.forward, out var info, 20);
        float wallZ = hit ? info.point.z : b.min.z + .3f * b.size.z;
        foreach (var c in colliders) UnityEngine.Object.DestroyImmediate(c);
        var at = new Vector3(b.center.x, JadeHeight, wallZ - JadeForward);
        report.Add($"jade,anchorWorld,{at.x:0.00},{at.y:0.00},{at.z:0.00},wallHit,{hit},wallZ,{wallZ:0.00},anchorLocalToLevelRoot,{root.transform.InverseTransformPoint(at)}");

        var closeFocus = at;
        var jadeViews = new List<(string name, Vector3 from, Vector3 at, float fov, bool ortho, float size)>(views)
        {
            ("jade_closeup_front", closeFocus + new Vector3(0, .2f, -5f), closeFocus, 30, false, 0),
            ("jade_closeup_45", closeFocus + new Vector3(3f, 2.2f, -4f), closeFocus, 30, false, 0),
        };
        jadeViews.RemoveAll(v => v.name == "site_top" || v.name == "side");
        // Baseline: the approved GLB alone.
        foreach (var v in jadeViews) Capture($"L5_noprop_{v.name}", v.from, v.at, v.fov, v.ortho, v.size);
        foreach (bool reduced in new[] { false, true })
        {
            SonMonUpgradeFx.SessionOverride = reduced;
            var prop = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(JadeProp));
            prop.name = "Jade crystal prop";
            prop.transform.SetParent(root.transform, true);
            prop.transform.position = at;
            prop.transform.rotation = Quaternion.identity;  // crystal faces -Z, the building front
            var aura = prop.AddComponent<SonMonJadeAura>();
            bool built = aura.Build();
            aura.Apply(PulseCaptureTime);
            var motes = prop.GetComponentInChildren<ParticleSystem>();
            if (motes != null) motes.Simulate(3f, true, true);
            var pb = Bounds(prop);
            report.Add($"jade,{(reduced ? "reduced" : "full")},built,{built},motes,{motes != null},propSize,{pb.size.x:0.00}x{pb.size.y:0.00}x{pb.size.z:0.00}");
            foreach (var v in jadeViews) Capture($"L5_jade_{(reduced ? "reduced" : "full")}_{v.name}", v.from, v.at, v.fov, v.ortho, v.size);
            UnityEngine.Object.DestroyImmediate(prop);
        }
    }

    const float PulseCaptureTime = 1.1f;  // near the pulse peak, so the full-effects frame shows the strongest glow

    static GameObject Load(string dir, string baseName)
    {
        var main = LoadPart(dir, baseName, true);
        if (main == null) return null;
        // Optional second part with its own material (refined L6 v2: the terrace keeps a small texture set so the
        // building keeps its original 2048 textures untouched).
        if (AssetDatabase.LoadAssetAtPath<GameObject>($"{dir}/{baseName}_terrace.fbx") == null) return main;
        var root = new GameObject(baseName);
        main.transform.SetParent(root.transform, false);
        LoadPart(dir, baseName + "_terrace", false).transform.SetParent(root.transform, false);
        return root;
    }

    static GameObject LoadPart(string dir, string baseName, bool hasNormal)
    {
        var modelPath = $"{dir}/{baseName}.fbx";
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (model == null) return null;
        void TextureType(string file, bool normal, bool linear)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath($"{dir}/{file}");
            if (importer == null) return;
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !linear && !normal;
            importer.maxTextureSize = 4096;  // the earlier refined L6 used a 4096 x 2048 derived atlas
            importer.textureCompression = TextureImporterCompression.CompressedHQ;  // BC7
            // v2 two-material model: keep full resolution where detail shows (albedo, normal), shrink what doesn't.
            bool v2 = dir.EndsWith("_L06_v2");
            var standalone = importer.GetPlatformTextureSettings("Standalone");
            standalone.overridden = v2;
            if (v2)
            {
                bool terraceAlbedo = file.EndsWith("_terrace_albedo.png"), mask = file.EndsWith("_mask.png");
                standalone.maxTextureSize = mask && !file.Contains("_terrace") ? 1024 : 2048;
                standalone.format = normal ? TextureImporterFormat.BC5 : terraceAlbedo ? TextureImporterFormat.DXT1 : TextureImporterFormat.BC7;
            }
            importer.SetPlatformTextureSettings(standalone);
            importer.SaveAndReimport();
        }
        TextureType(baseName + "_albedo.png", false, false);
        if (hasNormal) TextureType(baseName + "_normal.png", true, true);
        TextureType(baseName + "_mask.png", false, true);
        // In-memory material (not saved): same URP Lit setup as SonMonKitBuilder.ForgeGuiModelPrefab.
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{baseName}_albedo.png"));
        if (hasNormal)
        {
            mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{baseName}_normal.png"));
            mat.EnableKeyword("_NORMALMAP");
        }
        mat.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{baseName}_mask.png"));
        mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        mat.SetFloat("_Metallic", 1);
        mat.SetFloat("_Smoothness", 1);
        var root = (GameObject)PrefabUtility.InstantiatePrefab(model);
        foreach (var r in root.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
        }
        return root;
    }

    static void Outline(float x0, float x1, float z0, float z1, Material m, float w)
    {
        Bar(new Vector3((x0 + x1) / 2, .015f, z0), new Vector3(x1 - x0 + w, .012f, w), m);
        Bar(new Vector3((x0 + x1) / 2, .015f, z1), new Vector3(x1 - x0 + w, .012f, w), m);
        Bar(new Vector3(x0, .015f, (z0 + z1) / 2), new Vector3(w, .012f, z1 - z0 + w), m);
        Bar(new Vector3(x1, .015f, (z0 + z1) / 2), new Vector3(w, .012f, z1 - z0 + w), m);
    }

    static void Bar(Vector3 at, Vector3 size, Material m)
    {
        var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(bar.GetComponent<Collider>());
        bar.transform.position = at;
        bar.transform.localScale = size;
        bar.GetComponent<Renderer>().sharedMaterial = m;
        bar.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    static Bounds Bounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static void Capture(string name, Vector3 from, Vector3 at, float fov, bool ortho, float size, int width = 1920, int height = 1080)
    {
        var go = new GameObject("Capture " + name);
        var cam = go.AddComponent<Camera>();
        cam.orthographic = ortho;
        if (ortho) cam.orthographicSize = size; else cam.fieldOfView = fov;
        cam.nearClipPlane = .1f;
        cam.farClipPlane = 400;
        cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(.65f, .77f, .75f);
        cam.allowHDR = true;
        go.transform.position = from;
        go.transform.LookAt(at);
        var rt = new RenderTexture(width, height, 24) { antiAliasing = 4 };
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(Out, name + ".png"), tex.EncodeToPNG());
        RenderTexture.active = null;
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(go);
    }
}
