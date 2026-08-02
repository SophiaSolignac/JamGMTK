// Nordic Tree Scatter — Countdown Nordic
// Groves plus lone trees, on the gentle ground only, clear of the player's path.
// Menu:  Tools > Nordic > Tree Scatter
//
// Two rules make trees read as trees rather than as scattered props:
//   - they grow toward the sky, so they barely follow the ground angle
//   - they refuse steep slopes, because nothing takes root on a cliff face

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class NordicTreeScatter : EditorWindow
{
    const string Container = "Trees_Scatter";

    string treeFolder = "Assets/Prefabs/Environment";
    string nameContains = "Arbre";

    int groveCount = 22;
    Vector2Int treesPerGrove = new Vector2Int(3, 9);
    Vector2 groveRadius = new Vector2(5f, 13f);
    int lonerCount = 25;
    float groveSpacing = 45f;
    float treeSpacing = 3.4f;
    int seed = 42;

    Vector3 areaCenter;
    Vector2 areaSize = new Vector2(560f, 560f);
    bool areaInit;

    float gameplayClearance = 24f;
    float structureClearance = 12f;
    float navMeshClearance = 5f;
    float maxSlope = 30f;

    NordicScatterCore.BoundarySource boundarySource = NordicScatterCore.BoundarySource.GameplayObjects;
    float boundaryAmount = 45f;
    bool keepOffMountains = true;
    string mountainsPath = NordicScatterCore.MountainsPath;
    bool showBoundary = true;
    List<Vector2> boundary;
    Transform mountainT;
    int rejectedByBoundary;

    float alignToNormal = 0.12f;      // trees grow up, not perpendicular to the hill
    float randomTilt = 4f;
    Vector2 uniformScale = new Vector2(0.75f, 1.35f);
    Vector2 heightStretch = new Vector2(0.9f, 1.25f);
    float sinkFraction = 0.05f;

    bool markStatic = true;
    bool contributeGI = false;
    bool gpuInstancing = true;
    bool trunkColliders = true;
    float trunkFraction = 0.12f;

    Vector2 scroll;

    [MenuItem("Tools/Nordic/Tree Scatter")]
    static void Open()
    {
        var w = GetWindow<NordicTreeScatter>("Tree Scatter");
        w.minSize = new Vector2(380f, 560f);
    }

    void OnGUI()
    {
        if (!areaInit) { FitArea(); areaInit = true; }
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.HelpBox(
            "Groves with a dense middle, plus a few lone trees between them. " +
            "Change the Seed and press again to re-roll.", MessageType.None);

        Head("Source");
        treeFolder = EditorGUILayout.TextField("Folder", treeFolder);
        nameContains = EditorGUILayout.TextField("Name contains", nameContains);
        EditorGUILayout.LabelField(" ", $"{NordicScatterCore.LoadPrefabs(treeFolder, nameContains).Count} prefab(s) match");

        Head("Groves");
        groveCount = EditorGUILayout.IntSlider("How many groves", groveCount, 0, 120);
        treesPerGrove = EditorGUILayout.Vector2IntField("Trees per grove (min, max)", treesPerGrove);
        groveRadius = EditorGUILayout.Vector2Field("Grove radius (min, max)", groveRadius);
        groveSpacing = EditorGUILayout.Slider("Space between groves", groveSpacing, 10f, 200f);
        treeSpacing = EditorGUILayout.Slider("Space between trees", treeSpacing, 1f, 20f);
        EditorGUILayout.Space(4);
        lonerCount = EditorGUILayout.IntSlider("Lone trees", lonerCount, 0, 300);
        seed = EditorGUILayout.IntField("Seed", seed);

        int estimate = groveCount * (treesPerGrove.x + treesPerGrove.y) / 2 + lonerCount;
        EditorGUILayout.LabelField(" ", $"about {estimate} trees" + (estimate > 400 ? "  ← heavy, trees are not pebbles" : ""));

        Head("Where they may grow");
        areaCenter = EditorGUILayout.Vector3Field("Area centre", areaCenter);
        areaSize = EditorGUILayout.Vector2Field("Area size (X, Z)", areaSize);
        if (GUILayout.Button("Fit area to the level")) FitArea();
        EditorGUILayout.Space(4);
        var newSource = (NordicScatterCore.BoundarySource)EditorGUILayout.EnumPopup("Boundary", boundarySource);
        if (newSource != boundarySource) { boundarySource = newSource; RefreshBoundary(); }

        using (new EditorGUI.DisabledScope(boundarySource == NordicScatterCore.BoundarySource.None))
        {
            if (boundarySource == NordicScatterCore.BoundarySource.GameplayObjects)
            {
                boundaryAmount = EditorGUILayout.Slider("  Reach past gameplay (m)", boundaryAmount, 5f, 250f);
                EditorGUILayout.HelpBox("Wraps the spawns, gun spawners, doors and structures, then grows " +
                    "outward by this much. The mountain group is a bad boundary here — it mixes the arena " +
                    "walls with background mountains 230 m out.", MessageType.None);
            }
            else
            {
                mountainsPath = EditorGUILayout.TextField("  Mountain root", mountainsPath);
                boundaryAmount = EditorGUILayout.Slider("  Pull inward by (m)", boundaryAmount, -50f, 200f);
            }
            showBoundary = EditorGUILayout.Toggle("  Show the line", showBoundary);
            if (GUILayout.Button("Refresh boundary")) RefreshBoundary();
        }
        keepOffMountains = EditorGUILayout.Toggle("Never on the mountains", keepOffMountains);
        gameplayClearance = EditorGUILayout.Slider("Away from gameplay", gameplayClearance, 0f, 80f);
        structureClearance = EditorGUILayout.Slider("Away from buildings", structureClearance, 0f, 80f);
        navMeshClearance = EditorGUILayout.Slider("Away from walkable navmesh", navMeshClearance, 0f, 30f);
        maxSlope = EditorGUILayout.Slider("Max slope", maxSlope, 0f, 60f);

        Head("Look");
        alignToNormal = EditorGUILayout.Slider("Follow ground angle", alignToNormal, 0f, 1f);
        if (alignToNormal > 0.35f)
            EditorGUILayout.HelpBox("Above about 0.35 the trees start leaning out of the hillside like hair.", MessageType.Warning);
        randomTilt = EditorGUILayout.Slider("Random tilt", randomTilt, 0f, 20f);
        uniformScale = EditorGUILayout.Vector2Field("Scale (min, max)", uniformScale);
        heightStretch = EditorGUILayout.Vector2Field("Height stretch (min, max)", heightStretch);
        sinkFraction = EditorGUILayout.Slider("Buried in ground", sinkFraction, 0f, 0.3f);

        Head("Performance");
        markStatic = EditorGUILayout.Toggle("Mark static (batching)", markStatic);
        contributeGI = EditorGUILayout.Toggle("Contribute to lightmaps", contributeGI);
        gpuInstancing = EditorGUILayout.Toggle("GPU instancing on materials", gpuInstancing);
        trunkColliders = EditorGUILayout.Toggle("Trunk colliders", trunkColliders);
        using (new EditorGUI.DisabledScope(!trunkColliders))
            trunkFraction = EditorGUILayout.Slider("  Trunk width share", trunkFraction, 0.02f, 0.5f);

        EditorGUILayout.Space(12);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
            if (GUILayout.Button("Scatter trees", GUILayout.Height(34))) Scatter();
            GUI.backgroundColor = new Color(0.95f, 0.6f, 0.6f);
            if (GUILayout.Button("Clear", GUILayout.Height(34), GUILayout.Width(90))) Clear();
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.Space(16);
        EditorGUILayout.EndScrollView();
    }

    void OnEnable() { SceneView.duringSceneGui += OnScene; RefreshBoundary(); }
    void OnDisable() { SceneView.duringSceneGui -= OnScene; }

    void OnScene(SceneView sv)
    {
        if (!showBoundary) return;
        NordicScatterCore.DrawBoundary(boundary, areaCenter.y + 2f);
    }

    void RefreshBoundary()
    {
        boundary = NordicScatterCore.BuildBoundary(boundarySource, mountainsPath, boundaryAmount);
        if (boundarySource != NordicScatterCore.BoundarySource.None && boundary == null)
            Debug.LogWarning($"[Trees] Could not build a \"{boundarySource}\" boundary — it is off, " +
                             "so nothing will be rejected for being outside.");
        SceneView.RepaintAll();
    }

    void FitArea()
    {
        if (!NordicScatterCore.TryLevelBounds(out Bounds b)) return;
        areaCenter = new Vector3(b.center.x, 0f, b.center.z);
        areaSize = new Vector2(b.size.x + 40f, b.size.z + 40f);
    }

    void Clear()
    {
        var c = NordicScatterCore.FindContainer(Container, false);
        if (c == null) { Debug.Log("[Trees] Nothing to clear."); return; }
        Undo.DestroyObjectImmediate(c);
        Dirty();
    }

    void Scatter()
    {
        var trees = NordicScatterCore.LoadPrefabs(treeFolder, nameContains);
        if (trees.Count == 0)
        {
            EditorUtility.DisplayDialog("Tree Scatter",
                $"No prefab in {treeFolder} with \"{nameContains}\" in its name.", "OK");
            return;
        }

        RefreshBoundary();
        rejectedByBoundary = 0;

        var mountainRoot = keepOffMountains ? GameObject.Find(mountainsPath) : null;
        mountainT = mountainRoot != null ? mountainRoot.transform : null;
        if (keepOffMountains && mountainT == null)
            Debug.LogWarning($"[Trees] No \"{mountainsPath}\" — cannot tell mountain ground apart.");

        var hidden = NordicScatterCore.HideScatters();
        var keepOut = NordicScatterCore.BuildKeepOut(NordicScatterCore.DefaultAvoid,
                                                     NordicScatterCore.DefaultStructures,
                                                     gameplayClearance, structureClearance);
        bool nav = NordicScatterCore.HasNavMesh();
        var otherScatter = NordicScatterCore.ExistingScatterPositions();

        Random.State oldState = Random.state;
        Random.InitState(seed);

        // ---- grove centres ----
        var groves = new List<Vector3>();
        int attempts = 0, maxAttempts = Mathf.Max(1, groveCount) * 400;
        while (groves.Count < groveCount && attempts < maxAttempts)
        {
            attempts++;
            if (!TryPoint(out var g, keepOut, nav)) continue;
            if (!NordicScatterCore.Spaced(g.pos, groves, groveSpacing)) continue;
            groves.Add(g.pos);
        }

        var old = NordicScatterCore.FindContainer(Container, false);
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = NordicScatterCore.FindContainer(Container, true);
        root.SetActive(false);

        var placedPositions = new List<Vector3>(otherScatter);
        int placed = 0, tris = 0;

        // ---- groves ----
        for (int c = 0; c < groves.Count; c++)
        {
            float radius = Random.Range(groveRadius.x, groveRadius.y);
            int want = Random.Range(treesPerGrove.x, treesPerGrove.y + 1);

            var group = new GameObject($"Grove_{c:00}");
            group.transform.SetParent(root.transform, false);
            group.transform.position = groves[c];
            Undo.RegisterCreatedObjectUndo(group, "Scatter Trees");

            for (int i = 0; i < want; i++)
            {
                Vector2 off = NordicScatterCore.ClusterOffset(radius);
                var xz = new Vector2(groves[c].x + off.x, groves[c].z + off.y);
                if (!Accept(xz, keepOut, nav, placedPositions, out var g)) continue;
                if (PlaceTree(trees, group.transform, g, ref tris)) { placedPositions.Add(g.pos); placed++; }
            }

            if (group.transform.childCount == 0) DestroyImmediate(group);
        }

        // ---- lone trees ----
        if (lonerCount > 0)
        {
            var loners = new GameObject("Lone_Trees");
            loners.transform.SetParent(root.transform, false);
            Undo.RegisterCreatedObjectUndo(loners, "Scatter Trees");

            int tries = 0, maxTries = lonerCount * 200;
            int done = 0;
            while (done < lonerCount && tries < maxTries)
            {
                tries++;
                float x = areaCenter.x + (Random.value - 0.5f) * areaSize.x;
                float z = areaCenter.z + (Random.value - 0.5f) * areaSize.y;
                if (!Accept(new Vector2(x, z), keepOut, nav, placedPositions, out var g)) continue;
                // a lone tree standing next to a grove is not lone
                if (!NordicScatterCore.Spaced(g.pos, groves, groveRadius.y + 6f)) continue;
                if (PlaceTree(trees, loners.transform, g, ref tris)) { placedPositions.Add(g.pos); placed++; done++; }
            }

            if (loners.transform.childCount == 0) DestroyImmediate(loners);
        }

        root.SetActive(true);
        NordicScatterCore.RestoreScatters(hidden);
        Random.state = oldState;

        if (gpuInstancing) NordicScatterCore.EnableInstancing(trees);

        Dirty();
        Selection.activeGameObject = root;
        Debug.Log($"[Trees] {placed} trees in {groves.Count} groves — about {tris:N0} triangles. " +
                  $"Boundary: {(boundary != null ? $"{boundarySource}, {boundary.Count} sides, rejected {rejectedByBoundary} samples" : "OFF")}. " +
                  $"NavMesh check: {(nav ? "on" : "no navmesh baked")}.", root);
    }

    bool TryPoint(out NordicScatterCore.Ground g, List<NordicScatterCore.KeepOut> keepOut, bool nav)
    {
        float x = areaCenter.x + (Random.value - 0.5f) * areaSize.x;
        float z = areaCenter.z + (Random.value - 0.5f) * areaSize.y;
        return Accept(new Vector2(x, z), keepOut, nav, null, out g);
    }

    bool Accept(Vector2 xz, List<NordicScatterCore.KeepOut> keepOut, bool nav,
                List<Vector3> taken, out NordicScatterCore.Ground g)
    {
        if (!NordicScatterCore.SampleGround(xz, out g)) return false;
        if (!NordicScatterCore.Inside(g.pos, boundary)) { rejectedByBoundary++; return false; }
        if (NordicScatterCore.IsUnder(g.hit, mountainT)) return false;
        if (g.slope > maxSlope) return false;
        if (NordicScatterCore.IsAvoided(g.hit, NordicScatterCore.DefaultAvoid, NordicScatterCore.DefaultStructures)) return false;
        if (NordicScatterCore.TooClose(g.pos, keepOut)) return false;
        if (nav && NordicScatterCore.OnWalkable(g.pos, navMeshClearance)) return false;
        if (taken != null && !NordicScatterCore.Spaced(g.pos, taken, treeSpacing)) return false;
        return true;
    }

    bool PlaceTree(List<GameObject> trees, Transform parent, NordicScatterCore.Ground g, ref int tris)
    {
        var prefab = trees[Random.Range(0, trees.Count)];
        float s = Random.Range(uniformScale.x, uniformScale.y);
        float h = Random.Range(heightStretch.x, heightStretch.y);
        var scale = new Vector3(s, s * h, s);   // vary height more than width, like real trees

        var wrapper = NordicScatterCore.MakeWrapper(prefab.name, parent, g.pos);

        var go = NordicScatterCore.Place(prefab, wrapper.transform, g,
            alignToNormal, Random.value * 360f, randomTilt, scale, sinkFraction, out Bounds wb);
        if (go == null) { DestroyImmediate(wrapper); return false; }

        if (markStatic) { NordicScatterCore.MakeStatic(go, contributeGI); NordicScatterCore.MakeStatic(wrapper, false); }
        if (trunkColliders) NordicScatterCore.EnsureTrunkCollider(wrapper, wb, trunkFraction);
        tris += NordicScatterCore.CountTris(go);
        return true;
    }

    static void Head(string t)
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField(t, EditorStyles.boldLabel);
    }

    static void Dirty()
    {
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
