using GMTK.Inputs;
using UnityEngine;
using UnBocal.Events;

public class PlayerItemHolder : ItemHolder
{
    [SerializeField] Item _baseItem;

    protected override void Awake()
    {
        base.Awake();
        InputManager.onAttack.AddListener(TryUseItem);
        InputManager.onDrop.AddListener(Drop);
        InputManager.onSwitch.AddListener(Switch);

        EventBus<GameState>.Connect(EventGame.OnStateChanged, OnStateChanged);
    }

    private void OnStateChanged(GameState state)
    {
        if (_item)
        {
            Destroy(_item.gameObject);
            _item = null;
        }

        if (_secondaryItem)
        {
            Destroy(_secondaryItem.gameObject);
            _secondaryItem = null;
        }

        if (!_baseItem) return;

        Equip(Instantiate(_baseItem));
    }
}