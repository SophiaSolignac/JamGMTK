using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    public enum Crosshair { Empty, Full }

    [SerializeField] Image _crosshair;
    [SerializeField] Sprite[] _crosshairRenderer;
    [SerializeField] HealthTimeUi _healthText;



    public void ChangeCrosshair(Crosshair crosshair)
    {
        int index = (int)crosshair;

        if (!_crosshair || _crosshairRenderer == null || _crosshairRenderer.Length <= index) return;

        _crosshair.sprite = _crosshairRenderer[index];
    }

    public void UpdateHealthUI(float time)
    {
        _healthText.UpdateHealthTime(time);
    }
}