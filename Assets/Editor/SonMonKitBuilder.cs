using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LangTroTan.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Turns the Blender architecture kit (Assets/Art/SonMon/Models) and the Quaternius CC0 nature pack into
// URP materials, prefabs, an authored environment prefab and the runtime registry
// Resources/SonMon/SonMonKit.asset. Safe to re-run: every generated asset is rebuilt in place.
public static class SonMonKitBuilder
{
    const string Root = "Assets/Art/SonMon";
    const string ModelDir = Root + "/Models";
    const string MaterialDir = Root + "/Materials";
    const string PrefabDir = Root + "/Prefabs";
    const string TextureDir = Root + "/Textures";
    const string Nature = "Assets/ThirdParty/Quaternius/StylizedNatureMegaKit";
    const string PolyHaven = "Assets/ThirdParty/PolyHaven";
    const string RegistryPath = "Assets/Resources/SonMon/SonMonKit.asset";

    // Building footprint scale: the Blender kit is authored at 1 unit = 1 server cell already.
    // Quaternius trees are ~7-9 m tall; scale them so a pine reads slightly taller than a dwelling.
    const float TreeScale = .5f;

    [MenuItem("Tu Tien/Visual Slice/Build Son Mon kit")]
    public static void BuildAll()
    {
        foreach (var dir in new[] { MaterialDir, PrefabDir, TextureDir, "Assets/Resources/SonMon" })
            Directory.CreateDirectory(dir);
        AssetDatabase.Refresh();

        ConfigureTextures();
        var mats = BuildMaterials();
        var natureMats = BuildNatureMaterials();
        var kit = LoadOrCreate<SonMonKit>(RegistryPath);

        kit.buildings = BuildBuildingPrefabs(mats);
        kit.resourceTrees = new[] { "Pine_1", "Pine_2", "Pine_3", "Pine_4", "Pine_5", "Pine_2", "CommonTree_3", "Pine_1", "CommonTree_4" }
            .Select(name => NaturePrefab(name, natureMats, TreeScale)).ToArray();
        kit.resourceRocks = new[] { "Rock_Medium_1", "Rock_Medium_2", "Rock_Medium_3" }.Select(name => NaturePrefab(name, natureMats, .45f)).ToArray();
        kit.daySky = Skybox("SonMon Sky Day", PolyHaven + "/HDRI/kloofendal_48d_partly_cloudy_puresky_2k.hdr", 1.05f);
        kit.nightSky = Skybox("SonMon Sky Night", PolyHaven + "/HDRI/qwantani_night_puresky_2k.hdr", .55f);
        kit.cloudMaterial = mats["Cloud"];
        kit.cloudDayColor = mats["Cloud"].GetColor("_BaseColor");
        kit.dawnSky = Skybox("SonMon Sky Dawn", PolyHaven + "/HDRI/qwantani_sunrise_puresky_2k.hdr", .9f);
        kit.glowMaterials = new[] { "Paper", "Glow" }.Select(slot => new SonMonKit.GlowEntry
            { material = mats[slot], baseEmission = mats[slot].GetColor("_EmissionColor") }).ToArray();
        BuildCharacters(kit, mats);
        kit.visualTiers = BuildVisualTiers(mats);
        kit.environment = BuildEnvironment(mats, natureMats);
        EditorUtility.SetDirty(kit);
        AssetDatabase.SaveAssets();
        Debug.Log($"SONMON_KIT buildings={kit.buildings.Length} trees={kit.resourceTrees.Length} environment={kit.environment != null}");
    }

    // ------------------------------------------------------------------ textures and materials

    static void ConfigureTextures()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { PolyHaven + "/Textures", Nature + "/Textures" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
            bool normal = path.Contains("_nor_gl") || path.EndsWith("_Normal.png");
            bool changed = false;
            if (normal && importer.textureType != TextureImporterType.NormalMap) { importer.textureType = TextureImporterType.NormalMap; changed = true; }
            if (path.Contains("_arm_") && importer.sRGBTexture) { importer.sRGBTexture = false; changed = true; }
            if (importer.maxTextureSize > 1024) { importer.maxTextureSize = 1024; changed = true; }
            if (path.StartsWith(Nature) && !normal && !importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
            if (changed) importer.SaveAndReimport();
        }
        foreach (var guid in AssetDatabase.FindAssets("t:Texture", new[] { PolyHaven + "/HDRI" }))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            if (importer.mipmapEnabled || importer.wrapModeU != TextureWrapMode.Repeat)
            {
                importer.mipmapEnabled = false;
                importer.wrapModeU = TextureWrapMode.Repeat;
                importer.wrapModeV = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
        }
    }

    static Texture2D Tex(string path) => AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    static Texture2D Ph(string id, string map) => Tex($"{PolyHaven}/Textures/{id}/{id}_{map}_1k.jpg");

    static Material Lit(string name, Color color, Texture2D albedo = null, Texture2D normal = null, float tiling = 1,
        float smoothness = .25f, float metallic = 0, Color? emission = null, bool cutout = false, bool doubleSided = false)
    {
        var path = $"{MaterialDir}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = Shader.Find("Universal Render Pipeline/Lit");
        mat.SetColor("_BaseColor", color);
        mat.SetTexture("_BaseMap", albedo);
        mat.SetTextureScale("_BaseMap", Vector2.one * tiling);
        mat.SetTexture("_BumpMap", normal);
        mat.SetFloat("_BumpScale", .8f);
        if (normal != null) mat.EnableKeyword("_NORMALMAP"); else mat.DisableKeyword("_NORMALMAP");
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", metallic);
        if (emission.HasValue)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission.Value);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else mat.DisableKeyword("_EMISSION");
        mat.SetFloat("_AlphaClip", cutout ? 1 : 0);
        mat.SetFloat("_Cutoff", .45f);
        if (cutout) mat.EnableKeyword("_ALPHATEST_ON"); else mat.DisableKeyword("_ALPHATEST_ON");
        mat.SetFloat("_Cull", doubleSided ? (float)CullMode.Off : (float)CullMode.Back);
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Dictionary<string, Material> BuildMaterials()
    {
        var roofTex = Ph("grey_roof_tiles_02", "diff");
        var roofNor = Ph("grey_roof_tiles_02", "nor_gl");
        var cedar = Ph("japanese_cedar_planks", "diff");
        var cedarNor = Ph("japanese_cedar_planks", "nor_gl");
        var paving = Ph("large_grey_tiles", "diff");
        var pavingNor = Ph("large_grey_tiles", "nor_gl");
        return new Dictionary<string, Material>
        {
            ["Roof"] = Lit("SonMon Roof Jade", new Color(.36f, .66f, .64f), roofTex, roofNor, 1.5f, .45f),
            ["RoofTrim"] = Lit("SonMon Roof Jade Trim", new Color(.13f, .34f, .34f), null, null, 1, .55f),
            ["RoofGrey"] = Lit("SonMon Roof Grey", new Color(.62f, .64f, .66f), roofTex, roofNor, 1.5f, .35f),
            ["TrimGrey"] = Lit("SonMon Roof Grey Trim", new Color(.27f, .29f, .31f), null, null, 1, .4f),
            ["Lacquer"] = Lit("SonMon Red Lacquer", new Color(.72f, .16f, .11f), cedar, cedarNor, 1, .55f),
            ["Wood"] = Lit("SonMon Dark Wood", new Color(.45f, .3f, .2f), cedar, cedarNor, 1, .3f),
            ["Plaster"] = Lit("SonMon Ivory Plaster", new Color(.96f, .92f, .82f), null, null, 1, .15f),  // flat painted ivory reads cleaner than the scanned plaster
            ["Stone"] = Lit("SonMon Plinth Stone", new Color(.86f, .86f, .84f), Ph("mossy_stone_wall", "diff"), Ph("mossy_stone_wall", "nor_gl"), 1, .2f),
            ["Marble"] = Lit("SonMon White Marble", new Color(.95f, .94f, .9f), paving, pavingNor, 1, .35f),
            ["Gold"] = Lit("SonMon Gilded Bronze", new Color(.93f, .7f, .32f), null, null, 1, .62f, .85f),
            ["Paper"] = Lit("SonMon Lantern Paper", new Color(.93f, .32f, .16f), null, null, 1, .2f, 0, new Color(.9f, .25f, .08f) * 1.2f),
            ["Glow"] = Lit("SonMon Spirit Glow", new Color(.45f, .9f, .85f), null, null, 1, .8f, 0, new Color(.2f, .85f, .8f) * 1.3f),
            ["Soil"] = Lit("SonMon Soil", new Color(.6f, .48f, .38f), Ph("forest_ground_04", "diff"), Ph("forest_ground_04", "nor_gl"), 1, .1f),
            ["Leaf"] = Lit("SonMon Crop Leaf", new Color(.42f, .66f, .3f), null, null, 1, .2f),
            ["Bamboo"] = Lit("SonMon Bamboo", new Color(.62f, .72f, .36f), null, null, 1, .45f),
            ["Ground"] = Lit("SonMon Meadow", Color.white, MeadowTexture(), null, 1, .05f),
            ["Courtyard"] = Lit("SonMon Courtyard Paving", new Color(.9f, .9f, .87f), paving, pavingNor, 1, .3f),
            ["Cliff"] = Lit("SonMon Cliff", new Color(.62f, .72f, .74f), Ph("mossy_rock", "diff"), Ph("mossy_rock", "nor_gl"), 1, .15f),
            ["Peak"] = Lit("SonMon Distant Peak", new Color(.47f, .6f, .62f), null, null, 1, .05f),
            ["Water"] = Water(),
            ["Cloud"] = Cloud(),
            ["Formation"] = Additive("SonMon Formation", "Universal Render Pipeline/Unlit", RuneTexture(), new Color(.35f, 1.6f, 1.45f, .9f)),
            ["Mote"] = Additive("SonMon Spirit Mote", "Universal Render Pipeline/Particles/Unlit", MoteTexture(), new Color(.7f, 1.3f, 1.15f, 1f)),
            ["Mist"] = Additive("SonMon Waterfall Mist", "Universal Render Pipeline/Particles/Unlit", MoteTexture(), new Color(.75f, .85f, .9f, .35f), false),
        };
    }

    static Material Water()
    {
        var mat = Lit("SonMon Waterfall", new Color(.78f, .95f, 1f, .9f), WaterTexture(), null, 1, .92f, 0, new Color(.22f, .42f, .48f));
        MakeTransparent(mat);
        return mat;
    }

    static Material Cloud()
    {
        var path = $"{MaterialDir}/SonMon Cloud Sea.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(mat, path); }
        mat.shader = Shader.Find("Universal Render Pipeline/Unlit");
        mat.SetTexture("_BaseMap", CloudTexture());
        mat.SetColor("_BaseColor", new Color(.88f, .93f, .98f, .62f));
        MakeTransparent(mat);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void MakeTransparent(Material mat)
    {
        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_Blend", 0);
        mat.SetFloat("_ZWrite", 0);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.SetOverrideTag("RenderType", "Transparent");
    }

    // Soft tileable cloud alpha, generated once (fBm Perlin, wrapped by sampling a torus).
    static Texture2D CloudTexture()
    {
        var path = TextureDir + "/SonMonCloudNoise.png";
        var existing = Tex(path);
        if (existing != null) return existing;
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = x / (float)size * Mathf.PI * 2, v = y / (float)size * Mathf.PI * 2, value = 0, amp = .55f, freq = 1;
            for (int o = 0; o < 5; o++)
            {
                value += amp * Mathf.PerlinNoise(Mathf.Cos(u) * freq + 13 + o * 7, Mathf.Sin(u) * freq + Mathf.Cos(v) * freq * .7f + Mathf.Sin(v) * freq * .7f);
                amp *= .5f; freq *= 2.1f;
            }
            float a = Mathf.SmoothStep(.45f, .72f, value);
            tex.SetPixel(x, y, new Color(1, 1, 1, a));
        }
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.SaveAndReimport();
        return Tex(path);
    }

    // Hand-painted-looking meadow: two greens blended by soft low-frequency noise, faint grain, no
    // photographic litter (the Poly Haven ground scans read as autumn leaves at gameplay zoom).
    static Texture2D MeadowTexture()
    {
        var path = TextureDir + "/SonMonMeadow.png";
        var existing = Tex(path);
        if (existing != null) return existing;
        const int size = 512;
        var deep = new Color(.27f, .43f, .27f);
        var light = new Color(.42f, .56f, .33f);
        var sun = new Color(.55f, .60f, .38f);
        var tex = new Texture2D(size, size, TextureFormat.RGB24, true);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = x / (float)size * Mathf.PI * 2, v = y / (float)size * Mathf.PI * 2;
            float broad = Mathf.PerlinNoise(Mathf.Cos(u) * 1.3f + 4, Mathf.Sin(u) * 1.3f + Mathf.Cos(v) * 1.3f + 9);
            float patch = Mathf.PerlinNoise(Mathf.Cos(u) * 3.1f + 21, Mathf.Sin(v) * 3.1f + Mathf.Sin(u) * 2f + 2);
            float grain = Mathf.PerlinNoise(x * .21f, y * .21f);
            var c = Color.Lerp(deep, light, Mathf.SmoothStep(.25f, .75f, broad));
            c = Color.Lerp(c, sun, Mathf.SmoothStep(.62f, .85f, patch) * .55f);
            tex.SetPixel(x, y, c * (.94f + grain * .1f));
        }
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        return Tex(path);
    }

    static Dictionary<string, Material> BuildNatureMaterials()
    {
        string T(string file) => $"{Nature}/Textures/{file}.png";
        var map = new Dictionary<string, Material>();
        void Leaf(string name, string tex, Color tint) => map[name] = Lit("Nature " + name, tint, Tex(T(tex)), null, 1, .15f, 0, null, true, true);
        void Bark(string name, string tex) => map[name] = Lit("Nature " + name, new Color(.85f, .78f, .72f), Tex(T(tex)), Tex(T(tex + "_Normal")), 1, .1f);
        Leaf("Leaves_Pine", "Leaf_Pine_C", new Color(.78f, .95f, .82f));
        Leaf("Leaves_NormalTree", "Leaves_NormalTree_C", new Color(.88f, 1f, .82f));
        Leaf("Leaves_TwistedTree", "Leaves_TwistedTree_C", new Color(.9f, 1f, .85f));
        Leaf("Leaves", "Leaves", new Color(.85f, 1f, .8f));
        Leaf("Grass", "Grass", new Color(.8f, 1f, .7f));
        Leaf("Flowers", "Flowers", Color.white);
        Bark("Bark_NormalTree", "Bark_NormalTree");
        Bark("Bark_TwistedTree", "Bark_TwistedTree");
        Bark("Bark_DeadTree", "Bark_DeadTree");
        map["Rocks"] = Lit("Nature Rocks", new Color(.85f, .88f, .86f), Tex(T("Rocks_Diffuse")), null, 1, .15f);
        map["PathRocks"] = Lit("Nature Path Rocks", new Color(.9f, .9f, .88f), Tex(T("PathRocks_Diffuse")), null, 1, .2f);
        map["Mushrooms"] = Lit("Nature Mushrooms", Color.white, Tex(T("Mushrooms")), null, 1, .2f);
        map["Material"] = map["Leaves_TwistedTree"];
        return map;
    }

    static Material Skybox(string name, string hdrPath, float exposure)
    {
        var path = $"{MaterialDir}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("Skybox/Panoramic")); AssetDatabase.CreateAsset(mat, path); }
        mat.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture>(hdrPath));
        mat.SetFloat("_Exposure", exposure);
        mat.SetFloat("_Rotation", 200);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ------------------------------------------------------------------ prefabs

    static void Remap(string modelPath, Func<string, Material> lookup)
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.importAnimation = false;
        importer.importCameras = importer.importLights = false;
        importer.isReadable = false;
        foreach (var embedded in AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Material>())
        {
            var target = lookup(embedded.name);
            if (target != null) importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), embedded.name), target);
        }
        // Material descriptions are not loadable until first import; also remap known kit slot names.
        foreach (var slot in new[] { "Roof", "RoofTrim", "RoofGrey", "TrimGrey", "Lacquer", "Wood", "Plaster", "Stone", "Marble", "Gold", "Paper", "Glow", "Soil", "Leaf", "Bamboo" })
        {
            var target = lookup(slot);
            if (target != null) importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), slot), target);
        }
        importer.SaveAndReimport();
    }

    static SonMonKit.BuildingEntry[] BuildBuildingPrefabs(Dictionary<string, Material> mats)
    {
        var entries = new List<SonMonKit.BuildingEntry>();
        var models = Directory.GetFiles(ModelDir, "SonMon_*.fbx").Select(p => p.Replace('\\', '/')).ToArray();
        foreach (var modelPath in models) Remap(modelPath, name => mats.TryGetValue(name, out var m) ? m : null);
        // The kit puts every entrance on the same side. Measure it once on the hall (its stair and incense
        // burner stick out in front) and turn the whole kit so entrances face the game camera (-Z).
        var hallProbe = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir + "/SonMon_hall.fbx"));
        var hallBounds = Bounds(hallProbe);
        var facing = hallBounds.size.y < Mathf.Max(hallBounds.size.x, hallBounds.size.z) * .8f
            ? throw new InvalidOperationException("SonMon_hall.fbx is not Y-up; re-export with bake_space_transform")
            : hallBounds.center.z > .02f ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
        Object.DestroyImmediate(hallProbe);
        foreach (var modelPath in models)
        {
            string type = Path.GetFileNameWithoutExtension(modelPath).Substring("SonMon_".Length);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var root = new GameObject(type);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = facing;
            var bounds = Bounds(instance);
            if (type == "meditation") FormationDisc(root.transform, new Vector3(0, .32f, 0), 2.2f, mats["Formation"], 9);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/Building_{type}.prefab");
            Object.DestroyImmediate(root);
            Debug.Log($"SONMON_PREFAB {type} size={Bounds(prefab).size} centerZ={bounds.center.z:F2}");
            if (!type.StartsWith("prop_")) entries.Add(new SonMonKit.BuildingEntry { type = type, prefab = prefab });
        }
        return entries.ToArray();
    }

    static readonly Dictionary<string, GameObject> natureCache = new Dictionary<string, GameObject>();

    static GameObject NaturePrefab(string name, Dictionary<string, Material> natureMats, float scale)
    {
        string key = name + "@" + scale;
        if (natureCache.TryGetValue(key, out var cached) && cached != null) return cached;
        var modelPath = $"{Nature}/Models/{name}.fbx";
        Remap(modelPath, n => natureMats.TryGetValue(n, out var m) ? m : null);
        var root = new GameObject(name);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath), root.transform);
        instance.transform.localScale = Vector3.one * scale;
        foreach (var r in instance.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.On;
        AddLod(root, instance, name, natureMats, scale);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/Nature_{name}_{Mathf.RoundToInt(scale * 100)}.prefab");
        Object.DestroyImmediate(root);
        natureCache[key] = prefab;
        return prefab;
    }

    // LOD0 = Quaternius model, LOD1 = Blender-decimated copy (Tools/Blender/nature_lods.py), then culled.
    static void AddLod(GameObject root, GameObject lod0, string name, Dictionary<string, Material> natureMats, float scale)
    {
        var lodPath = $"{ModelDir}/NatureLOD/{name}_LOD1.fbx";
        var lodModel = AssetDatabase.LoadAssetAtPath<GameObject>(lodPath);
        var lod0Renderers = lod0.GetComponentsInChildren<Renderer>();
        var group = root.AddComponent<LODGroup>();
        if (lodModel == null)
        {
            group.SetLODs(new[] { new LOD(.02f, lod0Renderers) });
            return;
        }
        Remap(lodPath, n => natureMats.TryGetValue(n, out var m) ? m : null);
        var lod1 = (GameObject)PrefabUtility.InstantiatePrefab(lodModel, root.transform);
        lod1.transform.localScale = Vector3.one * scale;
        var a = Bounds(lod0);
        var b = Bounds(lod1);
        // The re-exported LOD may come in with a different unit scale (cm vs m): normalise on height,
        // then insist that all three axes agree, which catches an axis (Y/Z) mismatch.
        lod1.transform.localScale *= a.size.y / Mathf.Max(1e-4f, b.size.y);
        b = Bounds(lod1);
        for (int axis = 0; axis < 3; axis++)
            if (Mathf.Abs(a.size[axis] - b.size[axis]) > Mathf.Max(.05f, a.size[axis] * .2f))
                throw new InvalidOperationException($"LOD1 of {name} does not match LOD0: {a.size} vs {b.size}");
        lod1.transform.localPosition += a.center - b.center;
        foreach (var r in lod1.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
        group.SetLODs(new[] { new LOD(.14f, lod0Renderers), new LOD(.015f, lod1.GetComponentsInChildren<Renderer>()) });
        group.RecalculateBounds();
    }

    static Bounds Bounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        var b = renderers[0].bounds;
        foreach (var r in renderers.Skip(1)) b.Encapsulate(r.bounds);
        return b;
    }

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    // ------------------------------------------------------------------ environment

    // Mountain-top sect: a plateau (flat where the server places buildings and resources) ringed by cliffs
    // that drop into a sea of clouds, with distant karst peaks. Deterministic, so rebuilding is stable.
    static GameObject BuildEnvironment(Dictionary<string, Material> mats, Dictionary<string, Material> natureMats)
    {
        var random = new System.Random(1729);
        float R(float a, float b) => a + (float)random.NextDouble() * (b - a);
        var root = new GameObject("SonMon Environment");

        var terrain = new GameObject("Plateau and cliffs", typeof(MeshFilter), typeof(MeshRenderer));
        terrain.transform.SetParent(root.transform, false);
        terrain.GetComponent<MeshFilter>().sharedMesh = SaveMesh(PlateauMesh(), "SonMonPlateau");
        terrain.GetComponent<MeshRenderer>().sharedMaterials = new[] { mats["Ground"], mats["Cliff"] };

        // Central plaza around the Chánh Điện and the ceremonial axis to the scenic gate.
        Slab(root, "Central plaza", new Vector3(0, .02f, -1), new Vector3(15, .08f, 13), mats["Courtyard"], 2.5f);
        Slab(root, "Sacred axis", new Vector3(0, .02f, -12), new Vector3(3.2f, .08f, 10), mats["Courtyard"], 2.5f);
        Slab(root, "Gate terrace", new Vector3(0, .02f, -18.5f), new Vector3(8, .08f, 4), mats["Courtyard"], 2.5f);
        Place(root, "Building_gate", new Vector3(0, 0, -18.5f), 0, 1.7f);
        foreach (int side in new[] { -1, 1 })
            for (int k = 0; k < 4; k++)
                Place(root, "Building_prop_lantern", new Vector3(side * 2.2f, 0, -8.5f - k * 2.4f), 0, 1.1f);
        foreach (var corner in new[] { new Vector3(-6.8f, 0, 4.8f), new Vector3(6.8f, 0, 4.8f), new Vector3(-6.8f, 0, -6.8f), new Vector3(6.8f, 0, -6.8f) })
            Place(root, "Building_prop_lantern", corner, 0, 1.2f);

        var groves = new GameObject("Bamboo groves");
        groves.transform.SetParent(root.transform, false);
        foreach (var at in new[] { new Vector3(-8, 0, -1), new Vector3(-12.5f, 0, 7.5f), new Vector3(-13.5f, 0, 2.5f), new Vector3(10.5f, 0, -1.5f),
                                   new Vector3(4.5f, 0, 8.5f), new Vector3(-4.5f, 0, 8.5f), new Vector3(-4, 0, -16.5f), new Vector3(4, 0, -16.5f) })
            Place(groves, "Building_prop_bamboo", at, R(0, 360), R(.9f, 1.2f));

        // Stone paths from the plaza to each core hall (server core positions + half footprint).
        var paths = new GameObject("Stone paths");
        paths.transform.SetParent(root.transform, false);
        foreach (var target in new[] { new Vector3(-8.5f, 0, 5.5f), new Vector3(-7.5f, 0, -6.5f), new Vector3(8.5f, 0, 5.5f), new Vector3(8.5f, 0, -6.5f), new Vector3(14, 0, 1) })
        {
            var start = new Vector3(Mathf.Clamp(target.x, -7, 7), 0, Mathf.Clamp(target.z, -7.4f, 5.4f));
            float length = Vector3.Distance(start, target);
            for (float t = 0; t <= length; t += 1.6f)
            {
                var stone = PlaceNature(paths, natureMats, "RockPath_Square_Wide", Vector3.Lerp(start, target, t / Mathf.Max(.01f, length)), R(0, 360), .8f);
                stone.transform.position += Vector3.up * .01f;
            }
        }

        // Ancient pines and boulders along the plateau rim, flowers and grass in clusters.
        var rim = new GameObject("Rim forest");
        rim.transform.SetParent(root.transform, false);
        for (int i = 0; i < 70; i++)
        {
            float a = R(0, Mathf.PI * 2), r = R(49, 54);
            var at = RimPoint(a, r);
            PlaceNature(rim, natureMats, "Pine_" + (1 + i % 5), at, R(0, 360), TreeScale * R(.9f, 1.35f));
        }
        for (int i = 0; i < 26; i++)
        {
            float a = R(0, Mathf.PI * 2);
            PlaceNature(rim, natureMats, "Rock_Medium_" + (1 + i % 3), RimPoint(a, R(48, 55)) + Vector3.down * .3f, R(0, 360), R(.8f, 1.8f));
        }
        var meadow = new GameObject("Meadow dressing");
        meadow.transform.SetParent(root.transform, false);
        string[] small = { "Grass_Wispy_Tall", "Grass_Common_Tall", "Flower_3_Group", "Flower_4_Group", "Bush_Common_Flowers", "Fern_1", "Plant_1_Big" };
        for (int c = 0; c < 26; c++)
        {
            float a = R(0, Mathf.PI * 2), r = R(20, 46);
            var centre = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
            for (int k = 0; k < 6; k++)
                PlaceNature(meadow, natureMats, small[random.Next(small.Length)], centre + new Vector3(R(-2.5f, 2.5f), 0, R(-2.5f, 2.5f)), R(0, 360), R(.35f, .6f));
        }

        // Distant karst peaks crowned with pines, rising out of the cloud sea.
        var peaks = new GameObject("Distant peaks");
        peaks.transform.SetParent(root.transform, false);
        for (int i = 0; i < 18; i++)
        {
            float a = i / 18f * Mathf.PI * 2 + R(-.12f, .12f), r = R(150, 230), h = R(55, 110);
            var at = new Vector3(Mathf.Cos(a) * r, -30, Mathf.Sin(a) * r);
            var peak = new GameObject("Karst peak", typeof(MeshFilter), typeof(MeshRenderer));
            peak.transform.SetParent(peaks.transform, false);
            peak.transform.localPosition = at;
            peak.GetComponent<MeshFilter>().sharedMesh = SaveMesh(PeakMesh(R(9, 16), h, i), "SonMonPeak" + i);
            peak.GetComponent<MeshRenderer>().sharedMaterial = mats["Peak"];
            peak.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            for (int k = 0; k < 3; k++)
                PlaceNature(peaks, natureMats, "Pine_5", at + new Vector3(R(-2, 2), h * .97f, R(-2, 2)), R(0, 360), TreeScale * 3f);
        }

        // Sea of clouds below the cliffs and drifting veils at the rim.
        var clouds = new GameObject("Cloud sea");
        clouds.transform.SetParent(root.transform, false);
        for (int layer = 0; layer < 3; layer++)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.name = "Cloud layer";
            quad.transform.SetParent(clouds.transform, false);
            quad.transform.localPosition = new Vector3(0, -9 - layer * 3.5f, 0);
            quad.transform.localRotation = Quaternion.Euler(90, layer * 37, 0);
            quad.transform.localScale = Vector3.one * 420;
            quad.GetComponent<MeshRenderer>().sharedMaterial = mats["Cloud"];
            quad.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var drift = quad.AddComponent<SonMonDrift>();
            drift.speed = new Vector2(.004f + layer * .002f, .002f);
            drift.tiling = 5 + layer * 2;
        }

        // Waterfall pouring off the north-west rim into the clouds, fed by a spirit pool.
        var fall = new GameObject("Spirit waterfall");
        fall.transform.SetParent(root.transform, false);
        var lip = RimPoint(Mathf.PI * .78f, 50);
        for (int k = 0; k < 3; k++)
        {
            var ribbon = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(ribbon.GetComponent<Collider>());
            ribbon.name = "Falling water";
            ribbon.transform.SetParent(fall.transform, false);
            var outward = new Vector3(lip.x, 0, lip.z).normalized;
            ribbon.transform.localPosition = lip + outward * (1.5f + k * .4f) + Vector3.down * 12;
            ribbon.transform.localRotation = Quaternion.LookRotation(-outward);
            ribbon.transform.localScale = new Vector3(3.2f - k * .6f, 26, 1);
            ribbon.GetComponent<MeshRenderer>().sharedMaterial = mats["Water"];
            ribbon.AddComponent<SonMonDrift>().speed = new Vector2(0, -.35f - k * .1f);
        }

        BuildAtmosphere(root, mats, natureMats);
        PlaceDisciplePrototypes(root);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/SonMon_Environment.prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ------------------------------------------------------------------ atmosphere and magic

    static void BuildAtmosphere(GameObject root, Dictionary<string, Material> mats, Dictionary<string, Material> natureMats)
    {
        var magic = new GameObject("Spirit atmosphere");
        magic.transform.SetParent(root.transform, false);

        // Great formation in front of the Chánh Điện (plaza front, clear of the hall stair) and a smaller
        // echo where the sacred axis meets the gate terrace.
        FormationDisc(magic.transform, new Vector3(0, .07f, -5.6f), 3.4f, mats["Formation"], 4);
        FormationDisc(magic.transform, new Vector3(0, .07f, -15.2f), 2.2f, mats["Formation"], -6);

        Particles(magic, "SonMon Spirit motes", new Vector3(0, 1.5f, -1), new Vector3(34, 5, 30), mats["Mote"], 90, 70, 10,
            new Vector2(.07f, .16f), new Vector3(0, .12f, 0), new Color(.75f, 1f, .92f), new Color(1f, .9f, .6f));
        Particles(magic, "SonMon Formation updraft", new Vector3(0, .2f, -5.6f), new Vector3(2.6f, .2f, 2.6f), mats["Mote"], 40, 12, 3.5f,
            new Vector2(.06f, .12f), new Vector3(0, .65f, 0), new Color(.5f, 1f, .95f), new Color(.8f, 1f, 1f));
        var fireflies = Particles(magic, "SonMon Night fireflies", new Vector3(0, 1.2f, 0), new Vector3(60, 3, 60), mats["Mote"], 140, 30, 8,
            new Vector2(.08f, .14f), new Vector3(0, .05f, 0), new Color(1f, .9f, .55f), new Color(.7f, 1f, .7f));
        var fly = fireflies.emission;
        fly.enabled = false;  // switched on at night by SonMonSky

        BuildWaterfall(root, mats, natureMats);
    }

    static void FormationDisc(Transform parent, Vector3 at, float size, Material mat, float spin)
    {
        var disc = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.DestroyImmediate(disc.GetComponent<Collider>());
        disc.name = "Spirit formation";
        disc.transform.SetParent(parent, false);
        disc.transform.localPosition = at;
        disc.transform.localRotation = Quaternion.Euler(90, 0, 0);
        disc.transform.localScale = Vector3.one * size;
        var renderer = disc.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        var turn = disc.AddComponent<SonMonSpin>();
        turn.degreesPerSecond = spin;
    }

    static ParticleSystem Particles(GameObject parent, string name, Vector3 at, Vector3 box, Material mat, int max, float rate,
        float life, Vector2 size, Vector3 velocity, Color a, Color b)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = at;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.duration = life;
        main.maxParticles = max;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life * .6f, life);
        main.startSpeed = 0;
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        main.startColor = new ParticleSystem.MinMaxGradient(a, b);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = box;
        var emission = ps.emission;
        emission.rateOverTime = rate;
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(velocity.x - .05f, velocity.x + .05f);
        vel.y = new ParticleSystem.MinMaxCurve(velocity.y * .6f, velocity.y);
        vel.z = new ParticleSystem.MinMaxCurve(velocity.z - .05f, velocity.z + .05f);
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = .25f;
        noise.frequency = .4f;
        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .25f), new GradientAlphaKey(.8f, .7f), new GradientAlphaKey(0, 1) });
        fade.color = gradient;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        ps.Play();
        return ps;
    }

    // A mossy crag inside the main camera's view (upper left of the default framing) with a waterfall
    // facing the camera, a spirit pool at its foot and drifting mist.
    static void BuildWaterfall(GameObject root, Dictionary<string, Material> mats, Dictionary<string, Material> natureMats)
    {
        var fall = new GameObject("Crag waterfall");
        fall.transform.SetParent(root.transform, false);
        var basePoint = new Vector3(-21, -.3f, 17);
        var toCamera = new Vector3(31, 0, -38).normalized;
        const float height = 9.5f, radius = 4.2f;

        var crag = new GameObject("Waterfall crag", typeof(MeshFilter), typeof(MeshRenderer));
        crag.transform.SetParent(fall.transform, false);
        crag.transform.localPosition = basePoint;
        crag.GetComponent<MeshFilter>().sharedMesh = SaveMesh(PeakMesh(radius, height, 77), "SonMonWaterfallCrag");
        crag.GetComponent<MeshRenderer>().sharedMaterial = mats["Cliff"];
        for (int k = 0; k < 3; k++)
            PlaceNature(fall, natureMats, "Pine_" + (2 + k), basePoint + new Vector3(-1 + k, height - .4f, k - 1), k * 120, TreeScale * 1.1f);
        foreach (var offset in new[] { new Vector3(3.5f, 0, 1), new Vector3(-3, 0, 3), new Vector3(2, 0, -3.5f) })
            PlaceNature(fall, natureMats, "Rock_Medium_" + (1 + (int)Mathf.Abs(offset.x) % 3), basePoint + offset, offset.z * 40, 1.3f);

        var lip = basePoint + toCamera * (radius * .55f) + Vector3.up * (height * .82f);
        var poolCentre = basePoint + toCamera * (radius + 1.8f);
        for (int k = 0; k < 2; k++)
        {
            var ribbon = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(ribbon.GetComponent<Collider>());
            ribbon.name = "Falling water";
            ribbon.transform.SetParent(fall.transform, false);
            var top = lip + toCamera * (.25f * k);
            var bottom = poolCentre - toCamera * .6f + Vector3.up * .1f;
            ribbon.transform.localPosition = (top + bottom) * .5f;
            // Quad faces local -Z: point it at the camera, with local +Y running from pool up to the lip.
            ribbon.transform.localRotation = Quaternion.LookRotation(-toCamera, (top - bottom).normalized);
            ribbon.transform.localScale = new Vector3(1.9f - k * .5f, Vector3.Distance(top, bottom), 1);
            ribbon.GetComponent<MeshRenderer>().sharedMaterial = mats["Water"];
            ribbon.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var flow = ribbon.AddComponent<SonMonDrift>();
            flow.speed = new Vector2(0, .9f + k * .25f);
        }

        var pool = new GameObject("Spirit pool", typeof(MeshFilter), typeof(MeshRenderer));
        pool.transform.SetParent(fall.transform, false);
        pool.transform.localPosition = poolCentre + Vector3.up * .04f;
        pool.GetComponent<MeshFilter>().sharedMesh = SaveMesh(DiscMesh(3.1f, 40, 5), "SonMonSpiritPool");
        pool.GetComponent<MeshRenderer>().sharedMaterial = mats["Water"];
        for (int k = 0; k < 9; k++)
        {
            float a = k / 9f * Mathf.PI * 2;
            PlaceNature(fall, natureMats, k % 2 == 0 ? "RockPath_Round_Small_1" : "Pebble_Round_" + (1 + k % 5),
                poolCentre + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 3.2f, a * 57, 1.2f);
        }
        Particles(fall, "SonMon Waterfall mist", poolCentre + Vector3.up * .4f, new Vector3(2.2f, .4f, 1.2f), mats["Mist"], 50, 14, 3.5f,
            new Vector2(.7f, 1.6f), new Vector3(0, .45f, 0), new Color(.9f, .97f, 1f, .45f), new Color(.8f, .9f, .95f, .25f));
    }

    static Mesh DiscMesh(float radius, int segments, int seed)
    {
        var verts = new List<Vector3> { Vector3.zero };
        var uvs = new List<Vector2> { new Vector2(.5f, .5f) };
        for (int i = 0; i <= segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2;
            float r = radius * (.82f + .22f * Mathf.PerlinNoise(Mathf.Cos(a) * 1.7f + seed, Mathf.Sin(a) * 1.7f));
            verts.Add(new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r));
            uvs.Add(new Vector2(.5f + Mathf.Cos(a) * .5f, .5f + Mathf.Sin(a) * .5f));
        }
        var tris = new List<int>();
        for (int i = 1; i <= segments; i++) tris.AddRange(new[] { 0, i + 1, i });
        var mesh = new Mesh { name = "SonMon pool" };
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        return mesh;
    }

    static Material Additive(string name, string shader, Texture2D tex, Color color, bool additive = true)
    {
        var path = $"{MaterialDir}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(mat, path); }
        mat.shader = Shader.Find(shader);
        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_Blend", additive ? 2 : 0);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0);
        mat.SetFloat("_Cull", (float)CullMode.Off);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)RenderQueue.Transparent + 10;
        mat.SetOverrideTag("RenderType", "Transparent");
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Texture2D SaveTexture(string file, int width, int height, Func<int, int, Color> pixel, bool repeat)
    {
        var path = TextureDir + "/" + file;
        var existing = Tex(path);
        if (existing != null) return existing;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            tex.SetPixel(x, y, pixel(x, y));
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.alphaIsTransparency = true;
        importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        return Tex(path);
    }

    static Texture2D MoteTexture() => SaveTexture("SonMonMote.png", 64, 64, (x, y) =>
    {
        float r = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 31.5f;
        float core = Mathf.Pow(Mathf.Clamp01(1 - r), 3), halo = Mathf.Pow(Mathf.Clamp01(1 - r), 1.4f) * .35f;
        return new Color(1, 1, 1, Mathf.Clamp01(core + halo));
    }, false);

    static Texture2D WaterTexture() => SaveTexture("SonMonWaterStreaks.png", 128, 512, (x, y) =>
    {
        float u = x / 128f * Mathf.PI * 2, v = y / 512f;
        float streak = Mathf.PerlinNoise(Mathf.Cos(u) * 3 + 5, Mathf.Sin(u) * 3 + v * 2);
        float fine = Mathf.PerlinNoise(x * .35f, y * .02f);
        float alpha = Mathf.Clamp01(.45f + streak * .45f + (fine - .5f) * .3f);
        float foam = Mathf.SmoothStep(.62f, .9f, streak) * .3f;
        return new Color(.85f + foam, .85f + foam, .85f + foam, alpha);
    }, true);

    // Formation sigil: concentric rings, 64 radial ticks, eight trigram groups, an octagram and node dots.
    static Texture2D RuneTexture() => SaveTexture("SonMonFormation.png", 1024, 1024, (x, y) =>
    {
        var p = new Vector2(x - 511.5f, y - 511.5f) / 511.5f;
        float r = p.magnitude, deg = (Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg + 360) % 360;
        float Ring(float radius, float width) => Mathf.Clamp01(1 - Mathf.Abs(r - radius) / width);
        float v = Ring(.97f, .012f) + Ring(.92f, .006f) + Ring(.70f, .01f) + Ring(.64f, .005f) + Ring(.36f, .008f) + Ring(.30f, .004f);
        if (r > .92f && r < .97f && Mathf.Abs((deg % 5.625f) - 2.8f) < .35f) v += .9f;
        float sector = (deg + 22.5f) % 45f - 22.5f;
        int trigram = Mathf.FloorToInt((deg + 22.5f) / 45f) % 8;
        for (int bar = 0; bar < 3; bar++)
        {
            bool broken = ((trigram >> bar) & 1) == 1;
            if (Mathf.Abs(r - (.74f + bar * .055f)) < .013f && Mathf.Abs(sector) < 13 && !(broken && Mathf.Abs(sector) < 2.5f)) v += 1;
        }
        for (int k = 0; k < 8; k++)
        {
            float t1 = k * Mathf.PI / 4, t2 = t1 + Mathf.PI * 3 / 4;
            var a1 = new Vector2(Mathf.Cos(t1), Mathf.Sin(t1)) * .64f;
            var d = new Vector2(Mathf.Cos(t2), Mathf.Sin(t2)) * .64f - a1;
            float t = Mathf.Clamp01(Vector2.Dot(p - a1, d) / d.sqrMagnitude);
            v += Mathf.Clamp01(1 - Vector2.Distance(p, a1 + d * t) / .007f) * .8f;
        }
        for (int k = 0; k < 8; k++)
        {
            float t1 = k * Mathf.PI / 4 + Mathf.PI / 8;
            v += Mathf.Clamp01(1 - Vector2.Distance(p, new Vector2(Mathf.Cos(t1), Mathf.Sin(t1)) * .5f) / .03f);
        }
        v += Mathf.Clamp01(1 - r / .12f) * .6f;
        float glow = r < .97f ? Mathf.Clamp01(1 - r) * .08f : 0;
        return new Color(1, 1, 1, Mathf.Clamp01(v + glow));
    }, false);

    // ------------------------------------------------------------------ creatures and characters

    const string CharacterDir = ModelDir + "/Characters";

    static void BuildCharacters(SonMonKit kit, Dictionary<string, Material> mats)
    {
        var look = new Dictionary<string, Material>
        {
            ["Ivory"] = Lit("Creature Ivory Coat", new Color(.96f, .94f, .89f), null, null, 1, .38f),
            ["JadeGlow"] = Lit("Creature Jade Antler", new Color(.55f, 1f, .85f), null, null, 1, .85f, 0, new Color(.25f, 1.25f, .95f) * 1.4f),
            ["Marking"] = Lit("Creature Spirit Marking", new Color(.55f, .95f, .86f), null, null, 1, .6f, 0, new Color(.2f, .85f, .75f) * .8f),
            ["Hoof"] = Lit("Creature Jade Hoof", new Color(.42f, .58f, .58f), null, null, 1, .7f),
            ["Eye"] = Lit("Character Eye", new Color(.03f, .05f, .07f), null, null, 1, .92f),
            ["Highlight"] = Lit("Character Eye Highlight", Color.white, null, null, 1, .5f, 0, Color.white * 1.2f),
            ["Skin"] = Lit("Character Skin", new Color(1f, .87f, .78f), null, null, 1, .3f),
            ["Hair"] = Lit("Character Hair", new Color(.07f, .08f, .11f), null, null, 1, .55f),
            ["RobeWhite"] = Lit("Character Robe White", new Color(.95f, .96f, .97f), null, null, 1, .22f),
            ["RobeJade"] = Lit("Character Robe Jade", new Color(.33f, .63f, .6f), null, null, 1, .28f),
            ["Sash"] = Lit("Character Sash", new Color(.16f, .3f, .36f), null, null, 1, .35f),
            ["Gold"] = mats["Gold"],
            ["Iris"] = Lit("Character Iris", new Color(.26f, .62f, .64f), null, null, 1, .9f, 0, new Color(.05f, .2f, .2f)),
            ["Boot"] = Lit("Character Boot", new Color(.13f, .14f, .16f), null, null, 1, .4f),
            ["Blush"] = Lit("Character Blush", new Color(1f, .66f, .66f), null, null, 1, .2f),
            ["Mouth"] = Lit("Character Mouth", new Color(.62f, .3f, .32f), null, null, 1, .3f),
        };

        GameObject Character(string file, string name, SonMonPuppet.Kind kind, Action<GameObject> extra)
        {
            var path = $"{CharacterDir}/{file}.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            if (importer == null) return null;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.optimizeGameObjects = false;  // SonMonPuppet drives the bone transforms by name
            Remap(path, n => look.TryGetValue(n, out var m) ? m : null);
            var root = new GameObject(name);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), root.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0, 180, 0);  // Blender -Y front arrives as +Z; face -Z like the kit
            foreach (var skinned in model.GetComponentsInChildren<SkinnedMeshRenderer>()) skinned.updateWhenOffscreen = false;
            root.AddComponent<SonMonPuppet>().kind = kind;
            extra?.Invoke(root);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/Character_{file.Substring("SonMon_".Length)}.prefab");
            var size = Bounds(root).size;
            int tris = model.GetComponentsInChildren<SkinnedMeshRenderer>().Sum(r => r.sharedMesh.triangles.Length / 3) +
                       model.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.triangles.Length / 3);
            int boneCount = model.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(r => r.bones).Distinct().Count();
            Debug.Log($"SONMON_CHARACTER {name} size={size} tris={tris} bones={boneCount}");
            Object.DestroyImmediate(root);
            return prefab;
        }

        kit.spiritDeer = Character("SonMon_spiritdeer", "Bach Ngoc Linh Loc", SonMonPuppet.Kind.SpiritDeer, root =>
            Particles(root, "Spirit deer aura", new Vector3(0, .95f, 0), new Vector3(.9f, .7f, 1.3f), mats["Mote"], 24, 5, 4,
                new Vector2(.03f, .07f), new Vector3(0, .14f, 0), new Color(.55f, 1f, .85f), new Color(.9f, 1f, .95f)));
        kit.disciple = Character("SonMon_disciple", "SonMon Disciple", SonMonPuppet.Kind.Disciple, null);
    }

    // Two instances of the single disciple prototype as scenery (not server entities): one standing by
    // the great formation facing the default camera, one walking the sacred axis.
    static void PlaceDisciplePrototypes(GameObject root)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/Character_disciple.prefab");
        if (prefab == null) return;
        var toCamera = new Vector3(31, 0, -38).normalized;
        var idle = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
        idle.name = "SonMon Disciple (idle)";
        idle.transform.localPosition = new Vector3(2.1f, 0, -5.2f);
        idle.transform.localRotation = Quaternion.LookRotation(-toCamera);
        var walker = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
        walker.name = "SonMon Disciple (walking)";
        walker.transform.localPosition = new Vector3(.9f, 0, -8.5f);
        var route = walker.AddComponent<SonMonWalker>();
        route.waypoints = new[] { new Vector3(.9f, 0, -8.5f), new Vector3(.9f, 0, -15.6f) };
    }

    // ------------------------------------------------------------------ building visual tiers

    const string ForgeDir = "Assets/ThirdParty/ForgeGUI/ScripturePavilion";

    // Scripture Pavilion (server type "scripture"). Server facts (see docs/BUILDING_VISUAL_TIERS.md):
    // the core Tàng Kinh Các has no level cap in the reference rules; player-built spatial copies cap at 3.
    static BuildingVisualTierSet[] BuildVisualTiers(Dictionary<string, Material> mats)
    {
        var kitScripture = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/Building_scripture.prefab");
        var forge = ForgeGuiPavilionPrefab();
        var spirit = ScriptureDecoration("Deco_scripture_spirit", mats, false);
        var celestial = ScriptureDecoration("Deco_scripture_celestial", mats, true);
        return new[]
        {
            new BuildingVisualTierSet
            {
                type = "scripture",
                tiers = new[]
                {
                    new BuildingVisualTier { label = "I · Thư Các sơ lập", minLevel = 1, prefab = kitScripture },
                    new BuildingVisualTier { label = "II · Tàng Kinh Các trung phẩm", minLevel = 3, prefab = forge },
                    new BuildingVisualTier { label = "III · Linh Kinh Các", minLevel = 6, prefab = forge, decorations = new[] { spirit } },
                    // No bespoke tier-IV model yet: prefab left empty on purpose so the resolver falls back to
                    // tier III's model while still adding the tier-IV decorations.
                    new BuildingVisualTier { label = "IV · Thiên Kinh Các (dự kiến)", minLevel = 9, prefab = null, decorations = new[] { celestial } },
                },
            },
        };
    }

    static GameObject ForgeGuiPavilionPrefab()
    {
        var modelPath = $"{ForgeDir}/ForgeGUI_ScripturePavilion.fbx";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(modelPath) == null) return null;
        void TextureType(string file, bool normal, bool linear)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath($"{ForgeDir}/{file}");
            if (importer == null) return;
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !linear && !normal;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
        TextureType("ForgeGUI_ScripturePavilion_albedo.png", false, false);
        TextureType("ForgeGUI_ScripturePavilion_normal.png", true, true);
        TextureType("ForgeGUI_ScripturePavilion_mask.png", false, true);
        var mat = Lit("ForgeGUI Scripture Pavilion", Color.white, Tex($"{ForgeDir}/ForgeGUI_ScripturePavilion_albedo.png"),
            Tex($"{ForgeDir}/ForgeGUI_ScripturePavilion_normal.png"), 1, 1f);
        mat.SetTexture("_MetallicGlossMap", Tex($"{ForgeDir}/ForgeGUI_ScripturePavilion_mask.png"));
        mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        mat.SetFloat("_Metallic", 1);
        mat.SetFloat("_Smoothness", 1);  // scales the mask's alpha (smoothness)
        mat.SetFloat("_SmoothnessTextureChannel", 0);
        EditorUtility.SetDirty(mat);
        Remap(modelPath, _ => mat);

        var root = new GameObject("Tier_scripture_forgegui");
        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath), root.transform);
        // Fit the 3x3 footprint like the kit pavilion (≈3.1 wide) and face the entrance to -Z.
        var size = Bounds(model).size;
        model.transform.localScale = Vector3.one * (3.1f / Mathf.Max(size.x, size.z));
        model.transform.localRotation = Quaternion.Euler(0, 180, 0);
        var b = Bounds(model);
        model.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/Tier_scripture_forgegui.prefab");
        Debug.Log($"SONMON_TIER forgegui size={Bounds(root).size}");
        Object.DestroyImmediate(root);
        return prefab;
    }

    // Additive tier dressing built only from existing assets: stone lanterns, a formation disc and a mote
    // aura. The celestial variant adds a wide outer formation ring and a denser aura.
    static GameObject ScriptureDecoration(string name, Dictionary<string, Material> mats, bool celestial)
    {
        var root = new GameObject(name);
        var lantern = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/Building_prop_lantern.prefab");
        foreach (var corner in new[] { new Vector3(-1.75f, 0, -1.75f), new Vector3(1.75f, 0, -1.75f), new Vector3(-1.75f, 0, 1.75f), new Vector3(1.75f, 0, 1.75f) })
        {
            if (lantern == null) break;
            var l = (GameObject)PrefabUtility.InstantiatePrefab(lantern, root.transform);
            l.transform.localPosition = corner;
            l.transform.localScale = Vector3.one * 1.1f;
        }
        FormationDisc(root.transform, new Vector3(0, .06f, -2.3f), 1.7f, mats["Formation"], 8);
        if (celestial) FormationDisc(root.transform, new Vector3(0, .04f, 0), 5.2f, mats["Formation"], -3);
        Particles(root, "Scripture aura", new Vector3(0, 2.6f, 0), new Vector3(2.8f, 3.2f, 2.8f), mats["Mote"],
            celestial ? 90 : 40, celestial ? 22 : 9, 5, new Vector2(.05f, .12f), new Vector3(0, .25f, 0),
            new Color(.6f, 1f, .9f), celestial ? new Color(1f, .9f, .55f) : new Color(.85f, 1f, 1f));
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/{name}.prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }

    static Vector3 RimPoint(float angle, float radius)
    {
        // Superellipse rim so the square server area (±40) stays on the plateau.
        float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
        float k = Mathf.Pow(Mathf.Pow(Mathf.Abs(c), 4) + Mathf.Pow(Mathf.Abs(s), 4), .25f);
        return new Vector3(c, 0, s) * radius / k;
    }

    static float PlateauHeight(float x, float z)
    {
        float r = Mathf.Pow(Mathf.Pow(Mathf.Abs(x), 4) + Mathf.Pow(Mathf.Abs(z), 4), .25f);
        float noise = Mathf.PerlinNoise(x * .05f + 31, z * .05f + 17);
        float edge = 50 + noise * 4;
        if (r < edge) return -.03f - Mathf.SmoothStep(0, 1, (r - 38) / 12) * .6f * noise;
        float drop = Mathf.Clamp01((r - edge) / 7);
        return Mathf.Lerp(-.6f, -34, drop * drop * (3 - 2 * drop)) + Mathf.PerlinNoise(x * .2f, z * .2f) * 3 * drop;
    }

    static Mesh PlateauMesh()
    {
        const int cells = 150;
        const float half = 75;
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        for (int j = 0; j <= cells; j++)
        for (int i = 0; i <= cells; i++)
        {
            float x = -half + 2 * half * i / cells, z = -half + 2 * half * j / cells;
            var v = new Vector3(x, PlateauHeight(x, z), z);
            verts.Add(v);
            uvs.Add(new Vector2(x / 22, z / 22 + v.y / 7));
        }
        var ground = new List<int>();
        var cliff = new List<int>();
        for (int j = 0; j < cells; j++)
        for (int i = 0; i < cells; i++)
        {
            int a = j * (cells + 1) + i, b = a + cells + 1;
            var n = Vector3.Cross(verts[b] - verts[a], verts[a + 1] - verts[a]).normalized;
            (n.y > .75f ? ground : cliff).AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
        }
        var mesh = new Mesh { name = "SonMon plateau", indexFormat = IndexFormat.UInt32 };
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(ground, 0);
        mesh.SetTriangles(cliff, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        return mesh;
    }

    static Mesh PeakMesh(float radius, float height, int seed)
    {
        const int sides = 18, rings = 14;
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        for (int y = 0; y <= rings; y++)
        for (int x = 0; x <= sides; x++)
        {
            float u = y / (float)rings, a = x / (float)sides * Mathf.PI * 2;
            float r = radius * Mathf.Pow(1 - .8f * u, 1.3f) * (.7f + .45f * Mathf.PerlinNoise(Mathf.Cos(a) * 2.2f + seed, u * 5 + seed));
            if (y == rings) r *= .35f;
            verts.Add(new Vector3(Mathf.Cos(a) * r + Mathf.Sin(u * 3 + seed) * u * 2.5f, u * height, Mathf.Sin(a) * r));
            uvs.Add(new Vector2(x / (float)sides * radius / 3, u * height / 6));
        }
        var tris = new List<int>();
        for (int y = 0; y < rings; y++)
        for (int x = 0; x < sides; x++)
        {
            int a = y * (sides + 1) + x, b = a + sides + 1;
            tris.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
        }
        var mesh = new Mesh { name = "SonMon karst peak" };
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        return mesh;
    }

    static Mesh SaveMesh(Mesh mesh, string name)
    {
        var path = $"{Root}/Meshes/{name}.asset";
        Directory.CreateDirectory(Root + "/Meshes");
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); return existing; }
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    // Box with world-scaled UVs (one texture repeat per `unitsPerTile`), saved as a mesh asset so the
    // tiling survives in the prefab (property blocks are not serialized).
    static void Slab(GameObject parent, string name, Vector3 at, Vector3 size, Material mat, float unitsPerTile)
    {
        var h = size * .5f;
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        void Face(Vector3 o, Vector3 u, Vector3 v, float su, float sv)
        {
            int start = verts.Count;
            verts.AddRange(new[] { o, o + u, o + u + v, o + v });
            uvs.AddRange(new[] { Vector2.zero, new Vector2(su, 0), new Vector2(su, sv), new Vector2(0, sv) });
            tris.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        }
        float tx = size.x / unitsPerTile, ty = size.y / unitsPerTile, tz = size.z / unitsPerTile;
        Face(new Vector3(-h.x, h.y, -h.z), Vector3.forward * size.z, Vector3.right * size.x, tz, tx);   // top
        Face(new Vector3(-h.x, -h.y, -h.z), Vector3.right * size.x, Vector3.up * size.y, tx, ty);      // -Z
        Face(new Vector3(h.x, -h.y, h.z), Vector3.left * size.x, Vector3.up * size.y, tx, ty);         // +Z
        Face(new Vector3(-h.x, -h.y, h.z), Vector3.back * size.z, Vector3.up * size.y, tz, ty);        // -X
        Face(new Vector3(h.x, -h.y, -h.z), Vector3.forward * size.z, Vector3.up * size.y, tz, ty);     // +X
        var mesh = new Mesh { name = "SonMon slab " + name };
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        var slab = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        slab.transform.SetParent(parent.transform, false);
        slab.transform.localPosition = at - Vector3.up * h.y;
        slab.GetComponent<MeshFilter>().sharedMesh = SaveMesh(mesh, "SonMonSlab_" + name.Replace(' ', '_'));
        slab.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static void Place(GameObject parent, string prefabName, Vector3 at, float yaw, float scale)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{prefabName}.prefab");
        if (prefab == null) return;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
        go.transform.localPosition = at;
        go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        go.transform.localScale = Vector3.one * scale;
    }

    static GameObject PlaceNature(GameObject parent, Dictionary<string, Material> natureMats, string name, Vector3 at, float yaw, float scale)
    {
        var prefab = NaturePrefab(name, natureMats, 1);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
        go.transform.localPosition = new Vector3(at.x, at.y + PlateauHeight(at.x, at.z), at.z);
        go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        go.transform.localScale = Vector3.one * scale;
        return go;
    }
}
