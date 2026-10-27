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
            ["Plaster"] = Lit("SonMon Ivory Plaster", new Color(1f, .96f, .86f), Ph("clay_plaster", "diff"), Ph("clay_plaster", "nor_gl"), 1, .15f),
            ["Stone"] = Lit("SonMon Plinth Stone", new Color(.86f, .86f, .84f), Ph("mossy_stone_wall", "diff"), Ph("mossy_stone_wall", "nor_gl"), 1, .2f),
            ["Marble"] = Lit("SonMon White Marble", new Color(.95f, .94f, .9f), paving, pavingNor, 1, .35f),
            ["Gold"] = Lit("SonMon Gilded Bronze", new Color(.93f, .7f, .32f), null, null, 1, .62f, .85f),
            ["Paper"] = Lit("SonMon Lantern Paper", new Color(.93f, .32f, .16f), null, null, 1, .2f, 0, new Color(.9f, .25f, .08f) * 1.2f),
            ["Glow"] = Lit("SonMon Spirit Glow", new Color(.45f, .9f, .85f), null, null, 1, .8f, 0, new Color(.2f, .85f, .8f) * 1.3f),
            ["Soil"] = Lit("SonMon Soil", new Color(.6f, .48f, .38f), Ph("forest_ground_04", "diff"), Ph("forest_ground_04", "nor_gl"), 1, .1f),
            ["Leaf"] = Lit("SonMon Crop Leaf", new Color(.42f, .66f, .3f), null, null, 1, .2f),
            ["Ground"] = Lit("SonMon Meadow", Color.white, MeadowTexture(), null, 1, .05f),
            ["Courtyard"] = Lit("SonMon Courtyard Paving", new Color(.9f, .9f, .87f), paving, pavingNor, 1, .3f),
            ["Cliff"] = Lit("SonMon Cliff", new Color(.62f, .72f, .74f), Ph("mossy_rock", "diff"), Ph("mossy_rock", "nor_gl"), 1, .15f),
            ["Peak"] = Lit("SonMon Distant Peak", new Color(.47f, .6f, .62f), null, null, 1, .05f),
            ["Water"] = Water(),
            ["Cloud"] = Cloud(),
        };
    }

    static Material Water()
    {
        var mat = Lit("SonMon Waterfall", new Color(.75f, .95f, .97f, .85f), CloudTexture(), null, 1, .9f, 0, new Color(.25f, .45f, .5f));
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
        var deep = new Color(.33f, .52f, .27f);
        var light = new Color(.55f, .70f, .36f);
        var sun = new Color(.66f, .74f, .42f);
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
        foreach (var slot in new[] { "Roof", "RoofTrim", "RoofGrey", "TrimGrey", "Lacquer", "Wood", "Plaster", "Stone", "Marble", "Gold", "Paper", "Glow", "Soil", "Leaf" })
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
        Place(root, "Building_gate", new Vector3(0, 0, -18.5f), 0, 2.1f);
        foreach (int side in new[] { -1, 1 })
            for (int k = 0; k < 4; k++)
                Place(root, "Building_prop_lantern", new Vector3(side * 2.2f, 0, -8.5f - k * 2.4f), 0, 1.1f);
        foreach (var corner in new[] { new Vector3(-6.8f, 0, 4.8f), new Vector3(6.8f, 0, 4.8f), new Vector3(-6.8f, 0, -6.8f), new Vector3(6.8f, 0, -6.8f) })
            Place(root, "Building_prop_lantern", corner, 0, 1.2f);

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
        Slab(fall, "Spirit pool", lip + new Vector3(3.5f, -.05f, -2.5f) * .9f, new Vector3(7, .12f, 5), mats["Water"], 1);
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

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/SonMon_Environment.prefab");
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
