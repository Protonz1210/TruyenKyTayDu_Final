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

        hitEnemies.Clear();

        if (hitCollider != null)
        {
            hitCollider.enabled = true;
        }

        Debug.Log("Wukong mở hitbox | Attack Index: " + currentAttackIndex + " | Damage: " + currentDamage);
    }

    public void DeactivateHitbox()
    {
        isHitboxActive = false;

        if (hitCollider != null)
        {
            hitCollider.enabled = false;
        }

        hitEnemies.Clear();

        Debug.Log("Wukong đóng hitbox.");
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

        if (other == null)
            return;

        // Chặn tự đánh vào chính Wukong
        if (ownerRoot != null && other.transform.root == ownerRoot)
            return;

        // Ưu tiên riêng cho Boss4
        Boss4Controller boss4 = other.GetComponentInParent<Boss4Controller>();

        if (boss4 != null)
        {
            GameObject bossObject = boss4.gameObject;

            if (hitEnemies.Contains(bossObject))
                return;

            hitEnemies.Add(bossObject);

            boss4.TakeDamage(currentDamage);
            AddPassiveStackAfterSuccessfulHit();

            Debug.Log(
                "Wukong đánh trúng Boss4: " +
                bossObject.name +
                " | Damage: " +
                currentDamage +
                " | Attack Index: " +
                currentAttackIndex
            );

            return;
        }

        // Dùng cho Enemy4 và các enemy khác có Tag Enemy ở object cha
        Transform enemyTransform = FindTaggedParent(other.transform, enemyTag);

        if (enemyTransform == null)
            return;

        GameObject enemyObject = enemyTransform.gameObject;

        if (hitEnemies.Contains(enemyObject))
            return;

        hitEnemies.Add(enemyObject);

        // Gọi TakeDamage trên object có Tag Enemy
        enemyObject.SendMessage(
            "TakeDamage",
            currentDamage,
            SendMessageOptions.DontRequireReceiver
        );

        // Fallback: nếu TakeDamage nằm ở cha khác
        other.SendMessageUpwards(
            "TakeDamage",
            currentDamage,
            SendMessageOptions.DontRequireReceiver
        );

        AddPassiveStackAfterSuccessfulHit();

        Debug.Log(
            "Wukong đánh trúng Enemy: " +
            enemyObject.name +
            " | Collider: " +
            other.name +
            " | Damage: " +
            currentDamage +
            " | Attack Index: " +
            currentAttackIndex
        );
    }

    Transform FindTaggedParent(Transform startTransform, string tagName)
    {
        Transform current = startTransform;

        while (current != null)
        {
            if (current.CompareTag(tagName))
            {
                return current;
            }

            current = current.parent;
        }

        return null;
    }

    void AddPassiveStackAfterSuccessfulHit()
    {
        if (skillCooldown == null)
            return;

        if (currentAttackIndex == 0 ||
            currentAttackIndex == 1 ||
            currentAttackIndex == 2)
        {
            skillCooldown.AddPassiveStackFromHit();
        }
    }
}