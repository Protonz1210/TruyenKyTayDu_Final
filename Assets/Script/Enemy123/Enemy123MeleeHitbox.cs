using System.Collections.Generic;
using UnityEngine;

public class Enemy123MeleeHitbox : MonoBehaviour
{
    [Header("Owner")]
    [Tooltip("Enemy123 sở hữu hitbox này.")]
    public Enemy123Controller owner;

    [Tooltip("Gốc của Enemy123.")]
    public Transform ownerRoot;

    [Header("Hitbox Objects")]
    [Tooltip("Hitbox bên trái.")]
    public BoxCollider2D leftHitbox;

    [Tooltip("Hitbox bên phải.")]
    public BoxCollider2D rightHitbox;

    [Tooltip("Tự tìm hitbox.")]
    public bool autoFindHitboxes = true;

    [Header("Damage")]
    [Tooltip("Sát thương.")]
    public int damage = 50;

    [Header("Detect")]
    [Tooltip("Gây sát thương Wukong.")]
    public bool damagePlayer = true;

    [Tooltip("Gây sát thương đoàn.")]
    public bool damageParty = true;

    private bool isActive;
    private BoxCollider2D activeHitbox;
    private HashSet<GameObject> hitTargets = new HashSet<GameObject>();

    void Awake()
    {
        SetupReferences();
        DeactivateHitbox();
    }

    void Reset()
    {
        SetupReferences();
    }

    void OnValidate()
    {
        SetupReferences();
    }

    void SetupReferences()
    {
        if (owner == null)
        {
            owner = GetComponentInParent<Enemy123Controller>();
        }

        if (ownerRoot == null)
        {
            ownerRoot = owner != null ? owner.transform.root : transform.root;
        }

        if (autoFindHitboxes)
        {
            if (leftHitbox == null)
            {
                Transform left = transform.Find("LeftHitbox");

                if (left != null)
                {
                    leftHitbox = left.GetComponent<BoxCollider2D>();
                }
            }

            if (rightHitbox == null)
            {
                Transform right = transform.Find("RightHitbox");

                if (right != null)
                {
                    rightHitbox = right.GetComponent<BoxCollider2D>();
                }
            }
        }

        PrepareHitbox(leftHitbox);
        PrepareHitbox(rightHitbox);
    }

    void PrepareHitbox(BoxCollider2D box)
    {
        if (box == null) return;

        box.isTrigger = true;

        Enemy123MeleeHitboxChild child = box.GetComponent<Enemy123MeleeHitboxChild>();

        if (child == null)
        {
            child = box.gameObject.AddComponent<Enemy123MeleeHitboxChild>();
        }

        child.parentHitbox = this;
    }

    public void ActivateHitbox(Transform attackTarget)
    {
        isActive = true;
        hitTargets.Clear();

        DisableBothHitboxes();

        Vector2 direction = GetDirectionToTarget(attackTarget);
        activeHitbox = direction.x < 0f ? leftHitbox : rightHitbox;

        if (activeHitbox != null)
        {
            activeHitbox.enabled = true;
        }
    }

    public void DeactivateHitbox()
    {
        isActive = false;
        activeHitbox = null;
        hitTargets.Clear();

        DisableBothHitboxes();
    }

    void DisableBothHitboxes()
    {
        if (leftHitbox != null)
        {
            leftHitbox.enabled = false;
        }

        if (rightHitbox != null)
        {
            rightHitbox.enabled = false;
        }
    }

    Vector2 GetDirectionToTarget(Transform attackTarget)
    {
        if (owner == null) return Vector2.right;

        if (attackTarget == null)
        {
            return owner.GetEnemyFacingDirection();
        }

        float directionX = attackTarget.position.x - owner.transform.position.x;

        if (directionX < 0f)
        {
            return Vector2.left;
        }

        return Vector2.right;
    }

    public void ForceHitTarget(Transform forceTarget)
    {
        if (!isActive) return;
        if (forceTarget == null) return;

        if (owner != null && !owner.IsTargetStillInMeleeRange(forceTarget))
        {
            return;
        }

        PlayerHealth playerHealth = forceTarget.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null && damagePlayer)
        {
            HitPlayer(playerHealth);
            return;
        }

        PartyMemberHitReceiver partyMember = forceTarget.GetComponentInParent<PartyMemberHitReceiver>();

        if (partyMember != null && damageParty)
        {
            HitParty(partyMember);
            return;
        }
    }

    public void ReceiveTrigger(Collider2D other)
    {
        if (!isActive) return;
        if (other == null) return;

        if (ownerRoot != null && other.transform.root == ownerRoot)
        {
            return;
        }

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null && damagePlayer)
        {
            HitPlayer(playerHealth);
            return;
        }

        PartyMemberHitReceiver partyMember = other.GetComponentInParent<PartyMemberHitReceiver>();

        if (partyMember != null && damageParty)
        {
            HitParty(partyMember);
            return;
        }
    }

    void HitPlayer(PlayerHealth playerHealth)
    {
        GameObject key = playerHealth.gameObject;

        if (hitTargets.Contains(key)) return;

        hitTargets.Add(key);
        playerHealth.TakeDamage(damage);
    }

    void HitParty(PartyMemberHitReceiver partyMember)
    {
        GameObject key = partyMember.gameObject;

        if (hitTargets.Contains(key)) return;

        hitTargets.Add(key);
        partyMember.TakeDamage(damage);
    }
}