using TMPro;
using UnityEngine;

public class CoinHud : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI TextMesh;

    public void UpdateCoinsUi(float value)
    {
        if (TextMesh == null)
        {
            Debug.LogError("TextMeshProUGUI reference is not assigned.");
            return;
        }
        TextMesh.text = $"${value.ToString("F0")}";

    }
}
