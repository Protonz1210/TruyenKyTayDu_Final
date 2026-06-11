using System.Collections.Generic;
using UnityEngine;

public class WukongAttackHitbox : MonoBehaviour
{
    [Header("Owner")]
    public Transform ownerRoot;
    public WukongSkillCooldown skillCooldown;

    [Header("Detect")]
    public string enemyTag = "Enemy";

    [Header("Hitbox")]
    public Collider2D hitCollider;

    private int currentAttackIndex;
    private int currentDamage;
    private bool isHitboxActive;

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

    public void ActivateHitbox(int attackIndex, int damage)
    {
        currentAttackIndex = attackIndex;
        currentDamage = damage;
        isHitboxActive = true;

        // Mỗi lần bật hitbox là một nhịp đánh mới.
        // Enemy đã bị đánh ở nhịp trước được phép nhận damage lại.
        hitEnemies.Clear();

        if (hitCollider != null)
        {
            hitCollider.enabled = true;
        }
    }

    public void DeactivateHitbox()
    {
        isHitboxActive = false;

        if (hitCollider != null)
        {
            hitCollider.enabled = false;
        }

        hitEnemies.Clear();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryHitEnemy(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryHitEnemy(other);
    }

    void TryHitEnemy(Collider2D other)
    {
        if (!isHitboxActive)
            return;

        if (!other.CompareTag(enemyTag))
            return;

        GameObject enemyObject = other.transform.root.gameObject;

        // Trong cùng một lần bật hitbox, mỗi enemy chỉ nhận sát thương một lần.
        if (hitEnemies.Contains(enemyObject))
            return;

        Enemy4Controller enemy4 = other.GetComponentInParent<Enemy4Controller>();

        if (enemy4 != null)
        {
            enemy4.TakeDamage(currentDamage);

            hitEnemies.Add(enemyObject);

            AddPassiveStackAfterSuccessfulHit();

            Debug.Log(
                "Ngộ Không đánh trúng " +
                enemyObject.name +
                " | Damage: " +
                currentDamage +
                " | Attack Index: " +
                currentAttackIndex
            );

            return;
        }
    }

    void AddPassiveStackAfterSuccessfulHit()
    {
        if (skillCooldown == null)
            return;

        // Attack0, Attack1, Attack2 đánh trúng quái thì tích nội tại.
        // Attack3 là chiêu dùng nội tại, không tự tích lại nội tại.
        if (currentAttackIndex == 0 ||
            currentAttackIndex == 1 ||
            currentAttackIndex == 2)
        {
            skillCooldown.AddPassiveStackFromHit();
        }
    }
}