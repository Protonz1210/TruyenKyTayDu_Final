using System.Collections.Generic;
using UnityEngine;

public class Boss4MeleeHitbox : MonoBehaviour
{
    [Header("Owner")]
    [Tooltip("Boss sở hữu hitbox này.")]
    public Map4BossController owner;

    [Tooltip("Gốc của boss.")]
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
    public int damage = 80;

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
            owner = GetComponentInParent<Map4BossController>();
        }

        if (ownerRoot == null && owner != null)
        {
            ownerRoot = owner.transform;
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

    void PrepareCollider(BoxCollider2D boxCollider)
    {
        if (boxCollider == null) return;

        boxCollider.isTrigger = true;
    }

    void PrepareChildRelay(BoxCollider2D boxCollider)
    {
        if (boxCollider == null) return;

        Boss4MeleeHitboxChild relay = boxCollider.GetComponent<Boss4MeleeHitboxChild>();

        if (relay == null)
        {
            relay = boxCollider.gameObject.AddComponent<Boss4MeleeHitboxChild>();
        }

        relay.parentHitbox = this;
    }

    public void ActivateHitbox()
    {
        ActivateHitbox(GetCurrentAttackTarget());
    }

    public void ActivateHitbox(Transform attackTarget)
    {
        ActivateHitbox(GetDirectionToTarget(attackTarget));
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
            Debug.Log("Boss melee hitbox active: " + gameObject.name);
        }
    }

    public void DeactivateHitbox()
    {
        isActive = false;
        activeHitbox = null;
        hitTargets.Clear();

        DisableBothHitboxes();
    }

    public void OpenHitbox()
    {
        ActivateHitbox();
    }

    public void CloseHitbox()
    {
        DeactivateHitbox();
    }

    public void SetDamage(int newDamage)
    {
        damage = newDamage;
    }

    public void SetOwner(Transform newOwnerRoot)
    {
        ownerRoot = newOwnerRoot;

        if (ownerRoot != null && owner == null)
        {
            owner = ownerRoot.GetComponent<Map4BossController>();
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
        if (owner == null) return null;

        if (owner.GetLockedMeleeTarget() != null)
        {
            return owner.GetLockedMeleeTarget();
        }

        if (owner.currentCombatTarget != null)
        {
            return owner.currentCombatTarget;
        }

        return owner.target;
    }

    Vector2 GetDirectionToTarget(Transform attackTarget)
    {
        if (owner == null)
        {
            return Vector2.right;
        }

        if (attackTarget == null)
        {
            return owner.GetBossFacingDirection();
        }

        float directionX = attackTarget.position.x - owner.transform.position.x;

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

    void TryHit(Collider2D other)
    {
        if (!isActive) return;
        if (other == null) return;

        if (ownerRoot != null && other.transform.root == ownerRoot)
        {
            return;
        }

        GameObject rootObject = other.transform.root.gameObject;

        if (IsTargetAlreadyHit(rootObject))
        {
            return;
        }

        if (damagePlayer)
        {
            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

            if (playerHealth != null)
            {
                hitTargets.Add(rootObject);

                playerHealth.TakeDamage(damage);
                NotifyOwnerFirstMeleeHit();

                if (enableDebugLog)
                {
                    Debug.Log("Boss melee hit Wukong: " + damage);
                }

                return;
            }
        }

        if (damageParty)
        {
            PartyMemberHitReceiver partyReceiver = other.GetComponentInParent<PartyMemberHitReceiver>();

            if (partyReceiver != null)
            {
                hitTargets.Add(rootObject);

                partyReceiver.TakeDamage(damage);
                NotifyOwnerFirstMeleeHit();

                if (enableDebugLog)
                {
                    Debug.Log("Boss melee hit Party: " + damage);
                }

                return;
            }

            PartyHealth partyHealth = other.GetComponentInParent<PartyHealth>();

            if (partyHealth != null)
            {
                hitTargets.Add(rootObject);

                partyHealth.TakeDamage(damage);
                NotifyOwnerFirstMeleeHit();

                if (enableDebugLog)
                {
                    Debug.Log("Boss melee hit PartyHealth: " + damage);
                }

                return;
            }
        }
    }

    void NotifyOwnerFirstMeleeHit()
    {
        if (owner == null) return;

        owner.NotifyFirstMeleeHit();
    }

    bool IsTargetAlreadyHit(GameObject targetObject)
    {
        if (targetObject == null) return true;

        return hitTargets.Contains(targetObject);
    }

    void OnDrawGizmos()
    {
        if (!drawDebugGizmoAlways) return;

        DrawHitboxGizmo(leftHitbox, Color.red);
        DrawHitboxGizmo(rightHitbox, Color.red);
        DrawHitboxGizmo(activeHitbox, Color.green);
    }

    void DrawHitboxGizmo(BoxCollider2D boxCollider, Color color)
    {
        if (boxCollider == null) return;

        Gizmos.color = color;

        Vector3 worldCenter = boxCollider.transform.TransformPoint(boxCollider.offset);
        Vector3 worldSize = boxCollider.transform.TransformVector(boxCollider.size);

        Gizmos.DrawWireCube(worldCenter, new Vector3(Mathf.Abs(worldSize.x), Mathf.Abs(worldSize.y), 0f));
    }
}