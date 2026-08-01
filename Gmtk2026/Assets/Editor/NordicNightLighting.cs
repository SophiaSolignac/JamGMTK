// Nordic Night Lighting — Countdown Nordic
// Builds the night environment for the scene: HDRI sky, moonlight, ambient, global fog,
// a dense fog cap over the mountains, soft lights on the enemies, and local falling snow.
// Menu:  Tools > Nordic > Night Lighting
//
// Every section is its own button. Nothing runs unless you press it.
// Scene changes are logged with their old values so you can put them back by hand.

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class NordicNightLighting : EditorWindow
{
    // ---------- paths ----------
    const string SkyFolder = "Assets/Settings/Sky";
    const string HdriSource = "Assets/Editor/kloppenheim_02_puresky_2k.hdr";
    const string HdriTarget = SkyFolder + "/kloppenheim_02_puresky_2k.hdr";
    const string SkyMatPath = SkyFolder + "/M_Sky_NordicNight.mat";
    const string FogMatPath = SkyFolder + "/M_Fog_Volumetric.mat";
    const string FogNoisePath = SkyFolder + "/T_FogNoise3D.asset";
    const string SnowTexPath = SkyFolder + "/T_SnowFlake.png";
    const string SnowMatPath = SkyFolder + "/M_Snow_Falling.mat";
    const string SnowPrefabPath = "Assets/Prefabs/Environment/P_Snow_Local.prefab";
    const string PlayerPrefabPath = "Assets/Prefabs/Game/Player.prefab";
    const string FogShaderName = "Nordic/VolumetricFog";
    const string FeatureName = "Nordic Volumetric Fog";
    const string EnvRoot = "Environnement";
    const string AuraLightName = "Aura Light";
    static readonly string[] RendererPaths =
    {
        "Assets/Settings/PC_Renderer.asset",
        "Assets/Settings/Mobile_Renderer.asset"
    };

    // ---------- sky / ambient ----------
    Color skyTint = new Color(0.36f, 0.42f, 0.55f);
    float skyExposure = 0.45f;
    float skyRotation = 20f;

    Color ambientSky = new Color(0.105f, 0.140f, 0.215f);
    Color ambientEquator = new Color(0.070f, 0.090f, 0.135f);
    Color ambientGround = new Color(0.030f, 0.035f, 0.050f);
    float reflectionIntensity = 0.35f;

    // ---------- moon ----------
    Color moonColor = new Color(0.62f, 0.72f, 1.00f);
    float moonIntensity = 0.5f;
    Vector2 moonAngles = new Vector2(38f, -152f);
    float moonShadowStrength = 0.55f;

    // ---------- global fog ----------
    Color fogColor = new Color(0.100f, 0.130f, 0.190f);
    float fogDensity = 0.011f;

    // ---------- volumetric fog (raymarched fullscreen pass) ----------
    Color volFogColor = new Color(0.115f, 0.145f, 0.205f);
    float volMaxDistance = 90f;      // stop raymarching here, let the cheap exp2 fog do the rest
    float volStepSize = 3f;          // cost = maxDistance / stepSize samples per pixel
    float volDensityMultiplier = 1.3f;
    float volDensityThreshold = 0.34f;
    float volNoiseTiling = 1.2f;
    float volNoiseOffset = 2f;       // per-pixel dither, hides the stepping
    Color volLightContribution = new Color(0.30f, 0.38f, 0.55f);
    float volLightScattering = 0.25f;
    float volHeightDensity = 10f;    // how much denser the air is above the peaks
    float volHeightRange = 60f;
    float volHeightStart = 40f;
    int volNoiseRes = 64;

    // ---------- enemy aura ----------
    Color auraColor = new Color(0.55f, 0.78f, 1.00f);
    float auraIntensity = 2.4f;
    float auraRange = 9f;
    float auraHeight = 1.0f;

    // ---------- snow ----------
    int snowMaxParticles = 500;
    float snowRate = 70f;
    float snowBoxSize = 26f;
    float snowSpawnHeight = 10f;
    float snowLifetime = 6f;
    Vector2 snowSize = new Vector2(0.03f, 0.085f);
    Vector2 snowWind = new Vector2(0.4f, 0.15f);

    // ---------- grade (edits the shared volume profile) ----------
    bool addTonemapping = true;
    float postExposure = 0.15f;
    float contrast = 8f;
    Color colorFilter = new Color(0.86f, 0.92f, 1.00f);
    float saturation = -12f;
    float bloomThreshold = 0.75f;
    float bloomIntensity = 0.9f;
    Color bloomTint = new Color(0.78f, 0.86f, 1.00f);
    float vignetteIntensity = 0.28f;
    float vignetteSmoothness = 0.35f;
    Color vignetteColor = new Color(0.02f, 0.03f, 0.06f);
    float whiteBalanceTemp = -12f;
    float whiteBalanceTint = 2f;
    bool addFilmGrain = true;
    float grainIntensity = 0.18f;

    Vector2 scroll;

    [MenuItem("Tools/Nordic/Night Lighting")]
    static void Open()
    {
        var w = GetWindow<NordicNightLighting>("Night Lighting");
        w.minSize = new Vector2(380f, 560f);
    }

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.HelpBox(
            "Each button does one job. Press them top to bottom the first time.\n" +
            "Step 2 edits the URP renderer assets, steps 3 and 4 edit prefabs — " +
            "commit in git before you run those.",
            MessageType.Info);

        // ---- 1 ----
        Header("1 — Sky, ambient, moon, global fog");
        skyTint = EditorGUILayout.ColorField("Sky tint", skyTint);
        skyExposure = EditorGUILayout.Slider("Sky exposure", skyExposure, 0f, 2f);
        skyRotation = EditorGUILayout.Slider("Sky rotation", skyRotation, 0f, 360f);
        EditorGUILayout.Space(4);
        ambientSky = EditorGUILayout.ColorField("Ambient — top", ambientSky);
        ambientEquator = EditorGUILayout.ColorField("Ambient — middle", ambientEquator);
        ambientGround = EditorGUILayout.ColorField("Ambient — ground", ambientGround);
        reflectionIntensity = EditorGUILayout.Slider("Reflections", reflectionIntensity, 0f, 1f);
        EditorGUILayout.Space(4);
        moonColor = EditorGUILayout.ColorField("Moon colour", moonColor);
        moonIntensity = EditorGUILayout.Slider("Moon intensity", moonIntensity, 0f, 3f);
        moonAngles = EditorGUILayout.Vector2Field("Moon angle (pitch, yaw)", moonAngles);
        moonShadowStrength = EditorGUILayout.Slider("Moon shadow strength", moonShadowStrength, 0f, 1f);
        EditorGUILayout.Space(4);
        fogColor = EditorGUILayout.ColorField("Fog colour", fogColor);
        fogDensity = EditorGUILayout.Slider("Fog density", fogDensity, 0f, 0.05f);
        EditorGUILayout.LabelField(" ", $"readable to roughly {ReadableDistance(fogDensity):0} m");
        if (Button("Apply sky, ambient, moon and fog")) ApplyEnvironment();

        // ---- 2 ----
        Header("2 — Volumetric fog  (raymarched fullscreen pass)");
        volFogColor = EditorGUILayout.ColorField("Fog colour", volFogColor);
        volMaxDistance = EditorGUILayout.Slider("Max distance", volMaxDistance, 10f, 400f);
        volStepSize = EditorGUILayout.Slider("Step size", volStepSize, 0.5f, 20f);
        int samples = Mathf.CeilToInt(volMaxDistance / Mathf.Max(0.01f, volStepSize));
        EditorGUILayout.LabelField(" ", $"{samples} samples per pixel" +
            (samples > 40 ? "  ← too many, raise Step size" : "  ← fine"));
        EditorGUILayout.Space(4);
        volDensityMultiplier = EditorGUILayout.Slider("Density", volDensityMultiplier, 0f, 10f);
        volDensityThreshold = EditorGUILayout.Slider("Density threshold", volDensityThreshold, 0f, 1f);
        volNoiseTiling = EditorGUILayout.Slider("Noise tiling", volNoiseTiling, 0.1f, 8f);
        volNoiseOffset = EditorGUILayout.Slider("Dither", volNoiseOffset, 0f, 10f);
        volNoiseRes = EditorGUILayout.IntPopup("Noise texture size", volNoiseRes,
            new[] { "32³ (128 KB)", "64³ (1 MB)", "128³ (8 MB)" }, new[] { 32, 64, 128 });
        EditorGUILayout.Space(4);
        volLightContribution = EditorGUILayout.ColorField("Moon scattering", volLightContribution);
        volLightScattering = EditorGUILayout.Slider("Scattering", volLightScattering, 0f, 1f);
        EditorGUILayout.Space(4);
        volHeightStart = EditorGUILayout.FloatField("Height ramp starts at Y", volHeightStart);
        volHeightRange = EditorGUILayout.Slider("Height ramp range", volHeightRange, 1f, 300f);
        volHeightDensity = EditorGUILayout.Slider("Density high up", volHeightDensity, 1f, 30f);
        if (Button("Read peak height from the level")) ReadPeakHeight();
        if (Button("Build noise texture + fog material")) BuildVolumetricFog();
        if (Button("Install the fog on the URP renderers")) InstallFogFeature(true);
        if (Button("Remove the fog from the URP renderers")) InstallFogFeature(false);

        // ---- 3 ----
        Header("3 — Soft light on every enemy  (edits prefabs)");
        auraColor = EditorGUILayout.ColorField("Aura colour", auraColor);
        auraIntensity = EditorGUILayout.Slider("Aura intensity", auraIntensity, 0f, 8f);
        auraRange = EditorGUILayout.Slider("Aura range", auraRange, 1f, 30f);
        auraHeight = EditorGUILayout.Slider("Aura height", auraHeight, 0f, 4f);
        EditorGUILayout.HelpBox("Adds a shadowless point light to each Enemy prefab. " +
                                "Safe to run twice — it updates instead of stacking.", MessageType.None);
        if (Button("Add / update enemy aura lights")) ApplyEnemyAuras(true);
        if (Button("Remove enemy aura lights")) ApplyEnemyAuras(false);

        // ---- 4 ----
        Header("4 — Snow above the player  (edits the Player prefab)");
        snowMaxParticles = EditorGUILayout.IntSlider("Max particles", snowMaxParticles, 50, 2000);
        snowRate = EditorGUILayout.Slider("Emission per second", snowRate, 5f, 400f);
        snowBoxSize = EditorGUILayout.Slider("Emitter width", snowBoxSize, 5f, 80f);
        snowSpawnHeight = EditorGUILayout.Slider("Emitter height", snowSpawnHeight, 2f, 40f);
        snowLifetime = EditorGUILayout.Slider("Flake lifetime", snowLifetime, 1f, 20f);
        snowSize = EditorGUILayout.Vector2Field("Flake size (min, max)", snowSize);
        snowWind = EditorGUILayout.Vector2Field("Wind (x, z)", snowWind);
        EditorGUILayout.LabelField(" ", $"about {Mathf.RoundToInt(Mathf.Min(snowRate * snowLifetime, snowMaxParticles))} flakes alive, 1 draw call");
        if (Button("Build snow prefab")) BuildSnowPrefab();
        if (Button("Attach snow to the Player prefab")) AttachSnowToPlayer();

        // ---- 5 ----
        Header("5 — Night grade  (edits the shared volume profile)");
        addTonemapping = EditorGUILayout.Toggle("Add tonemapping", addTonemapping);
        postExposure = EditorGUILayout.Slider("Post exposure", postExposure, -2f, 2f);
        contrast = EditorGUILayout.Slider("Contrast", contrast, -50f, 50f);
        colorFilter = EditorGUILayout.ColorField("Colour filter", colorFilter);
        saturation = EditorGUILayout.Slider("Saturation", saturation, -100f, 50f);
        EditorGUILayout.Space(4);
        whiteBalanceTemp = EditorGUILayout.Slider("Temperature", whiteBalanceTemp, -100f, 100f);
        whiteBalanceTint = EditorGUILayout.Slider("Tint", whiteBalanceTint, -100f, 100f);
        EditorGUILayout.Space(4);
        bloomThreshold = EditorGUILayout.Slider("Bloom threshold", bloomThreshold, 0f, 3f);
        bloomIntensity = EditorGUILayout.Slider("Bloom intensity", bloomIntensity, 0f, 5f);
        bloomTint = EditorGUILayout.ColorField("Bloom tint", bloomTint);
        EditorGUILayout.Space(4);
        vignetteIntensity = EditorGUILayout.Slider("Vignette", vignetteIntensity, 0f, 1f);
        vignetteSmoothness = EditorGUILayout.Slider("Vignette softness", vignetteSmoothness, 0.01f, 1f);
        vignetteColor = EditorGUILayout.ColorField("Vignette colour", vignetteColor);
        EditorGUILayout.Space(4);
        addFilmGrain = EditorGUILayout.Toggle("Add film grain", addFilmGrain);
        using (new EditorGUI.DisabledScope(!addFilmGrain))
            grainIntensity = EditorGUILayout.Slider("  Grain", grainIntensity, 0f, 1f);
        EditorGUILayout.HelpBox("This writes into the profile the scene's Global Volume points at — " +
                                "shared with the other scenes. Old values go to the console.", MessageType.Warning);
        if (Button("Apply the night grade")) ApplyGrade();

        EditorGUILayout.Space(16);
        EditorGUILayout.EndScrollView();
    }

    // =====================================================================
    // 1 — environment
    // =====================================================================
    void ApplyEnvironment()
    {
        EnsureFolder(SkyFolder);

        // The HDRI sits in Assets/Editor, which Unity strips out of builds.
        // Move it somewhere shipped. Moving keeps the GUID, so nothing breaks.
        if (AssetDatabase.LoadAssetAtPath<Texture>(HdriSource) != null &&
            AssetDatabase.LoadAssetAtPath<Texture>(HdriTarget) == null)
        {
            string err = AssetDatabase.MoveAsset(HdriSource, HdriTarget);
            if (!string.IsNullOrEmpty(err)) Debug.LogError("[Night] Could not move the HDRI: " + err);
            else Debug.Log("[Night] Moved the HDRI out of Assets/Editor — that folder never ships in a build.");
        }

        string hdriPath = AssetDatabase.LoadAssetAtPath<Texture>(HdriTarget) != null ? HdriTarget : HdriSource;

        // import it as a cubemap so it can drive a skybox
        var importer = AssetImporter.GetAtPath(hdriPath) as TextureImporter;
        if (importer != null && importer.textureShape != TextureImporterShape.TextureCube)
        {
            importer.textureShape = TextureImporterShape.TextureCube;
            importer.generateCubemap = TextureImporterGenerateCubemap.FullCubemap;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            Debug.Log("[Night] Re-imported the HDRI as a cubemap.");
        }

        var cube = AssetDatabase.LoadAssetAtPath<Cubemap>(hdriPath);

        var skyShader = Shader.Find("Skybox/Cubemap");
        if (skyShader == null) { Debug.LogError("[Night] Skybox/Cubemap shader missing."); return; }

        var skyMat = AssetDatabase.LoadAssetAtPath<Material>(SkyMatPath);
        if (skyMat == null)
        {
            skyMat = new Material(skyShader);
            AssetDatabase.CreateAsset(skyMat, SkyMatPath);
        }
        skyMat.shader = skyShader;
        if (cube != null) skyMat.SetTexture("_Tex", cube);
        skyMat.SetColor("_Tint", skyTint);
        skyMat.SetFloat("_Exposure", skyExposure);
        skyMat.SetFloat("_Rotation", skyRotation);
        EditorUtility.SetDirty(skyMat);

        // log what we are replacing, so it can be put back by hand
        Debug.Log($"[Night] Previous settings — fog {RenderSettings.fog}, " +
                  $"density {RenderSettings.fogDensity}, ambient mode {RenderSettings.ambientMode}, " +
                  $"skybox {(RenderSettings.skybox != null ? RenderSettings.skybox.name : "none")}.");

        RenderSettings.skybox = skyMat;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambientSky;
        RenderSettings.ambientEquatorColor = ambientEquator;
        RenderSettings.ambientGroundColor = ambientGround;
        RenderSettings.ambientIntensity = 1f;

        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        RenderSettings.defaultReflectionResolution = 128;
        RenderSettings.reflectionIntensity = reflectionIntensity;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;

        var moon = FindMoon();
        if (moon != null)
        {
            Undo.RecordObject(moon, "Night Lighting");
            Undo.RecordObject(moon.transform, "Night Lighting");
            moon.type = LightType.Directional;
            moon.color = moonColor;
            moon.intensity = moonIntensity;
            moon.shadows = LightShadows.Soft;
            moon.shadowStrength = moonShadowStrength;
            moon.transform.rotation = Quaternion.Euler(moonAngles.x, moonAngles.y, 0f);
            RenderSettings.sun = moon;
            EditorUtility.SetDirty(moon);
        }
        else Debug.LogWarning("[Night] No directional light in the scene — moonlight not set.");

        DynamicGI.UpdateEnvironment();
        AssetDatabase.SaveAssets();
        MarkDirty();

        Debug.Log($"[Night] Environment applied. Fog fades things out by about {ReadableDistance(fogDensity):0} m.");
    }

    Light FindMoon()
    {
        Light best = null;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional) continue;
            if (best == null || l.intensity > best.intensity) best = l;
        }
        return best;
    }

    // exp2 fog: visibility where the fog factor drops to ~2%
    static float ReadableDistance(float density)
    {
        if (density <= 0.0001f) return 9999f;
        return Mathf.Sqrt(-Mathf.Log(0.02f)) / density;
    }

    // =====================================================================
    // 2 — fog volumes
    // =====================================================================
    void ReadPeakHeight()
    {
        if (!TryGetLevelBounds(out Bounds level))
        {
            Debug.LogWarning("[Night] Could not measure the level.");
            return;
        }
        // start thickening a little under the peaks, finish well above them
        volHeightStart = Mathf.Round(level.max.y * 0.45f);
        volHeightRange = Mathf.Round(Mathf.Max(20f, level.max.y - volHeightStart));
        Repaint();
        Debug.Log($"[Night] Peaks measured at y={level.max.y:0}. " +
                  $"Ramp set to start at y={volHeightStart:0} over {volHeightRange:0} m.");
    }

    void BuildVolumetricFog()
    {
        var shader = Shader.Find(FogShaderName);
        if (shader == null)
        {
            EditorUtility.DisplayDialog("Night Lighting",
                "Shader \"" + FogShaderName + "\" not found.\n\n" +
                "It should be at Assets/NordicShaders/VolumetricFog.shader. " +
                "If Unity is still compiling, wait and try again.", "OK");
            return;
        }

        EnsureFolder(SkyFolder);
        var noise = EnsureFogNoise();

        var m = AssetDatabase.LoadAssetAtPath<Material>(FogMatPath);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, FogMatPath); }
        m.shader = shader;

        m.SetColor("_Color", volFogColor);
        m.SetFloat("_MaxDistance", volMaxDistance);
        m.SetFloat("_StepSize", volStepSize);
        m.SetFloat("_DensityMultiplier", volDensityMultiplier);
        m.SetFloat("_DensityThreshold", volDensityThreshold);
        m.SetFloat("_NoiseOffset", volNoiseOffset);
        m.SetFloat("_NoiseTiling", volNoiseTiling);
        m.SetColor("_LightContribution", volLightContribution);
        m.SetFloat("_LightScattering", volLightScattering);
        m.SetFloat("_HeightStart", volHeightStart);
        m.SetFloat("_HeightRange", volHeightRange);
        m.SetFloat("_HeightDensity", volHeightDensity);
        if (noise != null) m.SetTexture("_FogNoise", noise);

        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        Selection.activeObject = m;

        int samples = Mathf.CeilToInt(volMaxDistance / Mathf.Max(0.01f, volStepSize));
        Debug.Log($"[Night] Fog material ready at {FogMatPath} — {samples} raymarch samples per pixel. " +
                  "Past the max distance the cheap built-in fog from step 1 takes over.", m);
    }

    // Tileable 3D value noise. Four channels at rising frequencies, because the shader
    // does dot(noise, noise) — that turns four octaves into one density in one sample.
    Texture3D EnsureFogNoise()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Texture3D>(FogNoisePath);
        if (existing != null && existing.width == volNoiseRes) return existing;

        int n = volNoiseRes;
        var tex = new Texture3D(n, n, n, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Trilinear
        };

        int[] freq = { 2, 4, 8, 16 };
        var pixels = new Color32[n * n * n];

        for (int z = 0; z < n; z++)
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float fx = (float)x / n, fy = (float)y / n, fz = (float)z / n;
                    var c = new Color32(
                        (byte)(Mathf.Clamp01(TileNoise(fx, fy, fz, freq[0])) * 255f),
                        (byte)(Mathf.Clamp01(TileNoise(fx, fy, fz, freq[1])) * 255f),
                        (byte)(Mathf.Clamp01(TileNoise(fx, fy, fz, freq[2])) * 255f),
                        (byte)(Mathf.Clamp01(TileNoise(fx, fy, fz, freq[3])) * 255f));
                    pixels[x + y * n + z * n * n] = c;
                }

        tex.SetPixels32(pixels);
        tex.Apply(false, false);

        if (existing != null) AssetDatabase.DeleteAsset(FogNoisePath);
        AssetDatabase.CreateAsset(tex, FogNoisePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Night] Built a {n}³ tileable 3D noise texture at {FogNoisePath}.");
        return AssetDatabase.LoadAssetAtPath<Texture3D>(FogNoisePath);
    }

    static float TileNoise(float x, float y, float z, int freq)
    {
        float px = x * freq, py = y * freq, pz = z * freq;
        int x0 = Mathf.FloorToInt(px), y0 = Mathf.FloorToInt(py), z0 = Mathf.FloorToInt(pz);
        float tx = Smooth(px - x0), ty = Smooth(py - y0), tz = Smooth(pz - z0);

        float c000 = Hash(x0, y0, z0, freq), c100 = Hash(x0 + 1, y0, z0, freq);
        float c010 = Hash(x0, y0 + 1, z0, freq), c110 = Hash(x0 + 1, y0 + 1, z0, freq);
        float c001 = Hash(x0, y0, z0 + 1, freq), c101 = Hash(x0 + 1, y0, z0 + 1, freq);
        float c011 = Hash(x0, y0 + 1, z0 + 1, freq), c111 = Hash(x0 + 1, y0 + 1, z0 + 1, freq);

        float x00 = Mathf.Lerp(c000, c100, tx), x10 = Mathf.Lerp(c010, c110, tx);
        float x01 = Mathf.Lerp(c001, c101, tx), x11 = Mathf.Lerp(c011, c111, tx);
        return Mathf.Lerp(Mathf.Lerp(x00, x10, ty), Mathf.Lerp(x01, x11, ty), tz);
    }

    static float Smooth(float t) => t * t * (3f - 2f * t);

    // wrapping the lattice at `freq` is what makes the texture tile seamlessly
    static float Hash(int x, int y, int z, int freq)
    {
        x = ((x % freq) + freq) % freq;
        y = ((y % freq) + freq) % freq;
        z = ((z % freq) + freq) % freq;
        uint h = (uint)(x * 73856093 ^ y * 19349663 ^ z * 83492791 ^ freq * 2654435761);
        h ^= h >> 13; h *= 1274126177u; h ^= h >> 16;
        return (h & 0xFFFFFF) / (float)0xFFFFFF;
    }

    // ---- install / remove the fullscreen pass on the URP renderers ----
    void InstallFogFeature(bool add)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(FogMatPath);
        if (add && mat == null)
        {
            EditorUtility.DisplayDialog("Night Lighting", "Build the fog material first.", "OK");
            return;
        }

        int done = 0;
        foreach (var path in RendererPaths)
        {
            var data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
            if (data == null) { Debug.LogWarning("[Night] Renderer not found: " + path); continue; }

            var so = new SerializedObject(data);
            var features = so.FindProperty("m_RendererFeatures");
            var map = so.FindProperty("m_RendererFeatureMap");
            if (features == null || map == null)
            {
                Debug.LogError("[Night] Unexpected renderer layout in " + path + " — add the feature by hand.");
                continue;
            }

            int found = -1;
            for (int i = 0; i < features.arraySize; i++)
            {
                var o = features.GetArrayElementAtIndex(i).objectReferenceValue;
                if (o != null && o.name == FeatureName) { found = i; break; }
            }

            if (!add)
            {
                if (found < 0) continue;
                var old = features.GetArrayElementAtIndex(found).objectReferenceValue;
                features.DeleteArrayElementAtIndex(found);
                features.DeleteArrayElementAtIndex(found);      // clears the now-null slot
                so.ApplyModifiedProperties();
                if (old != null) { AssetDatabase.RemoveObjectFromAsset(old); DestroyImmediate(old, true); }
                RebuildFeatureMap(data);
                EditorUtility.SetDirty(data);
                done++;
                continue;
            }

            Object feature;
            if (found >= 0) feature = features.GetArrayElementAtIndex(found).objectReferenceValue;
            else
            {
                var created = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                created.name = FeatureName;
                AssetDatabase.AddObjectToAsset(created, data);
                so.Update();
                int i = features.arraySize;
                features.InsertArrayElementAtIndex(i);
                features.GetArrayElementAtIndex(i).objectReferenceValue = created;
                so.ApplyModifiedProperties();
                feature = created;
            }

            // set the feature's fields through SerializedObject so a URP rename can't stop this compiling
            var fso = new SerializedObject(feature);
            SetProp(fso, "passMaterial", mat);
            SetEnum(fso, "injectionPoint", 1);          // Before Rendering Post Processing
            SetFlags(fso, "requirements", (int)ScriptableRenderPassInput.Depth);
            SetBool(fso, "fetchColorBuffer", true);
            fso.ApplyModifiedProperties();

            RebuildFeatureMap(data);
            EditorUtility.SetDirty(data);
            done++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Night] Fog {(add ? "installed on" : "removed from")} {done} renderer(s). " +
                  "Check it in Project Settings > Graphics, or by selecting PC_Renderer.");
    }

    static void RebuildFeatureMap(ScriptableRendererData data)
    {
        var so = new SerializedObject(data);
        var features = so.FindProperty("m_RendererFeatures");
        var map = so.FindProperty("m_RendererFeatureMap");
        if (features == null || map == null) return;

        map.arraySize = features.arraySize;
        for (int i = 0; i < features.arraySize; i++)
        {
            var o = features.GetArrayElementAtIndex(i).objectReferenceValue;
            long id = 0;
            if (o != null) AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out _, out id);
            map.GetArrayElementAtIndex(i).longValue = id;
        }
        so.ApplyModifiedProperties();
    }

    static void SetProp(SerializedObject so, string name, Object value)
    {
        var p = so.FindProperty(name);
        if (p != null) p.objectReferenceValue = value;
        else Debug.LogWarning("[Night] Field '" + name + "' not found — set it by hand on the feature.");
    }

    static void SetEnum(SerializedObject so, string name, int value)
    {
        var p = so.FindProperty(name);
        if (p != null) p.enumValueIndex = value;
    }

    // flags enums store the real bit value, not the list index
    static void SetFlags(SerializedObject so, string name, int value)
    {
        var p = so.FindProperty(name);
        if (p != null) p.intValue = value;
    }

    static void SetBool(SerializedObject so, string name, bool value)
    {
        var p = so.FindProperty(name);
        if (p != null && p.propertyType == SerializedPropertyType.Boolean) p.boolValue = value;
    }

    bool TryGetLevelBounds(out Bounds b)
    {
        b = new Bounds();
        bool got = false;

        var env = GameObject.Find(EnvRoot);
        if (env != null)
            foreach (var r in env.GetComponentsInChildren<MeshRenderer>())
            {
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

    // =====================================================================
    // 3 — enemy aura lights
    // =====================================================================
    void ApplyEnemyAuras(bool add)
    {
        var paths = new List<string>();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Game" }))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            var file = Path.GetFileNameWithoutExtension(p);
            if (!file.StartsWith("Enemy")) continue;
            paths.Add(p);
        }

        if (paths.Count == 0) { Debug.LogWarning("[Night] No Enemy prefabs found."); return; }

        int touched = 0;
        foreach (var p in paths)
        {
            var root = PrefabUtility.LoadPrefabContents(p);
            try
            {
                var existing = root.transform.Find(AuraLightName);

                if (!add)
                {
                    if (existing == null) continue;
                    DestroyImmediate(existing.gameObject);
                    PrefabUtility.SaveAsPrefabAsset(root, p);
                    touched++;
                    continue;
                }

                GameObject lightGo;
                if (existing != null) lightGo = existing.gameObject;
                else
                {
                    lightGo = new GameObject(AuraLightName);
                    lightGo.transform.SetParent(root.transform, false);
                }

                lightGo.transform.localPosition = new Vector3(0f, auraHeight, 0f);

                var l = lightGo.GetComponent<Light>();
                if (l == null) l = lightGo.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = auraColor;
                l.intensity = auraIntensity;
                l.range = auraRange;
                l.shadows = LightShadows.None;          // shadows on a swarm of enemies is a frame-rate hole
                l.renderMode = LightRenderMode.Auto;
                l.lightmapBakeType = LightmapBakeType.Realtime;

                PrefabUtility.SaveAsPrefabAsset(root, p);
                touched++;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[Night] Aura lights {(add ? "added to" : "removed from")} {touched} enemy prefab(s). " +
                  "URP allows 4 additional lights per object — if enemies pile up, some auras stop lighting nearby surfaces.");
    }

    // =====================================================================
    // 4 — snow
    // =====================================================================
    void BuildSnowPrefab()
    {
        EnsureFolder(SkyFolder);
        var tex = EnsureFlakeTexture();
        var mat = EnsureSnowMaterial(tex);

        var go = new GameObject("Snow_Local");
        var ps = go.AddComponent<ParticleSystem>();
        var psr = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = snowLifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(snowSize.x, snowSize.y);
        main.startColor = new Color(1f, 1f, 1f, 0.85f);
        main.gravityModifier = 0.05f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;  // flakes fall, they don't ride the player
        main.maxParticles = snowMaxParticles;
        main.playOnAwake = true;
        main.cullingMode = ParticleSystemCullingMode.Automatic;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = snowRate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(snowBoxSize, 0.5f, snowBoxSize);
        shape.position = new Vector3(0f, snowSpawnHeight, 0f);
        shape.rotation = new Vector3(90f, 0f, 0f);                  // emit downward
        shape.randomDirectionAmount = 0.05f;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(snowWind.x);
        vel.z = new ParticleSystem.MinMaxCurve(snowWind.y);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.25f);
        noise.frequency = 0.15f;
        noise.scrollSpeed = new ParticleSystem.MinMaxCurve(0.2f);
        noise.quality = ParticleSystemNoiseQuality.Low;             // cheapest noise that still reads
        noise.damping = true;

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-40f, 40f);

        // everything below is off on purpose — each one costs frames
        var col = ps.collision; col.enabled = false;
        var trails = ps.trails; trails.enabled = false;
        var sub = ps.subEmitters; sub.enabled = false;
        var lights = ps.lights; lights.enabled = false;
        var sheet = ps.textureSheetAnimation; sheet.enabled = false;

        psr.renderMode = ParticleSystemRenderMode.Billboard;
        psr.sharedMaterial = mat;
        psr.alignment = ParticleSystemRenderSpace.View;
        psr.sortMode = ParticleSystemSortMode.None;
        psr.shadowCastingMode = ShadowCastingMode.Off;
        psr.receiveShadows = false;
        psr.lightProbeUsage = LightProbeUsage.Off;
        psr.reflectionProbeUsage = ReflectionProbeUsage.Off;

        EnsureFolder("Assets/Prefabs/Environment");
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, SnowPrefabPath);
        DestroyImmediate(go);

        AssetDatabase.SaveAssets();
        Selection.activeObject = prefab;
        Debug.Log($"[Night] Snow prefab written to {SnowPrefabPath}. " +
                  $"Max {snowMaxParticles} flakes, one material, no collision, no shadows.", prefab);
    }

    void AttachSnowToPlayer()
    {
        var snow = AssetDatabase.LoadAssetAtPath<GameObject>(SnowPrefabPath);
        if (snow == null) { EditorUtility.DisplayDialog("Night Lighting", "Build the snow prefab first.", "OK"); return; }

        var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (player == null) { EditorUtility.DisplayDialog("Night Lighting", "Player prefab not found at " + PlayerPrefabPath, "OK"); return; }

        var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            if (root.transform.Find("Snow_Local") != null)
            {
                Debug.Log("[Night] Player already carries the snow — nothing to do.");
                return;
            }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(snow, root.scene);
            inst.name = "Snow_Local";
            inst.transform.SetParent(root.transform, false);
            inst.transform.localPosition = Vector3.zero;
            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Debug.Log("[Night] Snow attached to the Player prefab. It emits from a box " +
                      snowSpawnHeight + " m above the player and simulates in world space.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        AssetDatabase.SaveAssets();
    }

    Texture2D EnsureFlakeTexture()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(SnowTexPath);
        if (existing != null) return existing;

        const int s = 64;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = (x + 0.5f) / s - 0.5f;
                float dy = (y + 0.5f) / s - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;      // 0 centre, 1 edge
                float a = Mathf.Clamp01(1f - d);
                a = a * a;                                          // soft falloff
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();

        File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), SnowTexPath), tex.EncodeToPNG());
        DestroyImmediate(tex);
        AssetDatabase.ImportAsset(SnowTexPath, ImportAssetOptions.ForceUpdate);

        var imp = AssetImporter.GetAtPath(SnowTexPath) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Default;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.mipmapEnabled = true;
            imp.maxTextureSize = 64;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(SnowTexPath);
    }

    Material EnsureSnowMaterial(Texture2D tex)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        var m = AssetDatabase.LoadAssetAtPath<Material>(SnowMatPath);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, SnowMatPath); }
        m.shader = shader;

        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", new Color(0.95f, 0.97f, 1f, 1f));

        // URP transparent setup, done by hand because the material is made from script
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_AlphaClip", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_Cull", (float)CullMode.Off);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.DisableKeyword("_ALPHATEST_ON");
        m.renderQueue = (int)RenderQueue.Transparent;

        EditorUtility.SetDirty(m);
        return m;
    }

    // =====================================================================
    // 5 — night grade on the shared volume profile
    // =====================================================================
    void ApplyGrade()
    {
        var profile = FindSceneProfile();
        if (profile == null)
        {
            EditorUtility.DisplayDialog("Night Lighting",
                "No global Volume with a profile found in the open scene.", "OK");
            return;
        }

        string path = AssetDatabase.GetAssetPath(profile);
        Undo.RecordObject(profile, "Night Grade");

        // Tonemapping was missing. In HDR everything above 1.0 clips flat white without it —
        // the moon, the bloom and the enemy auras all turn into paper cut-outs.
        if (addTonemapping)
        {
            if (!profile.TryGet<Tonemapping>(out var tm)) tm = profile.Add<Tonemapping>(true);
            tm.active = true;
            tm.mode.overrideState = true;
            tm.mode.value = TonemappingMode.Neutral;
        }

        if (!profile.TryGet<ColorAdjustments>(out var ca)) ca = profile.Add<ColorAdjustments>(true);
        Debug.Log($"[Night] ColorAdjustments was — exposure {ca.postExposure.value}, " +
                  $"contrast {ca.contrast.value}, filter {ca.colorFilter.value}, saturation {ca.saturation.value}.");
        ca.active = true;
        ca.postExposure.overrideState = true; ca.postExposure.value = postExposure;
        ca.contrast.overrideState = true; ca.contrast.value = contrast;
        ca.colorFilter.overrideState = true; ca.colorFilter.value = colorFilter;
        ca.saturation.overrideState = true; ca.saturation.value = saturation;

        if (!profile.TryGet<WhiteBalance>(out var wb)) wb = profile.Add<WhiteBalance>(true);
        wb.active = true;
        wb.temperature.overrideState = true; wb.temperature.value = whiteBalanceTemp;
        wb.tint.overrideState = true; wb.tint.value = whiteBalanceTint;

        if (!profile.TryGet<Bloom>(out var bloom)) bloom = profile.Add<Bloom>(true);
        Debug.Log($"[Night] Bloom was — threshold {bloom.threshold.value}, intensity {bloom.intensity.value}.");
        bloom.active = true;
        bloom.threshold.overrideState = true; bloom.threshold.value = bloomThreshold;
        bloom.intensity.overrideState = true; bloom.intensity.value = bloomIntensity;
        bloom.tint.overrideState = true; bloom.tint.value = bloomTint;

        if (!profile.TryGet<Vignette>(out var vig)) vig = profile.Add<Vignette>(true);
        Debug.Log($"[Night] Vignette was — intensity {vig.intensity.value}, smoothness {vig.smoothness.value}.");
        vig.active = true;
        vig.intensity.overrideState = true; vig.intensity.value = vignetteIntensity;
        vig.smoothness.overrideState = true; vig.smoothness.value = vignetteSmoothness;
        vig.color.overrideState = true; vig.color.value = vignetteColor;

        // grain also breaks up the banding you get across big dark fog gradients
        if (addFilmGrain)
        {
            if (!profile.TryGet<FilmGrain>(out var fg)) fg = profile.Add<FilmGrain>(true);
            fg.active = true;
            fg.type.overrideState = true; fg.type.value = FilmGrainLookup.Thin1;
            fg.intensity.overrideState = true; fg.intensity.value = grainIntensity;
            fg.response.overrideState = true; fg.response.value = 0.8f;
        }

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        Selection.activeObject = profile;
        Debug.Log("[Night] Night grade written to " + path + ". Old values are logged above.", profile);
    }

    static VolumeProfile FindSceneProfile()
    {
        VolumeProfile best = null;
        float bestPriority = float.NegativeInfinity;
        foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            if (!v.isGlobal || v.sharedProfile == null) continue;
            if (v.priority >= bestPriority) { bestPriority = v.priority; best = v.sharedProfile; }
        }
        return best;
    }

    // =====================================================================
    // helpers
    // =====================================================================
    static void Header(string t)
    {
        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField(t, EditorStyles.boldLabel);
    }

    static bool Button(string label)
    {
        return GUILayout.Button(label, GUILayout.Height(26));
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

    static void MarkDirty()
    {
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
