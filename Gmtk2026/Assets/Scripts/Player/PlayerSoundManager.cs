using UnityEngine;

public class PlayerSoundManager : MonoBehaviour
{
    [SerializeField] GameObject _walk;
    [SerializeField] GameObject _jump;
    [SerializeField] GameObject _land;
    [SerializeField] GameObject _dash;

    public void BridgePlayerWalk(bool isWalking)
    {
        _walk?.SetActive(isWalking);
    }

    public void BridgePlayerJump()
    {

        _jump.SetActive(false);
        _jump.SetActive(true);
    }

    public void BridgePlayerLand()
    {
        _land.SetActive(false);
        _land.SetActive(true);
    }

    public void BridgePlayerDash()
    {
        _dash.SetActive(false);
        _dash.SetActive(true);
    }

}
