using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Validates the generated Level 6 v4 Shader Graph assets (Tools/ShaderGraph/sg_build.py): imports them, then reports
// compile errors / warnings and the exposed properties of the generated shader.
// Run: Unity -batchmode -projectPath <p> -executeMethod SpiritShaderGraphCheck.Run -quit
public static class SpiritShaderGraphCheck
{
    public static void Run()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (var path in Directory.GetFiles("Assets/Art/SonMon/VFX/L6/Shaders", "*.shadergraph"))
        {
            var p = path.Replace(Path.DirectorySeparatorChar, '/');
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(p);
            if (shader == null) { Debug.Log($"SG_CHECK {p} FAILED: no shader produced"); continue; }
            var msgs = ShaderUtil.GetShaderMessages(shader);
            var props = Enumerable.Range(0, shader.GetPropertyCount()).Select(i => shader.GetPropertyName(i));
            Debug.Log($"SG_CHECK {p} name={shader.name} errors={ShaderUtil.ShaderHasError(shader)} supported={shader.isSupported} passes={shader.passCount} messages={msgs.Length} props={string.Join(",", props)}");
            foreach (var m in msgs) Debug.Log($"SG_CHECK_MSG {p} {m.severity} {m.message} line {m.line}");
        }
    }
}
