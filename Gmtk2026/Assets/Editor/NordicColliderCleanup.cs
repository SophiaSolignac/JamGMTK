// Nordic Collider Cleanup — Countdown Nordic
// Menu:  Tools > Nordic > Strip Grass Colliders
//
// Fixes grass that is already in the scene without re-scattering it, and clears the
// collider off the grass prefabs so a re-scatter cannot bring it back.

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class NordicColliderCleanup
{
    const string GrassContainer = "Grass_Scatter";
    const string PrefabFolder = "Assets/Prefabs/Environment";

    [MenuItem("Tools/Nordic/Strip Grass Colliders")]
    static void Strip()
    {
        int inScene = 0, inPrefabs = 0;

        // ---- what is already placed ----
        var container = NordicScatterCore.FindContainer(GrassContainer, false);
        if (container != null)
        {
            var cols = container.GetComponentsInChildren<Collider>(true);
            inScene = cols.Length;
            foreach (var c in cols) Undo.DestroyObjectImmediate(c);
            if (inScene > 0)
                EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        // ---- and the prefabs, so it does not come back ----
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name.IndexOf("Grass", System.StringComparison.OrdinalIgnoreCase) < 0) continue;

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var cols = root.GetComponentsInChildren<Collider>(true);
                if (cols.Length == 0) continue;
                foreach (var c in cols) Object.DestroyImmediate(c, true);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                inPrefabs += cols.Length;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[Grass] Removed {inScene} collider(s) from the scene and {inPrefabs} from the grass prefabs. " +
                  (inScene + inPrefabs == 0
                      ? "Nothing was there — if shots still stop in grass, the blocker is another object."
                      : "Shots and the player pass through grass now."));
    }
}
