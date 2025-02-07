﻿using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace YooAsset.Editor
{
	[TaskAttribute("资源构建准备工作")]
	public class TaskPrepare : IBuildTask
	{
		void IBuildTask.Run(BuildContext context)
		{
			var buildParametersContext = context.GetContextObject<BuildParametersContext>();
			buildParametersContext.BeginWatch();

			var buildParameters = buildParametersContext.Parameters;

			// 检测构建参数合法性
			if (buildParameters.BuildTarget == BuildTarget.NoTarget)
				throw new Exception("请选择目标平台");
			if (string.IsNullOrEmpty(buildParameters.BuildPackage))
				throw new Exception("包裹名称不能为空");

			if (buildParameters.BuildMode != EBuildMode.SimulateBuild)
			{
				// 检测当前是否正在构建资源包
				if (BuildPipeline.isBuildingPlayer)
					throw new Exception("当前正在构建资源包，请结束后再试");

				// 检测是否有未保存场景
				if (EditorTools.HasDirtyScenes())
					throw new Exception("检测到未保存的场景文件");

				// 保存改动的资源
				AssetDatabase.SaveAssets();
			}
			if (buildParameters.BuildMode == EBuildMode.ForceRebuild)
			{
				// 删除平台总目录
				string platformDirectory = $"{buildParameters.OutputRoot}/{buildParameters.BuildPackage}/{buildParameters.BuildTarget}";
				if (EditorTools.DeleteDirectory(platformDirectory))
				{
					BuildRunner.Log($"删除平台总目录：{platformDirectory}");
				}
			}

			// 如果输出目录不存在
			string pipelineOutputDirectory = buildParametersContext.GetPipelineOutputDirectory();
			if (EditorTools.CreateDirectory(pipelineOutputDirectory))
			{
				BuildRunner.Log($"创建输出目录：{pipelineOutputDirectory}");
			}
		}
	}
}