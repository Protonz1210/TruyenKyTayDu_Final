using System.Collections.Generic;
using UnityEngine;

public class Boss4UltimateDamageZone : MonoBehaviour
{
    [Header("Damage")]
    [Tooltip("Sát thương mỗi lần gây damage.")]
    public int damage = 100;

    [Tooltip("Khoảng thời gian giữa mỗi lần gây damage.")]
    public float damageInterval = 0.5f;

    [Tooltip("Gây damage ngay khi mục tiêu chạm vào vùng đánh.")]
    public bool damageImmediatelyOnEnter = true;

    [Header("Target Tags")]
    [Tooltip("Tag của Wukong.")]
    public string playerTag = "Player";

    [Tooltip("Tag của nhóm thỉnh kinh.")]
    public string partyTag = "Party";

    [Header("Owner")]
    [Tooltip("Boss sở hữu projectile này.")]
    public Transform owner;

    Dictionary<int, float> nextDamageTimes = new Dictionary<int, float>();

    public void Init(int newDamage, Transform newOwner)
    {
        damage = newDamage;
        owner = newOwner;
        nextDamageTimes.Clear();
    }

    public void SetDamage(int newDamage, Transform newOwner)
    {
        Init(newDamage, newOwner);
    }

    void OnEnable()
    {
        nextDamageTimes.Clear();
    }

    void OnDisable()
    {
        nextDamageTimes.Clear();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!damageImmediatelyOnEnter) return;

        TryDamageTarget(other, true);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryDamageTarget(other, false);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Transform targetRoot = GetTargetRoot(other);
        if (targetRoot == null) return;

        int targetId = targetRoot.gameObject.GetInstanceID();

        if (nextDamageTimes.ContainsKey(targetId))
        {
            nextDamageTimes.Remove(targetId);
        }
    }

    void TryDamageTarget(Collider2D other, bool forceDamage)
    {
        if (other == null) return;

        Transform targetRoot = GetTargetRoot(other);
        if (targetRoot == null) return;

        if (owner != null && targetRoot == owner) return;
        if (owner != null && targetRoot.IsChildOf(owner)) return;

        if (!IsValidTarget(targetRoot, other)) return;

        int targetId = targetRoot.gameObject.GetInstanceID();

        if (!forceDamage)
        {
            if (nextDamageTimes.ContainsKey(targetId))
            {
                if (Time.time < nextDamageTimes[targetId])
                {
                    return;
                }
            }
        }

        ApplyDamage(targetRoot, other);

        nextDamageTimes[targetId] = Time.time + damageInterval;
    }

    Transform GetTargetRoot(Collider2D other)
    {
        if (other.attachedRigidbody != null)
        {
            return other.attachedRigidbody.transform;
        }

        return other.transform.root;
    }

    bool IsValidTarget(Transform targetRoot, Collider2D other)
    {
        if (targetRoot.CompareTag(playerTag)) return true;
        if (targetRoot.CompareTag(partyTag)) return true;

        if (other.CompareTag(playerTag)) return true;
        if (other.CompareTag(partyTag)) return true;

        Transform current = other.transform;

        while (current != null)
        {
            if (current.CompareTag(playerTag)) return true;
            if (current.CompareTag(partyTag)) return true;

            if (current == targetRoot) break;

            current = current.parent;
        }

        return false;
    }

    void ApplyDamage(Transform targetRoot, Collider2D other)
    {
        targetRoot.gameObject.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        targetRoot.gameObject.SendMessage("ReceiveDamage", damage, SendMessageOptions.DontRequireReceiver);
        targetRoot.gameObject.SendMessage("ApplyDamage", damage, SendMessageOptions.DontRequireReceiver);

        if (other != null && other.gameObject != targetRoot.gameObject)
        {
            other.gameObject.SendMessageUpwards("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            other.gameObject.SendMessageUpwards("ReceiveDamage", damage, SendMessageOptions.DontRequireReceiver);
            other.gameObject.SendMessageUpwards("ApplyDamage", damage, SendMessageOptions.DontRequireReceiver);
        }
    }
}