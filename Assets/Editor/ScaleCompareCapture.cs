using System.Collections.Generic;
using System.IO;
using LangTroTan.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Isolated world-scale comparison for the Scripture Pavilion (visual QA only). Builds a temporary, unsaved scene with
// the real L1 and L5 models on 3x3 server lots, the existing disciple (unchanged, 1.41 units) in front of each
// entrance, a cell grid and footprint outlines, then renders front, 45-degree and gameplay-camera views for the
// scale options A (current fit), B (door 0.62) and C (door 0.52). No project scene, gameplay, server or save is used.
// Run: Unity -batchmode -projectPath <p> -executeMethod ScaleCompareCapture.Run -quit   (needs graphics: no -nographics)
public static class ScaleCompareCapture
{
    const string Prefabs = "Assets/Art/SonMon/Prefabs";
    const string Out = "Reports/ForgeGUI/scale/unity";

    struct Option { public string id, label; public float l1Height, l5Height, door; }

    static readonly Option[] Options =
    {
        new Option { id = "A", label = "A current fit", l1Height = 2.84f, l5Height = 4.20f, door = 0 },
        new Option { id = "B", label = "B door 0.62", l1Height = 2.05f, l5Height = 4.33f, door = .62f },
        new Option { id = "C", label = "C door 0.52", l1Height = 1.72f, l5Height = 3.63f, door = .52f },
    };

    public static void Run()
    {
        Directory.CreateDirectory(Out);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);  // temporary, never saved
        var kit = AssetDatabase.LoadAssetAtPath<SonMonKit>("Assets/Resources/SonMon/SonMonKit.asset");
        var l1 = AssetDatabase.LoadAssetAtPath<GameObject>($"{Prefabs}/Tier_scripture_L01_forgegui.prefab");
        var l5 = AssetDatabase.LoadAssetAtPath<GameObject>($"{Prefabs}/Tier_scripture_forgegui.prefab");
        var disciple = AssetDatabase.LoadAssetAtPath<GameObject>($"{Prefabs}/Character_disciple.prefab");
        var meadow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SonMon/Materials/SonMon Meadow.mat");
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        Material Flat(Color c) { var m = new Material(shader); m.SetColor("_BaseColor", c); return m; }
        var gridMat = Flat(new Color(.18f, .22f, .2f));
        var lotMat = Flat(new Color(1f, .78f, .25f));

        // Lighting close to the game's day look.
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
        ground.transform.localScale = new Vector3(8, 1, 4);
        if (meadow != null) ground.GetComponent<Renderer>().sharedMaterial = meadow;

        var report = new List<string> { "option,level,worldSize(w x d x h),door,disciple/door,disciple/eave-height" };
        const float lotGap = 4.5f, groupGap = 11f;
        for (int o = 0; o < Options.Length; o++)
        {
            var opt = Options[o];
            float gx = (o - 1) * groupGap;
            for (int k = 0; k < 2; k++)
            {
                bool isL5 = k == 1;
                var center = new Vector3(gx + (isL5 ? lotGap / 2 : -lotGap / 2), 0, 0);
                Lot(center, gridMat, lotMat);
                var building = (GameObject)PrefabUtility.InstantiatePrefab(isL5 ? l5 : l1);
                building.transform.position = center;
                float target = isL5 ? opt.l5Height : opt.l1Height;
                float h = Bounds(building).size.y;
                building.transform.localScale = Vector3.one * (target / h);
                var b = Bounds(building);
                // Door heights: source door × the scale that maps source height to the target height.
                float doorSource = isL5 ? .272f : .449f, sourceHeight = isL5 ? 1.901f : 1.487f;
                float door = doorSource * target / sourceHeight;
                var person = (GameObject)PrefabUtility.InstantiatePrefab(disciple);
                person.transform.position = new Vector3(center.x, 0, b.min.z - .35f);  // just in front of the steps
                person.transform.rotation = Quaternion.identity;                        // faces the front camera
                report.Add($"{opt.id},L{(isL5 ? 5 : 1)},{b.size.x:0.00} x {b.size.z:0.00} x {b.size.y:0.00},{door:0.00},{1.41f / door:0.00},{1.41f / b.size.y:0.00}");
            }
            var focus = new Vector3(gx, 1.6f, 0);
            if (o == 0) { Capture("warmup", focus + new Vector3(0, .2f, -14f), focus, 34); File.Delete(Path.Combine(Out, "warmup.png")); }  // first batch frame compiles shaders
            Capture($"{opt.id}_front", focus + new Vector3(0, .2f, -14f), focus, 34);
            Capture($"{opt.id}_45", focus + new Vector3(9f, 7.5f, -11f), focus, 34);
            // Real gameplay camera: perspective FOV 42, offset (31,29,-38).normalized * zoom * 2.7 (closest zoom 10).
            var gfocus = new Vector3(gx, 0, 0);
            Capture($"{opt.id}_gameplay_zoom10", gfocus + new Vector3(31, 29, -38).normalized * (10 * 2.7f), gfocus, 42);
        }
        var all = Vector3.zero;
        Capture("ALL_gameplay_zoom21_default", all + new Vector3(31, 29, -38).normalized * (21 * 2.7f), all, 42);
        File.WriteAllLines(Path.Combine(Out, "measurements.csv"), report);
        Debug.Log("SCALE_COMPARE_DONE " + string.Join(" | ", report));
    }

    // Character display-height comparison at the provisional architectural door 0.52 (option C). The same scene,
    // lights and cameras are reused for every height; only the disciple's visual child is scaled. Each root keeps its
    // transform (position, rotation, scale 1), i.e. its authoritative world position.
    // Run: Unity -batchmode -projectPath <p> -executeMethod ScaleCompareCapture.RunCharacterScale -quit
    public static void RunCharacterScale()
    {
        const string outDir = "Reports/ForgeGUI/scale/characters";
        Directory.CreateDirectory(outDir);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);  // temporary, never saved
        var kit = AssetDatabase.LoadAssetAtPath<SonMonKit>("Assets/Resources/SonMon/SonMonKit.asset");
        var disciple = AssetDatabase.LoadAssetAtPath<GameObject>($"{Prefabs}/Character_disciple.prefab");
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        Material Flat(Color c) { var m = new Material(shader); m.SetColor("_BaseColor", c); return m; }
        var gridMat = Flat(new Color(.18f, .22f, .2f));
        var lotMat = Flat(new Color(1f, .78f, .25f));
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
        ground.transform.localScale = new Vector3(8, 1, 6);
        var meadow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SonMon/Materials/SonMon Meadow.mat");
        if (meadow != null) ground.GetComponent<Renderer>().sharedMaterial = meadow;

        // Buildings at the shared door 0.52 (L1 1.72 tall, L5 3.63 tall), on 3x3 lots.
        var people = new List<GameObject>();
        float[] heights = { 1.72f, 3.63f };
        string[] prefabs = { "Tier_scripture_L01_forgegui", "Tier_scripture_forgegui" };
        for (int k = 0; k < 2; k++)
        {
            var center = new Vector3(k == 0 ? -2.25f : 2.25f, 0, 0);
            Lot(center, gridMat, lotMat);
            var building = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{Prefabs}/{prefabs[k]}.prefab"));
            building.transform.position = center;
            building.transform.localScale = Vector3.one * (heights[k] / Bounds(building).size.y);
            var b = Bounds(building);
            people.Add(Person(disciple, new Vector3(center.x, 0, b.min.z - .35f)));  // directly in front of the entrance
        }
        // A group of three standing together (crowd readability), between the lots in front.
        for (int i = -1; i <= 1; i++) people.Add(Person(disciple, new Vector3(i * .55f, 0, -2.6f)));
        foreach (var p in people)
            if (p.transform.childCount != 1)
                throw new System.InvalidOperationException("disciple hierarchy changed: expected exactly one visual child under the root");

        var report = new List<string> { "displayHeight,visualScale,rootScale,doorRatio,pxHeight_zoom10,pxHeight_zoom21_default,pxHeight_zoom30,renderedMeshHeight,rootPositionX/Z" };
        var focus = new Vector3(0, 1.4f, -.6f);
        var gfocus = new Vector3(0, 0, -.6f);
        Vector3 Game(float zoom) => gfocus + new Vector3(31, 29, -38).normalized * (zoom * 2.7f);
        CaptureTo(outDir, "warmup", focus + new Vector3(0, .2f, -12.5f), focus, 34);
        File.Delete(Path.Combine(outDir, "warmup.png"));
        foreach (float display in new[] { 1.41f, .75f, .85f, .95f })
        {
            float s = display / 1.41f;
            foreach (var p in people) p.transform.GetChild(0).localScale = Vector3.one * s;  // visual only
            string id = display.ToString("0.00").Replace('.', '_');
            CaptureTo(outDir, $"h{id}_front", focus + new Vector3(0, .2f, -12.5f), focus, 34);
            CaptureTo(outDir, $"h{id}_45", focus + new Vector3(8.5f, 7f, -10.5f), focus, 34);
            CaptureTo(outDir, $"h{id}_gameplay_zoom10", Game(10), gfocus, 42);
            CaptureTo(outDir, $"h{id}_gameplay_zoom21_default", Game(21), gfocus, 42);
            CaptureTo(outDir, $"h{id}_gameplay_zoom30", Game(30), gfocus, 42);
            // Projected on-screen height (1080p) of the crowd's centre disciple at each gameplay zoom.
            var person = people[3];
            float Px(float zoom)
            {
                var cam = new GameObject("measure").AddComponent<Camera>();
                cam.fieldOfView = 42; cam.aspect = 16f / 9f;
                cam.transform.position = Game(zoom); cam.transform.LookAt(gfocus);
                var feet = person.transform.position;
                var head = feet + Vector3.up * display;
                float px = Mathf.Abs(cam.WorldToViewportPoint(head).y - cam.WorldToViewportPoint(feet).y) * 1080;
                Object.DestroyImmediate(cam.gameObject);
                return px;
            }
            float meshHeight = Bounds(person).size.y;  // actual rendered height, verifies the visual scale took effect
            report.Add($"{display:0.00},{s:0.000},{person.transform.localScale.x:0.0},{display / .52f:0.00},{Px(10):0},{Px(21):0},{Px(30):0},{meshHeight:0.000},{person.transform.position.x:0.00}/{person.transform.position.z:0.00}");
        }
        foreach (var p in people) p.transform.GetChild(0).localScale = Vector3.one;
        File.WriteAllLines(Path.Combine(outDir, "character_heights.csv"), report);
        Debug.Log("CHARACTER_SCALE_DONE " + string.Join(" | ", report));
    }

    static GameObject Person(GameObject prefab, Vector3 at)
    {
        var person = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        person.transform.position = at;
        person.transform.rotation = Quaternion.identity;  // faces the front camera
        return person;
    }

    static void CaptureTo(string dir, string name, Vector3 from, Vector3 at, float fov)
    {
        Capture(name, from, at, fov);
        var src = Path.Combine(Out, name + ".png");
        var dst = Path.Combine(dir, name + ".png");
        if (File.Exists(dst)) File.Delete(dst);
        File.Move(src, dst);
    }

    static void Lot(Vector3 c, Material grid, Material outline)
    {
        for (int i = 0; i <= 3; i++)
        {
            bool edge = i == 0 || i == 3;
            Bar(new Vector3(c.x - 1.5f + i, .012f, c.z), new Vector3(edge ? .06f : .025f, .01f, 3f + (edge ? .06f : 0)), edge ? outline : grid);
            Bar(new Vector3(c.x, .012f, c.z - 1.5f + i), new Vector3(3f + (edge ? .06f : 0), .01f, edge ? .06f : .025f), edge ? outline : grid);
        }
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
        var rs = go.GetComponentsInChildren<Renderer>();
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static void Capture(string name, Vector3 from, Vector3 at, float fov)
    {
        var go = new GameObject("Capture " + name);
        var cam = go.AddComponent<Camera>();
        cam.fieldOfView = fov;
        cam.nearClipPlane = .1f;
        cam.farClipPlane = 400;
        cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(.65f, .77f, .75f);
        cam.allowHDR = true;
        go.transform.position = from;
        go.transform.LookAt(at);
        var rt = new RenderTexture(1920, 1080, 24) { antiAliasing = 4 };
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(Out, name + ".png"), tex.EncodeToPNG());
        RenderTexture.active = null;
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(go);
    }
}
