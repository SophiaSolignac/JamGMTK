// Nordic Ruin Scatter — Countdown Nordic
// Drops small collapsed ruins around the level: modular kit pieces as the broken walls,
// rocks from the same kit as the rubble around them.
// Menu:  Tools > Nordic > Ruin Cluster Scatter
//
// Same keep-out rules as the rock scatter, shared through NordicScatterCore, so ruins
// never land on the player's path or on top of what is already scattered.

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class NordicRuinScatter : EditorWindow
{
    const string Container = "Ruins_Scatter";

    string kitFolder = "Assets/Prefabs/Building/Modular_Kit";
    string rubbleFolder = "Assets/Prefabs/Environment";
    string ruinsFolder = "Assets/Prefabs/Building/Ruins";
    bool useRuinsFolder = false;

    // never used in a scatter: they are landmarks, not debris
    string excludeNames = "Piedestal, Tower, Big";
    float maxPieceFootprint = 6f;

    int clusterCount = 12;
    Vector2Int piecesPerCluster = new Vector2Int(3, 6);
    Vector2 clusterRadius = new Vector2(2.5f, 5.5f);
    float siteSpacing = 55f;
    float pieceSpacing = 1.6f;
    int seed = 7;

    bool limitToMountains = true;
    string mountainsPath = NordicScatterCore.MountainsPath;
    float boundaryInset = 10f;
    bool showBoundary = true;
    List<Vector2> boundary;

    Vector3 areaCenter;
    Vector2 areaSize = new Vector2(560f, 560f);
    bool areaInit;

    float gameplayClearance = 26f;
    float structureClearance = 14f;
    float navMeshClearance = 5f;

    float maxSlope = 28f;
    [Range(0f, 1f)] float kitShare = 0.55f;

    float alignToNormal = 0.45f;
    float kitTilt = 20f;
    float rubbleTilt = 12f;
    Vector2 kitScale = new Vector2(0.30f, 0.65f);
    Vector2 rubbleScale = new Vector2(0.45f, 0.9f);
    Vector2 kitSink = new Vector2(0.20f, 0.50f);
    float rubbleSink = 0.22f;

    bool anchorPiece = true;
    bool markStatic = true;
    bool contributeGI = false;
    bool gpuInstancing = true;
    bool addColliders = true;
    float colliderMinSize = 1.5f;

    Vector2 scroll;

    [MenuItem("Tools/Nordic/Ruin Cluster Scatter")]
    static void Open()
    {
        var w = GetWindow<NordicRuinScatter>("Ruin Scatter");
        w.minSize = new Vector2(380f, 560f);
    }

    void OnGUI()
    {
        if (!areaInit) { FitArea(); areaInit = true; }
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.HelpBox(
            "Small collapsed ruins: a few kit pieces half-buried and leaning, with rock rubble " +
            "around them. Change the Seed and press again to re-roll.", MessageType.None);

        Head("Where the pieces come from");
        kitFolder = EditorGUILayout.TextField("Kit folder", kitFolder);
        rubbleFolder = EditorGUILayout.TextField("Rubble folder", rubbleFolder);
        useRuinsFolder = EditorGUILayout.Toggle("Also use the Ruins folder", useRuinsFolder);
        using (new EditorGUI.DisabledScope(!useRuinsFolder))
            ruinsFolder = EditorGUILayout.TextField("  Ruins folder", ruinsFolder);
        excludeNames = EditorGUILayout.TextField("Never use (name contains)", excludeNames);
        maxPieceFootprint = EditorGUILayout.Slider("Max piece size (m)", maxPieceFootprint, 0.5f, 30f);
        EditorGUILayout.HelpBox("Landmarks stay out of the scatter. Anything wider than the max is " +
                                "skipped too, so a ruin never grows big enough to block a sightline.",
                                MessageType.None);
        kitShare = EditorGUILayout.Slider("Kit vs rubble", kitShare, 0f, 1f);
        EditorGUILayout.LabelField(" ", $"{Mathf.RoundToInt(kitShare * 100)}% built pieces, {Mathf.RoundToInt((1 - kitShare) * 100)}% rock");

        Head("Clusters");
        clusterCount = EditorGUILayout.IntSlider("How many ruins", clusterCount, 1, 60);
        piecesPerCluster = EditorGUILayout.Vector2IntField("Pieces per ruin (min, max)", piecesPerCluster);
        clusterRadius = EditorGUILayout.Vector2Field("Ruin radius (min, max)", clusterRadius);
        siteSpacing = EditorGUILayout.Slider("Space between ruins", siteSpacing, 10f, 200f);
        pieceSpacing = EditorGUILayout.Slider("Space between pieces", pieceSpacing, 0.5f, 10f);
        seed = EditorGUILayout.IntField("Seed", seed);
        EditorGUILayout.LabelField(" ", $"about {clusterCount * (piecesPerCluster.x + piecesPerCluster.y) / 2} objects total");

        Head("Where they may land");
        areaCenter = EditorGUILayout.Vector3Field("Area centre", areaCenter);
        areaSize = EditorGUILayout.Vector2Field("Area size (X, Z)", areaSize);
        if (GUILayout.Button("Fit area to the level")) FitArea();
        EditorGUILayout.Space(4);
        limitToMountains = EditorGUILayout.Toggle("Stay inside the mountains", limitToMountains);
        using (new EditorGUI.DisabledScope(!limitToMountains))
        {
            mountainsPath = EditorGUILayout.TextField("  Mountain root", mountainsPath);
            boundaryInset = EditorGUILayout.Slider("  Pull inward by (m)", boundaryInset, -50f, 80f);
            showBoundary = EditorGUILayout.Toggle("  Show the line", showBoundary);
            if (GUILayout.Button("Refresh boundary")) RefreshBoundary();
        }
        gameplayClearance = EditorGUILayout.Slider("Away from gameplay", gameplayClearance, 0f, 80f);
        structureClearance = EditorGUILayout.Slider("Away from buildings", structureClearance, 0f, 80f);
        navMeshClearance = EditorGUILayout.Slider("Away from walkable navmesh", navMeshClearance, 0f, 30f);
        maxSlope = EditorGUILayout.Slider("Max slope", maxSlope, 0f, 60f);

        Head("Look");
        anchorPiece = EditorGUILayout.Toggle("One big piece per ruin", anchorPiece);
        alignToNormal = EditorGUILayout.Slider("Follow ground angle", alignToNormal, 0f, 1f);
        kitTilt = EditorGUILayout.Slider("Kit lean", kitTilt, 0f, 45f);
        rubbleTilt = EditorGUILayout.Slider("Rubble lean", rubbleTilt, 0f, 45f);
        kitScale = EditorGUILayout.Vector2Field("Kit scale (min, max)", kitScale);
        rubbleScale = EditorGUILayout.Vector2Field("Rubble scale (min, max)", rubbleScale);
        kitSink = EditorGUILayout.Vector2Field("Kit buried (min, max)", kitSink);
        rubbleSink = EditorGUILayout.Slider("Rubble buried", rubbleSink, 0f, 0.6f);

        Head("Performance");
        markStatic = EditorGUILayout.Toggle("Mark static (batching)", markStatic);
        contributeGI = EditorGUILayout.Toggle("Contribute to lightmaps", contributeGI);
        gpuInstancing = EditorGUILayout.Toggle("GPU instancing on materials", gpuInstancing);
        addColliders = EditorGUILayout.Toggle("Colliders on big pieces", addColliders);
        using (new EditorGUI.DisabledScope(!addColliders))
            colliderMinSize = EditorGUILayout.Slider("  Collider above size (m)", colliderMinSize, 0.2f, 8f);

        EditorGUILayout.Space(12);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
            if (GUILayout.Button("Scatter ruins", GUILayout.Height(34))) Scatter();
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
        if (!limitToMountains || !showBoundary) return;
        NordicScatterCore.DrawBoundary(boundary, areaCenter.y + 2f);
    }

    void RefreshBoundary()
    {
        boundary = limitToMountains ? NordicScatterCore.BuildBoundary(mountainsPath, boundaryInset) : null;
        if (limitToMountains && boundary == null)
            Debug.LogWarning($"[Ruins] No mountains under \"{mountainsPath}\" — the boundary is off.");
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
        if (c == null) { Debug.Log("[Ruins] Nothing to clear."); return; }
        Undo.DestroyObjectImmediate(c);
        Dirty();
    }

    void Scatter()
    {
        RefreshBoundary();

        var banned = excludeNames.Split(',')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToArray();

        bool Allowed(GameObject p)
        {
            foreach (var b in banned)
                if (p.name.IndexOf(b, System.StringComparison.OrdinalIgnoreCase) >= 0) return false;
            return NordicScatterCore.PrefabFootprint(p) <= maxPieceFootprint;
        }

        var kit = NordicScatterCore.LoadPrefabs(kitFolder).Where(Allowed).ToList();
        if (useRuinsFolder) kit.AddRange(NordicScatterCore.LoadPrefabs(ruinsFolder).Where(Allowed));

        // rubble = the rock kit, minus the cliff-sized pieces that would read as terrain
        var rubble = NordicScatterCore.LoadPrefabs(rubbleFolder)
            .Where(p =>
            {
                string n = p.name.ToLowerInvariant();
                if (n.Contains("arbre") || n.Contains("tree")) return false;
                if (n.Contains("cliff") || n.Contains("mountain") || n.Contains("pillar") || n.Contains("overhang")) return false;
                return true;
            }).ToList();

        if (kit.Count == 0 && rubble.Count == 0)
        {
            EditorUtility.DisplayDialog("Ruin Scatter", "No prefabs found in those folders.", "OK");
            return;
        }

        // biggest kit pieces first, so the anchor can pick from the top
        kit.Sort((a, b) => NordicScatterCore.PrefabFootprint(b).CompareTo(NordicScatterCore.PrefabFootprint(a)));

        var hidden = NordicScatterCore.HideScatters();
        var keepOut = NordicScatterCore.BuildKeepOut(NordicScatterCore.DefaultAvoid,
                                                     NordicScatterCore.DefaultStructures,
                                                     gameplayClearance, structureClearance);
        bool nav = NordicScatterCore.HasNavMesh();
        var taken = NordicScatterCore.ExistingScatterPositions();

        Random.State oldState = Random.state;
        Random.InitState(seed);

        // ---- find the cluster sites ----
        var sites = new List<NordicScatterCore.Ground>();
        var siteCentres = new List<Vector3>();
        int attempts = 0, maxAttempts = clusterCount * 400;

        while (sites.Count < clusterCount && attempts < maxAttempts)
        {
            attempts++;
            float x = areaCenter.x + (Random.value - 0.5f) * areaSize.x;
            float z = areaCenter.z + (Random.value - 0.5f) * areaSize.y;

            if (!NordicScatterCore.SampleGround(new Vector2(x, z), out var g)) continue;
            if (!NordicScatterCore.Inside(g.pos, boundary)) continue;
            if (g.slope > maxSlope) continue;
            if (NordicScatterCore.IsAvoided(g.hit, NordicScatterCore.DefaultAvoid, NordicScatterCore.DefaultStructures)) continue;
            if (NordicScatterCore.TooClose(g.pos, keepOut)) continue;
            if (nav && NordicScatterCore.OnWalkable(g.pos, navMeshClearance)) continue;
            if (!NordicScatterCore.Spaced(g.pos, siteCentres, siteSpacing)) continue;
            if (!NordicScatterCore.Spaced(g.pos, taken, 8f)) continue;

            sites.Add(g);
            siteCentres.Add(g.pos);
        }

        if (sites.Count == 0)
        {
            NordicScatterCore.RestoreScatters(hidden);
            Random.state = oldState;
            EditorUtility.DisplayDialog("Ruin Scatter",
                "No room found. Widen the area, lower the clearances, or raise the max slope.", "OK");
            return;
        }

        // ---- build ----
        var old = NordicScatterCore.FindContainer(Container, false);
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = NordicScatterCore.FindContainer(Container, true);
        root.SetActive(false);          // stays hidden while placing, or pieces land on each other

        int placed = 0, tris = 0, kitCount = 0, rubbleCount = 0;

        for (int c = 0; c < sites.Count; c++)
        {
            var site = sites[c];
            float radius = Random.Range(clusterRadius.x, clusterRadius.y);
            int want = Random.Range(piecesPerCluster.x, piecesPerCluster.y + 1);

            var group = new GameObject($"Ruin_{c:00}");
            group.transform.SetParent(root.transform, false);
            group.transform.position = site.pos;
            Undo.RegisterCreatedObjectUndo(group, "Scatter Ruins");

            var localTaken = new List<Vector3>();

            for (int i = 0; i < want; i++)
            {
                bool isAnchor = anchorPiece && i == 0 && kit.Count > 0;
                bool useKit = isAnchor || (kit.Count > 0 && Random.value < kitShare);
                if (useKit && kit.Count == 0) useKit = false;
                if (!useKit && rubble.Count == 0) useKit = true;
                if (useKit && kit.Count == 0) continue;

                // the anchor sits at the middle, everything else rings out from it
                Vector2 off = isAnchor ? Vector2.zero : NordicScatterCore.ClusterOffset(radius);
                var xz = new Vector2(site.pos.x + off.x, site.pos.z + off.y);

                if (!NordicScatterCore.SampleGround(xz, out var g)) continue;
                if (!NordicScatterCore.Inside(g.pos, boundary)) continue;
                if (g.slope > maxSlope + 12f) continue;
                if (NordicScatterCore.IsAvoided(g.hit, NordicScatterCore.DefaultAvoid, NordicScatterCore.DefaultStructures)) continue;
                if (NordicScatterCore.TooClose(g.pos, keepOut)) continue;
                if (!NordicScatterCore.Spaced(g.pos, localTaken, pieceSpacing)) continue;

                GameObject prefab;
                float tilt, sink;
                Vector2 scaleRange;

                if (useKit)
                {
                    // anchor comes from the biggest third of the kit
                    int top = Mathf.Max(1, kit.Count / 3);
                    prefab = isAnchor ? kit[Random.Range(0, top)] : kit[Random.Range(0, kit.Count)];
                    tilt = isAnchor ? kitTilt * 0.4f : kitTilt;      // the surviving wall stands straighter
                    sink = Random.Range(kitSink.x, kitSink.y);
                    scaleRange = kitScale;
                    kitCount++;
                }
                else
                {
                    prefab = rubble[Random.Range(0, rubble.Count)];
                    tilt = rubbleTilt;
                    sink = rubbleSink;
                    scaleRange = rubbleScale;
                    rubbleCount++;
                }

                float s = Random.Range(scaleRange.x, scaleRange.y);
                if (isAnchor) s = Mathf.Max(s, scaleRange.y * 0.9f);

                var wrapper = NordicScatterCore.MakeWrapper(prefab.name, group.transform, g.pos);

                var go = NordicScatterCore.Place(prefab, wrapper.transform, g,
                    alignToNormal, Random.value * 360f, tilt, Vector3.one * s, sink, out Bounds wb);
                if (go == null) { DestroyImmediate(wrapper); continue; }

                if (markStatic) { NordicScatterCore.MakeStatic(go, contributeGI); NordicScatterCore.MakeStatic(wrapper, false); }
                float size = Mathf.Max(wb.size.x, wb.size.y, wb.size.z);
                if (addColliders && size >= colliderMinSize) NordicScatterCore.EnsureBoxCollider(wrapper, wb);

                tris += NordicScatterCore.CountTris(go);
                localTaken.Add(g.pos);
                placed++;
            }

            if (group.transform.childCount == 0) DestroyImmediate(group);
        }

        root.SetActive(true);
        NordicScatterCore.RestoreScatters(hidden);
        Random.state = oldState;

        if (gpuInstancing)
        {
            var all = new List<GameObject>(kit); all.AddRange(rubble);
            NordicScatterCore.EnableInstancing(all);
        }

        Dirty();
        Selection.activeGameObject = root;
        Debug.Log($"[Ruins] {sites.Count} ruins, {placed} objects ({kitCount} kit, {rubbleCount} rubble) — " +
                  $"about {tris:N0} triangles. {kit.Count} kit prefab(s) passed the size and name filter. " +
                  $"Boundary: {(boundary != null ? boundary.Count + "-sided mountain ring" : "off")}. " +
                  $"NavMesh check: {(nav ? "on" : "no navmesh baked")}.", root);
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
