// Shared scatter logic for the Nordic tools.
// The ruin tool and the tree tool both sit on this, so "not in the way" means
// exactly the same thing in both. Nothing here has a menu item of its own.

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public static class NordicScatterCore
{
    public const string EnvRoot = "Environnement";

    public static readonly string[] DefaultAvoid =
        { "Player", "Spawn", "Enem", "Gun", "Door", "Shop", "Bricks" };
    public static readonly string[] DefaultStructures =
        { "Structure", "Building" };

    // names of every scatter container, so the tools never stack on each other
    public static readonly string[] ScatterContainers =
        { "Rocks_Scatter", "Ruins_Scatter", "Trees_Scatter", "Grass_Scatter" };

    public struct Ground
    {
        public Vector3 pos;
        public Vector3 normal;
        public float slope;
        public GameObject hit;
    }

    public struct KeepOut
    {
        public Vector3 c;
        public float r;
    }

    // ---------------------------------------------------------------- ground
    public static bool SampleGround(Vector2 xz, out Ground g, float top = 2000f)
    {
        g = default;

        var origin = new Vector3(xz.x, top, xz.y);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, top * 2.5f, ~0, QueryTriggerInteraction.Ignore))
        {
            g.pos = hit.point;
            g.normal = hit.normal;
            g.hit = hit.collider.gameObject;
            g.slope = Vector3.Angle(g.normal, Vector3.up);
            return true;
        }

        // fallback: read the terrain height directly, in case a collider is missing
        foreach (var t in Terrain.activeTerrains)
        {
            var tp = t.transform.position;
            var size = t.terrainData.size;
            if (xz.x < tp.x || xz.x > tp.x + size.x || xz.y < tp.z || xz.y > tp.z + size.z) continue;

            float u = (xz.x - tp.x) / size.x;
            float v = (xz.y - tp.z) / size.z;
            g.pos = new Vector3(xz.x, t.terrainData.GetInterpolatedHeight(u, v) + tp.y, xz.y);
            g.normal = t.terrainData.GetInterpolatedNormal(u, v);
            g.hit = t.gameObject;
            g.slope = Vector3.Angle(g.normal, Vector3.up);
            return true;
        }

        return false;
    }

    // ---------------------------------------------------------------- keep out
    public static List<KeepOut> BuildKeepOut(string[] gameplayKeys, string[] structureKeys,
                                             float gameplayPad, float structurePad)
    {
        var list = new List<KeepOut>();

        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            string n = t.name;
            bool gameplay = Matches(n, gameplayKeys);
            bool structure = Matches(n, structureKeys);
            if (!gameplay && !structure) continue;

            float pad = gameplay ? gameplayPad : structurePad;
            if (pad <= 0.01f) continue;

            // this object's own renderer only — a parent's combined bounds would blank the level
            var self = t.GetComponent<Renderer>();
            if (self != null && !(self is ParticleSystemRenderer))
            {
                var b = self.bounds;
                if (b.size.x > 400f || b.size.z > 400f) continue;   // huge triggers are not obstacles
                list.Add(new KeepOut { c = b.center, r = pad + Mathf.Max(b.extents.x, b.extents.z) });
            }
            else list.Add(new KeepOut { c = t.position, r = pad });
        }

        return list;
    }

    public static bool TooClose(Vector3 p, List<KeepOut> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            float dx = list[i].c.x - p.x, dz = list[i].c.z - p.z;
            if (dx * dx + dz * dz < list[i].r * list[i].r) return true;
        }
        return false;
    }

    public static bool IsAvoided(GameObject go, string[] gameplayKeys, string[] structureKeys)
    {
        var t = go != null ? go.transform : null;
        while (t != null)
        {
            if (Matches(t.name, gameplayKeys)) return true;
            if (Matches(t.name, structureKeys)) return true;
            if (ScatterContainers.Contains(t.name)) return true;
            t = t.parent;
        }
        return false;
    }

    static bool Matches(string name, string[] keys)
    {
        if (keys == null) return false;
        for (int i = 0; i < keys.Length; i++)
            if (name.IndexOf(keys[i], System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }

    // ---------------------------------------------------------------- navmesh
    public static bool HasNavMesh() => NavMesh.CalculateTriangulation().vertices.Length > 0;

    public static bool OnWalkable(Vector3 p, float clearance)
    {
        if (clearance <= 0.01f) return false;
        return NavMesh.SamplePosition(p, out NavMeshHit hit, clearance, NavMesh.AllAreas)
               && hit.distance < clearance;
    }

    // ---------------------------------------------------------------- bounds
    public static bool TryLevelBounds(out Bounds b)
    {
        b = new Bounds();
        bool got = false;

        var env = GameObject.Find(EnvRoot);
        if (env != null)
            foreach (var r in env.GetComponentsInChildren<MeshRenderer>())
            {
                if (IsUnderScatter(r.transform)) continue;
                if (r.gameObject.name.StartsWith("Fog_")) continue;
                if (!got) { b = r.bounds; got = true; } else b.Encapsulate(r.bounds);
            }

        if (!got)
            foreach (var t in Terrain.activeTerrains)
            {
                var tb = new Bounds(t.transform.position + t.terrainData.size * 0.5f, t.terrainData.size);
                if (!got) { b = tb; got = true; } else b.Encapsulate(tb);
            }

        return got;
    }

    /// Is the thing the ray landed on part of this hierarchy? Resolve the root once and
    /// pass it in — GameObject.Find inside a sampling loop is thousands of string searches.
    public static bool IsUnder(GameObject go, Transform root)
    {
        if (go == null || root == null) return false;
        var t = go.transform;
        while (t != null)
        {
            if (t == root) return true;
            t = t.parent;
        }
        return false;
    }

    static bool IsUnderScatter(Transform t)
    {
        while (t != null)
        {
            if (ScatterContainers.Contains(t.name)) return true;
            t = t.parent;
        }
        return false;
    }

    // ---------------------------------------------------------------- containers
    public static GameObject FindContainer(string name, bool create)
    {
        var parent = GameObject.Find(EnvRoot);
        Transform found = parent != null ? parent.transform.Find(name) : null;
        if (found == null)
        {
            var direct = GameObject.Find(name);
            if (direct != null) found = direct.transform;
        }

        if (found != null) return found.gameObject;
        if (!create) return null;

        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = Vector3.zero;
        Undo.RegisterCreatedObjectUndo(go, "Scatter");
        return go;
    }

    /// Hides every scatter container so raycasts hit the ground, not previous props.
    /// Returns what was active so it can be put back.
    public static List<KeyValuePair<GameObject, bool>> HideScatters()
    {
        var state = new List<KeyValuePair<GameObject, bool>>();
        foreach (var n in ScatterContainers)
        {
            var c = FindContainer(n, false);
            if (c == null) continue;
            state.Add(new KeyValuePair<GameObject, bool>(c, c.activeSelf));
            c.SetActive(false);
        }
        return state;
    }

    public static void RestoreScatters(List<KeyValuePair<GameObject, bool>> state)
    {
        foreach (var kv in state) if (kv.Key != null) kv.Key.SetActive(kv.Value);
    }

    /// Where everything already scattered stands, so a new pass keeps its distance.
    public static List<Vector3> ExistingScatterPositions()
    {
        var list = new List<Vector3>();
        foreach (var n in ScatterContainers)
        {
            var c = FindContainer(n, false);
            if (c == null) continue;
            foreach (Transform child in c.transform) list.Add(child.position);
        }
        return list;
    }

    public static bool Spaced(Vector3 p, List<Vector3> taken, float minDistance)
    {
        float sq = minDistance * minDistance;
        for (int i = 0; i < taken.Count; i++)
        {
            float dx = taken[i].x - p.x, dz = taken[i].z - p.z;
            if (dx * dx + dz * dz < sq) return false;
        }
        return true;
    }

    // ---------------------------------------------------------------- prefabs
    public static List<GameObject> LoadPrefabs(string folder, string nameContains = null)
    {
        var list = new List<GameObject>();
        if (!AssetDatabase.IsValidFolder(folder)) return list;

        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;
            if (go.GetComponentInChildren<MeshRenderer>() == null) continue;
            if (!string.IsNullOrEmpty(nameContains) &&
                go.name.IndexOf(nameContains, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            list.Add(go);
        }
        return list;
    }

    /// Longest horizontal side of the prefab, used to sort a kit into small / medium / big.
    public static float PrefabFootprint(GameObject prefab)
    {
        if (!TryPrefabBounds(prefab, out Bounds b)) return 1f;
        return Mathf.Max(b.size.x, b.size.z);
    }

    /// Size of a prefab asset, measured in the root's own space. Renderer.bounds is meaningless
    /// on an asset that was never in a scene, so this goes through the mesh data too.
    public static bool TryPrefabBounds(GameObject prefab, out Bounds b)
    {
        b = new Bounds();
        bool got = false;
        var toRoot = prefab.transform.worldToLocalMatrix;

        foreach (var mf in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            Encapsulate(ref b, ref got, mf.sharedMesh.bounds, toRoot * mf.transform.localToWorldMatrix);
        }
        return got;
    }

    // ---------------------------------------------------------------- placing
    /// Drops a prefab onto the ground. Keeps the prefab's own baked rotation (the -90 X that
    /// Unity puts on a Blender FBX) and layers the random yaw and lean on top of it.
    public static GameObject Place(GameObject prefab, Transform parent, Ground g,
                                   float alignToNormal, float yawDeg, float tiltDeg,
                                   Vector3 scaleMul, float sinkFraction, out Bounds worldBounds)
    {
        worldBounds = new Bounds();

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
        if (go == null) return null;

        Undo.RegisterCreatedObjectUndo(go, "Scatter");
        go.transform.SetParent(parent, true);

        Quaternion baseRot = prefab.transform.localRotation;
        Quaternion yaw = Quaternion.Euler(0f, yawDeg, 0f);
        Quaternion lean = Quaternion.Slerp(Quaternion.identity,
                                           Quaternion.FromToRotation(Vector3.up, g.normal),
                                           alignToNormal);
        Quaternion tilt = Quaternion.Euler(
            Random.Range(-tiltDeg, tiltDeg), 0f, Random.Range(-tiltDeg, tiltDeg));

        go.transform.rotation = lean * tilt * yaw * baseRot;
        go.transform.localScale = Vector3.Scale(prefab.transform.localScale, scaleMul);
        go.transform.position = g.pos;

        if (TryWorldBounds(go, out worldBounds))
        {
            float lift = g.pos.y - worldBounds.min.y;
            float sink = worldBounds.size.y * sinkFraction;
            go.transform.position = g.pos + Vector3.up * (lift - sink);
            TryWorldBounds(go, out worldBounds);
        }

        return go;
    }

    /// World bounds built from the mesh data and the transform matrix, NOT from Renderer.bounds.
    /// The tools hide the container while they place, and Renderer.bounds is empty on an inactive
    /// object — which silently skipped the grounding step and left everything floating at its pivot.
    public static bool TryWorldBounds(GameObject go, out Bounds b)
    {
        b = new Bounds();
        bool got = false;

        foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            Encapsulate(ref b, ref got, mf.sharedMesh.bounds, mf.transform.localToWorldMatrix);
        }

        foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr.sharedMesh == null) continue;
            Encapsulate(ref b, ref got, smr.sharedMesh.bounds, smr.transform.localToWorldMatrix);
        }

        return got;
    }

    static void Encapsulate(ref Bounds b, ref bool got, Bounds local, Matrix4x4 m)
    {
        for (int i = 0; i < 8; i++)
        {
            var corner = new Vector3(
                (i & 1) == 0 ? local.min.x : local.max.x,
                (i & 2) == 0 ? local.min.y : local.max.y,
                (i & 4) == 0 ? local.min.z : local.max.z);
            var w = m.MultiplyPoint3x4(corner);
            if (!got) { b = new Bounds(w, Vector3.zero); got = true; }
            else b.Encapsulate(w);
        }
    }

    // ---------------------------------------------------------------- optimisation
    public static void MakeStatic(GameObject go, bool contributeGI)
    {
        var flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic;
        if (contributeGI) flags |= StaticEditorFlags.ContributeGI;
        GameObjectUtility.SetStaticEditorFlags(go, flags);
    }

    /// An empty, unrotated, unscaled parent to hang a collider on.
    /// These prefabs carry the -90 X rotation Unity puts on a Blender FBX, and the tree scale
    /// is non-uniform — a collider added straight to that root comes out rotated and squashed.
    /// Putting it on a clean wrapper keeps the collider maths in plain world space.
    public static GameObject MakeWrapper(string name, Transform parent, Vector3 position)
    {
        var w = new GameObject(name);
        w.transform.SetParent(parent, false);
        w.transform.position = position;
        w.transform.rotation = Quaternion.identity;
        w.transform.localScale = Vector3.one;
        Undo.RegisterCreatedObjectUndo(w, "Scatter");
        return w;
    }

    /// Host must be a wrapper: identity rotation, unit scale.
    public static void EnsureBoxCollider(GameObject host, Bounds worldBounds)
    {
        if (host.GetComponentInChildren<Collider>(true) != null) return;
        var bc = host.AddComponent<BoxCollider>();
        bc.center = worldBounds.center - host.transform.position;
        bc.size = worldBounds.size;
    }

    /// A capsule around the trunk, on the same kind of wrapper. A tree the player walks through
    /// reads as a hologram, but a box the width of the canopy blocks a path that looks open.
    public static void EnsureTrunkCollider(GameObject host, Bounds worldBounds, float trunkFraction)
    {
        if (host.GetComponentInChildren<Collider>(true) != null) return;
        var cc = host.AddComponent<CapsuleCollider>();
        cc.direction = 1;                                   // world Y, because the wrapper is unrotated
        cc.height = worldBounds.size.y;
        cc.radius = Mathf.Max(0.05f, Mathf.Max(worldBounds.size.x, worldBounds.size.z) * trunkFraction);
        cc.center = worldBounds.center - host.transform.position;
    }

    public static int CountTris(GameObject go)
    {
        int n = 0;
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            if (mf.sharedMesh != null) n += mf.sharedMesh.triangles.Length / 3;
        return n;
    }

    public static int EnableInstancing(IEnumerable<GameObject> prefabs)
    {
        var done = new HashSet<Material>();
        int changed = 0;
        foreach (var p in prefabs)
            foreach (var r in p.GetComponentsInChildren<MeshRenderer>())
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null || !done.Add(m)) continue;
                    if (m.enableInstancing) continue;
                    m.enableInstancing = true;
                    EditorUtility.SetDirty(m);
                    changed++;
                }
        if (changed > 0) AssetDatabase.SaveAssets();
        return changed;
    }

    // ---------------------------------------------------------------- boundary
    // "Inside the walls" is not a rectangle — the mountain ring is a shape. So we take the
    // outline of every mountain piece, wrap it in a convex hull, then pull that hull inward
    // by `inset` metres. Anything outside it is the flat non-playable ground and is refused.
    public const string MountainsPath = EnvRoot + "/Montagnes";

    public enum BoundarySource
    {
        GameplayObjects,      // wrap where the game actually happens, then reach outward
        MountainRingInner,    // foot of the wall
        MountainRingOuter,    // outside of the wall, props may sit on the slopes
        None
    }

    public static List<Vector2> BuildBoundary(BoundarySource source, string mountainsPath, float amount)
    {
        switch (source)
        {
            case BoundarySource.GameplayObjects:
                return BuildGameplayBoundary(DefaultAvoid, DefaultStructures, amount);
            case BoundarySource.MountainRingInner:
                return BuildMountainBoundary(mountainsPath, amount, true);
            case BoundarySource.MountainRingOuter:
                return BuildMountainBoundary(mountainsPath, amount, false);
            default:
                return null;
        }
    }

    /// The honest definition of "the gameplay area": wrap the spawn points, the gun spawners,
    /// the doors, the enemies and the structures, then push that outline out by `margin`.
    /// The mountain group is no good for this — it mixes the arena walls with background
    /// mountains hundreds of metres away, so its hull is several times the size of the level.
    public static List<Vector2> BuildGameplayBoundary(string[] gameplayKeys, string[] structureKeys, float margin)
    {
        var pts = new List<Vector2>();

        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (!Matches(t.name, gameplayKeys) && !Matches(t.name, structureKeys)) continue;

            var r = t.GetComponent<Renderer>();
            if (r != null && !(r is ParticleSystemRenderer))
            {
                var b = r.bounds;
                if (b.size.x > 400f || b.size.z > 400f) continue;    // music triggers etc.
                pts.Add(new Vector2(b.min.x, b.min.z));
                pts.Add(new Vector2(b.max.x, b.min.z));
                pts.Add(new Vector2(b.max.x, b.max.z));
                pts.Add(new Vector2(b.min.x, b.max.z));
            }
            else pts.Add(new Vector2(t.position.x, t.position.z));
        }

        if (pts.Count < 3) return null;
        var hull = ConvexHull(pts);
        if (hull.Count < 3) return null;

        InsetHull(hull, -Mathf.Abs(margin));    // negative inset = grow outward
        return hull;
    }

    /// innerEdge = false wraps the outside of the mountain ring (props may sit on the slopes).
    /// innerEdge = true wraps the inner faces instead — the ground the player actually plays on.
    public static List<Vector2> BuildMountainBoundary(string mountainsPath, float inset, bool innerEdge = false)
    {
        var root = GameObject.Find(mountainsPath);
        if (root == null) return null;

        var boxes = new List<Bounds>();
        foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
        {
            if (IsUnderScatter(r.transform)) continue;
            if (r.gameObject.name.StartsWith("Fog_")) continue;
            boxes.Add(r.bounds);
        }
        if (boxes.Count == 0) return null;

        Vector2 middle = Vector2.zero;
        foreach (var b in boxes) middle += new Vector2(b.center.x, b.center.z);
        middle /= boxes.Count;

        var pts = new List<Vector2>();
        foreach (var b in boxes)
        {
            var corners = new[]
            {
                new Vector2(b.min.x, b.min.z), new Vector2(b.max.x, b.min.z),
                new Vector2(b.max.x, b.max.z), new Vector2(b.min.x, b.max.z)
            };

            if (!innerEdge) { pts.AddRange(corners); continue; }

            // only the corner facing the middle of the level — that is the foot of the wall
            Vector2 nearest = corners[0];
            float best = float.MaxValue;
            foreach (var c in corners)
            {
                float d = (c - middle).sqrMagnitude;
                if (d < best) { best = d; nearest = c; }
            }
            pts.Add(nearest);
        }

        if (pts.Count < 3) return null;              // no mountains found — caller falls back

        var hull = ConvexHull(pts);
        if (hull.Count < 3) return null;

        InsetHull(hull, inset);
        return hull;
    }

    /// Positive moves every corner toward the middle, negative pushes it outward.
    static void InsetHull(List<Vector2> hull, float inset)
    {
        if (Mathf.Abs(inset) < 0.001f) return;

        Vector2 centre = Vector2.zero;
        foreach (var p in hull) centre += p;
        centre /= hull.Count;

        for (int i = 0; i < hull.Count; i++)
        {
            Vector2 toCentre = centre - hull[i];
            float d = toCentre.magnitude;
            if (d < 0.001f) continue;
            hull[i] += toCentre / d * Mathf.Min(inset, d * 0.9f);
        }
    }

    // Andrew's monotone chain
    static List<Vector2> ConvexHull(List<Vector2> pts)
    {
        var p = new List<Vector2>(pts);
        p.Sort((a, b) => a.x == b.x ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

        var hull = new List<Vector2>();
        for (int pass = 0; pass < 2; pass++)
        {
            int start = hull.Count;
            for (int i = 0; i < p.Count; i++)
            {
                var pt = pass == 0 ? p[i] : p[p.Count - 1 - i];
                while (hull.Count >= start + 2 &&
                       Cross(hull[hull.Count - 2], hull[hull.Count - 1], pt) <= 0)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(pt);
            }
            hull.RemoveAt(hull.Count - 1);
        }
        return hull;
    }

    static float Cross(Vector2 o, Vector2 a, Vector2 b)
        => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);

    public static bool Inside(Vector3 worldPos, List<Vector2> poly)
    {
        if (poly == null || poly.Count < 3) return true;      // no boundary = no restriction

        float x = worldPos.x, z = worldPos.z;
        bool inside = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            if ((poly[i].y > z) != (poly[j].y > z) &&
                x < (poly[j].x - poly[i].x) * (z - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                inside = !inside;
        }
        return inside;
    }

    public static void DrawBoundary(List<Vector2> poly, float y)
    {
        if (poly == null || poly.Count < 3) return;
        var pts = new Vector3[poly.Count + 1];
        for (int i = 0; i < poly.Count; i++) pts[i] = new Vector3(poly[i].x, y, poly[i].y);
        pts[poly.Count] = pts[0];

        var old = Handles.color;
        Handles.color = new Color(0.3f, 1f, 0.6f, 0.9f);
        Handles.DrawAAPolyLine(4f, pts);
        Handles.color = old;
    }

    // ---------------------------------------------------------------- clusters
    /// Points around a centre, biased toward the middle so a cluster has a core
    /// instead of looking like a ring.
    public static Vector2 ClusterOffset(float radius)
    {
        float a = Random.value * Mathf.PI * 2f;
        float r = radius * Mathf.Pow(Random.value, 0.65f);
        return new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
    }
}
