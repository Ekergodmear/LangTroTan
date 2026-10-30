using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Builds the Tàng Kinh Các Level 6 spirit VFX v5 candidate assets, isolated from v4: imports the v5 textures
// (Tools/ShaderGraph/vfx_textures_v5.py), creates materials on the existing v4 Shader Graphs (no new shaders) and the
// v5 Particle System prefabs used by SonMonSpiritGatheringV5. Re-runnable; writes only under the L6v5 folders.
// Run: Unity -batchmode -projectPath <p> -executeMethod SpiritV5Builder.Build -quit
public static class SpiritV5Builder
{
    const string Shaders = "Assets/Art/SonMon/VFX/L6/Shaders";
    const string V4Art = "Assets/Art/SonMon/VFX/L6";
    const string Art = "Assets/Art/SonMon/VFX/L6v5";
    const string Res = "Assets/Resources/SonMon/VFX/L6v5";  // runtime-loadable (Resources.Load)

    public static void Build()
    {
        try
        {
            BuildAll();
            Debug.Log("SPIRIT_V5_BUILD_OK");
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }

    static void BuildAll()
    {
        Directory.CreateDirectory(Res);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Texture2D Tex(string path, bool repeat, int max)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) throw new System.Exception("Missing texture " + path);
            imp.textureType = TextureImporterType.Default;
            imp.alphaSource = TextureImporterAlphaSource.FromInput;
            imp.alphaIsTransparency = true;
            imp.sRGBTexture = true;
            imp.mipmapEnabled = true;
            imp.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            imp.maxTextureSize = max;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;  // BC7
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        var mist = Tex($"{Art}/Textures/T_SpiritMistV5_Flipbook.png", false, 512);
        var wave = Tex($"{Art}/Textures/T_ReleaseWaveV5.png", false, 512);
        var bloom = Tex($"{Art}/Textures/T_ReleaseGlowV5.png", false, 256);
        var seal = Tex($"{Art}/Textures/T_SealBrushV5.png", false, 512);
        var runes = Tex($"{Art}/Textures/T_RuneStripV5.png", true, 1024);
        var glyphs = Tex($"{Art}/Textures/T_ScriptureGlyphsV5.png", false, 512);
        var halo = Tex($"{Art}/Textures/T_OrnamentHaloV5.png", false, 128);
        var plumeTex = Tex($"{Art}/Textures/T_PlumeV5.png", false, 256);
        var mote = AssetDatabase.LoadAssetAtPath<Texture2D>($"{V4Art}/Textures/T_SpiritMote.png");

        var mistSg = AssetDatabase.LoadAssetAtPath<Shader>($"{Shaders}/SG_SpiritMist.shadergraph");
        var formSg = AssetDatabase.LoadAssetAtPath<Shader>($"{Shaders}/SG_SpiritFormation.shadergraph");
        var glyphSg = AssetDatabase.LoadAssetAtPath<Shader>($"{Shaders}/SG_ScriptureGlyph.shadergraph");
        if (mistSg == null || formSg == null || glyphSg == null || mote == null) throw new System.Exception("Missing v4 shader or mote texture");

        Material Mat(string name, Shader sh, System.Action<Material> setup)
        {
            var path = $"{Res}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            m.shader = sh;
            setup(m);
            m.enableInstancing = false;
            EditorUtility.SetDirty(m);
            return m;
        }
        // Soft horizontal wisps, more transparent and less cyan than v4
        var mMist = Mat("M_SpiritMistV5", mistSg, m =>
        {
            m.SetTexture("_MainTex", mist); m.SetColor("_Tint", new Color(.72f, 1f, .92f, .5f)); m.SetFloat("_EmissionBoost", .25f);
            m.SetFloat("_DistortStrength", .05f); m.SetFloat("_DistortScale", 4f); m.SetFloat("_ScrollSpeed", .08f);
            m.SetFloat("_FlipbookTiles", 4); m.SetFloat("_EdgeSoftness", .35f);
        });
        var mGlow = Mat("M_PillarGlowV5", mistSg, m =>
        {
            m.SetTexture("_MainTex", mote); m.SetColor("_Tint", new Color(.5f, 1f, .86f, .95f)); m.SetFloat("_EmissionBoost", 1f);
            m.SetFloat("_DistortStrength", 0); m.SetFloat("_FlipbookTiles", 1); m.SetFloat("_EdgeSoftness", .45f);
        });
        var mHalo = Mat("M_OrnamentHaloV5", mistSg, m =>
        {
            m.SetTexture("_MainTex", halo); m.SetColor("_Tint", new Color(.78f, 1f, .86f, .8f)); m.SetFloat("_EmissionBoost", .5f);
            m.SetFloat("_DistortStrength", .01f); m.SetFloat("_DistortScale", 3f); m.SetFloat("_ScrollSpeed", .05f);
            m.SetFloat("_FlipbookTiles", 1); m.SetFloat("_EdgeSoftness", .45f);
        });
        // The wave texture carries the colour (jade wash inside, warm gold band); alpha-blended so it tints pale stone
        var mWave = Mat("M_ReleaseWaveV5", mistSg, m =>
        {
            m.SetTexture("_MainTex", wave); m.SetColor("_Tint", new Color(1f, 1f, 1f, .9f)); m.SetFloat("_EmissionBoost", .15f);
            m.SetFloat("_DistortStrength", .02f); m.SetFloat("_DistortScale", 5f); m.SetFloat("_ScrollSpeed", .05f);
            m.SetFloat("_FlipbookTiles", 1); m.SetFloat("_EdgeSoftness", .02f);
        });
        var mBloom = Mat("M_ReleaseGlowV5", mistSg, m =>
        {
            m.SetTexture("_MainTex", bloom); m.SetColor("_Tint", new Color(1f, 1f, 1f, .6f)); m.SetFloat("_EmissionBoost", .1f);
            m.SetFloat("_DistortStrength", 0); m.SetFloat("_FlipbookTiles", 1); m.SetFloat("_EdgeSoftness", .45f);
        });
        // V6 plume: soft vertical wisps; the scrolling distortion keeps them moving like rising smoke, not a beam
        var mPlume = Mat("M_PlumeV5", mistSg, m =>
        {
            m.SetTexture("_MainTex", plumeTex); m.SetColor("_Tint", new Color(1f, 1f, 1f, .7f)); m.SetFloat("_EmissionBoost", .3f);
            m.SetFloat("_DistortStrength", .08f); m.SetFloat("_DistortScale", 3f); m.SetFloat("_ScrollSpeed", .35f);
            m.SetFloat("_FlipbookTiles", 1); m.SetFloat("_EdgeSoftness", .12f);
        });
        var mSwirl = Mat("M_SwirlV5", mistSg, m =>
        {
            m.SetTexture("_MainTex", mote); m.SetColor("_Tint", new Color(1f, 1f, 1f, 1f)); m.SetFloat("_EmissionBoost", 1.1f);
            m.SetFloat("_DistortStrength", 0); m.SetFloat("_FlipbookTiles", 1); m.SetFloat("_EdgeSoftness", .45f);
        });
        Mat("M_FormationBandV5", formSg, m =>
        {
            m.SetTexture("_RuneMask", runes); m.SetColor("_Tint", new Color(.38f, .95f, .8f, .75f)); m.SetFloat("_Emission", 1f);
            m.SetFloat("_Tiling", 10); m.SetFloat("_ActivationSoftness", .06f); m.SetFloat("_BaseLevel", .22f); m.SetFloat("_LitLevel", 1.3f);
            m.SetFloat("_Dissolve", 0); m.SetFloat("_DissolveScale", 28f);
        });
        Mat("M_SealV5", formSg, m =>
        {
            m.SetTexture("_RuneMask", seal); m.SetColor("_Tint", new Color(.45f, .95f, .8f, .8f)); m.SetFloat("_Emission", 1f);
            m.SetFloat("_Tiling", 1); m.SetFloat("_ActivationSoftness", .08f); m.SetFloat("_BaseLevel", .22f); m.SetFloat("_LitLevel", 1.3f);
            m.SetFloat("_Dissolve", 0); m.SetFloat("_DissolveScale", 28f);
        });
        var mGlyph = Mat("M_ScriptureGlyphV5", glyphSg, m =>
        {
            m.SetTexture("_MainTex", glyphs); m.SetColor("_EdgeColor", new Color(1f, .95f, .75f, 1f)); m.SetFloat("_GlowBoost", .5f);
            m.SetFloat("_FadeIn", .18f); m.SetFloat("_DissolveStart", .6f); m.SetFloat("_DissolveScale", 14f);
        });

        Prefab("PS_SpiritMistV5", mMist, 40, ps =>
        {
            var sheet = ps.textureSheetAnimation; sheet.enabled = true; sheet.numTilesX = 4; sheet.numTilesY = 4;
            sheet.animation = ParticleSystemAnimationType.WholeSheet; sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0);
            sheet.startFrame = new ParticleSystem.MinMaxCurve(0, 15.99f);
            Noise(ps, .02f, .3f, .1f);
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-.08f, .08f);
            Size(ps, .7f, 1.1f);
            Fade(ps, .3f, .6f);
        });
        // glow billboards sit on opaque crystals: shift them towards the camera so the crystal does not hide them
        Prefab("PS_PillarGlowV5", mGlow, 8, ps => ps.GetComponent<ParticleSystemRenderer>().pivot = new Vector3(0, 0, .5f));
        Prefab("PS_OrnamentHaloV5", mHalo, 4, ps => ps.GetComponent<ParticleSystemRenderer>().pivot = new Vector3(0, 0, .4f));
        // flat release waves: fast early expansion, long readable hold, soft fade; 3D start size gives elliptical waves
        Prefab("PS_ReleaseWaveV5", mWave, 6, ps =>
        {
            var main = ps.main; main.startSize3D = true;
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            r.maxParticleSize = 4f;
            // starts already around the hall (no hidden first half), holds full strength for most of its life
            SizeCurve(ps, new Keyframe(0, .5f, 0, 1.4f), new Keyframe(.5f, .92f, .3f, .3f), new Keyframe(1, 1f, .1f, 0));
            Fade(ps, .08f, .75f);
        });
        Prefab("PS_ReleaseGlowV5", mBloom, 2, ps =>
        {
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            r.maxParticleSize = 4f;
            Size(ps, .6f, 1.15f);
            Fade(ps, .15f, .45f);
        });
        // stretched along the (upward) velocity so each wisp is a soft vertical flame of mist
        Prefab("PS_PlumeV5", mPlume, 24, ps =>
        {
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0;
            r.lengthScale = 2.4f;
            r.maxParticleSize = 1f;
            Noise(ps, .25f, .6f, .4f);
            SizeCurve(ps, new Keyframe(0, .45f, 0, 1.8f), new Keyframe(.45f, 1f, .4f, .4f), new Keyframe(1, 1.35f, .2f, 0));
            Fade(ps, .15f, .5f);
        });
        // motes that spiral up round the plume (orbital velocity about the system's vertical axis); also the seeds
        Prefab("PS_SwirlV5", mSwirl, 40, ps =>
        {
            var v = ps.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.Local;
            v.orbitalX = 0; v.orbitalY = 2.6f; v.orbitalZ = 0;
            Size(ps, 1f, .5f);
            Fade(ps, .15f, .6f);
        });
        Prefab("PS_ScriptureGlyphsV5", mGlyph, 16, ps =>
        {
            var sheet = ps.textureSheetAnimation; sheet.enabled = true; sheet.numTilesX = 4; sheet.numTilesY = 2;
            sheet.animation = ParticleSystemAnimationType.WholeSheet; sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0);
            sheet.startFrame = new ParticleSystem.MinMaxCurve(0, 7.99f);
            Noise(ps, .03f, .35f, .1f);
            var col = ps.colorOverLifetime; col.enabled = false;  // the glyph shader fades and dissolves by age
            ps.GetComponent<ParticleSystemRenderer>().SetActiveVertexStreams(new List<ParticleSystemVertexStream>
            {
                ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV,
                ParticleSystemVertexStream.AgePercent, ParticleSystemVertexStream.StableRandomX,
            });
        });
        AssetDatabase.SaveAssets();
    }

    static void Prefab(string name, Material mat, int max, System.Action<ParticleSystem> setup)
    {
        var go = new GameObject(name) { layer = 2 };
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.maxParticles = max;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startSpeed = 0;
        main.startLifetime = 5;
        main.cullingMode = ParticleSystemCullingMode.Automatic;
        var e = ps.emission; e.rateOverTime = 0;  // SonMonSpiritGatheringV5 emits by script (EmitParams, no allocations)
        var shape = ps.shape; shape.enabled = false;
        var col = ps.colorOverLifetime; col.enabled = false;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.Off;
        r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        r.sortMode = ParticleSystemSortMode.Distance;
        r.maxParticleSize = .5f;
        setup(ps);
        PrefabUtility.SaveAsPrefabAsset(go, $"{Res}/{name}.prefab");
        Object.DestroyImmediate(go);
    }

    static void Noise(ParticleSystem ps, float strength, float frequency, float scroll)
    {
        var n = ps.noise;
        n.enabled = true;
        n.strength = strength;
        n.frequency = frequency;
        n.scrollSpeed = scroll;
        n.damping = true;
        n.octaveCount = 1;
        n.quality = ParticleSystemNoiseQuality.Medium;
    }

    static void Size(ParticleSystem ps, float start, float end) =>
        SizeCurve(ps, new Keyframe(0, start, 0, 0), new Keyframe(1, end, 0, 0));

    static void SizeCurve(ParticleSystem ps, params Keyframe[] keys)
    {
        var s = ps.sizeOverLifetime;
        s.enabled = true;
        s.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(keys));
    }

    // Smooth fade in, hold, fade out (no popping)
    static void Fade(ParticleSystem ps, float inEnd, float outStart)
    {
        var c = ps.colorOverLifetime;
        c.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, inEnd), new GradientAlphaKey(1, outStart), new GradientAlphaKey(0, 1) });
        c.color = g;
    }
}
