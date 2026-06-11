using System.Collections.Generic;
using UnityEngine;

public class CombatScreenTrigger : MonoBehaviour
{
    [Header("Detect")]
    public string enemyTag = "Enemy";

    private HashSet<GameObject> enemiesInScreen = new HashSet<GameObject>();

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(enemyTag))
            return;

        GameObject enemyObject = other.gameObject;

        if (enemiesInScreen.Contains(enemyObject))
            return;

        enemiesInScreen.Add(enemyObject);

        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.EnemyEnterScreen();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(enemyTag))
            return;

        GameObject enemyObject = other.gameObject;

        if (!enemiesInScreen.Contains(enemyObject))
            return;

        enemiesInScreen.Remove(enemyObject);

        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.EnemyExitScreen();
        }
    }

    public void RemoveEnemy(GameObject enemyObject)
    {
        if (enemyObject == null)
            return;

        if (!enemiesInScreen.Contains(enemyObject))
            return;

        enemiesInScreen.Remove(enemyObject);

        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.EnemyExitScreen();
        }
    }
}