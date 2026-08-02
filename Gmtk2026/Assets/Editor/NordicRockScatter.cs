// Nordic Rock Scatter — Countdown Nordic
// Scatters the rock prefabs from Assets/Prefabs/Environment over the ground,
// keeping clear of the player, the enemies, the structures and the walkable path.
// Menu:  Tools > Nordic > Rock Scatter
//
// Everything it creates lives under a single "Rocks_Scatter" object, so one
// button removes it all again. Ctrl+Z also undoes a whole scatter in one step.

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

public class NordicRockScatter : EditorWindow
{
    // ---------- settings ----------
    string prefabFolder = "Assets/Prefabs/Environment";
    string excludeNames = "Arbre, Tree, Bush, Plant";
    string containerName = "Rocks_Scatter";
    string parentPath = "Environnement";

    int targetCount = 160;
    int seed = 1234;

    Vector3 areaCenter = new Vector3(0f, 0f, 0f);
    Vector2 areaSize = new Vector2(560f, 560f);
    bool areaInitialised = false;

    NordicScatterCore.BoundarySource boundarySource = NordicScatterCore.BoundarySource.GameplayObjects;
    float boundaryAmount = 70f;
    bool terrainOnly = true;        // ground means the terrain, not a mountain and not a prop
    bool keepOffMountains = true;
    string mountainsPath = NordicScatterCore.MountainsPath;
    Transform mountainT;
    int rejectedNotGround;
    bool showBoundary = true;
    List<Vector2> boundary;

    float gameplayClearance = 22f;   // metres kept free around player / enemies / guns / doors
    float structureClearance = 10f;  // metres kept free around buildings
    float navMeshClearance = 4f;     // metres kept free around walkable navmesh (0 = off)
    float minSpacing = 7f;           // metres between two scattered rocks

    float minSlope = 0f;
    float maxSlope = 58f;
    float flatGroundAcceptance = 0.22f; // on near-flat ground, only this share of samples is kept
    float flatSlopeThreshold = 8f;
    float largeMinSlope = 12f;          // cliff / mountain pieces need at least this slope

    float alignToNormal = 0.6f;
    float randomTilt = 7f;
    float scaleMin = 0.7f;
    float scaleMax = 1.6f;
    float sinkFraction = 0.14f;         // share of the rock's height buried in the ground

    bool markStatic = true;
    bool enableGpuInstancing = true;
    bool addColliders = true;
    float colliderMinSize = 1.5f;       // rocks smaller than this stay collider-free
    bool contributeGI = false;

    string[] avoidKeywords = { "Player", "Spawn", "Enem", "Gun", "Door", "Shop", "Bricks" };
    string[] structureKeywords = { "Structure", "Building" };

    Vector2 scroll;

    [MenuItem("Tools/Nordic/Rock Scatter")]
    static void Open()
    {
        var w = GetWindow<NordicRockScatter>("Rock Scatter");
        w.minSize = new Vector2(360f, 520f);
    }

    void OnGUI()
    {
        if (!areaInitialised) { GuessArea(); areaInitialised = true; }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.HelpBox(
            "Scatters rocks on the ground, away from the player, the enemies and the walkable path.\n" +
            "Run it, look, change the Seed, run it again. Clear removes every scattered rock.",
            MessageType.None);

        EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
        prefabFolder = EditorGUILayout.TextField("Prefab folder", prefabFolder);
        excludeNames = EditorGUILayout.TextField("Never use (name contains)", excludeNames);
        EditorGUILayout.LabelField(" ", $"{LoadPrefabs().Count} rock prefab(s) match");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("How many, and where", EditorStyles.boldLabel);
        targetCount = EditorGUILayout.IntSlider("Rock count", targetCount, 10, 600);
        seed = EditorGUILayout.IntField("Seed (change to re-roll)", seed);
        areaCenter = EditorGUILayout.Vector3Field("Area centre", areaCenter);
        areaSize = EditorGUILayout.Vector2Field("Area size (X, Z)", areaSize);
        if (GUILayout.Button("Fit area to the level")) GuessArea();

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
        terrainOnly = EditorGUILayout.Toggle("Only on the terrain", terrainOnly);
        keepOffMountains = EditorGUILayout.Toggle("Never on the mountains", keepOffMountains);
        EditorGUILayout.HelpBox("\"Only on the terrain\" refuses any spot where the ray lands on " +
                                "something other than a Terrain — mountains, platforms, walls, other props. " +
                                "It is the rule that stops rocks stacking on top of things.",
                                MessageType.None);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Keep out of the way", EditorStyles.boldLabel);
        gameplayClearance = EditorGUILayout.Slider("Away from gameplay", gameplayClearance, 0f, 60f);
        structureClearance = EditorGUILayout.Slider("Away from buildings", structureClearance, 0f, 60f);
        navMeshClearance = EditorGUILayout.Slider("Away from walkable navmesh", navMeshClearance, 0f, 30f);
        minSpacing = EditorGUILayout.Slider("Space between rocks", minSpacing, 1f, 40f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Ground rules", EditorStyles.boldLabel);
        EditorGUILayout.MinMaxSlider(new GUIContent("Slope range (deg)"), ref minSlope, ref maxSlope, 0f, 80f);
        EditorGUILayout.LabelField(" ", $"{minSlope:0}° – {maxSlope:0}°");
        flatSlopeThreshold = EditorGUILayout.Slider("\"Flat\" below (deg)", flatSlopeThreshold, 0f, 25f);
        flatGroundAcceptance = EditorGUILayout.Slider("Keep on flat ground", flatGroundAcceptance, 0f, 1f);
        largeMinSlope = EditorGUILayout.Slider("Cliff pieces need slope", largeMinSlope, 0f, 45f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Look", EditorStyles.boldLabel);
        alignToNormal = EditorGUILayout.Slider("Follow ground angle", alignToNormal, 0f, 1f);
        randomTilt = EditorGUILayout.Slider("Random tilt (deg)", randomTilt, 0f, 25f);
        EditorGUILayout.BeginHorizontal();
        scaleMin = EditorGUILayout.FloatField("Scale min", scaleMin);
        scaleMax = EditorGUILayout.FloatField("max", scaleMax);
        EditorGUILayout.EndHorizontal();
        sinkFraction = EditorGUILayout.Slider("Buried in ground", sinkFraction, 0f, 0.5f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Performance", EditorStyles.boldLabel);
        markStatic = EditorGUILayout.Toggle("Mark static (batching)", markStatic);
        contributeGI = EditorGUILayout.Toggle("Contribute to lightmaps", contributeGI);
        enableGpuInstancing = EditorGUILayout.Toggle("GPU instancing on material", enableGpuInstancing);
        addColliders = EditorGUILayout.Toggle("Colliders on big rocks", addColliders);
        using (new EditorGUI.DisabledScope(!addColliders))
            colliderMinSize = EditorGUILayout.Slider("  Collider above size (m)", colliderMinSize, 0.2f, 6f);

        EditorGUILayout.Space(12);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
            if (GUILayout.Button("Scatter", GUILayout.Height(34))) Scatter();
            GUI.backgroundColor = new Color(0.95f, 0.6f, 0.6f);
            if (GUILayout.Button("Clear", GUILayout.Height(34), GUILayout.Width(90))) Clear();
            GUI.backgroundColor = Color.white;
        }

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
            Debug.LogWarning($"[Rock Scatter] Could not build a \"{boundarySource}\" boundary — it is off.");
        SceneView.RepaintAll();
    }

    // ---------- area guess ----------
    void GuessArea()
    {
        var env = FindByPath(parentPath);
        Bounds b = new Bounds();
        bool got = false;

        if (env != null)
        {
            foreach (var r in env.GetComponentsInChildren<Renderer>())
            {
                if (!got) { b = r.bounds; got = true; }
                else b.Encapsulate(r.bounds);
            }
        }

        if (!got)
        {
            foreach (var t in Terrain.activeTerrains)
            {
                var tb = new Bounds(t.transform.position + t.terrainData.size * 0.5f, t.terrainData.size);
                if (!got) { b = tb; got = true; } else b.Encapsulate(tb);
            }
        }

        if (!got) return;

        areaCenter = new Vector3(b.center.x, 0f, b.center.z);
        areaSize = new Vector2(b.size.x + 60f, b.size.z + 60f);
    }

    // ---------- clearing ----------
    void Clear()
    {
        var c = FindContainer(false);
        if (c == null) { Debug.Log("[Rock Scatter] Nothing to clear."); return; }
        Undo.DestroyObjectImmediate(c);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[Rock Scatter] Cleared.");
    }

    // ---------- the work ----------
    void Scatter()
    {
        var prefabs = LoadPrefabs();
        if (prefabs.Count == 0)
        {
            EditorUtility.DisplayDialog("Rock Scatter",
                "No prefabs found in " + prefabFolder + ".", "OK");
            return;
        }

        // hide any previous scatter so raycasts don't land on old rocks
        var oldContainer = FindContainer(false);
        bool oldWasActive = oldContainer != null && oldContainer.activeSelf;
        if (oldContainer != null) oldContainer.SetActive(false);

        RefreshBoundary();
        rejectedNotGround = 0;

        var mr = keepOffMountains ? GameObject.Find(mountainsPath) : null;
        mountainT = mr != null ? mr.transform : null;

        var keepOut = BuildKeepOutList();
        bool hasNavMesh = NavMesh.CalculateTriangulation().vertices.Length > 0;

        var rng = new System.Random(seed);
        var placed = new List<Vector3>(targetCount);
        var picks = new List<Sample>(targetCount);

        int attempts = 0;
        int maxAttempts = targetCount * 60;
        float top = 2000f;

        while (picks.Count < targetCount && attempts < maxAttempts)
        {
            attempts++;

            float x = areaCenter.x + ((float)rng.NextDouble() - 0.5f) * areaSize.x;
            float z = areaCenter.z + ((float)rng.NextDouble() - 0.5f) * areaSize.y;

            if (!SampleGround(new Vector2(x, z), top, out Vector3 point, out Vector3 normal, out GameObject hitObj))
                continue;

            if (!NordicScatterCore.Inside(point, boundary)) continue;

            // ground means the terrain itself — never a mountain, never on top of a prop
            if (terrainOnly && (hitObj == null || hitObj.GetComponent<Terrain>() == null))
            { rejectedNotGround++; continue; }
            if (NordicScatterCore.IsUnder(hitObj, mountainT)) { rejectedNotGround++; continue; }

            float slope = Vector3.Angle(normal, Vector3.up);
            if (slope < minSlope || slope > maxSlope) continue;

            // near-flat ground is where the game is played: keep only a few, isolated
            if (slope < flatSlopeThreshold && rng.NextDouble() > flatGroundAcceptance) continue;

            // don't sit on the level geometry itself
            if (hitObj != null && IsAvoided(hitObj)) continue;

            if (TooCloseToKeepOut(point, keepOut)) continue;

            if (navMeshClearance > 0.01f && hasNavMesh &&
                NavMesh.SamplePosition(point, out NavMeshHit nh, navMeshClearance, NavMesh.AllAreas) &&
                nh.distance < navMeshClearance)
                continue;

            bool spaced = true;
            for (int i = 0; i < placed.Count; i++)
            {
                float dx = placed[i].x - point.x, dz = placed[i].z - point.z;
                if (dx * dx + dz * dz < minSpacing * minSpacing) { spaced = false; break; }
            }
            if (!spaced) continue;

            placed.Add(point);
            picks.Add(new Sample { pos = point, normal = normal, slope = slope });
        }

        if (oldContainer != null) oldContainer.SetActive(oldWasActive);

        if (picks.Count == 0)
        {
            EditorUtility.DisplayDialog("Rock Scatter",
                "No valid spot found. Loosen the clearances, widen the area, or check the ground has colliders.",
                "OK");
            return;
        }

        // fresh container
        var container = FindContainer(false);
        if (container != null) Undo.DestroyObjectImmediate(container);
        container = FindContainer(true);

        int small = 0, medium = 0, large = 0, tris = 0;

        for (int i = 0; i < picks.Count; i++)
        {
            var s = picks[i];
            bool allowLarge = s.slope >= largeMinSlope;

            var entry = PickPrefab(prefabs, rng, allowLarge);
            if (entry == null) continue;

            var go = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab, container.scene);
            if (go == null) continue;

            Undo.RegisterCreatedObjectUndo(go, "Scatter Rocks");
            go.transform.SetParent(container.transform, true);

            // ground alignment, on top of the prefab's own baked rotation (FBX -90 X)
            Quaternion baseRot = entry.prefab.transform.localRotation;
            Quaternion yaw = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            Quaternion lean = Quaternion.Slerp(Quaternion.identity,
                                               Quaternion.FromToRotation(Vector3.up, s.normal),
                                               alignToNormal);
            Quaternion tilt = Quaternion.Euler(
                ((float)rng.NextDouble() - 0.5f) * 2f * randomTilt, 0f,
                ((float)rng.NextDouble() - 0.5f) * 2f * randomTilt);

            go.transform.rotation = lean * tilt * yaw * baseRot;

            float sc = Mathf.Lerp(scaleMin, scaleMax, (float)rng.NextDouble());
            go.transform.localScale = entry.prefab.transform.localScale * sc;

            // sit the rock on the ground using its real bounds, not its pivot
            go.transform.position = s.pos;
            if (TryWorldBounds(go, out Bounds wb))
            {
                float lift = s.pos.y - wb.min.y;
                float sink = wb.size.y * sinkFraction;
                go.transform.position = s.pos + Vector3.up * (lift - sink);
                tris += CountTris(go);
                float size = Mathf.Max(wb.size.x, wb.size.y, wb.size.z);
                if (addColliders && size >= colliderMinSize) EnsureCollider(go, wb);
            }

            if (markStatic)
            {
                var flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic;
                if (contributeGI) flags |= StaticEditorFlags.ContributeGI;
                GameObjectUtility.SetStaticEditorFlags(go, flags);
            }

            switch (entry.size)
            {
                case RockSize.Small: small++; break;
                case RockSize.Medium: medium++; break;
                default: large++; break;
            }
        }

        if (enableGpuInstancing) EnableInstancing(prefabs);

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Selection.activeGameObject = container;

        Debug.Log($"[Rock Scatter] {picks.Count} rocks placed " +
                  $"(small {small}, medium {medium}, cliff {large}) — about {tris:N0} triangles, " +
                  $"{attempts} tries, {rejectedNotGround} refused for not being on the terrain. " +
                  $"Boundary: {(boundary != null ? $"{boundarySource}, {boundary.Count} sides" : "OFF")}. " +
                  $"NavMesh check: {(hasNavMesh ? "on" : "no navmesh baked")}.", container);
    }

    // ---------- ground ----------
    bool SampleGround(Vector2 xz, float top, out Vector3 point, out Vector3 normal, out GameObject hitObj)
    {
        var origin = new Vector3(xz.x, top, xz.y);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, top * 2.5f,
                            ~0, QueryTriggerInteraction.Ignore))
        {
            point = hit.point; normal = hit.normal; hitObj = hit.collider.gameObject;
            return true;
        }

        // fallback: read the terrain height directly, in case colliders are off
        foreach (var t in Terrain.activeTerrains)
        {
            var tp = t.transform.position;
            var size = t.terrainData.size;
            if (xz.x < tp.x || xz.x > tp.x + size.x || xz.y < tp.z || xz.y > tp.z + size.z) continue;

            float u = (xz.x - tp.x) / size.x;
            float v = (xz.y - tp.z) / size.z;
            float h = t.terrainData.GetInterpolatedHeight(u, v) + tp.y;
            point = new Vector3(xz.x, h, xz.y);
            normal = t.terrainData.GetInterpolatedNormal(u, v);
            hitObj = t.gameObject;
            return true;
        }

        point = Vector3.zero; normal = Vector3.up; hitObj = null;
        return false;
    }

    // ---------- keep-out ----------
    struct KeepOut { public Vector3 c; public float r; }

    List<KeepOut> BuildKeepOutList()
    {
        var list = new List<KeepOut>();
        var all = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);

        foreach (var t in all)
        {
            string n = t.name;
            bool gameplay = avoidKeywords.Any(k => n.IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0);
            bool structure = structureKeywords.Any(k => n.IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0);
            if (!gameplay && !structure) continue;

            float pad = gameplay ? gameplayClearance : structureClearance;
            if (pad <= 0.01f) continue;

            // only this object's own renderer — never its children's, or a parent like
            // "Guns Spawner" would blank out everything between its two far-apart spawns
            var self = t.GetComponent<Renderer>();
            if (self != null && !(self is ParticleSystemRenderer))
            {
                var b = self.bounds;
                // huge triggers (music zones etc.) would swallow the level — ignore those
                if (b.size.x > 400f || b.size.z > 400f) continue;
                list.Add(new KeepOut { c = b.center, r = pad + Mathf.Max(b.extents.x, b.extents.z) });
            }
            else
            {
                list.Add(new KeepOut { c = t.position, r = pad });
            }
        }
        return list;
    }

    bool TooCloseToKeepOut(Vector3 p, List<KeepOut> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            float dx = list[i].c.x - p.x, dz = list[i].c.z - p.z;
            if (dx * dx + dz * dz < list[i].r * list[i].r) return true;
        }
        return false;
    }

    bool IsAvoided(GameObject go)
    {
        var t = go.transform;
        while (t != null)
        {
            string n = t.name;
            if (avoidKeywords.Any(k => n.IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0)) return true;
            if (structureKeywords.Any(k => n.IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0)) return true;
            if (n == containerName) return true;
            t = t.parent;
        }
        return false;
    }

    // Environment/ is a shared folder — trees live there too. Anything whose name matches
    // one of these is not a rock and is left to its own tool.
    bool IsExcluded(string prefabName)
    {
        foreach (var raw in excludeNames.Split(','))
        {
            var key = raw.Trim();
            if (key.Length == 0) continue;
            if (prefabName.IndexOf(key, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }
        return false;
    }

    // ---------- prefabs ----------
    enum RockSize { Small, Medium, Large }
    class Entry { public GameObject prefab; public RockSize size; }
    class Sample { public Vector3 pos; public Vector3 normal; public float slope; }

    List<Entry> LoadPrefabs()
    {
        var list = new List<Entry>();
        if (!AssetDatabase.IsValidFolder(prefabFolder)) return list;

        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;
            if (go.GetComponentInChildren<MeshRenderer>() == null) continue;
            if (IsExcluded(go.name)) continue;   // the folder holds more than rocks now

            string n = go.name.ToLowerInvariant();
            RockSize size;
            if (n.Contains("pebble") || n.Contains("shard")) size = RockSize.Small;
            else if (n.Contains("cliff") || n.Contains("mountain") || n.Contains("pillar") || n.Contains("overhang"))
                size = RockSize.Large;
            else size = RockSize.Medium;

            list.Add(new Entry { prefab = go, size = size });
        }
        return list;
    }

    Entry PickPrefab(List<Entry> prefabs, System.Random rng, bool allowLarge)
    {
        // 45% small, 40% medium, 15% cliff — cliff only where the ground already slopes
        double roll = rng.NextDouble();
        RockSize want = roll < 0.45 ? RockSize.Small : (roll < 0.85 ? RockSize.Medium : RockSize.Large);
        if (want == RockSize.Large && !allowLarge) want = RockSize.Medium;

        var pool = prefabs.Where(p => p.size == want).ToList();
        if (pool.Count == 0) pool = prefabs;
        return pool[rng.Next(pool.Count)];
    }

    // ---------- helpers ----------
    bool TryWorldBounds(GameObject go, out Bounds b)
    {
        b = new Bounds();
        bool got = false;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            if (!got) { b = r.bounds; got = true; } else b.Encapsulate(r.bounds);
        }
        return got;
    }

    int CountTris(GameObject go)
    {
        int n = 0;
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            if (mf.sharedMesh != null) n += mf.sharedMesh.triangles.Length / 3;
        return n;
    }

    void EnsureCollider(GameObject go, Bounds worldBounds)
    {
        if (go.GetComponentInChildren<Collider>() != null) return;
        var bc = go.AddComponent<BoxCollider>();
        // BoxCollider is local space; convert the world bounds back
        bc.center = go.transform.InverseTransformPoint(worldBounds.center);
        var ls = go.transform.lossyScale;
        bc.size = new Vector3(
            worldBounds.size.x / Mathf.Max(0.0001f, Mathf.Abs(ls.x)),
            worldBounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(ls.y)),
            worldBounds.size.z / Mathf.Max(0.0001f, Mathf.Abs(ls.z)));
    }

    void EnableInstancing(List<Entry> prefabs)
    {
        var done = new HashSet<Material>();
        foreach (var e in prefabs)
            foreach (var r in e.prefab.GetComponentsInChildren<MeshRenderer>())
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null || !done.Add(m)) continue;
                    if (m.enableInstancing) continue;
                    m.enableInstancing = true;
                    EditorUtility.SetDirty(m);
                }
        AssetDatabase.SaveAssets();
    }

    GameObject FindByPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var go = GameObject.Find(path);
        return go;
    }

    GameObject FindContainer(bool create)
    {
        var parent = FindByPath(parentPath);
        Transform found = null;

        if (parent != null)
        {
            found = parent.transform.Find(containerName);
        }
        else
        {
            var direct = GameObject.Find(containerName);
            if (direct != null) found = direct.transform;
        }

        if (found != null) return found.gameObject;
        if (!create) return null;

        var go = new GameObject(containerName);
        if (parent != null) go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = Vector3.zero;
        Undo.RegisterCreatedObjectUndo(go, "Scatter Rocks");
        return go;
    }
}
