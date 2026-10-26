using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;
using System.Linq;
public static class VisualSliceBuild
{
    [MenuItem("Tu Tien/Visual Slice/Build Windows preview")]
    public static void Build()
    {
        Directory.CreateDirectory("Assets/VisualSlice");
        var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/VisualSlice/JadeMountainURP.asset");
        if(pipeline==null){var renderer=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(renderer,"Assets/VisualSlice/JadeMountainRenderer.asset");pipeline=UniversalRenderPipelineAsset.Create(renderer);AssetDatabase.CreateAsset(pipeline,"Assets/VisualSlice/JadeMountainURP.asset");}
        pipeline.name="Jade Mountain · URP";pipeline.supportsHDR=true;pipeline.msaaSampleCount=2;pipeline.shadowDistance=65;pipeline.mainLightShadowmapResolution=2048;
        var quality=new SerializedObject(pipeline);quality.FindProperty("m_MainLightShadowsSupported").boolValue=true;quality.FindProperty("m_AdditionalLightShadowsSupported").boolValue=false;quality.FindProperty("m_AdditionalLightsRenderingMode").intValue=0;quality.FindProperty("m_SoftShadowsSupported").boolValue=true;quality.FindProperty("m_ShadowCascadeCount").intValue=2;quality.ApplyModifiedPropertiesWithoutUndo();pipeline.supportsCameraOpaqueTexture=false;pipeline.supportsCameraDepthTexture=false;
        GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;QualitySettings.vSyncCount=0;PlayerSettings.colorSpace=ColorSpace.Linear;
        var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var list=settings.FindProperty("m_AlwaysIncludedShaders");
        foreach(string name in new[]{"Universal Render Pipeline/Lit","Universal Render Pipeline/Simple Lit","Universal Render Pipeline/Unlit","Universal Render Pipeline/Particles/Unlit"}){
            var shader=Shader.Find(name);if(shader==null)throw new System.Exception("Missing URP shader "+name);bool found=false;for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;if(!found){int i=list.arraySize;list.InsertArrayElementAtIndex(i);list.GetArrayElementAtIndex(i).objectReferenceValue=shader;}
        }
        settings.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);AssetDatabase.SaveAssets();ProjectBuild.Build();
    }
}
