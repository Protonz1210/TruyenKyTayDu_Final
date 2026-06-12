using System.Collections.Generic;
using UnityEngine;

public class WukongAttackHitbox : MonoBehaviour
{
    [Header("Owner")]
    [Tooltip("Gốc của nhân vật sở hữu hitbox.")]
    public Transform ownerRoot;

    [Tooltip("Script quản lý hồi chiêu và nội tại của Wukong.")]
    public WukongSkillCooldown skillCooldown;

    [Header("Detect")]
    [Tooltip("Tag của kẻ địch.")]
    public string enemyTag = "Enemy";

    [Header("Hitbox")]
    [Tooltip("Collider dùng làm vùng gây sát thương.")]
    public Collider2D hitCollider;

    private bool isActive;
    private int currentDamage;
    private int passiveGainAmount;
    private HashSet<GameObject> hitTargets = new HashSet<GameObject>();

    void Awake()
    {
        if (hitCollider == null)
        {
            hitCollider = GetComponent<Collider2D>();
        }

        if (hitCollider != null)
        {
            hitCollider.isTrigger = true;
            hitCollider.enabled = false;
        }

        if (ownerRoot == null)
        {
            ownerRoot = transform.root;
        }

        if (skillCooldown == null && ownerRoot != null)
        {
            skillCooldown = ownerRoot.GetComponent<WukongSkillCooldown>();
        }
    }

    public void ActivateHitbox(int damage)
    {
        ActivateHitbox(damage, 0);
    }

    public void ActivateHitbox(int damage, int passiveGain)
    {
        currentDamage = damage;
        passiveGainAmount = passiveGain;
        isActive = true;
        hitTargets.Clear();

        if (hitCollider != null)
        {
            hitCollider.enabled = true;
        }
    }

    public void ActivateHitbox(int damage, bool canGainPassive)
    {
        currentDamage = damage;
        passiveGainAmount = canGainPassive ? 1 : 0;
        isActive = true;
        hitTargets.Clear();

        if (hitCollider != null)
        {
            hitCollider.enabled = true;
        }
    }

    public void DeactivateHitbox()
    {
        isActive = false;
        currentDamage = 0;
        passiveGainAmount = 0;
        hitTargets.Clear();

        if (hitCollider != null)
        {
            hitCollider.enabled = false;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    void TryHit(Collider2D other)
    {
        if (!isActive) return;
        if (other == null) return;

        if (ownerRoot != null && other.transform.root == ownerRoot)
        {
            return;
        }

        GameObject targetRoot = other.transform.root.gameObject;

        if (hitTargets.Contains(targetRoot))
        {
            return;
        }

        bool hasHit = false;

        Enemy4Controller enemy4 = other.GetComponentInParent<Enemy4Controller>();

        if (enemy4 != null)
        {
            enemy4.TakeDamage(currentDamage);
            hasHit = true;
        }

        if (!hasHit)
        {
            Map4BossController map4Boss = other.GetComponentInParent<Map4BossController>();

            if (map4Boss != null)
            {
                map4Boss.TakeDamage(currentDamage);
                hasHit = true;
            }
        }

        if (!hasHit)
        {
            return;
        }

        hitTargets.Add(targetRoot);

        AddPassiveAfterHit();
    }

    void AddPassiveAfterHit()
    {
        if (skillCooldown == null) return;
        if (passiveGainAmount <= 0) return;

        skillCooldown.SendMessage("GainPassive", passiveGainAmount, SendMessageOptions.DontRequireReceiver);
        skillCooldown.SendMessage("AddPassive", passiveGainAmount, SendMessageOptions.DontRequireReceiver);
        skillCooldown.SendMessage("AddPassiveStack", passiveGainAmount, SendMessageOptions.DontRequireReceiver);
        skillCooldown.SendMessage("GainPassivePoint", passiveGainAmount, SendMessageOptions.DontRequireReceiver);
    }
}