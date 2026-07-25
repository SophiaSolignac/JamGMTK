using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "new singleton", menuName = "Scriptable Objects/Singleton")]
public class SOSingleton : ScriptableObject
{
    public enum DestroyType { DestroyNew, DestroyOld, DestroyAll ,KeepBoth }

    [SerializeField] DestroyType _destroyBehavior = DestroyType.DestroyNew;

    public DestroyType DestroyBehavior => _destroyBehavior;
    private List<Singleton> _instances = new List<Singleton>();

    public void CheckDesroy(Singleton singleton)
    {
        CleanUpInstances();

        switch (_destroyBehavior)
        {
            case DestroyType.KeepBoth:
                if (!_instances.Contains(singleton))
                    _instances.Add(singleton);

                DontDestroyOnLoad(singleton);

                return;

            case DestroyType.DestroyNew:
                if (_instances.Count > 0) Destroy(singleton.gameObject);
                else
                {
                    _instances.Add(singleton);
                    DontDestroyOnLoad(singleton);
                }
                return;

            case DestroyType.DestroyOld:
                foreach (Singleton old in _instances)
                    if (old != singleton)
                        Destroy(old.gameObject);

                if (!_instances.Contains(singleton))
                    _instances.Add(singleton);

                DontDestroyOnLoad(singleton);

                return;

            case DestroyType.DestroyAll:
                foreach (Singleton old in _instances)
                    if (old) Destroy(old.gameObject);

                if (!singleton) Destroy(singleton.gameObject);
                return;
        }
    }

    public void CleanUpInstances()
    {
        if (_instances == null)
        {
            _instances = new List<Singleton>();
            return;
        }

        List<Singleton> cleanList = new List<Singleton>();
        Singleton current;
        for (int index = _instances.Count - 1; index >= 0; index--)
        {
            current = _instances[index];
            if (current == null) continue;
            cleanList.Add(current);
        }

        _instances = cleanList;
    }
}