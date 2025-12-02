using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HisTools.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HisTools.Prefabs
{
    public class PrefabDatabase
    {
        private static PrefabDatabase _instance;
        public static PrefabDatabase Instance => _instance ??= new PrefabDatabase();

        private readonly Dictionary<string, AssetBundle> _loadedBundles = new();
        private readonly Dictionary<string, Object> _loadedAssets = new();

        public Option<GameObject> GetPrefab(string prefabName, bool active)
        {
            var result = _loadedAssets.TryGetValue(prefabName, out var cachedAsset)
                ? Option<GameObject>.Some((GameObject)cachedAsset)
                : Option<GameObject>.None();

            if (result.TryGet(out var prefab))
            {
                prefab.SetActive(active);
                return Option<GameObject>.Some(prefab);
            }
            
            Utils.Logger.Error($"PrefabDatabase: Prefab {prefabName} not found");
            return result;
        }

        public Option<AssetBundle> LoadBundle(string bundleName)
        {
            if (_loadedBundles.TryGetValue(bundleName, out var bundle))
                return Option<AssetBundle>.Some(bundle);

            var bundlePath = Path.Combine(Plugin.PluginDllDir, "Assets", bundleName);

            if (!File.Exists(bundlePath))
                return Option<AssetBundle>.None();

            bundle = AssetBundle.LoadFromFile(bundlePath);

            if (!bundle)
                return Option<AssetBundle>.None();

            _loadedBundles[bundleName] = bundle;
            return Option<AssetBundle>.Some(bundle);
        }


        public Option<T> LoadAsset<T>(string bundleName, string assetName) where T : Object
        {
            var cacheKey = $"{bundleName}/{assetName}";

            if (_loadedAssets.TryGetValue(cacheKey, out var cachedAsset))
                return Option<T>.Some((T)cachedAsset);

            var bundleOpt = LoadBundle(bundleName);

            if (!bundleOpt.TryGet(out var bundle))
                return Option<T>.None();

            var asset = bundle.LoadAsset<T>(assetName);

            if (!asset)
                return Option<T>.None();

            _loadedAssets[cacheKey] = asset;

            return Option<T>.Some(asset);
        }
    }
}