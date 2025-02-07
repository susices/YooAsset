﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace YooAsset
{
	internal class HostPlayModeImpl : IBundleServices
	{
		// 补丁清单
		internal PatchManifest LocalPatchManifest { private set; get; }

		// 参数相关
		private bool _locationToLower;
		private string _defaultHostServer;
		private string _fallbackHostServer;
		private IQueryServices _queryServices;

		/// <summary>
		/// 异步初始化
		/// </summary>
		public InitializationOperation InitializeAsync(bool locationToLower, string defaultHostServer, string fallbackHostServer, IQueryServices queryServices)
		{
			_locationToLower = locationToLower;
			_defaultHostServer = defaultHostServer;
			_fallbackHostServer = fallbackHostServer;
			_queryServices = queryServices;

			var operation = new HostPlayModeInitializationOperation();
			OperationSystem.StartOperation(operation);
			return operation;
		}

		/// <summary>
		/// 异步更新资源版本号
		/// </summary>
		public UpdateStaticVersionOperation UpdateStaticVersionAsync(string packageName, int timeout)
		{
			var operation = new HostPlayModeUpdateStaticVersionOperation(this, packageName, timeout);
			OperationSystem.StartOperation(operation);
			return operation;
		}

		/// <summary>
		/// 异步更新补丁清单
		/// </summary>
		public UpdateManifestOperation UpdatePatchManifestAsync(string packageName, string packageCRC, int timeout)
		{
			var operation = new HostPlayModeUpdateManifestOperation(this, packageName, packageCRC, timeout);
			OperationSystem.StartOperation(operation);
			return operation;
		}

		/// <summary>
		/// 异步更新补丁清单（弱联网）
		/// </summary>
		public UpdateManifestOperation WeaklyUpdatePatchManifestAsync(string packageName, string packageCRC)
		{
			var operation = new HostPlayModeWeaklyUpdateManifestOperation(this, packageName, packageCRC);
			OperationSystem.StartOperation(operation);
			return operation;
		}

		/// <summary>
		/// 异步更新资源包裹
		/// </summary>
		public UpdatePackageOperation UpdatePackageAsync(string packageName, string packageCRC, int timeout)
		{
			var operation = new HostPlayModeUpdatePackageOperation(this, packageName, packageCRC, timeout);
			OperationSystem.StartOperation(operation);
			return operation;
		}

		/// <summary>
		/// 创建下载器
		/// </summary>
		public PatchDownloaderOperation CreatePatchDownloaderByAll(int fileLoadingMaxNumber, int failedTryAgain)
		{
			List<BundleInfo> downloadList = GetDownloadListByAll();
			var operation = new PatchDownloaderOperation(downloadList, fileLoadingMaxNumber, failedTryAgain);
			return operation;
		}
		private List<BundleInfo> GetDownloadListByAll()
		{
			List<PatchBundle> downloadList = new List<PatchBundle>(1000);
			foreach (var patchBundle in LocalPatchManifest.BundleList)
			{
				// 忽略缓存文件
				if (CacheSystem.IsCached(patchBundle))
					continue;

				// 忽略APP资源
				if (IsBuildinPatchBundle(patchBundle))
					continue;

				downloadList.Add(patchBundle);
			}

			return ConvertToDownloadList(downloadList);
		}

		/// <summary>
		/// 创建下载器
		/// </summary>
		public PatchDownloaderOperation CreatePatchDownloaderByTags(string[] tags, int fileLoadingMaxNumber, int failedTryAgain)
		{
			List<BundleInfo> downloadList = GetDownloadListByTags(tags);
			var operation = new PatchDownloaderOperation(downloadList, fileLoadingMaxNumber, failedTryAgain);
			return operation;
		}
		private List<BundleInfo> GetDownloadListByTags(string[] tags)
		{
			List<PatchBundle> downloadList = new List<PatchBundle>(1000);
			foreach (var patchBundle in LocalPatchManifest.BundleList)
			{
				// 忽略缓存文件
				if (CacheSystem.IsCached(patchBundle))
					continue;

				// 忽略APP资源
				if (IsBuildinPatchBundle(patchBundle))
					continue;

				// 如果未带任何标记，则统一下载
				if (patchBundle.HasAnyTags() == false)
				{
					downloadList.Add(patchBundle);
				}
				else
				{
					// 查询DLC资源
					if (patchBundle.HasTag(tags))
					{
						downloadList.Add(patchBundle);
					}
				}
			}

			return ConvertToDownloadList(downloadList);
		}

		/// <summary>
		/// 创建下载器
		/// </summary>
		public PatchDownloaderOperation CreatePatchDownloaderByPaths(AssetInfo[] assetInfos, int fileLoadingMaxNumber, int failedTryAgain)
		{
			List<BundleInfo> downloadList = GetDownloadListByPaths(assetInfos);
			var operation = new PatchDownloaderOperation(downloadList, fileLoadingMaxNumber, failedTryAgain);
			return operation;
		}
		private List<BundleInfo> GetDownloadListByPaths(AssetInfo[] assetInfos)
		{
			// 获取资源对象的资源包和所有依赖资源包
			List<PatchBundle> checkList = new List<PatchBundle>();
			foreach (var assetInfo in assetInfos)
			{
				if (assetInfo.IsInvalid)
				{
					YooLogger.Warning(assetInfo.Error);
					continue;
				}

				// 注意：如果补丁清单里未找到资源包会抛出异常！
				PatchBundle mainBundle = LocalPatchManifest.GetMainPatchBundle(assetInfo.AssetPath);
				if (checkList.Contains(mainBundle) == false)
					checkList.Add(mainBundle);

				// 注意：如果补丁清单里未找到资源包会抛出异常！
				PatchBundle[] dependBundles = LocalPatchManifest.GetAllDependencies(assetInfo.AssetPath);
				foreach (var dependBundle in dependBundles)
				{
					if (checkList.Contains(dependBundle) == false)
						checkList.Add(dependBundle);
				}
			}

			List<PatchBundle> downloadList = new List<PatchBundle>(1000);
			foreach (var patchBundle in checkList)
			{
				// 忽略缓存文件
				if (CacheSystem.IsCached(patchBundle))
					continue;

				// 忽略APP资源
				if (IsBuildinPatchBundle(patchBundle))
					continue;

				downloadList.Add(patchBundle);
			}

			return ConvertToDownloadList(downloadList);
		}

		/// <summary>
		/// 创建解压器
		/// </summary>
		public PatchUnpackerOperation CreatePatchUnpackerByTags(string[] tags, int fileUpackingMaxNumber, int failedTryAgain)
		{
			List<BundleInfo> unpcakList = GetUnpackListByTags(tags);
			var operation = new PatchUnpackerOperation(unpcakList, fileUpackingMaxNumber, failedTryAgain);
			return operation;
		}
		private List<BundleInfo> GetUnpackListByTags(string[] tags)
		{
			List<PatchBundle> downloadList = new List<PatchBundle>(1000);
			foreach (var patchBundle in LocalPatchManifest.BundleList)
			{
				// 忽略缓存文件
				if (CacheSystem.IsCached(patchBundle))
					continue;

				// 查询DLC资源
				if (IsBuildinPatchBundle(patchBundle))
				{
					if (patchBundle.HasTag(tags))
					{
						downloadList.Add(patchBundle);
					}
				}
			}

			return PatchHelper.ConvertToUnpackList(downloadList);
		}

		/// <summary>
		/// 创建解压器
		/// </summary>
		public PatchUnpackerOperation CreatePatchUnpackerByAll(int fileUpackingMaxNumber, int failedTryAgain)
		{
			List<BundleInfo> unpcakList = GetUnpackListByAll();
			var operation = new PatchUnpackerOperation(unpcakList, fileUpackingMaxNumber, failedTryAgain);
			return operation;
		}
		private List<BundleInfo> GetUnpackListByAll()
		{
			List<PatchBundle> downloadList = new List<PatchBundle>(1000);
			foreach (var patchBundle in LocalPatchManifest.BundleList)
			{
				// 忽略缓存文件
				if (CacheSystem.IsCached(patchBundle))
					continue;

				if (IsBuildinPatchBundle(patchBundle))
				{
					downloadList.Add(patchBundle);
				}
			}

			return PatchHelper.ConvertToUnpackList(downloadList);
		}

		// WEB相关
		public string GetPatchDownloadMainURL(string fileName)
		{
			return $"{_defaultHostServer}/{fileName}";
		}
		public string GetPatchDownloadFallbackURL(string fileName)
		{
			return $"{_fallbackHostServer}/{fileName}";
		}

		// 下载相关
		public List<BundleInfo> ConvertToDownloadList(List<PatchBundle> downloadList)
		{
			List<BundleInfo> result = new List<BundleInfo>(downloadList.Count);
			foreach (var patchBundle in downloadList)
			{
				var bundleInfo = ConvertToDownloadInfo(patchBundle);
				result.Add(bundleInfo);
			}
			return result;
		}
		public BundleInfo ConvertToDownloadInfo(PatchBundle patchBundle)
		{
			string remoteMainURL = GetPatchDownloadMainURL(patchBundle.FileName);
			string remoteFallbackURL = GetPatchDownloadFallbackURL(patchBundle.FileName);
			BundleInfo bundleInfo = new BundleInfo(patchBundle, BundleInfo.ELoadMode.LoadFromRemote, remoteMainURL, remoteFallbackURL);
			return bundleInfo;
		}

		internal void SetLocalPatchManifest(PatchManifest patchManifest)
		{
			LocalPatchManifest = patchManifest;
			LocalPatchManifest.InitAssetPathMapping(_locationToLower);
		}
		internal bool IsBuildinPatchBundle(PatchBundle patchBundle)
		{
			return _queryServices.QueryStreamingAssets(patchBundle.FileName);
		}

		#region IBundleServices接口
		private BundleInfo CreateBundleInfo(PatchBundle patchBundle)
		{
			if (patchBundle == null)
				throw new Exception("Should never get here !");

			// 查询沙盒资源
			if (CacheSystem.IsCached(patchBundle))
			{
				BundleInfo bundleInfo = new BundleInfo(patchBundle, BundleInfo.ELoadMode.LoadFromCache);
				return bundleInfo;
			}

			// 查询APP资源
			if (IsBuildinPatchBundle(patchBundle))
			{
				BundleInfo bundleInfo = new BundleInfo(patchBundle, BundleInfo.ELoadMode.LoadFromStreaming);
				return bundleInfo;
			}

			// 从服务端下载
			return ConvertToDownloadInfo(patchBundle);
		}
		BundleInfo IBundleServices.GetBundleInfo(AssetInfo assetInfo)
		{
			if (assetInfo.IsInvalid)
				throw new Exception("Should never get here !");

			// 注意：如果补丁清单里未找到资源包会抛出异常！
			var patchBundle = LocalPatchManifest.GetMainPatchBundle(assetInfo.AssetPath);
			return CreateBundleInfo(patchBundle);
		}
		BundleInfo[] IBundleServices.GetAllDependBundleInfos(AssetInfo assetInfo)
		{
			if (assetInfo.IsInvalid)
				throw new Exception("Should never get here !");

			// 注意：如果补丁清单里未找到资源包会抛出异常！
			var depends = LocalPatchManifest.GetAllDependencies(assetInfo.AssetPath);
			List<BundleInfo> result = new List<BundleInfo>(depends.Length);
			foreach (var patchBundle in depends)
			{
				BundleInfo bundleInfo = CreateBundleInfo(patchBundle);
				result.Add(bundleInfo);
			}
			return result.ToArray();
		}
		AssetInfo[] IBundleServices.GetAssetInfos(string[] tags)
		{
			return PatchHelper.GetAssetsInfoByTags(LocalPatchManifest, tags);
		}
		PatchAsset IBundleServices.TryGetPatchAsset(string assetPath)
		{
			if (LocalPatchManifest.TryGetPatchAsset(assetPath, out PatchAsset patchAsset))
				return patchAsset;
			else
				return null;
		}
		string IBundleServices.MappingToAssetPath(string location)
		{
			return LocalPatchManifest.MappingToAssetPath(location);
		}
		string IBundleServices.GetPackageName()
		{
			return LocalPatchManifest.PackageName;
		}
		#endregion
	}
}