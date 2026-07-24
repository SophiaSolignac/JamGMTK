using UnityEngine;

[CreateAssetMenu(fileName = "new gun", menuName = "Scriptable Objects/Gun")]
public class SOGun : SOWeapon
{
    public const float DEFAULT_CORRECTION_AIM_DISTANCE = 99999f;

    // -------~~~~~~~~~~================# // Weapon Type
    public enum HitType { Raycast, Bullet }
    public enum AimType { Auto, Self, Owner}

    // -------~~~~~~~~~~================# // Settings
    [Header("Gun Settings")]
    [SerializeField] HitType _type = HitType.Raycast;
    [SerializeField] AimType _aim = AimType.Self;
    [SerializeField] bool _correctAimWithHolder = true;

    public HitType Type => _type;
    public AimType Aim => _aim;
    public bool CorrectAimWithHolder => _correctAimWithHolder;


    // -------~~~~~~~~~~================# // Firing
    [Header("Firing")]
    [SerializeField] int _amoMax = 1;
    [SerializeField] float _fireRate = 1f;

    public int AmoMax => _amoMax;
    public float FireRate => _fireRate;

    // -------~~~~~~~~~~================# // Scan
    [Header("Raycast")]
    [SerializeField] int _damage = 1;
    [SerializeField] float _distance = 999f;
    [SerializeField] LayerMask _layerMask = Physics.AllLayers;
    [SerializeField] bool _perssing;

    public int Damage => _damage;
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