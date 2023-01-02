using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace App.Editor
{
	public static class BatchBuild
	{
		public class Builder
		{
			public string outputRelativePath;
			public BuildTargetGroup targetGroup => BuildPipeline.GetBuildTargetGroup(targetPlatform);
			public BuildTarget targetPlatform;
			public int subTarget;
			public string[] scenesInBuild;
			public string[] extraScriptingDefines;

			public BuildReport Build(BuildOptions options = BuildOptions.None)
			{
				string path = GetOutputPath(outputRelativePath ?? Application.productName, targetGroup);
				string dir = Path.GetDirectoryName(path);
				if (!Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}

				var scenes = scenesInBuild ?? EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);
				var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
				{
					targetGroup = targetGroup,
					target = targetPlatform,
					subtarget = subTarget,
					locationPathName = path,
					scenes = scenes,
					extraScriptingDefines = extraScriptingDefines,
					options = options,
				});
				return report;
			}
		}

		public static string Dir = "Build";

		public class DefineSymbolScope : System.IDisposable
		{
			public string Symbols { get; private set; }
			public BuildTargetGroup TargetGroup { get; private set; }
			public DefineSymbolScope(BuildTargetGroup tg, string overwrite)
			{
				Symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(tg);
				TargetGroup = tg;
				PlayerSettings.SetScriptingDefineSymbolsForGroup(tg, overwrite);
			}
			public void Dispose()
			{
				PlayerSettings.SetScriptingDefineSymbolsForGroup(TargetGroup, Symbols);
			}
		}

		public static string GetOutputPath(string filename, BuildTargetGroup tg)
		{
			string ext = tg switch
			{
				BuildTargetGroup.Standalone => UnityEditor.WindowsStandalone.UserBuildSettings.createSolution ? ".sln" : ".exe",
				BuildTargetGroup.Android => ".apk",
				_ => "",
			};
			string path = Path.Combine(Dir, filename + ext);
			return path;
		}

		public static AddressablesPlayerBuildResult BuildAddressables(string profileName)
		{
			if (!string.IsNullOrEmpty(profileName))
			{
				var id = AddressableAssetSettingsDefaultObject.Settings.profileSettings.GetProfileId(profileName);
				if (string.IsNullOrEmpty(id))
				{
					throw new ArgumentException($"{nameof(profileName)} doesn't exist in profileSettings");
				}
			}
			AddressablesPlayerBuildResult result;
			AddressableAssetSettings.BuildPlayerContent(out result);
			return result;
		}

	}
}
