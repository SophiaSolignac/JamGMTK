using UnityEngine;
using UnityEngine.Events;

public class RessourceSystem : MonoBehaviour
{
    private int currentCoins = 100;
    public UnityEvent<int> OnCoinsChanged = new UnityEvent<int>();

    public int CurrentCoins 
    { 
        get => currentCoins; 
        set 
        {
            currentCoins = Mathf.Max(0, value); // Ensure coins don't go below 0
            OnCoinsChanged.Invoke(currentCoins); // Notify listeners of the change
        }
    }
    
    public void AddCoins(int amount)
    {
        CurrentCoins += amount;
        Debug.Log($"Added {amount} coins. Current coins: {CurrentCoins}");
    }

    public bool TrySpendCoins(int amount)
    {
        if (CurrentCoins < amount)
        {
            Debug.LogWarning($"Not enough coins to spend. Current coins: {CurrentCoins}, attempted to spend: {amount}");
            return false;
        }
        CurrentCoins -= amount;
        Debug.Log($"Spent {amount} coins. Current coins: {CurrentCoins}");
        return true;
    }

}
