using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Builds the Tàng Kinh Các Level 6 v4 spirit VFX assets: texture import settings, materials on the generated Shader
// Graph shaders (Tools/ShaderGraph/sg_build.py) and the Particle System prefabs used by SonMonSpiritGatheringV4.
// Re-runnable; overwrites only its own assets.
// Run: Unity -batchmode -projectPath <p> -executeMethod SpiritV4Builder.Build -quit
public static class SpiritV4Builder
{
    const string Art = "Assets/Art/SonMon/VFX/L6";
    const string Res = "Assets/Resources/SonMon/VFX/L6";  // runtime-loadable (Resources.Load) for the QA scene

    public static void Build()
    {
        Directory.CreateDirectory(Res);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Texture2D Tex(string file, bool repeat, int max)
        {
            var path = $"{Art}/Textures/{file}";
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
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
        var energy = Tex("T_SpiritEnergy_Flipbook.png", false, 512);
        var mote = Tex("T_SpiritMote.png", false, 64);
        var ring = Tex("T_SpiritPulseRing.png", false, 256);
        var runes = Tex("T_RuneStrip.png", true, 1024);
        var glyphs = Tex("T_ScriptureGlyphs.png", false, 512);

        var mistSg = AssetDatabase.LoadAssetAtPath<Shader>($"{Art}/Shaders/SG_SpiritMist.shadergraph");
        var formSg = AssetDatabase.LoadAssetAtPath<Shader>($"{Art}/Shaders/SG_SpiritFormation.shadergraph");
        var glyphSg = AssetDatabase.LoadAssetAtPath<Shader>($"{Art}/Shaders/SG_ScriptureGlyph.shadergraph");

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
        var mMist = Mat("M_SpiritMist", mistSg, m =>
        {
            m.SetTexture("_MainTex", energy); m.SetColor("_Tint", new Color(.55f, 1f, .88f, .75f)); m.SetFloat("_EmissionBoost", .45f);
            m.SetFloat("_DistortStrength", .07f); m.SetFloat("_DistortScale", 4.5f); m.SetFloat("_ScrollSpeed", .1f);
            m.SetFloat("_FlipbookTiles", 4); m.SetFloat("_EdgeSoftness", .3f);
        });
        var mJade = Mat("M_JadeMote", mistSg, m =>
        {
            m.SetTexture("_MainTex", mote); m.SetColor("_Tint", new Color(.6f, 1f, .9f, 1f)); m.SetFloat("_EmissionBoost", 1.2f);
            m.SetFloat("_DistortStrength", 0); m.SetFloat("_FlipbookTiles", 1); m.SetFloat("_EdgeSoftness", .45f);
        });
        var mGold = Mat("M_GoldenMote", mistSg, m =>
        {
            m.SetTexture("_MainTex", mote); m.SetColor("_Tint", new Color(1f, .86f, .5f, 1f)); m.SetFloat("_EmissionBoost", 1.4f);
            m.SetFloat("_DistortStrength", 0); m.SetFloat("_FlipbookTiles", 1); m.SetFloat("_EdgeSoftness", .45f);
        });
        var mGlow = Mat("M_SpiritGlow", mistSg, m =>
        {
            m.SetTexture("_MainTex", mote); m.SetColor("_Tint", new Color(.45f, 1f, .86f, .9f)); m.SetFloat("_EmissionBoost", .9f);
            m.SetFloat("_DistortStrength", .02f); m.SetFloat("_DistortScale", 3f); m.SetFloat("_ScrollSpeed", .05f);
            m.SetFloat("_FlipbookTiles", 1); m.SetFloat("_EdgeSoftness", .45f);
        });
        var mPulse = Mat("M_SpiritPulse", mistSg, m =>
        {
            m.SetTexture("_MainTex", ring); m.SetColor("_Tint", new Color(.7f, 1f, .85f, .85f)); m.SetFloat("_EmissionBoost", .8f);
            m.SetFloat("_DistortStrength", .015f); m.SetFloat("_DistortScale", 6f); m.SetFloat("_ScrollSpeed", .1f);
            m.SetFloat("_FlipbookTiles", 1); m.SetFloat("_EdgeSoftness", .02f);
        });
        Mat("M_SpiritFormation", formSg, m =>
        {
            m.SetTexture("_RuneMask", runes); m.SetColor("_Tint", new Color(.32f, .92f, .78f, .65f)); m.SetFloat("_Emission", 1f);
            m.SetFloat("_Tiling", 10); m.SetFloat("_ActivationSoftness", .06f); m.SetFloat("_BaseLevel", .22f); m.SetFloat("_LitLevel", 1.3f);
            m.SetFloat("_Dissolve", 0); m.SetFloat("_DissolveScale", 28f);
        });
        var mGlyph = Mat("M_ScriptureGlyph", glyphSg, m =>
        {
            m.SetTexture("_MainTex", glyphs); m.SetColor("_EdgeColor", new Color(1f, .96f, .78f, 1f)); m.SetFloat("_GlowBoost", .45f);
            m.SetFloat("_FadeIn", .18f); m.SetFloat("_DissolveStart", .6f); m.SetFloat("_DissolveScale", 16f);
        });

        Prefab("PS_SpiritMist", mMist, 48, ps =>
        {
            var main = ps.main; main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            var sheet = ps.textureSheetAnimation; sheet.enabled = true; sheet.numTilesX = 4; sheet.numTilesY = 4;
            sheet.animation = ParticleSystemAnimationType.WholeSheet; sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0);
            sheet.startFrame = new ParticleSystem.MinMaxCurve(0, 15.99f);
            Noise(ps, .025f, .35f, .12f);
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-.25f, .25f);
            Size(ps, .55f, 1.15f);
            Fade(ps, .25f, .65f);
        });
        Prefab("PS_JadeMotes", mJade, 48, ps => { Noise(ps, .07f, .6f, .2f); Size(ps, .8f, .5f); Fade(ps, .15f, .7f); });
        Prefab("PS_GoldenMotes", mGold, 40, ps => { Noise(ps, .07f, .55f, .2f); Size(ps, .9f, .6f); Fade(ps, .2f, .7f); });
        // glow billboards sit on opaque crystals: shift them towards the camera (positive pivot z, in particle sizes) so
        // the crystal does not hide half of each glow
        Prefab("PS_PillarGlow", mGlow, 8, ps => ps.GetComponent<ParticleSystemRenderer>().pivot = new Vector3(0, 0, .5f));
        Prefab("PS_OrnamentGlow", mGlow, 4, ps => ps.GetComponent<ParticleSystemRenderer>().pivot = new Vector3(0, 0, .4f));
        Prefab("PS_ReleasePulse", mPulse, 2, ps =>
        {
            ps.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            Size(ps, .15f, 1f);
            Fade(ps, .15f, .45f);
        });
        Prefab("PS_ScriptureGlyphs", mGlyph, 16, ps =>
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
        Debug.Log("SPIRIT_V4_BUILD done");
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
        var e = ps.emission; e.rateOverTime = 0;  // SonMonSpiritGatheringV4 emits by script (EmitParams, no allocations)
        var shape = ps.shape; shape.enabled = false;
        var col = ps.colorOverLifetime; col.enabled = false;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.Off;
        r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        r.sortMode = ParticleSystemSortMode.Distance;  // back-to-front within each system
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

    static void Size(ParticleSystem ps, float start, float end)
    {
        var s = ps.sizeOverLifetime;
        s.enabled = true;
        s.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, start, 0, 0), new Keyframe(1, end, 0, 0)));
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
