using System;
using System.Collections.Generic;
using System.Linq;
using PlayModeChangesSaver.Editor.ChangesTracker.PlayModeFlow.SceneApplyHelper;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlayModeChangesSaver.Editor.ChangesTracker.PlayModeFlow
{
    public static class SceneApplyProcessor
    {
        private static readonly Dictionary<string, Action<Transform, RectTransform, ChangesStore.TransformChange>>
            TransformPropertyAppliers =
                new()
                {
                    { "position", (t, _, change) => t.localPosition = change.position },
                    { "rotation", (t, _, change) => t.localRotation = change.rotation },
                    { "scale", (t, _, change) => t.localScale = change.scale },
                    {
                        "anchoredPosition", (_, rt, change) =>
                        {
                            if (rt)
                            {
                                rt.anchoredPosition = change.anchoredPosition;
                            }
                        }
                    },
                    {
                        "anchoredPosition3D", (_, rt, change) =>
                        {
                            if (rt)
                            {
                                rt.anchoredPosition3D = change.anchoredPosition3D;
                            }
                        }
                    },
                    {
                        "anchorMin", (_, rt, change) =>
                        {
                            if (rt)
                            {
                                rt.anchorMin = change.anchorMin;
                            }
                        }
                    },
                    {
                        "anchorMax", (_, rt, change) =>
                        {
                            if (rt)
                            {
                                rt.anchorMax = change.anchorMax;
                            }
                        }
                    },
                    {
                        "pivot", (_, rt, change) =>
                        {
                            if (rt)
                            {
                                rt.pivot = change.pivot;
                            }
                        }
                    },
                    {
                        "sizeDelta", (_, rt, change) =>
                        {
                            if (rt)
                            {
                                rt.sizeDelta = change.sizeDelta;
                            }
                        }
                    },
                    {
                        "offsetMin", (_, rt, change) =>
                        {
                            if (rt)
                            {
                                rt.offsetMin = change.offsetMin;
                            }
                        }
                    },
                    {
                        "offsetMax", (_, rt, change) =>
                        {
                            if (rt)
                            {
                                rt.offsetMax = change.offsetMax;
                            }
                        }
                    }
                };


        public static void ProcessNextSceneInQueue(List<string> remainingScenes, SceneApplyContext context)
        {
            try
            {
                ProcessNextSceneInQueueUnsafe(remainingScenes, context);
            }
            catch (Exception e)
            {
                // Without this the flow would stay "processing" and never run again in this session.
                PlayModeOverrideFlow.StopProcessing();
                Debug.LogException(e);
            }
        }

        private static void ProcessNextSceneInQueueUnsafe(List<string> remainingScenes, SceneApplyContext context)
        {
            if (TryFinishQueue(remainingScenes, context))
            {
                return;
            }

            var currentPath = PopNextScene(remainingScenes);

            // A loaded scene is applied where it is, active or not, so a multi-scene setup stays as it is.
            if (!IsSceneLoaded(currentPath))
            {
                HandleSceneSwitch(currentPath, remainingScenes, context);
                return;
            }

            PromptApplyOrDiscard(currentPath, remainingScenes, context);
        }

        private static bool IsSceneLoaded(string scenePath)
        {
            var scene = SceneManager.GetSceneByPath(scenePath);
            return scene.IsValid() && scene.isLoaded;
        }

        /// <summary>
        ///     Runs the next step of the flow on a later editor update. An exception there ends the flow, so
        ///     it does not stay "processing" for the rest of the session.
        /// </summary>
        private static void ContinueLater(Action step)
        {
            ExitFlowPrompts.Defer(() =>
            {
                try
                {
                    step();
                }
                catch (Exception e)
                {
                    PlayModeOverrideFlow.StopProcessing();
                    Debug.LogException(e);
                }
            });
        }

        private static bool TryFinishQueue(List<string> remainingScenes, SceneApplyContext context)
        {
            if (remainingScenes.Count > 0)
            {
                return false;
            }

            CloseScenesOpenedForApply(context);
            return true;
        }

        private static string PopNextScene(List<string> remainingScenes)
        {
            var currentPath = remainingScenes[0];
            remainingScenes.RemoveAt(0);
            return currentPath;
        }

        private static void HandleSceneSwitch(string targetPath, List<string> remainingScenes,
            SceneApplyContext context)
        {
            if (!CanOpenScene(targetPath))
            {
                // Unsaved (untitled) scenes cannot be reopened; their changes stay in the stores.
                Debug.LogWarning($"[Play Mode Changes Saver] Cannot open scene '{targetPath}', skipping its overrides.");
                ContinueLater(() => ProcessNextSceneInQueue(remainingScenes, context));
                return;
            }

            // The scene is opened next to the open ones instead of replacing them: nothing has to be saved or
            // unloaded for it, and the scenes applied before keep their unsaved changes.
            var openScene = ExitFlowPrompts.DisplayDialog("Scene Switch",
                $"Open scene to apply its overrides?\n\n{targetPath}\n\nIt is opened next to the open scenes.",
                "Open", "Skip remaining");
            if (!openScene)
            {
                // The remaining scenes keep their overrides in the stores.
                CloseScenesOpenedForApply(context);
                return;
            }

            EditorSceneManager.OpenScene(targetPath, OpenSceneMode.Additive);
            context.OpenedScenePaths.Add(targetPath);

            ContinueLater(() =>
            {
                SceneView.RepaintAll();
                var newQueue = new List<string> { targetPath }.Concat(remainingScenes).ToList();
                ProcessNextSceneInQueue(newQueue, context);
            });
        }

        private static void PromptApplyOrDiscard(string currentPath, List<string> remainingScenes,
            SceneApplyContext context)
        {
            var msg = $"Apply play mode overrides for scene?\n\n{currentPath}";

            var apply = ExitFlowPrompts.DisplayDialog("Apply Overrides", msg, "Apply", "Discard");
            if (apply)
            {
                ApplySceneChangesAndContinue(currentPath, remainingScenes, context);
                return;
            }

            ChangesStoreManager.RemoveChangesForSceneFromStore(currentPath, context.TransformStore,
                context.ComponentStore, context.NameStore);
            OverridesBrowserWindow.RefreshOpenInstances();
            ContinueLater(() => ProcessNextSceneInQueue(remainingScenes, context));
        }

        private static void ApplySceneChangesAndContinue(string currentPath, List<string> remainingScenes,
            SceneApplyContext context)
        {
            ApplyChangesFromStoreToEditModeForScene(currentPath, context.TransformStore, context.ComponentStore);
            ApplyNameChangesFromStoreToEditModeForScene(currentPath);

            ContinueLater(() =>
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                ContinueLater(() =>
                {
                    SceneView.RepaintAll();
                    EditorApplication.DirtyHierarchyWindowSorting();
                    ContinueLater(() => ProcessNextSceneInQueue(remainingScenes, context));
                });
            });
        }

        private static bool CanOpenScene(string scenePath)
        {
            return !string.IsNullOrEmpty(scenePath) &&
                   AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null;
        }

        private static bool IsSameScenePath(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        public static void ApplyChangesFromStoreToEditModeForScene(string targetScenePath, ChangesStore transformStore,
            CompChangesStore compStore)
        {
            var normalizedTarget = SceneAndPathUtilities.NormalizeScenePath(targetScenePath);

            var commands = new List<IApplyCommand>();
            var tStore = transformStore != null ? transformStore : ChangesStore.LoadExisting();
            var cStore = compStore != null ? compStore : CompChangesStore.LoadExisting();

            if (tStore != null)
            {
                BuildTransformCommands(normalizedTarget, tStore, commands);
            }

            if (cStore != null)
            {
                BuildComponentCommands(normalizedTarget, cStore, commands);
            }

            ExecuteCommands(commands);
        }

        private static void BuildTransformCommands(string targetScenePath, ChangesStore store,
            List<IApplyCommand> commands)
        {
            foreach (var change in store.changes)
            {
                var request = new StoreChangeRequest(
                    targetScenePath,
                    change.scenePath,
                    // () => Debug.Log($"[Transform] Begin scene='{SceneAndPathUtilities.NormalizeScenePath(change.scenePath)}' guid='{change.globalObjectId}' path='{change.objectPath}' props={string.Join(",", change.modifiedProperties ?? new List<string>())}"),
                    () => { }, //remove line if Debug enabled
                    scene => commands.Add(new ApplyTransformChangeCommand(scene, change)));
                ProcessStoreChange(request);
            }
        }

        private static void BuildComponentCommands(string targetScenePath, CompChangesStore store,
            List<IApplyCommand> commands)
        {
            foreach (var change in store.changes)
            {
                var request = new StoreChangeRequest(
                    targetScenePath,
                    change.scenePath,
                    //() => Debug.Log($" Begin scene='{SceneAndPathUtilities.NormalizeScenePath(change.scenePath)}' guid='{change.globalObjectId}' path='{change.objectPath}' type='{change.componentType}' idx={change.componentIndex}"),
                    () => { }, //remove line if Debug enabled
                    scene => commands.Add(new ApplyComponentChangeCommand(scene, change)));
                ProcessStoreChange(request);
            }
        }

        private static void ProcessStoreChange(StoreChangeRequest request)
        {
            var normalizedChangePath = SceneAndPathUtilities.NormalizeScenePath(request.ChangeScenePath);
            if (!IsSameScenePath(normalizedChangePath, request.TargetScenePath))
            {
                return;
            }

            request.LogBegin();

            if (!TryGetScene(normalizedChangePath, request.ChangeScenePath, out var scene))
            {
                return;
            }

            request.AddCommand(scene);
        }

        private static bool TryGetScene(string normalizedPath, string fallbackName, out Scene scene)
        {
            scene = SceneManager.GetSceneByPath(normalizedPath);
            if (scene.IsValid())
            {
                return true;
            }

            scene = SceneManager.GetSceneByName(fallbackName);
            if (scene.IsValid())
            {
                return true;
            }

            return false;
        }

        private static void ExecuteCommands(IEnumerable<IApplyCommand> commands)
        {
            foreach (var command in commands)
            {
                command.Execute();
            }
        }

        public static void ApplyPropertyToTransform(Transform t, RectTransform rt, ChangesStore.TransformChange change,
            string prop)
        {
            if (TransformPropertyAppliers.TryGetValue(prop, out var apply))
            {
                apply(t, rt, change);
            }
        }

        /// <summary>
        ///     End of the flow. Scenes that were opened only to apply their overrides can be closed again, after
        ///     the user decided about saving them; the scenes that were open before stay as they are.
        /// </summary>
        private static void CloseScenesOpenedForApply(SceneApplyContext context)
        {
            try
            {
                var opened = context.OpenedScenePaths
                    .Select(SceneManager.GetSceneByPath)
                    .Where(s => s.IsValid() && s.isLoaded && s != SceneManager.GetActiveScene())
                    .ToArray();
                if (opened.Length == 0)
                {
                    return;
                }

                var list = string.Join("\n", opened.Select(s => s.path));
                if (ExitFlowPrompts.DisplayDialog("Close Scenes",
                        $"Close the scenes that were opened to apply overrides?\n\n{list}", "Close", "Keep Open") &&
                    ExitFlowPrompts.SaveModifiedScenesIfUserWantsTo(opened))
                {
                    foreach (var scene in opened)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }

                    SceneView.RepaintAll();
                }
            }
            finally
            {
                PlayModeOverrideFlow.StopProcessing();
            }
        }

        public static void ApplyMaterials(Renderer renderer, List<string> materialGuids)
        {
            if (!renderer || materialGuids == null)
            {
                return;
            }

            var targetCount = materialGuids.Count;
            if (targetCount == 0)
            {
                return;
            }

            var current = renderer.sharedMaterials;
            var applied = new Material[targetCount];

            for (var i = 0; i < targetCount; i++)
            {
                applied[i] = ResolveMaterial(materialGuids[i], i < current.Length ? current[i] : null);
            }

            renderer.sharedMaterials = applied;
        }

        private static Material ResolveMaterial(string guid, Material fallback)
        {
            // No GUID: a runtime material instance or an empty slot. The slot reference applied before
            // (m_Materials) already holds the right value, so keep it.
            if (string.IsNullOrEmpty(guid))
            {
                return fallback;
            }

            var path = AssetDatabase.GUIDToAssetPath(guid);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material != null)
            {
                return material;
            }

            return fallback;
        }

        public static void ApplyNameChangesFromStoreToEditModeForScene(string targetScenePath)
        {
            targetScenePath = SceneAndPathUtilities.NormalizeScenePath(targetScenePath);

            var nameStore = NameChangesStore.LoadExisting();
            if (!nameStore || nameStore.changes.Count == 0)
            {
                return;
            }

            foreach (var change in nameStore.changes)
            {
                ProcessNameChangeIfInTargetScene(change, targetScenePath);
            }
        }

        private static void ProcessNameChangeIfInTargetScene(NameChangesStore.NameChange change, string targetScenePath)
        {
            var normalizedChangePath = SceneAndPathUtilities.NormalizeScenePath(change.scenePath);
            if (!string.Equals(normalizedChangePath, targetScenePath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!TryResolveSceneForNameChange(normalizedChangePath, change.scenePath, out var scene))
            {
                return;
            }

            if (!TryFindTargetForNameChange(scene, change, out var go))
            {
                return;
            }

            ApplyNameChange(scene, go, change.newName);

            var newPath = SceneAndPathUtilities.GetGameObjectPath(go.transform);
            var editorGoid = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString();
            var newIndex = go.transform.GetSiblingIndex();

            UpdateNameOriginalPath(change, newPath, editorGoid, newIndex);

            change.objectPath = newPath;
            change.globalObjectId = editorGoid;
            change.siblingIndex = newIndex;
        }

        private static bool TryResolveSceneForNameChange(string normalizedChangePath, string fallbackPath,
            out Scene scene)
        {
            return TryGetScene(normalizedChangePath, fallbackPath, out scene);
        }

        private static bool TryFindTargetForNameChange(Scene scene, NameChangesStore.NameChange change,
            out GameObject go)
        {
            go = SceneAndPathUtilities.FindGameObjectByGuidOrPath(scene, change.globalObjectId, change.objectPath);
            if (go != null)
            {
                return true;
            }

            return false;
        }

        private static void ApplyNameChange(Scene scene, GameObject go, string newName)
        {
            Undo.RecordObject(go, "Apply Play Mode Name Changes");
            go.name = newName;
            EditorUtility.SetDirty(go);
            if (scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        private static void UpdateNameOriginalPath(NameChangesStore.NameChange change, string newPath,
            string editorGoid, int newIndex)
        {
            var originalStore = NameOriginalStore.LoadExisting();
            if (!originalStore)
            {
                return;
            }

            var originalEntry = originalStore.entries.Find(e =>
                SceneAndPathUtilities.IsSameObject(e.globalObjectId, change.globalObjectId,
                    e.scenePath == change.scenePath && e.objectPath == change.objectPath));

            if (originalEntry != null)
            {
                originalEntry.objectPath = newPath;
                originalEntry.globalObjectId = editorGoid;
                originalEntry.siblingIndex = newIndex;
                EditorUtility.SetDirty(originalStore);
            }
        }

        private readonly struct StoreChangeRequest
        {
            public string TargetScenePath { get; }
            public string ChangeScenePath { get; }
            public Action LogBegin { get; }
            public Action<Scene> AddCommand { get; }

            public StoreChangeRequest(string targetScenePath, string changeScenePath, Action logBegin,
                Action<Scene> addCommand)
            {
                TargetScenePath = targetScenePath;
                ChangeScenePath = changeScenePath;
                LogBegin = logBegin;
                AddCommand = addCommand;
            }
        }

        public sealed class SceneApplyContext
        {
            public SceneApplyContext(string startScenePath, ChangesStore transformStore,
                CompChangesStore componentStore, NameChangesStore nameStore)
            {
                StartScenePath = startScenePath;
                TransformStore = transformStore;
                ComponentStore = componentStore;
                NameStore = nameStore;
            }

            public string StartScenePath { get; }

            /// <summary>Scenes the flow opened additively; they are offered for closing at the end.</summary>
            public List<string> OpenedScenePaths { get; } = new();
            public ChangesStore TransformStore { get; }
            public CompChangesStore ComponentStore { get; }
            public NameChangesStore NameStore { get; }
        }

        public interface IApplyCommand
        {
            void Execute();
        }
    }
}