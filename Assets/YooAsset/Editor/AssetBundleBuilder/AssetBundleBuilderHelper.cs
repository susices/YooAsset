﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace YooAsset.Editor
{
	public static class AssetBundleBuilderHelper
	{
		/// <summary>
		/// 获取默认的输出根路录
		/// </summary>
		public static string GetDefaultOutputRoot()
		{
			string projectPath = EditorTools.GetProjectPath();
			return $"{projectPath}/Bundles";
		}

		/// <summary>
		/// 获取流文件夹路径
		/// </summary>
		public static string GetStreamingAssetsFolderPath()
		{
			return $"{Application.dataPath}/StreamingAssets/YooAssets/";
		}

		/// <summary>
		/// 清空流文件夹
		/// </summary>
		public static void ClearStreamingAssetsFolder()
		{
			string streamingFolderPath = GetStreamingAssetsFolderPath();
			EditorTools.ClearFolder(streamingFolderPath);
		}

		/// <summary>
		/// 删除流文件夹内无关的文件
		/// 删除.manifest文件和.meta文件
		/// </summary>
		public static void DeleteStreamingAssetsIgnoreFiles()
		{
			string streamingFolderPath = GetStreamingAssetsFolderPath();
			if (Directory.Exists(streamingFolderPath))
			{
				string[] files = Directory.GetFiles(streamingFolderPath, "*.manifest", SearchOption.AllDirectories);
				foreach (var file in files)
				{
					FileInfo info = new FileInfo(file);
					info.Delete();
				}

				files = Directory.GetFiles(streamingFolderPath, "*.meta", SearchOption.AllDirectories);
				foreach (var item in files)
				{
					FileInfo info = new FileInfo(item);
					info.Delete();
				}
			}
		}

		/// <summary>
		/// 获取构建管线的输出目录
		/// </summary>
		public static string MakePipelineOutputDirectory(string outputRoot, string buildPackage, BuildTarget buildTarget, EBuildMode buildMode)
		{
			string result = $"{outputRoot}/{buildPackage}/{buildTarget}/{YooAssetSettings.OutputFolderName}";
			if (buildMode == EBuildMode.DryRunBuild)
				result += $"_{EBuildMode.DryRunBuild}";
			else if (buildMode == EBuildMode.SimulateBuild)
				result += $"_{EBuildMode.SimulateBuild}";
			return result;
		}

		/// <summary>
		/// 加载补丁清单文件
		/// </summary>
		internal static PatchManifest LoadPatchManifestFile(string fileDirectory, string packageName, string packageCRC)
		{
			string filePath = $"{fileDirectory}/{YooAssetSettingsData.GetPatchManifestFileName(packageName, packageCRC)}";
			if (File.Exists(filePath) == false)
			{
				throw new System.Exception($"Not found patch manifest file : {filePath}");
			}

			string jsonData = FileUtility.ReadFile(filePath);
			return PatchManifest.Deserialize(jsonData);
		}
	}
}