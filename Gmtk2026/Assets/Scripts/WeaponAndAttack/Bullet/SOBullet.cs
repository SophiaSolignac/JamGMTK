using UnityEngine;

[CreateAssetMenu(fileName = "new bullet", menuName = "Scriptable Objects/Bullet")]
public class SOBullet : ScriptableObject
{
    [Header("Settings")]
    [SerializeField] string _title = "new bullet";
    [SerializeField] int _damage = 1;
    public string title => _title;
    public int Damage => _damage;

    [Header("Despawning")]
    [SerializeField] float _despawnDistance = 1000f;
    [SerializeField] float _LifeTimie = 10f;
    public float DespawnDistance => _despawnDistance;
    public float LifeTime => _LifeTimie;

    [Header("Movement")]
    [SerializeField] float _speed;
    public float Speed => _speed;
}