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
    private HashSet<GameObject> hitEnemies = new HashSet<GameObject>();

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
        hitEnemies.Clear();

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
        hitEnemies.Clear();

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
        hitEnemies.Clear();

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

        if (TryHitBoss2(other)) return;
        if (TryHitEnemy4(other)) return;
        if (TryHitMap4Boss(other)) return;
    }

    bool TryHitBoss2(Collider2D other)
    {
        Boss2Controller boss2 = other.GetComponentInParent<Boss2Controller>();

        if (boss2 == null)
        {
            return false;
        }

        GameObject targetKey = boss2.gameObject;

        if (hitEnemies.Contains(targetKey))
        {
            return true;
        }

        hitEnemies.Add(targetKey);
        boss2.TakeDamage(currentDamage);

        AddPassiveAfterHit();

        return true;
    }

    bool TryHitEnemy4(Collider2D other)
    {
        Enemy4Controller enemy4 = other.GetComponentInParent<Enemy4Controller>();

        if (enemy4 == null)
        {
            return false;
        }

        GameObject targetKey = enemy4.gameObject;

        if (hitEnemies.Contains(targetKey))
        {
            return true;
        }

        hitEnemies.Add(targetKey);
        enemy4.TakeDamage(currentDamage);

        AddPassiveAfterHit();

        return true;
    }

    bool TryHitMap4Boss(Collider2D other)
    {
        Map4BossController map4Boss = other.GetComponentInParent<Map4BossController>();

        if (map4Boss == null)
        {
            return false;
        }

        GameObject targetKey = map4Boss.gameObject;

        if (hitEnemies.Contains(targetKey))
        {
            return true;
        }

        hitEnemies.Add(targetKey);
        map4Boss.TakeDamage(currentDamage);

        AddPassiveAfterHit();

        return true;
    }

    void AddPassiveAfterHit()
    {
        if (skillCooldown == null) return;
        if (passiveGainAmount <= 0) return;

        skillCooldown.SendMessage("GainPassiveByHit", SendMessageOptions.DontRequireReceiver);
    }
}