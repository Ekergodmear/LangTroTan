using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LangTroTan.Presentation;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

// Isolated QA player benchmark for the Tàng Kinh Các Level 6 spirit effects (QA scene only, never in production).
// For each target frame rate (30, 60, uncapped), effects mode (Off, Reduced, Full) and gameplay zoom (10, 21), it
// rebuilds the effects, warms up, then samples frame pacing, CPU/GPU frame time (FrameTimingManager), draw calls /
// batches / set-pass calls and texture memory (ProfilerRecorder), GC collections and memory, and writes a CSV.
// Optionally records short fixed-rate motion captures (Time.captureFramerate) at 30 and 60 fps. Quits when done.
// Args: -qaout <csv path>  -fxver 3|4 (default 4)  -qacapture <dir> (motion captures, full effects, zoom 10 and hero)
public sealed class L6SpiritBenchmark : MonoBehaviour
{
    public SonMonSpiritGathering effects;
    public SonMonSpiritGatheringV4 effectsV4;
    public Transform focus;
    public float warmup = 2f, sample = 8f;

    readonly FrameTiming[] timing = new FrameTiming[1];

    bool V4 => Arg("-fxver") != "3" && effectsV4 != null;

    void SetMode(SonMonSpiritGathering.Mode mode)
    {
        SonMonUpgradeFx.SessionOverride = mode == SonMonSpiritGathering.Mode.Reduced;
        if (V4)
        {
            SonMonSpiritGatheringV4.ModeOverride = mode;
            if (effects != null) { effects.Clear(); effects.enabled = false; }
            effectsV4.enabled = true;
            effectsV4.Build(false);
        }
        else
        {
            SonMonSpiritGathering.ModeOverride = mode;
            if (effectsV4 != null) { effectsV4.Clear(); effectsV4.enabled = false; }
            effects.enabled = true;
            effects.Build(false);
        }
    }

    int Particles() => V4 ? effectsV4.ParticleCount() : effects.ParticleCount();

    IEnumerator Start()
    {
        string outPath = Arg("-qaout") ?? Path.Combine(Application.persistentDataPath, "l6_spirit_benchmark.csv");
        string capDir = Arg("-qacapture");
        Application.runInBackground = true;  // keep sampling when the QA window loses focus
        var cam = Camera.main;
        var sb = new StringBuilder("version,fps_target,mode,zoom,frames,mean_ms,p50_ms,p95_ms,p99_ms,max_ms,stdev_ms,frames_over_1_5x,cpu_ms_mean,gpu_ms_mean,draw_calls,batches,setpass,gc_collections,gc_alloc_bytes_per_frame,mono_used_mb,total_alloc_mb,gfx_driver_mb,texture_mb,particles\n");
        string ver = V4 ? "v4" : "v3";
        QualitySettings.vSyncCount = 0;
        using var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        using var batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
        using var setpass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        using var texMem = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Texture Memory");
        using var gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");

        if (capDir != null)
        {
            // fixed-step motion captures: game time advances exactly 1/fps per rendered frame
            Directory.CreateDirectory(capDir);
            SetMode(SonMonSpiritGathering.Mode.Full);
            foreach (var shot in new[] { ("zoom10", focus.position + new Vector3(31, 29, -38).normalized * 27f, focus.position),
                                         ("hero", focus.position + new Vector3(5.6f, 4.4f, -7.4f), focus.position + new Vector3(0, 1.7f, -.4f)) })
                foreach (int fps in new[] { 30, 60 })
                {
                    cam.transform.position = shot.Item2;
                    cam.transform.LookAt(shot.Item3);
                    Time.captureFramerate = 0;
                    float w = 0;
                    while (w < 3f) { w += Time.unscaledDeltaTime; yield return null; }
                    Time.captureFramerate = fps;
                    string dir = Path.Combine(capDir, $"{ver}_{shot.Item1}_{fps}fps");
                    Directory.CreateDirectory(dir);
                    for (int f = 0; f < fps * 3; f++)  // 3 s of motion
                    {
                        yield return new WaitForEndOfFrame();
                        ScreenCapture.CaptureScreenshot(Path.Combine(dir, $"frame_{f:000}.png"));
                    }
                    Time.captureFramerate = 0;
                }
        }

        foreach (int fps in new[] { 30, 60, -1 })
            foreach (var mode in new[] { SonMonSpiritGathering.Mode.Off, SonMonSpiritGathering.Mode.Reduced, SonMonSpiritGathering.Mode.Full })
                foreach (int zoom in new[] { 10, 21 })
                {
                    Application.targetFrameRate = fps;
                    SetMode(mode);
                    cam.transform.position = focus.position + new Vector3(31, 29, -38).normalized * (zoom * 2.7f);
                    cam.transform.LookAt(focus.position);
                    float t = 0;
                    while (t < warmup) { t += Time.unscaledDeltaTime; yield return null; }
                    var dts = new List<float>(4096);
                    double cpu = 0, gpu = 0, dc = 0, bt = 0, sp = 0, alloc = 0;
                    int timed = 0, counted = 0, gc0 = GC.CollectionCount(0), particles = 0;
                    t = 0;
                    while (t < sample)
                    {
                        yield return null;
                        float dt = Time.unscaledDeltaTime;
                        t += dt;
                        dts.Add(dt * 1000f);
                        FrameTimingManager.CaptureFrameTimings();
                        if (FrameTimingManager.GetLatestTimings(1, timing) > 0 && timing[0].gpuFrameTime > 0)
                        {
                            cpu += timing[0].cpuFrameTime;
                            gpu += timing[0].gpuFrameTime;
                            timed++;
                        }
                        dc += draws.LastValue; bt += batches.LastValue; sp += setpass.LastValue; alloc += gcAlloc.LastValue; counted++;
                        particles = Math.Max(particles, Particles());
                    }
                    dts.Sort();
                    float mean = 0; foreach (var d in dts) mean += d; mean /= dts.Count;
                    float var = 0; foreach (var d in dts) var += (d - mean) * (d - mean); var /= dts.Count;
                    int over = 0; float budget = fps > 0 ? 1500f / fps : mean * 1.5f;
                    foreach (var d in dts) if (d > budget) over++;
                    float P(float q) => dts[Mathf.Clamp(Mathf.RoundToInt(q * (dts.Count - 1)), 0, dts.Count - 1)];
                    sb.Append($"{ver},{(fps > 0 ? fps.ToString() : "uncapped")},{mode},{zoom},{dts.Count},{mean:0.00},{P(.5f):0.00},{P(.95f):0.00},{P(.99f):0.00},{dts[dts.Count - 1]:0.00},{Mathf.Sqrt(var):0.00},{over}," +
                              $"{(timed > 0 ? cpu / timed : -1):0.00},{(timed > 0 ? gpu / timed : -1):0.00},{dc / counted:0.0},{bt / counted:0.0},{sp / counted:0.0}," +
                              $"{GC.CollectionCount(0) - gc0},{alloc / counted:0},{Profiler.GetMonoUsedSizeLong() / 1048576.0:0.0},{Profiler.GetTotalAllocatedMemoryLong() / 1048576.0:0.0},{Profiler.GetAllocatedMemoryForGraphicsDriver() / 1048576.0:0.0},{texMem.LastValue / 1048576.0:0.0},{particles}\n");
                    File.WriteAllText(outPath, sb.ToString());
                }
        File.WriteAllText(outPath, sb.ToString() + $"# {SystemInfo.graphicsDeviceName} | {SystemInfo.processorType} | {Screen.width}x{Screen.height} | {SystemInfo.graphicsDeviceType}\n");
        Application.Quit();
    }

    static string Arg(string name)
    {
        var a = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(a, name);
        return i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
    }
}
