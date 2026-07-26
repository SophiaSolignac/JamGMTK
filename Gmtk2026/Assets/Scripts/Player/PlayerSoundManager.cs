using UnityEngine;
using FMOD.Studio;
using System;

public class PlayerSoundManager : MonoBehaviour
{
    [Serializable]
    private class FMODSound
    {
        [SerializeField] FMODUnity.EventReference _reference;
        public EventInstance instance;

        public void Init()
        {
            instance = FMODUnity.RuntimeManager.CreateInstance(_reference);
        }
    }

    [SerializeField] FMODSound _walk;
    [SerializeField] FMODSound _jump;
    [SerializeField] FMODSound _land;
    [SerializeField] FMODSound _dash;

    private void Start()
    {
        _walk.Init();
        _jump.Init();
        _land.Init();
        _dash.Init();
    }

    
    public void BridgePlayerWalk(bool isWalking)
    {
        if (isWalking) _walk.instance.start();
        else _walk.instance.stop(STOP_MODE.ALLOWFADEOUT);
    }

    public void BridgePlayerJump()
        => _jump.instance.start();

    public void BridgePlayerLand()
        => _land.instance.start();

    public void BridgePlayerDash()
        => _dash.instance.start();
}
