using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance;

    [Header("Combat State")]
    public bool hasEnemyInCombat;

    [Header("Debug")]
    [SerializeField] private int enemyCount;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void EnemyEnterCombat()
    {
        enemyCount++;

        if (enemyCount < 0)
            enemyCount = 0;

        hasEnemyInCombat = enemyCount > 0;
    }

    public void EnemyExitCombat()
    {
        enemyCount--;

        if (enemyCount < 0)
            enemyCount = 0;

        hasEnemyInCombat = enemyCount > 0;
    }

    public void ClearAllEnemies()
    {
        enemyCount = 0;
        hasEnemyInCombat = false;
    }
}