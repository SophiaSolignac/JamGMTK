using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Drains the screen of colour as the run timer runs down (the "Countdown" mechanic:
/// the closer death gets, the more desaturated everything becomes).
///
/// This is the reliable URP way to do a global saturation VFX: it drives the built-in
/// Color Adjustments (Saturation) post-process via a Volume, so no fullscreen shader or
/// renderer feature is needed. Optionally also closes a Vignette as death nears.
///
/// SETUP:
///  1. GameObject > Volume > Global Volume  (or add a Volume component set to Global).
///  2. On that Volume, create/assign a Volume Profile.
///  3. Add this component to the same GameObject.
///  4. From your timer/health system, each frame call SetTimeRemaining01(remaining / maxTime),
///     or set deathProximity directly (0 = full colour, 1 = dead / fully grey).
/// Requires Post Processing enabled on the Camera and a Color Adjustments-capable URP asset.
/// </summary>
[RequireComponent(typeof(Volume))]
[DisallowMultipleComponent]
public class TimerDesaturation : MonoBehaviour
{
    [Header("Drive this from the countdown")]
    [Tooltip("0 = full colour (timer full)  ->  1 = fully desaturated (death).")]
    [Range(0f, 1f)] public float deathProximity = 0f;

    [Header("Tuning")]
    [Tooltip("Saturation applied at deathProximity = 1 (-100 = fully grey).")]
    [Range(-100f, 0f)] public float saturationAtDeath = -100f;
    [Tooltip("Optional: ease the curve. 1 = linear, >1 = colour holds then drops late.")]
    [Range(1f, 4f)] public float falloffPower = 1.6f;

    [Header("Optional vignette")]
    public bool alsoVignette = true;
    [Range(0f, 1f)] public float vignetteAtDeath = 0.45f;

    Volume _volume;
    ColorAdjustments _color;
    Vignette _vignette;

    void Awake()
    {
        _volume = GetComponent<Volume>();
        if (_volume.profile == null)
        {
            Debug.LogWarning("[TimerDesaturation] Volume has no profile assigned.", this);
            return;
        }
        if (!_volume.profile.TryGet(out _color))
            _color = _volume.profile.Add<ColorAdjustments>(true);
        _color.saturation.overrideState = true;

        if (alsoVignette)
        {
            if (!_volume.profile.TryGet(out _vignette))
                _vignette = _volume.profile.Add<Vignette>(true);
            _vignette.intensity.overrideState = true;
        }
        Apply();
    }

    /// <summary>Convenience: pass normalized time remaining (1 = full, 0 = dead).</summary>
    public void SetTimeRemaining01(float timeRemaining01)
    {
        deathProximity = 1f - Mathf.Clamp01(timeRemaining01);
        Apply();
    }

    void Apply()
    {
        float t = Mathf.Pow(Mathf.Clamp01(deathProximity), falloffPower);
        if (_color != null)
            _color.saturation.value = Mathf.Lerp(0f, saturationAtDeath, t);
        if (_vignette != null)
            _vignette.intensity.value = Mathf.Lerp(0f, vignetteAtDeath, t);
    }

#if UNITY_EDITOR
    // Live preview when dragging deathProximity in the Inspector during play.
    void OnValidate() { if (Application.isPlaying) Apply(); }
#endif
}
