using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

public static class AndroidBuild
{
    [MenuItem("Làng Tro Tàn/Run checks and build Android APK")]
    public static void Build()
    {
        try
        {
            // User requires the complete native 3D port before any further APK is produced.
            FullPortReleaseGate.Verify();
            ProjectBuild.RunChecks();
            PlayerSettings.companyName="LangTroTan";
            PlayerSettings.productName="Làng Tro Tàn";
            PlayerSettings.bundleVersion="0.4.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.langtrotan.prototype");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.bundleVersionCode=5;
            PlayerSettings.Android.useCustomKeystore=false;
            PlayerSettings.Android.splitApplicationBinary=false;
            PlayerSettings.Android.buildApkPerCpuArchitecture=false;
            PlayerSettings.Android.forceInternetPermission=true;
            PlayerSettings.Android.renderOutsideSafeArea=true;
            PlayerSettings.Android.optimizedFramePacing=true;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait=false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            PlayerSettings.runInBackground=false;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});
            var ps=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input=ps.FindProperty("activeInputHandler");if(input!=null){input.intValue=0;ps.ApplyModifiedPropertiesWithoutUndo();}
            var gs=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var always=gs.FindProperty("m_AlwaysIncludedShaders");
            foreach(var name in new[]{"Standard","Sprites/Default"})
            {
                var shader=Shader.Find(name);bool found=false;
                for(int i=0;i<always.arraySize;i++)if(always.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;
                if(shader!=null&&!found){int i=always.arraySize;always.InsertArrayElementAtIndex(i);always.GetArrayElementAtIndex(i).objectReferenceValue=shader;}
            }
            gs.ApplyModifiedPropertiesWithoutUndo();
            if(!File.Exists("Assets/Scenes/Village.unity"))throw new FileNotFoundException("Missing Village scene");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Village.unity",true)};
            EditorUserBuildSettings.buildAppBundle=false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject=false;
            AssetDatabase.SaveAssets();
            string root=Directory.GetParent(Application.dataPath).FullName;
            string apk=Path.Combine(root,"../LangTroTan_Android_0.4.0.apk");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Village.unity"},target=BuildTarget.Android,locationPathName=apk,options=BuildOptions.None});
            File.WriteAllText(Path.Combine(root,"Reports/AndroidBuild.txt"),
                "Result: "+report.summary.result+"\nVersion: 0.4.0 (5)\nPackage: com.langtrotan.prototype\nABI: arm64-v8a\nMinimum: Android 10 API 29\nGraphics: OpenGLES3\nSigning: Unity debug key for local testing\nSize: "+report.summary.totalSize+" bytes\nErrors: "+report.summary.totalErrors+"\n");
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Android build failed, errors: "+report.summary.totalErrors);
            Debug.Log("LANG_TRO_TAN_ANDROID_OK "+Path.GetFullPath(apk));
        }
        catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
    }
}
