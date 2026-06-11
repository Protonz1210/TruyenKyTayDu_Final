using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance;

    [Header("Combat State")]
    public bool hasEnemyInCombat;

    [Header("Debug")]
    [SerializeField] private int enemyCountInScreen;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void EnemyEnterScreen()
    {
        enemyCountInScreen++;

        if (enemyCountInScreen < 0)
            enemyCountInScreen = 0;

        hasEnemyInCombat = enemyCountInScreen > 0;

        Debug.Log("Enemy vào màn hình | Count: " + enemyCountInScreen);
    }

    public void EnemyExitScreen()
    {
        enemyCountInScreen--;

        if (enemyCountInScreen < 0)
            enemyCountInScreen = 0;

        hasEnemyInCombat = enemyCountInScreen > 0;

        Debug.Log("Enemy rời màn hình | Count: " + enemyCountInScreen);
    }

    public void ClearAllEnemies()
    {
        enemyCountInScreen = 0;
        hasEnemyInCombat = false;

        Debug.Log("Clear All Enemies");
    }

    // Giữ lại để các script cũ không bị lỗi.
    public void EnemyEnterCombat()
    {
        EnemyEnterScreen();
    }

    // Giữ lại để các script cũ không bị lỗi.
    public void EnemyExitCombat()
    {
        EnemyExitScreen();
    }
}