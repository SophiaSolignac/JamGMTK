using TMPro;
using UnityEngine;

public class HealthTimeUi : MonoBehaviour
{
    public const int MS_IN_SECOND = 1000;
    public const int MS_IN_MINUTE = 60000;
    
    [SerializeField]
    TextMeshProUGUI TextMesh;
   
    public void UpdateHealthTime(float time)
    {
        if (TextMesh == null)
        {
            Debug.LogError("TextMeshProUGUI reference is not assigned.");
            return;
        }
        float min = Mathf.Floor(time / 60000);
        time -= min * MS_IN_MINUTE;
        float sec = Mathf.Floor(time / MS_IN_SECOND);
        float ms = Mathf.Floor(time % 1000);
        ms /= 10; // Convert milliseconds to centiseconds for display
        if (min > 0)
        {
            TextMesh.text = $"{min:00}:{sec:00}:{ms:00}";
        }
        else 
        {
            TextMesh.text = $"{sec:00}:{ms:00}";
        }
    }
}
