// Nordic Mountain Material — Countdown Nordic
// Menu:  Tools > Nordic > Make Mountain Triplanar Material
//
// Copies M_MountainIce onto the Nordic/Mountain Triplanar shader, carrying every texture
// and colour across. The source material is never touched — the copy sits next to it so
// you can put one on a mountain, keep the other, and compare.

using UnityEditor;
using UnityEngine;

public static class NordicMountainMaterial
{
    const string Source = "Assets/NordicShaders/Materials/M_MountainIce.mat";
    const string Target = "Assets/NordicShaders/Materials/M_MountainIce_Triplanar.mat";
    const string ShaderName = "Nordic/Mountain Triplanar";

    [MenuItem("Tools/Nordic/Make Mountain Triplanar Material")]
    static void Make()
    {
        var src = AssetDatabase.LoadAssetAtPath<Material>(Source);
        if (src == null)
        {
            EditorUtility.DisplayDialog("Mountain material", "Not found:\n" + Source, "OK");
            return;
        }

        var shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            EditorUtility.DisplayDialog("Mountain material",
                $"Shader \"{ShaderName}\" not found.\n\nIt should be at " +
                "Assets/NordicShaders/MountainTriplanar.shader. If Unity is still compiling, " +
                "wait and try again.", "OK");
            return;
        }

        var dst = AssetDatabase.LoadAssetAtPath<Material>(Target);
        if (dst == null)
        {
            dst = new Material(shader);
            AssetDatabase.CreateAsset(dst, Target);
        }
        dst.shader = shader;

        // textures carried straight over — same names on both shaders
        CopyTexture(src, dst, "_BaseAlbedo");
        CopyTexture(src, dst, "_BaseNormal");
        CopyTexture(src, dst, "_RoughnessMap");
        CopyTexture(src, dst, "_OcclusionMap");
        CopyTexture(src, dst, "_SnowAlbedo");

        CopyColor(src, dst, "_Tint", Color.white);
        CopyColor(src, dst, "_SnowColor", new Color(0.9f, 0.93f, 0.97f));

        CopyFloat(src, dst, "_Smoothness", 0.6f);
        CopyFloat(src, dst, "_RoughnessStrength", 0.75f);
        CopyFloat(src, dst, "_Metallic", 0f);
        CopyFloat(src, dst, "_NormalScale", 1f);
        CopyFloat(src, dst, "_OcclusionStrength", 1f);
        CopyFloat(src, dst, "_SnowAmount", 0.45f);
        CopyFloat(src, dst, "_SnowSmoothness", 0.35f);
        CopyFloat(src, dst, "_Cull", 2f);

        if (src.HasProperty("_SnowDirection"))
            dst.SetVector("_SnowDirection", src.GetVector("_SnowDirection"));
        else
            dst.SetVector("_SnowDirection", new Vector4(0, 1, 0, 0));

        // starting point sized for mountains, not for props
        if (dst.GetFloat("_TileSize") <= 0.011f) dst.SetFloat("_TileSize", 14f);

        EditorUtility.SetDirty(dst);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = dst;
        EditorGUIUtility.PingObject(dst);

        Debug.Log($"[Mountain] {Target} created from M_MountainIce on {ShaderName}. " +
                  "Tile size is in METRES per tile — raise it until the repeat stops reading, " +
                  "then use Macro strength to break what is left. The original is untouched.", dst);
    }

    static void CopyTexture(Material a, Material b, string name)
    {
        if (a.HasProperty(name) && b.HasProperty(name)) b.SetTexture(name, a.GetTexture(name));
    }

    static void CopyColor(Material a, Material b, string name, Color fallback)
    {
        if (!b.HasProperty(name)) return;
        b.SetColor(name, a.HasProperty(name) ? a.GetColor(name) : fallback);
    }

    static void CopyFloat(Material a, Material b, string name, float fallback)
    {
        if (!b.HasProperty(name)) return;
        b.SetFloat(name, a.HasProperty(name) ? a.GetFloat(name) : fallback);
    }
}
