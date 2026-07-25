using UnityEngine;

public class Singleton : MonoBehaviour
{
    [SerializeField] SOSingleton _settingsAndID;

    private void Awake()
    {
        transform.parent = null;
        _settingsAndID?.CheckDesroy(this);
    }
}