using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static SOUpgrade;
using static UnityEngine.Rendering.DebugUI;

[CreateAssetMenu(fileName = "new upgrade", menuName = "Scriptable Objects/Upgrade")]
public class SOUpgrade : ScriptableObject
{
    public enum Usage { Set, Add, Remove, Divide, Multiply }

    [Serializable]
    public struct UpgradeValue
    {
        public float value;
        public Usage usage;

        public override bool Equals(object obj)
        {
            if (!(obj is UpgradeValue asUV)) return false;
            return asUV.value == value && asUV.usage == usage;
        }
    }

    public struct Upgrade
    {
        [Header("Time")]
        public UpgradeValue _time;

        [Header("Movement")]
        public UpgradeValue _movmentSpeed;

        [Header("Dash")]
        public UpgradeValue _dashForce;
        public UpgradeValue _dashDuration;
        public UpgradeValue _dashCooldown;

        [Header("Jump")]
        public UpgradeValue _jumpForce;

        /*public override bool Equals(object obj)
        {
            if (!(obj is UpgradeValue asUV)) return false;
            return asUV.value == value && asUV.usage == usage;
        }*/
    }

    [SerializeField] List<Upgrade> _upgrades = new List<Upgrade>();

    public Upgrade this[int index]
    {
        get
        {
            int count = _upgrades.Count;

            if (count <= 0 || index >= count) return default;
            return _upgrades[index];
        }
    }
}