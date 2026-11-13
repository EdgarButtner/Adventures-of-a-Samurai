using System.IO;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor
{
    internal static class PlayModeChangesSaverPaths
    {
        private const string RootFolderName = "PlayModeChangesSaver";
        private const string ScriptableObjectsFolderName = "Scriptable_Objects";
        private const string AsmdefFileName = "PlayModeChangesSaver.Editor.asmdef";
        private const string MarkerScriptFileName = "PlayModeChangesSaverPaths.cs";

        private static string NormalizePath(string path)
        {
            return path?.Replace("\\", "/");
        }

        /// <summary>
        ///     Returns the store asset of type <typeparamref name="T" />, using <paramref name="cache" /> while the
        ///     cached asset is alive. Store lookups happen during GUI repaints, so this avoids a project-wide
        ///     search per call.
        /// </summary>
        internal static T FindStoreAsset<T>(ref T cache) where T : ScriptableObject
        {
            if (cache)
            {
                return cache;
            }

            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                // Another package may declare a type with the same name; only accept ours.
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset)
                {
                    cache = asset;
                    return asset;
                }
            }

            return null;
        }

        internal static T CreateStoreAsset<T>(ref T cache, string fileName) where T : ScriptableObject
        {
            var store = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(store, $"{GetScriptableObjectsFolder()}/{fileName}");
            AssetDatabase.SaveAssets();
            // Cache only a persisted store; otherwise the next call retries instead of using an unsaved instance.
            if (AssetDatabase.Contains(store))
            {
                cache = store;
            }

            return store;
        }

        private static string GetRootFolder()
        {
            // Locate any known asset (asmdef preferred) and walk up to the PlayModeChangesSaver folder,
            // so the stores follow the package when it is moved (e.g. into Assets/Plugins).
            string[] asmdefGuids = AssetDatabase.FindAssets("PlayModeChangesSaver.Editor t:AssemblyDefinitionAsset");
            string candidatePath = TryResolveRootFromGuids(asmdefGuids);

            if (string.IsNullOrEmpty(candidatePath))
            {
                string[] scriptGuids = AssetDatabase.FindAssets("PlayModeChangesSaverPaths t:Script");
                candidatePath = TryResolveRootFromGuids(scriptGuids);
            }

            if (!string.IsNullOrEmpty(candidatePath))
            {
                return candidatePath;
            }

            // Fallbacks: prefer Assets/PlayModeChangesSaver if present, otherwise Assets.
            if (AssetDatabase.IsValidFolder("Assets/" + RootFolderName))
            {
                return $"Assets/{RootFolderName}";
            }

            return "Assets";
        }

        public static string GetScriptableObjectsFolder()
        {
            string root = GetRootFolder();
            string target = $"{root}/{ScriptableObjectsFolderName}";
            EnsureFolderExists(target);
            return target;
        }

        private static string TryResolveRootFromGuids(string[] guids)
        {
            if (guids is not { Length: > 0 })
            {
                return null;
            }

            foreach (string guid in guids)
            {
                if (TryGetRootFromGuid(guid, out string root))
                {
                    return root;
                }
            }

            return null;
        }

        private static bool TryGetRootFromGuid(string guid, out string root)
        {
            root = null;
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            // FindAssets matches names partially; only accept this package's own marker files.
            string fileName = Path.GetFileName(assetPath);
            if (fileName != AsmdefFileName && fileName != MarkerScriptFileName)
            {
                return false;
            }

            string directory = NormalizePath(Path.GetDirectoryName(assetPath));
            root = FindRootFolderInAncestors(directory);

            // The root folder may have been renamed: use the folder that contains Editor/.
            // Only inside Assets: an installed UPM package folder is read-only.
            if (string.IsNullOrEmpty(root) && Path.GetFileName(directory) == "Editor" &&
                directory.StartsWith("Assets/"))
            {
                root = NormalizePath(Path.GetDirectoryName(directory));
            }

            return !string.IsNullOrEmpty(root);
        }

        private static string FindRootFolderInAncestors(string directory)
        {
            while (!string.IsNullOrEmpty(directory) && directory.StartsWith("Assets"))
            {
                string folderName = Path.GetFileName(directory);
                if (folderName == RootFolderName)
                {
                    return directory;
                }

                directory = NormalizePath(Path.GetDirectoryName(directory));
            }

            return null;
        }

        private static void EnsureFolderExists(string folderPath)
        {
            folderPath = NormalizePath(folderPath);
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = NormalizePath(Path.GetDirectoryName(folderPath));
            string leaf = Path.GetFileName(folderPath);

            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(parent))
            {
                return;
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}