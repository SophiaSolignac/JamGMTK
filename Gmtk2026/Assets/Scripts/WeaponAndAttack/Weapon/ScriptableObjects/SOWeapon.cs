using UnityEngine;

public class SOWeapon : ScriptableObject
{
    // -------~~~~~~~~~~================# // Settings
    [Header("Settings")]
    [SerializeField] protected string _title = "new weapon";
    [SerializeField] protected float _reloadDuration = 1f;
    [SerializeField] protected float _waitBetweenInput = 0f;
    [SerializeField] protected Item.InputType _input;

    // Getters
    public string Title => _title;
    public Item.InputType Input => _input;
    public float ReloadDuration => _reloadDuration;
    public float WaitBetweenInput => _waitBetweenInput;
}