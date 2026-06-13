using UnityEngine;

public class PartyMemberHitReceiver : MonoBehaviour
{
    [Header("Party Health")]
    [Tooltip("Máu chung của đoàn.")]
    public PartyHealth partyHealth;

    [Header("Hit Settings")]
    [Tooltip("Cho phép nhận sát thương.")]
    public bool canReceiveDamage = true;


public void TakeDamage(int damage)
    {
        if (!canReceiveDamage)
            return;

        if (damage <= 0)
            return;

        if (partyHealth == null)
        {
            Debug.LogWarning(gameObject.name + " chưa được gán PartyHealth.");
            return;
        }

        partyHealth.TakeDamage(damage);
    }
}