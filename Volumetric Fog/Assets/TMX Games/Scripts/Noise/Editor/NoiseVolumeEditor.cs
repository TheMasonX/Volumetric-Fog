using UnityEngine;
using UnityEditor;
using System.IO;
using TMX.Editor;
using System.Collections;

namespace Noise
{
    [CustomEditor(typeof(NoiseVolume))]
    public class NoiseVolumeEditor : Editor
    {

        static EditorCoroutine coroutine;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            GUILayout.Space(15f);

            var shouldRebuild = false;

            if (coroutine != null)
            {
                if(GUILayout.Button("Cancel"))
                {
                    CancelGeneration(target as NoiseVolume);
                }

                GUILayout.Space(10f);

                var rect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth, 20f);
                EditorGUI.ProgressBar(rect, NoiseVolume.progress, NoiseVolume.progressText);
                Repaint();
            }
            else
            {
                shouldRebuild = GUILayout.Button("Rebuild");
            }

            if (shouldRebuild)
            {
                foreach (var t in targets)
                {
                    var noiseVolume = ((NoiseVolume)t);
                    if (noiseVolume.resolution != noiseVolume.texture.width || ((TextureFormat)noiseVolume.format) != noiseVolume.texture.format)
                    {
                        DestroyImmediate(noiseVolume.texture, true);
                        AssetDatabase.SaveAssets();

                        noiseVolume.ChangeResolution(noiseVolume.resolution);
                        AssetDatabase.AddObjectToAsset(noiseVolume.texture, noiseVolume);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }

                    RebuildTexture(noiseVolume);
                }
            }
        }

        static void CreateAsset(int resolution)
        {
            // Make a proper path from the current selection.
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (string.IsNullOrEmpty(path))
                path = "Assets";
            else if (Path.GetExtension(path) != "")
                path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            var assetPathName = AssetDatabase.GenerateUniqueAssetPath(path + "/NewNoiseVolume.asset");

            // Create an asset.
            var asset = ScriptableObject.CreateInstance<NoiseVolume>();
            asset.ChangeResolution(resolution);
            AssetDatabase.CreateAsset(asset, assetPathName);
            AssetDatabase.AddObjectToAsset(asset.texture, asset);

            // Build an initial volume for the asset.
            //asset.RebuildTexture();
            RebuildTexture(asset);

            // Save the generated mesh asset.
            AssetDatabase.SaveAssets();

            // Tweak the selection.
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }

        static void RebuildTexture (NoiseVolume noiseVolume)
        {
            noiseVolume.RebuildTexture();
            if(coroutine != null)
            {
                coroutine.stop();
            }

            coroutine = EditorCoroutine.Start(CompletionWait(noiseVolume));
        }

        static void CancelGeneration (NoiseVolume noiseVolume)
        {
            if (coroutine != null)
                coroutine.stop();

            coroutine = null;

            noiseVolume.ClearThreads();
        }

        static IEnumerator CompletionWait (NoiseVolume noiseVolume)
        {
            var routine = noiseVolume.CompletionWait();
            while (routine.MoveNext())
            {
                yield return null;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            coroutine.stop();
            coroutine = null;
        }

        [MenuItem("Assets/Create/NoiseVolume/32")]
        public static void CreateNoiseVolume32()
        {
            CreateAsset(32);
        }

        [MenuItem("Assets/Create/NoiseVolume/64")]
        public static void CreateNoiseVolume64()
        {
            CreateAsset(64);
        }

        [MenuItem("Assets/Create/NoiseVolume/128")]
        public static void CreateNoiseVolume128()
        {
            CreateAsset(128);
        }
    }
}
