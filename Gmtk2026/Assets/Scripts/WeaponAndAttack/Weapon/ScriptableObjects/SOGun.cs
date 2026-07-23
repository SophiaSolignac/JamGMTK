using UnityEngine;

[CreateAssetMenu(fileName = "new gun", menuName = "Scriptable Objects/Gun")]
public class SOGun : SOWeapon
{
    // -------~~~~~~~~~~================# // Weapon Type
    public enum HitType { Raycast, Bullet }

    // -------~~~~~~~~~~================# // Settings
    [Header("Gun Settings")]
    [SerializeField] HitType _type;

    public HitType Type => _type;

    // -------~~~~~~~~~~================# // Firing
    [Header("Firing")]
    [SerializeField] int _amoMax = 1;
    [SerializeField] float _fireRate = 1f;

    public int AmoMax => _amoMax;
    public float FireRate => _fireRate;

    // -------~~~~~~~~~~================# // Scan
    [Header("Raycast")]
    [SerializeField] float _distance = 999f;
    [SerializeField] LayerMask _layerMask = Physics.AllLayers;
    [SerializeField] bool _perssing;

    public float Distance => _distance;
    public LayerMask LayerMask => _layerMask;
    public bool Perssing => _perssing;

    // -------~~~~~~~~~~================# // Bullet
    [Header("Bullet")]
    [Tooltip("Is Only Used For Bullet Type Weapon")]
    [SerializeField] Bullet _bullet;
    [SerializeField] Vector2 _speedMultipler = Vector2.one;
    [SerializeField] float _angleOpening = 0f;
    [SerializeField] int _bulletPerShot = 1;

    public Bullet Bullet => _bullet;
    public Vector2 SpeedMultiplyer => _speedMultipler;
    public float AngleOpening  => _angleOpening;
    public int BulletPerShot  => _bulletPerShot;
}