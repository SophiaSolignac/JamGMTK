using GMTK.Inputs;

public class PlayerItemHolder : ItemHolder
{
    protected override void Awake()
    {
        base.Awake();
        InputManager.onAttack.AddListener(TryUseItem);
    }
}