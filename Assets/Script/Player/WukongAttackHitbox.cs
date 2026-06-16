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

        // Không tự đánh trúng chính Wukong.
        if (ownerRoot != null && other.transform.root == ownerRoot)
        {
            return;
        }

        // Thêm Boss1 vào danh sách xử lý damage.
        if (TryHitBoss1(other)) return;

        if (TryHitEnemy123(other)) return;
        if (TryHitBoss2(other)) return;
        if (TryHitEnemy4(other)) return;
        if (TryHitMap4Boss(other)) return;
    }

    bool TryHitBoss1(Collider2D other)
    {
        Boss1Controller boss1 = other.GetComponentInParent<Boss1Controller>();

        if (boss1 == null)
        {
            return false;
        }

        GameObject targetKey = boss1.gameObject;

        if (hitEnemies.Contains(targetKey))
        {
            return true;
        }

        hitEnemies.Add(targetKey);
        boss1.TakeDamage(currentDamage);

        AddPassiveAfterHit();

        Debug.Log("Wukong đánh trúng Boss1 - Mãng Xà Tinh: -" + currentDamage);

        return true;
    }

    bool TryHitEnemy123(Collider2D other)
    {
        Enemy123Controller enemy123 = other.GetComponentInParent<Enemy123Controller>();

        if (enemy123 == null)
        {
            return false;
        }

        GameObject targetKey = enemy123.gameObject;

        if (hitEnemies.Contains(targetKey))
        {
            return true;
        }

        hitEnemies.Add(targetKey);
        enemy123.TakeDamage(currentDamage);

        AddPassiveAfterHit();

        Debug.Log("Wukong đánh trúng Enemy123: -" + currentDamage);

        return true;
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

        Debug.Log("Wukong đánh trúng Boss2: -" + currentDamage);

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

        Debug.Log("Wukong đánh trúng Enemy4: -" + currentDamage);

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

        Debug.Log("Wukong đánh trúng Map4Boss: -" + currentDamage);

        return true;
    }

    void AddPassiveAfterHit()
    {
        if (skillCooldown == null) return;
        if (passiveGainAmount <= 0) return;

        skillCooldown.SendMessage("GainPassiveByHit", SendMessageOptions.DontRequireReceiver);
    }
}