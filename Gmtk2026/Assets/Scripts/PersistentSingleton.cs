using UnityEngine;

public abstract class PersistentSingleton<T> : MonoBehaviour where T : PersistentSingleton<T>
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        transform.parent = null; // Ensure the singleton is not a child of any other GameObject
        Instance = (T)this;
        DontDestroyOnLoad(gameObject);
    }
}