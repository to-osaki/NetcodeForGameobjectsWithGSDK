using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace App
{
	public static class BuildPostProcess
	{
		[PostProcessBuild(1)]
		public static void PostProcess(BuildTarget target, string pathToBuildProject)
		{
			UnityEngine.Debug.Log($"BuildPostProcess:{target}:{pathToBuildProject}");

			var processStartInfo = new ProcessStartInfo("Bat/Zip.bat");
			processStartInfo.WorkingDirectory = Path.GetDirectoryName(Application.dataPath);
			processStartInfo.CreateNoWindow = true;
			processStartInfo.UseShellExecute = false;
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.RedirectStandardError = true;

			using (var process = Process.Start(processStartInfo))
			{
				string standardOutput = process.StandardOutput.ReadToEnd();
				string standardError = process.StandardError.ReadToEnd();

				process.WaitForExit();
				int exitCode = process.ExitCode;
				if (exitCode == 0)
				{
					UnityEngine.Debug.Log(standardOutput);
				}
				else
				{
					UnityEngine.Debug.LogError(standardError);
				}
			}
		}
	}
}
