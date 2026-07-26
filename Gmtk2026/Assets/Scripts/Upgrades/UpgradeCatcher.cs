using UnBocal.Events;
using UnityEngine;

public class UpgradeCatcher : MonoBehaviour
{
    private void Awake()
    {
        EventBus<SOUpgrade.Upgrade>.Connect(EventGame.Upgrade, ApplyUpgrade);
    }

    private void OnDestroy()
    {
        EventBus<SOUpgrade.Upgrade>.Disconnect(EventGame.Upgrade, ApplyUpgrade);
    }

    private void ApplyUpgrade(SOUpgrade.Upgrade u)
    {
        if (u == null) return;

        I_Upgradable[] upgradables = GetComponents<I_Upgradable>();

        if (upgradables == null) return;

        foreach (I_Upgradable current in upgradables)
            current.ApplyUpgrade(u);
    }
}

public interface I_Upgradable
{
    public void ApplyUpgrade(SOUpgrade.Upgrade u);
}