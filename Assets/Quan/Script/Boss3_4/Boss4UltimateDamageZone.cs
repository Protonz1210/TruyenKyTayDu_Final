using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Boss4UltimateDamageZone : MonoBehaviour
{
    [Header("Damage")]
    [Tooltip("Sát thương ulti.")]
    public int damage = 180;

    [Tooltip("Mỗi target chỉ trúng một lần.")]
    public bool hitEachTargetOnlyOnce = true;

    [Tooltip("Xóa projectile sau khi gây sát thương.")]
    public bool destroyProjectileAfterHit = true;

    [Header("Detect")]
    [Tooltip("Gây sát thương Wukong.")]
    public bool damagePlayer = true;

    [Tooltip("Gây sát thương đoàn thỉnh kinh.")]
    public bool damageParty = true;

    [Header("Debug")]
    [Tooltip("Bật log debug.")]
    public bool enableDebugLog = true;

    private Transform ownerRoot;
    private Boss4UltimateProjectile projectileOwner;
    private Collider2D hitCollider;

    private HashSet<GameObject> hitTargets = new HashSet<GameObject>();
    private bool hasDestroyedProjectile;


void Awake()
    {
        hitCollider = GetComponent<Collider2D>();

        if (hitCollider != null)
        {
            hitCollider.isTrigger = true;
            hitCollider.enabled = true;
        }
    }

    public void Init(int newDamage, Transform newOwnerRoot, Boss4UltimateProjectile newProjectileOwner)
    {
        damage = newDamage;
        ownerRoot = newOwnerRoot;
        projectileOwner = newProjectileOwner;

        hitTargets.Clear();
        hasDestroyedProjectile = false;

        if (hitCollider == null)
        {
            hitCollider = GetComponent<Collider2D>();
        }

        if (hitCollider != null)
        {
            hitCollider.isTrigger = true;
            hitCollider.enabled = true;
        }

        if (enableDebugLog)
        {
            Debug.Log("Boss4UltimateDamageZone Init | Damage: " + damage);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other);
    }

    void TryDamage(Collider2D other)
    {
        if (hasDestroyedProjectile)
            return;

        if (other == null)
            return;

        if (ownerRoot != null && other.transform.root == ownerRoot)
            return;

        if (enableDebugLog)
        {
            Debug.Log(
                "Boss4 Ulti Hitbox chạm: " +
                other.name +
                " | Tag: " +
                other.tag +
                " | Root: " +
                other.transform.root.name
            );
        }

        if (damagePlayer)
        {
            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

            if (playerHealth != null)
            {
                GameObject targetKey = playerHealth.gameObject;

                if (hitEachTargetOnlyOnce && hitTargets.Contains(targetKey))
                    return;

                hitTargets.Add(targetKey);

                playerHealth.TakeDamage(damage);

                Debug.Log("Boss4 Ulti gây damage Wukong: -" + damage);

                DestroyProjectileAfterSuccessfulHit();

                return;
            }
        }

        if (damageParty)
        {
            PartyMemberHitReceiver partyMember =
                other.GetComponentInParent<PartyMemberHitReceiver>();

            if (partyMember != null)
            {
                GameObject targetKey = partyMember.gameObject;

                if (hitEachTargetOnlyOnce && hitTargets.Contains(targetKey))
                    return;

                hitTargets.Add(targetKey);

                partyMember.TakeDamage(damage);

                Debug.Log("Boss4 Ulti gây damage đoàn thỉnh kinh: -" + damage);

                DestroyProjectileAfterSuccessfulHit();

                return;
            }
        }
    }

    void DestroyProjectileAfterSuccessfulHit()
    {
        if (!destroyProjectileAfterHit)
            return;

        if (hasDestroyedProjectile)
            return;

        hasDestroyedProjectile = true;

        if (hitCollider != null)
        {
            hitCollider.enabled = false;
        }

        if (projectileOwner != null)
        {
            projectileOwner.DestroyProjectile();
        }
        else
        {
            Destroy(transform.root.gameObject);
        }
    }
}