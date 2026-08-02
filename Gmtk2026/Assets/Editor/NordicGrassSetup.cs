// Nordic Grass Setup — Countdown Nordic
// Import settings, the two grass materials, and a prefab per mesh.
// Menu:  Tools > Nordic > Grass Setup
//
// Meshes came out of the Sketchfab grass.glb, split in Blender:
//   SM_Grass_Reed_01..09  — the nine reed clumps, 10-29 triangles each
//   SM_Grass_Field_01     — the 13 x 13 m ground patch, 1153 triangles for 593 blades
//
// Materials use Nordic/Grass: wind, root-to-tip gradient, per-blade random height.
// Opaque + alpha clip, never Transparent — transparent foliage sorts every frame,
// kills early-Z, and looks identical.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class NordicGrassSetup : EditorWindow
{
    const string MeshFolder = "Assets/Assets/Meshs";
    const string TexFolder = "Assets/Assets/Textures";
    const string MatFolder = "Assets/Assets/Materials";
    const string PrefabFolder = "Assets/Prefabs/Environment";

    const string ReedMat = MatFolder + "/M_Grass_Reeds.mat";
    const string BaseMat = MatFolder + "/M_Grass_Base.mat";
    const string ShaderName = "Nordic/Grass";

    float reedCutoff = 0.376f;      // from the glTF alphaCutoff
    float baseCutoff = 0.401f;
    bool doubleSided = true;
    int reedMaxSize = 1024;
    int baseMaxSize = 2048;

    // ---- look ----
    Color bottomColor = new Color(0.24f, 0.27f, 0.22f);
    Color topColor = new Color(0.58f, 0.64f, 0.47f);
    float gradientPower = 1.6f;
    float reedHeight = 2.1f;
    float fieldHeight = 1.45f;
    float randomHeight = 0.35f;

    // ---- wind ----
    float windStrength = 0.18f;
    float windSpeed = 1.4f;
    float windScale = 0.25f;
    float windSway = 2.0f;
    Vector2 windDir = new Vector2(1f, 0.35f);

    // ---- lighting ----
    float normalUp = 0.6f;
    float ambientBoost = 1.0f;

    Vector2 scroll;

    [MenuItem("Tools/Nordic/Grass Setup")]
    static void Open()
    {
        var w = GetWindow<NordicGrassSetup>("Grass Setup");
        w.minSize = new Vector2(390f, 560f);
    }

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.HelpBox(
            "Builds the materials and a prefab per grass mesh. Safe to run again — it updates " +
            "in place. Press it after changing anything below to push the new look through.",
            MessageType.Info);

        Head("Gradient");
        using (new EditorGUILayout.HorizontalScope())
        {
            bottomColor = EditorGUILayout.ColorField("Bottom colour", bottomColor);
            if (GUILayout.Button("From ground", GUILayout.Width(100))) TakeGroundColour();
        }
        topColor = EditorGUILayout.ColorField("Top colour", topColor);
        gradientPower = EditorGUILayout.Slider("Falloff", gradientPower, 0.1f, 6f);
        EditorGUILayout.HelpBox("The root takes the floor colour so the grass melts into the ground " +
                                "instead of sitting on it like stickers. \"From ground\" averages your " +
                                "first terrain layer texture.", MessageType.None);

        Head("Wind");
        windStrength = EditorGUILayout.Slider("Strength", windStrength, 0f, 2f);
        windSpeed = EditorGUILayout.Slider("Speed", windSpeed, 0f, 10f);
        windScale = EditorGUILayout.Slider("Scale (gust size)", windScale, 0.01f, 2f);
        windSway = EditorGUILayout.Slider("Root stiffness", windSway, 1f, 6f);
        windDir = EditorGUILayout.Vector2Field("Direction (X, Z)", windDir);

        Head("Variation");
        randomHeight = EditorGUILayout.Slider("Random height", randomHeight, 0f, 0.9f);
        reedHeight = EditorGUILayout.FloatField("Reed mesh height (m)", reedHeight);
        fieldHeight = EditorGUILayout.FloatField("Field mesh height (m)", fieldHeight);
        EditorGUILayout.HelpBox("Mesh height normalises the gradient and the bend. Wrong value and " +
                                "the whole blade sways as one piece, or none of it does.", MessageType.None);

        Head("Lighting");
        normalUp = EditorGUILayout.Slider("Normals toward up", normalUp, 0f, 1f);
        ambientBoost = EditorGUILayout.Slider("Ambient boost", ambientBoost, 0f, 3f);

        Head("Alpha and textures");
        reedCutoff = EditorGUILayout.Slider("Reeds cutoff", reedCutoff, 0f, 1f);
        baseCutoff = EditorGUILayout.Slider("Field cutoff", baseCutoff, 0f, 1f);
        doubleSided = EditorGUILayout.Toggle("Double sided", doubleSided);
        reedMaxSize = EditorGUILayout.IntPopup("Reeds max size", reedMaxSize,
            new[] { "512", "1024", "2048" }, new[] { 512, 1024, 2048 });
        baseMaxSize = EditorGUILayout.IntPopup("Field max size", baseMaxSize,
            new[] { "512", "1024", "2048" }, new[] { 512, 1024, 2048 });

        EditorGUILayout.Space(12);
        GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
        if (GUILayout.Button("Set up the grass", GUILayout.Height(34))) Run();
        GUI.backgroundColor = Color.white;
        if (GUILayout.Button("Update materials only", GUILayout.Height(24))) UpdateMaterialsOnly();

        EditorGUILayout.Space(16);
        EditorGUILayout.EndScrollView();
    }

    // ---------------------------------------------------------------- main
    void Run()
    {
        AssetDatabase.Refresh();

        var reedTex = ImportTexture(TexFolder + "/Grass_Reeds_baseColor.png", reedMaxSize);
        var fieldTex = ImportTexture(TexFolder + "/GrassBase_baseColor.png", baseMaxSize);

        if (reedTex == null || fieldTex == null)
        {
            EditorUtility.DisplayDialog("Grass Setup",
                "Grass base colour textures not found in " + TexFolder + ".", "OK");
            return;
        }

        EnsureFolder(MatFolder);
        var mReed = MakeMaterial(ReedMat, reedTex, reedCutoff, reedHeight);
        var mField = MakeMaterial(BaseMat, fieldTex, baseCutoff, fieldHeight);
        if (mReed == null || mField == null) return;

        EnsureFolder(PrefabFolder);

        int made = 0, tris = 0;
        var names = new List<string>();
        for (int i = 1; i <= 9; i++) names.Add($"SM_Grass_Reed_{i:00}");
        names.Add("SM_Grass_Field_01");

        foreach (var n in names)
        {
            string fbx = $"{MeshFolder}/{n}.fbx";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(fbx) == null)
            {
                Debug.LogWarning("[Grass] Missing " + fbx);
                continue;
            }

            SetModelImport(fbx);
            var mat = n.Contains("Field") ? mField : mReed;
            if (MakePrefab(fbx, $"{PrefabFolder}/{n}.prefab", mat, ref tris)) made++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Grass] {made} prefab(s) ready in {PrefabFolder} — {tris:N0} triangles across the set. " +
                  "Nordic/Grass: wind, gradient, random height. Alpha clipped, double sided, instanced.");
    }

    void UpdateMaterialsOnly()
    {
        var reedTex = ImportTexture(TexFolder + "/Grass_Reeds_baseColor.png", reedMaxSize);
        var fieldTex = ImportTexture(TexFolder + "/GrassBase_baseColor.png", baseMaxSize);
        MakeMaterial(ReedMat, reedTex, reedCutoff, reedHeight);
        MakeMaterial(BaseMat, fieldTex, baseCutoff, fieldHeight);
        AssetDatabase.SaveAssets();
        SceneView.RepaintAll();
        Debug.Log("[Grass] Materials updated. Prefabs untouched.");
    }

    // ---------------------------------------------------------------- ground colour
    /// Averages the first terrain layer's texture down to one pixel with a chain of
    /// half-size blits. A single 1x1 blit would point-sample, not average.
    void TakeGroundColour()
    {
        Texture tex = null;
        foreach (var t in Terrain.activeTerrains)
        {
            var layers = t.terrainData != null ? t.terrainData.terrainLayers : null;
            if (layers == null) continue;
            foreach (var l in layers)
                if (l != null && l.diffuseTexture != null) { tex = l.diffuseTexture; break; }
            if (tex != null) break;
        }

        if (tex == null)
        {
            EditorUtility.DisplayDialog("Grass Setup",
                "No terrain layer texture found. Set the bottom colour by hand.", "OK");
            return;
        }

        int size = 256;
        var cur = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(tex, cur);
        while (size > 1)
        {
            size /= 2;
            var next = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(cur, next);                   // bilinear halving is a box average
            RenderTexture.ReleaseTemporary(cur);
            cur = next;
        }

        var prev = RenderTexture.active;
        RenderTexture.active = cur;
        var read = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        read.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
        read.Apply();
        bottomColor = read.GetPixel(0, 0);
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(cur);
        DestroyImmediate(read);

        Repaint();
        Debug.Log($"[Grass] Bottom colour taken from \"{tex.name}\": {bottomColor}");
    }

    // ---------------------------------------------------------------- textures
    Texture2D ImportTexture(string path, int maxSize)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null) return null;

        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return tex;

        bool dirty = false;
        if (imp.textureType != TextureImporterType.Default) { imp.textureType = TextureImporterType.Default; dirty = true; }
        if (!imp.sRGBTexture) { imp.sRGBTexture = true; dirty = true; }
        if (!imp.alphaIsTransparency) { imp.alphaIsTransparency = true; dirty = true; }
        if (imp.maxTextureSize != maxSize) { imp.maxTextureSize = maxSize; dirty = true; }
        if (!imp.mipmapEnabled) { imp.mipmapEnabled = true; dirty = true; }
        if (imp.wrapMode != TextureWrapMode.Clamp) { imp.wrapMode = TextureWrapMode.Clamp; dirty = true; }
        if (dirty) imp.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ---------------------------------------------------------------- materials
    Material MakeMaterial(string path, Texture2D albedo, float cutoff, float height)
    {
        var shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError($"[Grass] Shader \"{ShaderName}\" not found. If Unity is still compiling, wait and retry.");
            return null;
        }

        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        m.shader = shader;

        if (albedo != null) m.SetTexture("_BaseMap", albedo);
        m.SetFloat("_Cutoff", cutoff);

        m.SetColor("_BottomColor", bottomColor);
        m.SetColor("_TopColor", topColor);
        m.SetFloat("_GradientPower", gradientPower);
        m.SetFloat("_Height", height);
        m.SetFloat("_RandomHeight", randomHeight);

        m.SetFloat("_WindStrength", windStrength);
        m.SetFloat("_WindSpeed", windSpeed);
        m.SetFloat("_WindScale", windScale);
        m.SetFloat("_WindSway", windSway);
        m.SetVector("_WindDirection", new Vector4(windDir.x, windDir.y, 0f, 0f));

        m.SetFloat("_NormalUp", normalUp);
        m.SetFloat("_AmbientBoost", ambientBoost);

        m.SetFloat("_Cull", doubleSided ? (float)CullMode.Off : (float)CullMode.Back);
        m.doubleSidedGI = doubleSided;
        m.renderQueue = (int)RenderQueue.AlphaTest;
        m.enableInstancing = true;

        EditorUtility.SetDirty(m);
        return m;
    }

    // ---------------------------------------------------------------- model import
    void SetModelImport(string fbxPath)
    {
        var imp = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (imp == null) return;

        bool dirty = false;
        if (imp.materialImportMode != ModelImporterMaterialImportMode.None)
        { imp.materialImportMode = ModelImporterMaterialImportMode.None; dirty = true; }
        if (imp.importAnimation) { imp.importAnimation = false; dirty = true; }
        if (imp.importCameras) { imp.importCameras = false; dirty = true; }
        if (imp.importLights) { imp.importLights = false; dirty = true; }
        if (imp.importBlendShapes) { imp.importBlendShapes = false; dirty = true; }
        if (imp.isReadable) { imp.isReadable = false; dirty = true; }
        if (imp.addCollider) { imp.addCollider = false; dirty = true; }
        if (imp.generateSecondaryUV) { imp.generateSecondaryUV = false; dirty = true; }
        // no normal map in Nordic/Grass, so tangents are dead weight on every vertex
        if (imp.importTangents != ModelImporterTangents.None)
        { imp.importTangents = ModelImporterTangents.None; dirty = true; }

        if (dirty) imp.SaveAndReimport();
    }

    // ---------------------------------------------------------------- prefabs
    bool MakePrefab(string fbxPath, string prefabPath, Material mat, ref int tris)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (model == null) return false;

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
        if (inst == null) return false;

        try
        {
            // grass never blocks a shot or a step, so it never carries a collider
            NordicScatterCore.StripColliders(inst);

            foreach (var r in inst.GetComponentsInChildren<MeshRenderer>(true))
            {
                int n = Mathf.Max(1, r.sharedMaterials.Length);
                var mats = new Material[n];
                for (int i = 0; i < n; i++) mats[i] = mat;
                r.sharedMaterials = mats;

                r.shadowCastingMode = ShadowCastingMode.Off;   // grass shadows cost more than they show
                r.receiveShadows = true;
                r.lightProbeUsage = LightProbeUsage.BlendProbes;
                r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }

            foreach (var mf in inst.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh != null) tris += mf.sharedMesh.triangles.Length / 3;

            PrefabUtility.SaveAsPrefabAsset(inst, prefabPath);
            return true;
        }
        finally { DestroyImmediate(inst); }
    }

    static void Head(string t)
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField(t, EditorStyles.boldLabel);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parts = path.Split('/');
        string acc = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = acc + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(acc, parts[i]);
            acc = next;
        }
    }
}
