using FMOD.Studio;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static SOUpgrade;
using static UnityEngine.Rendering.DebugUI;

[CreateAssetMenu(fileName = "new upgrade", menuName = "Scriptable Objects/Upgrade")]
public class SOUpgrade : ScriptableObject
{
    public enum Usage { None, Set, Add, Remove, Multiply, Divide }

    [Serializable]
    public struct UpgradeValue
    {
        [SerializeField] Usage _usage;
        [SerializeField] AnimationCurve _curve;
        [SerializeField] Vector3 _value;

        public Usage Usage => _usage;
        public float Value => _value.z;

        public float GetValue(float ratio)
        {
            if (_curve == null) return Mathf.LerpUnclamped(_value.x, _value.y, ratio);
            return Mathf.LerpUnclamped(_value.x, _value.y, _curve.Evaluate(ratio));
        }

        public void UpdateValue(float ratio)
            => _value.z = GetValue(ratio);

        public void TryChange(ref float value)
        {
            switch (Usage)
            {
                case Usage.None: return;
                case Usage.Set: value = Value; return;
                case Usage.Add: value += Value; return;
                case Usage.Remove: value -= Value; return;
                case Usage.Multiply: value *= Value; return;
                case Usage.Divide: value /= Value; return;
                default: return;
            }
        }

        public void TryChange(ref int value)
        {
            float floatRef = value;
            TryChange(ref floatRef);
            value = Mathf.RoundToInt(floatRef);
        }
    }

    [Serializable]
    public class Upgrade
    {
        public UpgradeValue Price = new();

        // [Header("Time")]
        public UpgradeValue time = new();

        // [Header("Movement")]
        public UpgradeValue movmentSpeed = new();

        // [Header("Dash")]
        public UpgradeValue dashForce = new();
        public UpgradeValue dashDuration = new();
        public UpgradeValue dashCooldown = new();

        // [Header("Jump")]
        public UpgradeValue jumpForce = new();

        public void UpdateValues(float ratio)
        {
            Price.UpdateValue(ratio);
            time.UpdateValue(ratio);
            movmentSpeed.UpdateValue(ratio);
            dashDuration.UpdateValue(ratio);
            dashCooldown.UpdateValue(ratio);
            jumpForce.UpdateValue(ratio);
        }

        public Upgrade Copy()
        {
            Upgrade u = new Upgrade();
            u.Price = Price;
            u.time = time;
            u.movmentSpeed = movmentSpeed;
            u.dashForce = dashForce;
            u.dashCooldown = dashCooldown;
            u.jumpForce = jumpForce;
            return u;
        }
    }
    
    [SerializeField] Vector2Int _count = Vector2Int.right;
    [SerializeField] Upgrade _upgrade = new();

    public int CountMax => _count.x;

    public Upgrade this[int index]
    {
        get
        {
            if (index < 0 || index >= _count.x) return null;
            
            Upgrade u = _upgrade.Copy();

            u.UpdateValues((float)index / (float)(_count.x));
            return u;
        }
    }

    private void OnValidate()
        => _upgrade.UpdateValues((float)_count.y / (float)_count.x);
}