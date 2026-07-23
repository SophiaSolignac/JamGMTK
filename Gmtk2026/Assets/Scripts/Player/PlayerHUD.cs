using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    public enum Crosshair { Empty, Full }

    [SerializeField] Image _crosshair;
    [SerializeField] Sprite[] _crosshairRenderer;
}