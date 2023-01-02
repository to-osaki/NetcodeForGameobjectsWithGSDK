using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace App.Editor
{
	public static class CommandBuild
	{
		[MenuItem(itemName: "App/CommandBuild &b")]
		public static void _MenuItem()
		{
			var result = BuildServer();
			if (result.summary.result == BuildResult.Succeeded)
			{
				Debug.Log("Build Succeeded");
			}
			else if (result.summary.result == BuildResult.Cancelled)
			{
				Debug.LogWarning("Build Cancelled");
			}
			else
			{
				Debug.LogError($"Build Failed with {result.summary}");
			}
		}

		public static void Build()
		{
			var result = BuildServer();
			EditorApplication.Exit(result.summary.result == BuildResult.Succeeded ? 0 : 1);
		}

		private static BuildReport BuildServer()
		{
			var builder = new BatchBuild.Builder()
			{
				targetPlatform = BuildTarget.StandaloneWindows64,
				subTarget = (int)StandaloneBuildSubtarget.Server,
				outputRelativePath = "DedicatedServer/DedicatedServer"
			};
			return builder.Build();
		}
	}
}