﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace YooAsset.Editor
{
	[Serializable]
	public class AssetBundleCollectorPackage
	{
		/// <summary>
		/// 包裹名称
		/// </summary>
		public string PackageName = string.Empty;

		/// <summary>
		/// 包裹描述
		/// </summary>
		public string PackageDesc = string.Empty;

		/// <summary>
		/// 分组列表
		/// </summary>
		public List<AssetBundleCollectorGroup> Groups = new List<AssetBundleCollectorGroup>();


		/// <summary>
		/// 检测配置错误
		/// </summary>
		public void CheckConfigError()
		{
			foreach (var group in Groups)
			{
				group.CheckConfigError();
			}
		}

		/// <summary>
		/// 修复配置错误
		/// </summary>
		public bool FixConfigError()
		{
			bool isFixed = false;
			foreach (var group in Groups)
			{
				if (group.FixConfigError())
				{
					isFixed = true;
				}
			}
			return isFixed;
		}

		/// <summary>
		/// 获取打包收集的资源文件
		/// </summary>
		public List<CollectAssetInfo> GetAllCollectAssets(EBuildMode buildMode, bool enableAddressable)
		{
			Dictionary<string, CollectAssetInfo> result = new Dictionary<string, CollectAssetInfo>(10000);

			// 收集打包资源
			foreach (var group in Groups)
			{
				var temper = group.GetAllCollectAssets(buildMode, enableAddressable);
				foreach (var assetInfo in temper)
				{
					if (result.ContainsKey(assetInfo.AssetPath) == false)
						result.Add(assetInfo.AssetPath, assetInfo);
					else
						throw new Exception($"The collecting asset file is existed : {assetInfo.AssetPath}");
				}
			}

			// 检测可寻址地址是否重复
			if (enableAddressable)
			{
				HashSet<string> adressTemper = new HashSet<string>();
				foreach (var collectInfoPair in result)
				{
					if (collectInfoPair.Value.CollectorType == ECollectorType.MainAssetCollector)
					{
						string address = collectInfoPair.Value.Address;
						if (adressTemper.Contains(address) == false)
							adressTemper.Add(address);
						else
							throw new Exception($"The address is existed : {address}");
					}
				}
			}

			// 返回列表
			return result.Values.ToList();
		}

		/// <summary>
		/// 获取所有的资源标签
		/// </summary>
		public List<string> GetAllTags()
		{
			HashSet<string> result = new HashSet<string>();
			foreach (var group in Groups)
			{
				List<string> groupTags = StringUtility.StringToStringList(group.AssetTags, ';');
				foreach (var tag in groupTags)
				{
					if (result.Contains(tag) == false)
						result.Add(tag);
				}

				foreach (var collector in group.Collectors)
				{
					List<string> collectorTags = StringUtility.StringToStringList(collector.AssetTags, ';');
					foreach (var tag in collectorTags)
					{
						if (result.Contains(tag) == false)
							result.Add(tag);
					}
				}
			}
			return result.ToList();
		}
	}
}