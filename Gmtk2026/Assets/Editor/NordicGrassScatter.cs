// Nordic Grass Scatter — Countdown Nordic
// Menu:  Tools > Nordic > Grass Scatter
//
// Two pools, two behaviours, because they are not the same kind of asset:
//   Field patches (SM_Grass_Field_01) — 13 x 13 m of ground cover, 1153 triangles.
//       Cheap coverage, but it is a FLAT card: only near-level ground, or the edges float.
//   Reed clumps (SM_Grass_Reed_01..09) — 10 to 29 triangles each, dropped in clusters
//       to break the patches up and to dress slopes the patches cannot take.
//
// No colliders on anything here. Grass the player bumps into is a bug, not a feature.

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class NordicGrassScatter : EditorWindow
{
    const string Container = "Grass_Scatter";

    string prefabFolder = "Assets/Prefabs/Environment";
    string fieldFilter = "Grass_Field";
    string reedFilter = "Grass_Reed";

    int fieldCount = 26;
    float fieldSpacing = 11f;
    float fieldMaxSlope = 6f;
    Vector2 fieldScale = new Vector2(0.9f, 1.35f);
    float fieldSink = 0.25f;
    float fieldAlign = 0.9f;

    int clumpCount = 30;
    Vector2Int reedsPerClump = new Vector2Int(4, 10);
    Vector2 clumpRadius = new Vector2(2f, 6f);
    float clumpSpacing = 22f;
    float reedSpacing = 1.1f;
    float reedMaxSlope = 26f;
    Vector2 reedScale = new Vector2(0.55f, 1.15f);
    float reedAlign = 0.15f;
    float reedTilt = 6f;
    float reedSink = 0.06f;

    int seed = 99;

    Vector3 areaCenter;
    Vector2 areaSize = new Vector2(560f, 560f);
    bool areaInit;

    NordicScatterCore.BoundarySource boundarySource = NordicScatterCore.BoundarySource.GameplayObjects;
    float boundaryAmount = 45f;
    bool keepOffMountains = true;
    string mountainsPath = NordicScatterCore.MountainsPath;
    bool showBoundary = true;
    List<Vector2> boundary;
    Transform mountainT;
    int rejectedByBoundary;

    float gameplayClearance = 6f;      // grass may come close, it blocks nothing
    float structureClearance = 3f;
    float navMeshClearance = 0f;       // off: grass on the path is fine

    bool markStatic = true;
    bool gpuInstancing = true;

    Vector2 scroll;

    [MenuItem("Tools/Nordic/Grass Scatter")]
    static void Open()
    {
        var w = GetWindow<NordicGrassScatter>("Grass Scatter");
        w.minSize = new Vector2(380f, 560f);
    }

    void OnEnable() { SceneView.duringSceneGui += OnScene; RefreshBoundary(); }
    void OnDisable() { SceneView.duringSceneGui -= OnScene; }
    void OnScene(SceneView sv) { if (showBoundary) NordicScatterCore.DrawBoundary(boundary, areaCenter.y + 2f); }

    void OnGUI()
    {
        if (!areaInit) { FitArea(); areaInit = true; }
        scroll = EditorGUILayout.BeginScrollView(scroll);

        int fields = NordicScatterCore.LoadPrefabs(prefabFolder, fieldFilter).Count;
        int reeds = NordicScatterCore.LoadPrefabs(prefabFolder, reedFilter).Count;

        EditorGUILayout.HelpBox(
            $"{fields} field patch(es), {reeds} reed clump(s) found.\n" +
            (fields + reeds == 0 ? "Run Tools > Nordic > Grass Setup first." :
             "Field patches carpet the flat ground, reed clumps break them up."),
            fields + reeds == 0 ? MessageType.Warning : MessageType.None);

        Head("Source");
        prefabFolder = EditorGUILayout.TextField("Folder", prefabFolder);
        fieldFilter = EditorGUILayout.TextField("Field name contains", fieldFilter);
        reedFilter = EditorGUILayout.TextField("Reed name contains", reedFilter);
        seed = EditorGUILayout.IntField("Seed", seed);

        Head("Field patches  (flat ground only)");
        fieldCount = EditorGUILayout.IntSlider("How many", fieldCount, 0, 200);
        fieldSpacing = EditorGUILayout.Slider("Spacing", fieldSpacing, 2f, 40f);
        fieldMaxSlope = EditorGUILayout.Slider("Max slope", fieldMaxSlope, 0f, 30f);
        if (fieldMaxSlope > 10f)
            EditorGUILayout.HelpBox("A 13 m flat card past about 10° lifts its corners off the ground.",
                                    MessageType.Warning);
        fieldScale = EditorGUILayout.Vector2Field("Scale (min, max)", fieldScale);
        fieldAlign = EditorGUILayout.Slider("Follow ground angle", fieldAlign, 0f, 1f);
        fieldSink = EditorGUILayout.Slider("Buried", fieldSink, 0f, 0.9f);
        EditorGUILayout.LabelField(" ", $"about {fieldCount * 1153:N0} triangles");

        Head("Reed clumps");
        clumpCount = EditorGUILayout.IntSlider("How many clumps", clumpCount, 0, 200);
        reedsPerClump = EditorGUILayout.Vector2IntField("Reeds per clump (min, max)", reedsPerClump);
        clumpRadius = EditorGUILayout.Vector2Field("Clump radius (min, max)", clumpRadius);
        clumpSpacing = EditorGUILayout.Slider("Space between clumps", clumpSpacing, 2f, 80f);
        reedSpacing = EditorGUILayout.Slider("Space between reeds", reedSpacing, 0.2f, 6f);
        reedMaxSlope = EditorGUILayout.Slider("Max slope", reedMaxSlope, 0f, 50f);
        reedScale = EditorGUILayout.Vector2Field("Scale (min, max)", reedScale);
        reedAlign = EditorGUILayout.Slider("Follow ground angle", reedAlign, 0f, 1f);
        reedTilt = EditorGUILayout.Slider("Random tilt", reedTilt, 0f, 20f);
        reedSink = EditorGUILayout.Slider("Buried", reedSink, 0f, 0.4f);
        int reedEstimate = clumpCount * (reedsPerClump.x + reedsPerClump.y) / 2;
        EditorGUILayout.LabelField(" ", $"about {reedEstimate} reeds, {reedEstimate * 20:N0} triangles");

        Head("Where it may land");
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
        gameplayClearance = EditorGUILayout.Slider("Away from gameplay", gameplayClearance, 0f, 60f);
        structureClearance = EditorGUILayout.Slider("Away from buildings", structureClearance, 0f, 60f);
        navMeshClearance = EditorGUILayout.Slider("Away from walkable navmesh", navMeshClearance, 0f, 30f);

        Head("Performance");
        markStatic = EditorGUILayout.Toggle("Mark static (batching)", markStatic);
        gpuInstancing = EditorGUILayout.Toggle("GPU instancing on materials", gpuInstancing);
        EditorGUILayout.HelpBox("No colliders and no shadow casting on grass — both are set on the " +
                                "prefabs by Grass Setup.", MessageType.None);

        EditorGUILayout.Space(12);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
            if (GUILayout.Button("Scatter grass", GUILayout.Height(34))) Scatter();
            GUI.backgroundColor = new Color(0.95f, 0.6f, 0.6f);
            if (GUILayout.Button("Clear", GUILayout.Height(34), GUILayout.Width(90))) Clear();
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.Space(16);
        EditorGUILayout.EndScrollView();
    }

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
            Debug.LogWarning($"[Grass] Could not build a \"{boundarySource}\" boundary — it is off.");
        SceneView.RepaintAll();
    }

    void Clear()
    {
        var c = NordicScatterCore.FindContainer(Container, false);
        if (c == null) { Debug.Log("[Grass] Nothing to clear."); return; }
        Undo.DestroyObjectImmediate(c);
        Dirty();
    }

    void Scatter()
    {
        var fields = NordicScatterCore.LoadPrefabs(prefabFolder, fieldFilter);
        var reeds = NordicScatterCore.LoadPrefabs(prefabFolder, reedFilter);
        if (fields.Count == 0 && reeds.Count == 0)
        {
            EditorUtility.DisplayDialog("Grass Scatter",
                "No grass prefabs found. Run Tools > Nordic > Grass Setup first.", "OK");
            return;
        }

        RefreshBoundary();
        rejectedByBoundary = 0;

        var mr = keepOffMountains ? GameObject.Find(mountainsPath) : null;
        mountainT = mr != null ? mr.transform : null;

        var hidden = NordicScatterCore.HideScatters();
        var keepOut = NordicScatterCore.BuildKeepOut(NordicScatterCore.DefaultAvoid,
                                                     NordicScatterCore.DefaultStructures,
                                                     gameplayClearance, structureClearance);
        bool nav = NordicScatterCore.HasNavMesh();

        var oldState = Random.state;
        Random.InitState(seed);

        var old = NordicScatterCore.FindContainer(Container, false);
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = NordicScatterCore.FindContainer(Container, true);
        root.SetActive(false);

        int tris = 0, fieldsPlaced = 0, reedsPlaced = 0;

        // ---- field patches ----
        if (fields.Count > 0 && fieldCount > 0)
        {
            var group = new GameObject("Field_Patches");
            group.transform.SetParent(root.transform, false);
            Undo.RegisterCreatedObjectUndo(group, "Scatter Grass");

            var taken = new List<Vector3>();
            int tries = 0, maxTries = fieldCount * 300;
            while (taken.Count < fieldCount && tries < maxTries)
            {
                tries++;
                if (!Accept(RandomXZ(), keepOut, nav, fieldMaxSlope, out var g)) continue;
                if (!NordicScatterCore.Spaced(g.pos, taken, fieldSpacing)) continue;

                var prefab = fields[Random.Range(0, fields.Count)];
                float s = Random.Range(fieldScale.x, fieldScale.y);
                var go = NordicScatterCore.Place(prefab, group.transform, g,
                    fieldAlign, Random.value * 360f, 0f, Vector3.one * s, fieldSink, out _);
                if (go == null) continue;

                if (markStatic) NordicScatterCore.MakeStatic(go, false);
                tris += NordicScatterCore.CountTris(go);
                taken.Add(g.pos);
                fieldsPlaced++;
            }

            if (group.transform.childCount == 0) DestroyImmediate(group);
        }

        // ---- reed clumps ----
        if (reeds.Count > 0 && clumpCount > 0)
        {
            var centres = new List<Vector3>();
            int tries = 0, maxTries = clumpCount * 300;
            while (centres.Count < clumpCount && tries < maxTries)
            {
                tries++;
                if (!Accept(RandomXZ(), keepOut, nav, reedMaxSlope, out var g)) continue;
                if (!NordicScatterCore.Spaced(g.pos, centres, clumpSpacing)) continue;
                centres.Add(g.pos);
            }

            for (int c = 0; c < centres.Count; c++)
            {
                float radius = Random.Range(clumpRadius.x, clumpRadius.y);
                int want = Random.Range(reedsPerClump.x, reedsPerClump.y + 1);

                var group = new GameObject($"Reeds_{c:00}");
                group.transform.SetParent(root.transform, false);
                group.transform.position = centres[c];
                Undo.RegisterCreatedObjectUndo(group, "Scatter Grass");

                var local = new List<Vector3>();
                for (int i = 0; i < want; i++)
                {
                    var off = NordicScatterCore.ClusterOffset(radius);
                    var xz = new Vector2(centres[c].x + off.x, centres[c].z + off.y);
                    if (!Accept(xz, keepOut, nav, reedMaxSlope, out var g)) continue;
                    if (!NordicScatterCore.Spaced(g.pos, local, reedSpacing)) continue;

                    var prefab = reeds[Random.Range(0, reeds.Count)];
                    float s = Random.Range(reedScale.x, reedScale.y);
                    var scale = new Vector3(s, s * Random.Range(0.85f, 1.2f), s);
                    var go = NordicScatterCore.Place(prefab, group.transform, g,
                        reedAlign, Random.value * 360f, reedTilt, scale, reedSink, out _);
                    if (go == null) continue;

                    if (markStatic) NordicScatterCore.MakeStatic(go, false);
                    tris += NordicScatterCore.CountTris(go);
                    local.Add(g.pos);
                    reedsPlaced++;
                }

                if (group.transform.childCount == 0) DestroyImmediate(group);
            }
        }

        root.SetActive(true);
        NordicScatterCore.RestoreScatters(hidden);
        Random.state = oldState;

        if (gpuInstancing)
        {
            var all = new List<GameObject>(fields); all.AddRange(reeds);
            NordicScatterCore.EnableInstancing(all);
        }

        Dirty();
        Selection.activeGameObject = root;
        Debug.Log($"[Grass] {fieldsPlaced} field patches + {reedsPlaced} reeds — about {tris:N0} triangles. " +
                  $"Boundary: {(boundary != null ? $"{boundarySource}, rejected {rejectedByBoundary} samples" : "OFF")}. " +
                  $"NavMesh check: {(nav && navMeshClearance > 0.01f ? "on" : "off")}.", root);
    }

    Vector2 RandomXZ()
    {
        return new Vector2(
            areaCenter.x + (Random.value - 0.5f) * areaSize.x,
            areaCenter.z + (Random.value - 0.5f) * areaSize.y);
    }

    bool Accept(Vector2 xz, List<NordicScatterCore.KeepOut> keepOut, bool nav, float maxSlope,
                out NordicScatterCore.Ground g)
    {
        if (!NordicScatterCore.SampleGround(xz, out g)) return false;
        if (!NordicScatterCore.Inside(g.pos, boundary)) { rejectedByBoundary++; return false; }
        if (NordicScatterCore.IsUnder(g.hit, mountainT)) return false;
        if (g.slope > maxSlope) return false;
        if (NordicScatterCore.IsAvoided(g.hit, NordicScatterCore.DefaultAvoid, NordicScatterCore.DefaultStructures)) return false;
        if (NordicScatterCore.TooClose(g.pos, keepOut)) return false;
        if (nav && navMeshClearance > 0.01f && NordicScatterCore.OnWalkable(g.pos, navMeshClearance)) return false;
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
