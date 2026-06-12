using System;
using System.Collections.Generic;
using UnityEngine;

public class Boss2MeleeHitbox : MonoBehaviour
{
    [Header("Owner")]
    [Tooltip("Boss2 sở hữu hitbox này.")]
    public Boss2Controller owner;

    [Tooltip("Gốc của Boss2.")]
    public Transform ownerRoot;

    [Header("Hitbox Objects")]
    [Tooltip("Hitbox bên trái.")]
    public BoxCollider2D leftHitbox;

    [Tooltip("Hitbox bên phải.")]
    public BoxCollider2D rightHitbox;

    [Tooltip("Tự tìm LeftHitbox và RightHitbox.")]
    public bool autoFindHitboxes = true;

    [Header("Damage")]
    [Tooltip("Sát thương cận chiến.")]
    public int damage = 120;

    [Header("Detect")]
    [Tooltip("Gây sát thương Wukong.")]
    public bool damagePlayer = true;

    [Tooltip("Gây sát thương đoàn thỉnh kinh.")]
    public bool damageParty = true;

    [Header("Debug")]
    [Tooltip("Bật log debug.")]
    public bool enableDebugLog = true;

    [Tooltip("Luôn vẽ gizmo hitbox.")]
    public bool drawDebugGizmoAlways = true;

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
            owner = GetComponentInParent<Boss2Controller>();
        }

        if (ownerRoot == null)
        {
            if (owner != null)
            {
                ownerRoot = owner.transform.root;
            }
            else
            {
                ownerRoot = transform.root;
            }
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

        PrepareCollider(leftHitbox);
        PrepareCollider(rightHitbox);
        PrepareChildRelay(leftHitbox);
        PrepareChildRelay(rightHitbox);
    }

    void PrepareCollider(BoxCollider2D collider)
    {
        if (collider == null)
            return;

        collider.isTrigger = true;
    }

    void PrepareChildRelay(BoxCollider2D collider)
    {
        if (collider == null)
            return;

        Boss2MeleeHitboxChild child = collider.GetComponent<Boss2MeleeHitboxChild>();

        if (child == null)
        {
            child = collider.gameObject.AddComponent<Boss2MeleeHitboxChild>();
        }

        child.parentHitbox = this;
    }

    public void ActivateHitbox()
    {
        Transform attackTarget = GetCurrentAttackTarget();
        ActivateHitbox(attackTarget);
    }

    public void ActivateHitbox(Transform attackTarget)
    {
        Vector2 attackDirection = GetDirectionToTarget(attackTarget);
        ActivateHitbox(attackDirection);
    }

    public void ActivateHitbox(Vector2 attackDirection)
    {
        isActive = true;
        hitTargets.Clear();

        DisableBothHitboxes();

        if (attackDirection.x < 0f)
        {
            activeHitbox = leftHitbox;
        }
        else
        {
            activeHitbox = rightHitbox;
        }

        if (activeHitbox != null)
        {
            activeHitbox.enabled = true;
        }

        if (enableDebugLog)
        {
            Debug.Log(
                "Boss2 OPEN melee hitbox | Direction: " +
                attackDirection +
                " | Active Hitbox: " +
                (activeHitbox != null ? activeHitbox.name : "None")
            );
        }
    }

    public void DeactivateHitbox()
    {
        isActive = false;
        hitTargets.Clear();
        activeHitbox = null;

        DisableBothHitboxes();

        if (enableDebugLog)
        {
            Debug.Log("Boss2 CLOSE melee hitbox.");
        }
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

    Transform GetCurrentAttackTarget()
    {
        if (owner == null)
            return null;

        Transform lockedTarget = owner.GetLockedMeleeTarget();

        if (lockedTarget != null)
        {
            return lockedTarget;
        }

        if (owner.target != null)
        {
            return owner.target;
        }

        return null;
    }

    Vector2 GetDirectionToTarget(Transform attackTarget)
    {
        if (owner == null)
            return Vector2.right;

        if (attackTarget == null)
            return owner.GetBossFacingDirection();

        float directionX = attackTarget.position.x - owner.transform.position.x;

        if (Mathf.Abs(directionX) < 0.05f)
        {
            return owner.GetBossFacingDirection();
        }

        if (directionX < 0f)
        {
            return Vector2.left;
        }

        return Vector2.right;
    }

    public void ReceiveTrigger(Collider2D other)
    {
        TryHit(other);
    }

    public void ForceHitTarget(Transform forceTarget)
    {
        if (!isActive)
            return;

        if (forceTarget == null)
            return;

        if (owner != null && !owner.IsTargetStillInMeleeRange(forceTarget))
            return;

        PlayerHealth playerHealth = forceTarget.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            GameObject targetKey = playerHealth.gameObject;

            if (hitTargets.Contains(targetKey))
                return;

            hitTargets.Add(targetKey);
            playerHealth.TakeDamage(damage);

            if (enableDebugLog)
            {
                Debug.Log("Boss2 FORCE melee gây damage Wukong: -" + damage);
            }

            return;
        }

        PartyMemberHitReceiver partyMember = forceTarget.GetComponentInParent<PartyMemberHitReceiver>();

        if (partyMember != null)
        {
            GameObject targetKey = partyMember.gameObject;

            if (hitTargets.Contains(targetKey))
                return;

            hitTargets.Add(targetKey);
            partyMember.TakeDamage(damage);

            if (enableDebugLog)
            {
                Debug.Log("Boss2 FORCE melee gây damage đoàn thỉnh kinh: -" + damage);
            }

            return;
        }
    }

    void TryHit(Collider2D other)
    {
        if (!isActive)
            return;

        if (other == null)
            return;

        if (ownerRoot != null && other.transform.root == ownerRoot)
            return;

        if (damagePlayer)
        {
            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

            if (playerHealth != null)
            {
                GameObject targetKey = playerHealth.gameObject;

                if (hitTargets.Contains(targetKey))
                    return;

                hitTargets.Add(targetKey);
                playerHealth.TakeDamage(damage);

                if (enableDebugLog)
                {
                    Debug.Log("Boss2 melee gây damage Wukong: -" + damage);
                }

                return;
            }
        }

        if (damageParty)
        {
            PartyMemberHitReceiver partyMember = other.GetComponentInParent<PartyMemberHitReceiver>();

            if (partyMember != null)
            {
                GameObject targetKey = partyMember.gameObject;

                if (hitTargets.Contains(targetKey))
                    return;

                hitTargets.Add(targetKey);
                partyMember.TakeDamage(damage);

                if (enableDebugLog)
                {
                    Debug.Log("Boss2 melee gây damage đoàn thỉnh kinh: -" + damage);
                }

                return;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!drawDebugGizmoAlways)
            return;

        DrawHitboxGizmo(leftHitbox, Color.red);
        DrawHitboxGizmo(rightHitbox, Color.red);
        DrawHitboxGizmo(activeHitbox, Color.green);
    }

    void DrawHitboxGizmo(BoxCollider2D collider, Color color)
    {
        if (collider == null)
            return;

        Gizmos.color = color;

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = collider.transform.localToWorldMatrix;

        Vector3 center = new Vector3(collider.offset.x, collider.offset.y, 0f);
        Vector3 size = new Vector3(collider.size.x, collider.size.y, 0f);

        Gizmos.DrawWireCube(center, size);

        Gizmos.matrix = oldMatrix;
    }
}