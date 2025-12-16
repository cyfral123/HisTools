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

        public Option<Texture2D> GetTexture(string spriteName)
        {
            var result = _loadedAssets.TryGetValue(spriteName, out var cachedAsset)
                ? Option.Some((Texture2D)cachedAsset)
                : Option<Texture2D>.None();

            if (result.TryGet(out var sprite))
                return Option.Some(sprite);

            Utils.Logger.Error($"PrefabDatabase: Sprite {spriteName} not found");
            return result;
        }

        public Option<GameObject> GetObject(string prefabName, bool active)
        {
            var result = _loadedAssets.TryGetValue(prefabName, out var cachedAsset)
                ? Option.Some((GameObject)cachedAsset)
                : Option<GameObject>.None();

            if (result.TryGet(out var prefab))
            {
                prefab.SetActive(active);
                return Option.Some(prefab);
            }

            Utils.Logger.Error($"PrefabDatabase: Prefab {prefabName} not found");
            return result;
        }

        public Option<AssetBundle> LoadBundle(string bundleName)
        {
            if (_loadedBundles.TryGetValue(bundleName, out var bundle))
                return Option.Some(bundle);

            var bundlePath = Path.Combine(Constants.Paths.PluginDllDir, "Assets", bundleName);

            if (!File.Exists(bundlePath))
            {
                var fallbackPath = Path.Combine(Constants.Paths.PluginDllDir, bundleName);
                if (!File.Exists(fallbackPath))
                    return Option<AssetBundle>.None();

                bundlePath = fallbackPath;
            }

            bundle = AssetBundle.LoadFromFile(bundlePath);

            if (!bundle)
                return Option<AssetBundle>.None();

            _loadedBundles[bundleName] = bundle;
            return Option.Some(bundle);
        }


        public Option<T> LoadAsset<T>(string bundleName, string assetName) where T : Object
        {
            var cacheKey = $"{bundleName}/{assetName}";

            if (_loadedAssets.TryGetValue(cacheKey, out var cachedAsset))
                return Option.Some((T)cachedAsset);

            var bundleOpt = LoadBundle(bundleName);

            if (!bundleOpt.TryGet(out var bundle))
                return Option<T>.None();

            var asset = bundle.LoadAsset<T>(assetName);

            if (!asset)
                return Option<T>.None();

            _loadedAssets[cacheKey] = asset;

            return Option.Some(asset);
        }
    }
}