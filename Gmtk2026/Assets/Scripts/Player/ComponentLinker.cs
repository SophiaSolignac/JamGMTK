using UnityEngine;

public class ComponentLinker : MonoBehaviour
{
    [SerializeField]
    private PlayerController playerController;
    [SerializeField]
    private PlayerInteractor interactor;
    [SerializeField]
    private TimerHealth timerHealth;
    [SerializeField]
    private PlayerHUD playerHUD;
    void Start()
    {
        
    }
}