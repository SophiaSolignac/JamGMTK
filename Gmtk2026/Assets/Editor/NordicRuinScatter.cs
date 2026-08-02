// Nordic Ruin Scatter — Countdown Nordic
// Menu:  Tools > Nordic > Ruin Cluster Scatter
//
// Ruins are the remains of something BUILT, so they are not scattered — they are laid out:
//
//   Wall      a run of pieces set edge to edge along one line, with sections missing
//   Room      four corners of a former room: two short wall runs meeting at each corner
//   Pile      a collapsed heap, for variety between the built shapes
//
// The two rules that make it read as architecture rather than as props:
//   1. every piece in a run shares the run's angle (a few degrees of jitter, no more).
//      Random yaw per piece is what makes ruins look like litter.
//   2. the cursor advances by the piece's MEASURED width, so pieces touch instead of
//      floating apart at some average spacing.

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

    string excludeNames = "Piedestal, Tower, Big";
    float maxPieceFootprint = 6f;

    int clusterCount = 12;
    float siteSpacing = 55f;
    int seed = 7;

    // ---- layout ----
    float wallShare = 0.40f;
    float roomShare = 0.40f;                                    // the rest are piles
    Vector2 wallLength = new Vector2(6f, 16f);
    Vector2 roomSize = new Vector2(6f, 14f);
    Vector2 cornerRun = new Vector2(2.5f, 5f);
    float gapChance = 0.28f;
    float gapSize = 0.8f;
    float stepOverlap = 0.92f;                                  // <1 = pieces bite into each other
    float yawJitter = 7f;
    float fallenChance = 0.30f;
    Vector2Int rubblePerRuin = new Vector2Int(3, 8);
    float rubbleSpread = 1.6f;
    Vector2Int pilePieces = new Vector2Int(3, 6);
    Vector2 pileRadius = new Vector2(1.6f, 3.5f);

    // ---- look ----
    float alignToNormal = 0.45f;
    float standingTilt = 6f;
    float fallenTilt = 34f;
    float rubbleTilt = 12f;
    Vector2 kitScale = new Vector2(0.30f, 0.65f);
    Vector2 rubbleScale = new Vector2(0.45f, 0.9f);
    Vector2 kitSink = new Vector2(0.12f, 0.35f);
    float fallenExtraSink = 0.18f;
    float rubbleSink = 0.22f;

    // ---- placement rules ----
    Vector3 areaCenter;
    Vector2 areaSize = new Vector2(560f, 560f);
    bool areaInit;

    NordicScatterCore.BoundarySource boundarySource = NordicScatterCore.BoundarySource.GameplayObjects;
    float boundaryAmount = 35f;
    bool keepOffMountains = true;
    string mountainsPath = NordicScatterCore.MountainsPath;
    bool showBoundary = true;
    List<Vector2> boundary;
    Transform mountainT;
    int rejectedByBoundary;

    float gameplayClearance = 26f;
    float structureClearance = 14f;
    float navMeshClearance = 5f;
    float maxSlope = 15f;
    float kitShare = 0.55f;

    // ---- performance ----
    bool markStatic = true;
    bool contributeGI = false;
    bool gpuInstancing = true;
    bool addColliders = true;
    float colliderMinSize = 1.5f;

    // ---- run state ----
    List<GameObject> kit, rubble;
    List<NordicScatterCore.KeepOut> keepOut;
    bool navBaked;
    int placedCount, triCount;

    Vector2 scroll;

    [MenuItem("Tools/Nordic/Ruin Cluster Scatter")]
    static void Open()
    {
        var w = GetWindow<NordicRuinScatter>("Ruin Scatter");
        w.minSize = new Vector2(390f, 580f);
    }

    void OnEnable() { SceneView.duringSceneGui += OnScene; RefreshBoundary(); }
    void OnDisable() { SceneView.duringSceneGui -= OnScene; }
    void OnScene(SceneView sv) { if (showBoundary) NordicScatterCore.DrawBoundary(boundary, areaCenter.y + 2f); }

    void OnGUI()
    {
        if (!areaInit) { FitArea(); areaInit = true; }
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.HelpBox(
            "Ruins are laid out as walls and room corners, not scattered. Change the Seed and " +
            "press again to re-roll.", MessageType.None);

        Head("Where the pieces come from");
        kitFolder = EditorGUILayout.TextField("Kit folder", kitFolder);
        rubbleFolder = EditorGUILayout.TextField("Rubble folder", rubbleFolder);
        useRuinsFolder = EditorGUILayout.Toggle("Also use the Ruins folder", useRuinsFolder);
        using (new EditorGUI.DisabledScope(!useRuinsFolder))
            ruinsFolder = EditorGUILayout.TextField("  Ruins folder", ruinsFolder);
        excludeNames = EditorGUILayout.TextField("Never use (name contains)", excludeNames);
        maxPieceFootprint = EditorGUILayout.Slider("Max piece size (m)", maxPieceFootprint, 0.5f, 30f);

        Head("How many");
        clusterCount = EditorGUILayout.IntSlider("How many ruins", clusterCount, 1, 60);
        siteSpacing = EditorGUILayout.Slider("Space between ruins", siteSpacing, 10f, 200f);
        seed = EditorGUILayout.IntField("Seed", seed);

        Head("Shape of a ruin");
        wallShare = EditorGUILayout.Slider("Walls", wallShare, 0f, 1f);
        roomShare = EditorGUILayout.Slider("Room corners", roomShare, 0f, 1f - wallShare);
        EditorGUILayout.LabelField(" ", $"{Pct(wallShare)}% wall, {Pct(roomShare)}% room, {Pct(1f - wallShare - roomShare)}% pile");
        wallLength = EditorGUILayout.Vector2Field("Wall length (min, max)", wallLength);
        roomSize = EditorGUILayout.Vector2Field("Room size (min, max)", roomSize);
        cornerRun = EditorGUILayout.Vector2Field("Corner run length (min, max)", cornerRun);
        EditorGUILayout.Space(4);
        stepOverlap = EditorGUILayout.Slider("Piece overlap", stepOverlap, 0.5f, 1.2f);
        EditorGUILayout.LabelField(" ", stepOverlap < 1f ? "pieces bite into each other" : "pieces leave a seam");
        gapChance = EditorGUILayout.Slider("Missing sections", gapChance, 0f, 0.8f);
        gapSize = EditorGUILayout.Slider("Gap width", gapSize, 0.2f, 3f);
        yawJitter = EditorGUILayout.Slider("Angle jitter", yawJitter, 0f, 30f);
        if (yawJitter > 15f)
            EditorGUILayout.HelpBox("Past about 15° the run stops reading as one wall.", MessageType.Warning);
        fallenChance = EditorGUILayout.Slider("Fallen pieces", fallenChance, 0f, 1f);
        rubblePerRuin = EditorGUILayout.Vector2IntField("Rubble per ruin (min, max)", rubblePerRuin);
        rubbleSpread = EditorGUILayout.Slider("Rubble spread from wall", rubbleSpread, 0.2f, 8f);
        pilePieces = EditorGUILayout.Vector2IntField("Pile pieces (min, max)", pilePieces);
        pileRadius = EditorGUILayout.Vector2Field("Pile radius (min, max)", pileRadius);

        Head("Look");
        alignToNormal = EditorGUILayout.Slider("Follow ground angle", alignToNormal, 0f, 1f);
        standingTilt = EditorGUILayout.Slider("Standing lean", standingTilt, 0f, 30f);
        fallenTilt = EditorGUILayout.Slider("Fallen lean", fallenTilt, 0f, 90f);
        rubbleTilt = EditorGUILayout.Slider("Rubble lean", rubbleTilt, 0f, 45f);
        kitScale = EditorGUILayout.Vector2Field("Kit scale (min, max)", kitScale);
        rubbleScale = EditorGUILayout.Vector2Field("Rubble scale (min, max)", rubbleScale);
        kitSink = EditorGUILayout.Vector2Field("Kit buried (min, max)", kitSink);
        fallenExtraSink = EditorGUILayout.Slider("Fallen extra burial", fallenExtraSink, 0f, 0.5f);
        rubbleSink = EditorGUILayout.Slider("Rubble buried", rubbleSink, 0f, 0.6f);
        kitShare = EditorGUILayout.Slider("Kit vs rubble in piles", kitShare, 0f, 1f);

        Head("Where they may land");
        areaCenter = EditorGUILayout.Vector3Field("Area centre", areaCenter);
        areaSize = EditorGUILayout.Vector2Field("Area size (X, Z)", areaSize);
        if (GUILayout.Button("Fit area to the level")) FitArea();
        EditorGUILayout.Space(4);
        var newSource = (NordicScatterCore.BoundarySource)EditorGUILayout.EnumPopup("Boundary", boundarySource);
        if (newSource != boundarySource) { boundarySource = newSource; RefreshBoundary(); }
        using (new EditorGUI.DisabledScope(boundarySource == NordicScatterCore.BoundarySource.None))
        {
            if (boundarySource == NordicScatterCore.BoundarySource.GameplayObjects)
                boundaryAmount = EditorGUILayout.Slider("  Reach past gameplay (m)", boundaryAmount, 5f, 250f);
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

    static int Pct(float v) => Mathf.RoundToInt(Mathf.Clamp01(v) * 100f);

    void FitArea()
    {
        if (!NordicScatterCore.TryLevelBounds(out Bounds b)) return;
        areaCenter = new Vector3(b.center.x, 0f, b.center.z);
        areaSize = new Vector2(b.size.x + 40f, b.size.z + 40f);
    }

    void RefreshBoundary()
    {
        boundary = NordicScatterCore.BuildBoundary(boundarySource, mountainsPath, boundaryAmount);
        if (boundarySource != NordicScatterCore.BoundarySource.None && boundary == null)
            Debug.LogWarning($"[Ruins] Could not build a \"{boundarySource}\" boundary — it is off.");
        SceneView.RepaintAll();
    }

    void Clear()
    {
        var c = NordicScatterCore.FindContainer(Container, false);
        if (c == null) { Debug.Log("[Ruins] Nothing to clear."); return; }
        Undo.DestroyObjectImmediate(c);
        Dirty();
    }

    // =====================================================================
    void Scatter()
    {
        RefreshBoundary();
        rejectedByBoundary = 0;
        placedCount = 0;
        triCount = 0;

        var banned = excludeNames.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();

        bool Allowed(GameObject p)
        {
            foreach (var b in banned)
                if (p.name.IndexOf(b, System.StringComparison.OrdinalIgnoreCase) >= 0) return false;
            return NordicScatterCore.PrefabFootprint(p) <= maxPieceFootprint;
        }

        kit = NordicScatterCore.LoadPrefabs(kitFolder).Where(Allowed).ToList();
        if (useRuinsFolder) kit.AddRange(NordicScatterCore.LoadPrefabs(ruinsFolder).Where(Allowed));

        rubble = NordicScatterCore.LoadPrefabs(rubbleFolder).Where(p =>
        {
            string n = p.name.ToLowerInvariant();
            if (n.Contains("arbre") || n.Contains("tree") || n.Contains("grass")) return false;
            if (n.Contains("cliff") || n.Contains("mountain") || n.Contains("pillar") || n.Contains("overhang")) return false;
            return true;
        }).ToList();

        if (kit.Count == 0 && rubble.Count == 0)
        {
            EditorUtility.DisplayDialog("Ruin Scatter", "No prefabs survived the filters.", "OK");
            return;
        }

        var mr = keepOffMountains ? GameObject.Find(mountainsPath) : null;
        mountainT = mr != null ? mr.transform : null;

        var hidden = NordicScatterCore.HideScatters();
        keepOut = NordicScatterCore.BuildKeepOut(NordicScatterCore.DefaultAvoid,
                                                 NordicScatterCore.DefaultStructures,
                                                 gameplayClearance, structureClearance);
        navBaked = NordicScatterCore.HasNavMesh();
        var alreadyThere = NordicScatterCore.ExistingScatterPositions();

        var oldState = Random.state;
        Random.InitState(seed);

        // ---- pick the sites ----
        var sites = new List<Vector3>();
        int tries = 0, maxTries = clusterCount * 400;
        while (sites.Count < clusterCount && tries < maxTries)
        {
            tries++;
            var xz = new Vector2(areaCenter.x + (Random.value - 0.5f) * areaSize.x,
                                 areaCenter.z + (Random.value - 0.5f) * areaSize.y);
            if (!Accept(xz, maxSlope, out var g)) continue;
            if (!NordicScatterCore.Spaced(g.pos, sites, siteSpacing)) continue;
            if (!NordicScatterCore.Spaced(g.pos, alreadyThere, 8f)) continue;
            sites.Add(g.pos);
        }

        if (sites.Count == 0)
        {
            NordicScatterCore.RestoreScatters(hidden);
            Random.state = oldState;
            EditorUtility.DisplayDialog("Ruin Scatter",
                "No room found. Widen the area, lower the clearances, or raise the max slope.", "OK");
            return;
        }

        var old = NordicScatterCore.FindContainer(Container, false);
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = NordicScatterCore.FindContainer(Container, true);
        root.SetActive(false);

        int walls = 0, rooms = 0, piles = 0;

        for (int i = 0; i < sites.Count; i++)
        {
            float roll = Random.value;
            string kind = roll < wallShare ? "Wall" : (roll < wallShare + roomShare ? "Room" : "Pile");

            var group = new GameObject($"Ruin_{i:00}_{kind}");
            group.transform.SetParent(root.transform, false);
            group.transform.position = sites[i];
            Undo.RegisterCreatedObjectUndo(group, "Scatter Ruins");

            float yaw = Random.value * 360f;

            if (kind == "Wall") { BuildWall(group.transform, sites[i], yaw, Random.Range(wallLength.x, wallLength.y)); walls++; }
            else if (kind == "Room") { BuildRoom(group.transform, sites[i], yaw); rooms++; }
            else { BuildPile(group.transform, sites[i]); piles++; }

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
        Debug.Log($"[Ruins] {sites.Count} ruins — {walls} wall, {rooms} room, {piles} pile — " +
                  $"{placedCount} objects, about {triCount:N0} triangles. " +
                  $"{kit.Count} kit prefab(s) passed the filters. " +
                  $"Boundary: {(boundary != null ? $"{boundarySource}, rejected {rejectedByBoundary}" : "OFF")}.", root);
    }

    // =====================================================================
    // layouts
    // =====================================================================

    /// A run of pieces set edge to edge along one line.
    void BuildWall(Transform parent, Vector3 start, float yaw, float length)
    {
        if (kit.Count == 0) return;

        Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 from = start - dir * (length * 0.5f);

        float cursor = 0f;
        int guard = 0;
        var line = new List<Vector3>();

        while (cursor < length && guard++ < 200)
        {
            var prefab = kit[Random.Range(0, kit.Count)];
            float scale = Random.Range(kitScale.x, kitScale.y);

            // lay the piece along its own long axis, and step by that measured width
            float extraYaw = LongAxisYaw(prefab, out float width);
            width = Mathf.Max(0.25f, width * scale);

            if (Random.value < gapChance)      // a section that has fallen away entirely
            {
                cursor += width * gapSize;
                continue;
            }

            Vector3 at = from + dir * (cursor + width * 0.5f);
            bool fallen = Random.value < fallenChance;

            if (PlacePiece(prefab, parent, new Vector2(at.x, at.z),
                           yaw + extraYaw + Random.Range(-yawJitter, yawJitter),
                           fallen ? fallenTilt : standingTilt,
                           scale,
                           Random.Range(kitSink.x, kitSink.y) + (fallen ? fallenExtraSink : 0f)))
                line.Add(at);

            cursor += width * stepOverlap;
        }

        ScatterRubbleAlong(parent, line);
    }

    /// Four corners of a room: two short runs meeting at each corner, open in between.
    void BuildRoom(Transform parent, Vector3 centre, float yaw)
    {
        float w = Random.Range(roomSize.x, roomSize.y) * 0.5f;
        float d = Random.Range(roomSize.x, roomSize.y) * 0.5f;
        var rot = Quaternion.Euler(0f, yaw, 0f);

        // corner sign pairs, and the two directions each run travels from that corner
        var corners = new[]
        {
            new Vector2(+1f, +1f), new Vector2(-1f, +1f),
            new Vector2(-1f, -1f), new Vector2(+1f, -1f),
        };

        foreach (var c in corners)
        {
            Vector3 corner = centre + rot * new Vector3(c.x * w, 0f, c.y * d);

            // one run back along X, one back along Z — that is what makes a corner
            RunFrom(parent, corner, rot * new Vector3(-c.x, 0f, 0f), yaw + 90f, Random.Range(cornerRun.x, cornerRun.y));
            RunFrom(parent, corner, rot * new Vector3(0f, 0f, -c.y), yaw, Random.Range(cornerRun.x, cornerRun.y));
        }
    }

    void RunFrom(Transform parent, Vector3 from, Vector3 dir, float baseYaw, float length)
    {
        if (kit.Count == 0) return;
        dir = dir.normalized;

        float cursor = 0f;
        int guard = 0;
        var line = new List<Vector3>();

        while (cursor < length && guard++ < 60)
        {
            var prefab = kit[Random.Range(0, kit.Count)];
            float scale = Random.Range(kitScale.x, kitScale.y);
            float extraYaw = LongAxisYaw(prefab, out float width);
            width = Mathf.Max(0.25f, width * scale);

            if (Random.value < gapChance * 0.6f)    // corners survive better than mid-wall
            {
                cursor += width * gapSize;
                continue;
            }

            Vector3 at = from + dir * (cursor + width * 0.5f);
            bool fallen = Random.value < fallenChance;

            if (PlacePiece(prefab, parent, new Vector2(at.x, at.z),
                           baseYaw + extraYaw + Random.Range(-yawJitter, yawJitter),
                           fallen ? fallenTilt : standingTilt,
                           scale,
                           Random.Range(kitSink.x, kitSink.y) + (fallen ? fallenExtraSink : 0f)))
                line.Add(at);

            cursor += width * stepOverlap;
        }

        ScatterRubbleAlong(parent, line);
    }

    /// A collapsed heap — no line, just pieces leaning on each other.
    void BuildPile(Transform parent, Vector3 centre)
    {
        int want = Random.Range(pilePieces.x, pilePieces.y + 1);
        float radius = Random.Range(pileRadius.x, pileRadius.y);
        float yaw = Random.value * 360f;

        for (int i = 0; i < want; i++)
        {
            bool useKit = kit.Count > 0 && (rubble.Count == 0 || Random.value < kitShare);
            var prefab = useKit ? kit[Random.Range(0, kit.Count)] : rubble[Random.Range(0, rubble.Count)];

            var off = i == 0 ? Vector2.zero : NordicScatterCore.ClusterOffset(radius);
            var xz = new Vector2(centre.x + off.x, centre.z + off.y);

            PlacePiece(prefab, parent, xz,
                       yaw + Random.Range(-40f, 40f),
                       useKit ? fallenTilt : rubbleTilt,
                       Random.Range(useKit ? kitScale.x : rubbleScale.x, useKit ? kitScale.y : rubbleScale.y),
                       useKit ? Random.Range(kitSink.x, kitSink.y) + fallenExtraSink : rubbleSink);
        }
    }

    /// Rubble hugging the base of a run, where a wall sheds its stone.
    void ScatterRubbleAlong(Transform parent, List<Vector3> line)
    {
        if (rubble.Count == 0 || line.Count == 0) return;

        int want = Random.Range(rubblePerRuin.x, rubblePerRuin.y + 1);
        for (int i = 0; i < want; i++)
        {
            var at = line[Random.Range(0, line.Count)];
            var off = Random.insideUnitCircle * rubbleSpread;
            PlacePiece(rubble[Random.Range(0, rubble.Count)], parent,
                       new Vector2(at.x + off.x, at.z + off.y),
                       Random.value * 360f, rubbleTilt,
                       Random.Range(rubbleScale.x, rubbleScale.y), rubbleSink);
        }
    }

    // =====================================================================
    // helpers
    // =====================================================================

    /// Extra yaw that turns a piece so its longest horizontal side runs along the wall,
    /// and that side's length, so the cursor can step by it.
    float LongAxisYaw(GameObject prefab, out float width)
    {
        if (!NordicScatterCore.TryPrefabBounds(prefab, out Bounds b)) { width = 1f; return 0f; }
        if (b.size.z > b.size.x) { width = b.size.z; return 90f; }
        width = b.size.x;
        return 0f;
    }

    bool PlacePiece(GameObject prefab, Transform parent, Vector2 xz,
                    float yaw, float tilt, float scale, float sink)
    {
        if (!Accept(xz, maxSlope + 12f, out var g)) return false;

        var wrapper = NordicScatterCore.MakeWrapper(prefab.name, parent, g.pos);
        var go = NordicScatterCore.Place(prefab, wrapper.transform, g,
            alignToNormal, yaw, tilt, Vector3.one * scale, sink, out Bounds wb);
        if (go == null) { DestroyImmediate(wrapper); return false; }

        if (markStatic) { NordicScatterCore.MakeStatic(go, contributeGI); NordicScatterCore.MakeStatic(wrapper, false); }
        float size = Mathf.Max(wb.size.x, wb.size.y, wb.size.z);
        if (addColliders && size >= colliderMinSize) NordicScatterCore.EnsureBoxCollider(wrapper, wb);

        triCount += NordicScatterCore.CountTris(go);
        placedCount++;
        return true;
    }

    bool Accept(Vector2 xz, float slopeLimit, out NordicScatterCore.Ground g)
    {
        if (!NordicScatterCore.SampleGround(xz, out g)) return false;
        if (!NordicScatterCore.Inside(g.pos, boundary)) { rejectedByBoundary++; return false; }
        if (NordicScatterCore.IsUnder(g.hit, mountainT)) return false;
        if (g.slope > slopeLimit) return false;
        if (NordicScatterCore.IsAvoided(g.hit, NordicScatterCore.DefaultAvoid, NordicScatterCore.DefaultStructures)) return false;
        if (NordicScatterCore.TooClose(g.pos, keepOut)) return false;
        if (navBaked && navMeshClearance > 0.01f && NordicScatterCore.OnWalkable(g.pos, navMeshClearance)) return false;
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
