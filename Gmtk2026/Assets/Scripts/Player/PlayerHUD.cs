using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    public enum Crosshair { Empty, Full }

    public FMODUnity.EventReference PlayerStateEvent;
    [SerializeField] Image _crosshair;
    [SerializeField] Sprite[] _crosshairRenderer;
    [SerializeField] HealthTimeUi _healthText;
    [SerializeField] CoinHud _coins;
    [SerializeField] Crosshair _crosshairType = Crosshair.Full;

    private void Start()
        => ChangeCrosshair(_crosshairType);

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
    public void UpdateCoinUI(int value)
    {
        FMOD.Studio.EventInstance playerState = FMODUnity.RuntimeManager.CreateInstance(PlayerStateEvent);
        playerState.start(); 
        _coins.UpdateCoinsUi(value);
    }
}