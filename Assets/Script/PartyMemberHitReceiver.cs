using UnityEngine;

public class PartyMemberHitReceiver : MonoBehaviour
{
    [Header("Party Health")]
    public PartyHealth partyHealth;

    [Header("Hit Settings")]
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