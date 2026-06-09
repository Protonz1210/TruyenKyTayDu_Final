using UnityEngine;

public class EnemyDetector : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.EnemyEnterCombat();
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.EnemyExitCombat();
            }
        }
    }
}