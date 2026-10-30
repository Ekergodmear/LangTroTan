using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LangTroTan.Presentation;
using UnityEngine;

// Level 6 spirit VFX v5 candidate (SonMonSpiritGatheringV5; isolated, v4 stays the baseline). Same site, model, lights
// and cameras as RunL6SpiritV4: Off / Reduced / Full at gameplay zoom 10 / 21 / 30 and the close-ups (convergence
// peak 7.0 s, like v4), ambient 1.5 s and release-peak 8.6 s frames at hero and every gameplay zoom, one 10 s cycle
// from the hero camera, and the v4 measurements (footprint, clipping, allocations, script time, particle overdraw).
// Needs the v5 assets: run SpiritV5Builder.Build first.
// Run: Unity -batchmode -projectPath <p> -executeMethod StageSiteCapture.RunL6SpiritV5 -quit   (needs graphics: no -nographics)
public static partial class StageSiteCapture
{
    public static void RunL6SpiritV5()
    {
        bool gate = SonMonTiers.Enabled;
        var reducedOverride = SonMonUpgradeFx.SessionOverride;
        var modeOverride = SonMonSpiritGatheringV5.ModeOverride;
        try
        {
            SonMonTiers.Enabled = true;
            spiritTarget = L6V2;
            customShots = SpiritShotsV5;
            Capture(new[] { L5, L6V2 }, "Reports/ForgeGUI/scripture_L06/spirit_v5/unity");
        }
        finally
        {
            SonMonTiers.Enabled = gate;
            SonMonUpgradeFx.SessionOverride = reducedOverride;
            SonMonSpiritGatheringV5.ModeOverride = modeOverride;
        }
    }

    static void SpiritShotsV5(List<GameObject> built, List<(string name, Vector3 from, Vector3 at, float fov, bool ortho, float size)> views, List<string> report)
    {
        foreach (var root in built) root.SetActive(root.name == spiritTarget.label);
        var l6 = built.Find(r => r.name == spiritTarget.label);
        var model = l6.transform.GetChild(0).gameObject;
        var b = Bounds(model);
        var fxGo = new GameObject("SpiritGatheringV5");
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
        // identical cameras to SpiritShotsV4
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

        var sg = fxGo.AddComponent<SonMonSpiritGatheringV5>();
        sg.Configure(pillars, orn, V(a.hallMin, 0), V(a.hallMax, 0), t1, t2, a.tier1[4] * k, a.tier2[4] * k, allowedLocal);
        void Run(float seconds, Vector3 camPos) { for (float t = 0; t < seconds; t += dt) sg.Step(dt, camPos); }
        void RunTo(float phase, Vector3 camPos) { float wait = (phase - sg.Phase + SonMonSpiritGatheringV5.Cycle) % SonMonSpiritGatheringV5.Cycle; Run(wait < 2.5f ? wait + 10f : wait, camPos); }
        foreach (var mode in new[] { SonMonSpiritGathering.Mode.Off, SonMonSpiritGathering.Mode.Full, SonMonSpiritGathering.Mode.Reduced })
        {
            SonMonSpiritGatheringV5.ModeOverride = mode;
            SonMonUpgradeFx.SessionOverride = mode == SonMonSpiritGathering.Mode.Reduced;
            bool builtOk = sg.Build(false);
            Run(4f, near);
            string tag = mode.ToString().ToLower();
            foreach (var v in cams)
            {
                if (mode != SonMonSpiritGathering.Mode.Full && v.name.StartsWith("close_") && v.name != "close_formation") continue;
                RunTo(7f, v.from);  // includes settling at this camera distance (LOD)
                if (v.name.StartsWith("gameplay"))
                    report.Add($"v5,{tag},{v.name},lod,{sg.ActiveLod},particles,{sg.ParticleCount()},activeRenderers,{ActiveRenderers(fxGo)},particleOverdraw,{ParticleOverdraw(fxGo, v.from, v.at, v.fov)}");
                Capture($"v5_{tag}_{v.name}", v.from, v.at, v.fov, v.ortho, v.size);
            }
            report.Add($"v5,{tag},built,{builtOk},colliders,{fxGo.GetComponentsInChildren<Collider>(true).Length},layers,{string.Join("/", LayersOf(fxGo))}");
            if (mode != SonMonSpiritGathering.Mode.Full) continue;

            // ambient vs release peak at every distance (the release must read at gameplay zoom too)
            foreach (var name in new[] { "hero", "gameplay_zoom10", "gameplay_zoom21_default", "gameplay_zoom30" })
            {
                var zv = cams.Find(v => v.name == name);
                RunTo(1.5f, zv.from);
                Capture($"v5_full_ambient_{name}", zv.from, zv.at, zv.fov, false, 0);
                RunTo(8.6f, zv.from);
                report.Add($"v5,release,{name},lod,{sg.ActiveLod},particles,{sg.ParticleCount()},particleOverdraw,{ParticleOverdraw(fxGo, zv.from, zv.at, zv.fov)}");
                Capture($"v5_full_release_{name}", zv.from, zv.at, zv.fov, false, 0);
            }
            // one complete 10 s loop at 10 fps from the hero camera (frame 100 = frame 0 + 10 s, to check the seam)
            var hero = cams.Find(v => v.name == "hero");
            var z21 = cams.Find(v => v.name == "gameplay_zoom21_default");
            RunTo(0f, hero.from);
            Directory.CreateDirectory(Path.Combine(Out, "cycle"));
            for (int f = 0; f <= 100; f++)
            {
                Capture($"cycle/frame_{f:000}", hero.from, hero.at, hero.fov, false, 0, 960, 540);
                Run(.1f, hero.from);
            }
            report.Add($"v5,cycle,heroFrames,101,fps,10,loopSeconds,{SonMonSpiritGatheringV5.Cycle}");
            // footprint and clipping over a full cycle (sampled every 0.5 s), as v4
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
                    bool skipClip = ps.name == "PS_PillarGlowV5" || ps.name == "PS_OrnamentHaloV5" || ps.name == "PS_ReleaseWaveV5" || ps.name == "PS_ReleaseGlowV5";
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
                        bool inHall = p.x > a.hallMin[0] && p.x < a.hallMax[0] && p.z > a.ornament[2] + .05f && p.z < a.hallMax[2] && p.y < a.hallMax[1];
                        if (!skipClip && (in1 || in2 || inHall)) { insideTerrace++; e.inside++; }
                        perSystem[ps.name] = e;
                    }
                }
            }
            bool inside = all.min.x >= StageIILotWorld.x - .001f && all.max.x <= StageIILotWorld.y + .001f && all.min.z >= StageIILotWorld.z - .001f && all.max.z <= StageIILotWorld.w + .001f;
            report.Add($"v5,footprint,insideStageIILot,{inside},vfxBoundsX,{all.min.x:0.00},{all.max.x:0.00},Z,{all.min.z:0.00},{all.max.z:0.00},Y,{all.min.y:0.00},{all.max.y:0.00},lotX,-13,-4,Z,4,11");
            report.Add($"v5,clipping,particleSamplesInsideTerraceOrHall,{insideTerrace},of,{total},maxParticlesNear,{maxParticles}");
            foreach (var kv in perSystem)
                report.Add($"v5,system,{kv.Key},X,{kv.Value.b.min.x:0.00},{kv.Value.b.max.x:0.00},Y,{kv.Value.b.min.y:0.00},{kv.Value.b.max.y:0.00},Z,{kv.Value.b.min.z:0.00},{kv.Value.b.max.z:0.00},insideSolids,{kv.Value.inside},of,{kv.Value.total}");
            foreach (var r in fxGo.GetComponentsInChildren<MeshRenderer>())
                report.Add($"v5,strip,{r.name},X,{r.bounds.min.x:0.00},{r.bounds.max.x:0.00},Z,{r.bounds.min.z:0.00},{r.bounds.max.z:0.00}");
            // allocations: runtime path (edit-mode manual particle simulation switched off for the measurement)
            SonMonSpiritGathering.SimulateInEditMode = false;
            foreach (var cv in new[] { near, z21.from, cams.Find(v => v.name == "gameplay_zoom30").from })
            {
                Run(1f, cv);
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 600; i++) sg.Step(dt, cv);  // 20 s: two full cycles including two releases
                report.Add($"v5,gc,bytesPer600Steps,lod,{sg.ActiveLod},{GC.GetAllocatedBytesForCurrentThread() - before}");
            }
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 600; i++) sg.Step(dt, near);
            sw.Stop();
            report.Add($"v5,cpu,msPerStepEditorScriptOnly,{sw.Elapsed.TotalMilliseconds / 600:0.0000}");
            SonMonSpiritGathering.SimulateInEditMode = true;
        }
        // no awakening on load: Build() without the flag and without a playing upgrade
        SonMonSpiritGatheringV5.ModeOverride = SonMonSpiritGathering.Mode.Full;
        SonMonUpgradeFx.SessionOverride = false;
        sg.Build();
        report.Add($"v5,awakenOnPlainBuild,{sg.Awakening}");
        var mats = new HashSet<Material>();
        foreach (var r in fxGo.GetComponentsInChildren<Renderer>(true)) if (r.sharedMaterial != null) mats.Add(r.sharedMaterial);
        report.Add($"v5,memory,materials,{mats.Count},shaders,{string.Join("/", mats.Select(m => m.shader.name).Distinct())}");
    }
}
