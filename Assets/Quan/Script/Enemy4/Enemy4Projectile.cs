using UnityEngine;

public class Enemy4Projectile : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 7f;
    public float lifeTime = 3f;

    [Header("Damage")]
    public int damage = 100;
    public bool destroyOnHit = true;

    [Header("Target Tags")]
    public string playerTag = "Player";
    public string partyTag = "Party";

    [Header("Visual")]
    public Transform visualRoot;
    public bool flipVisualByDirection = true;

    [Header("Debug")]
    public bool enableDebugLog = false;

    Rigidbody2D rb;
    Vector2 moveDirection = Vector2.right;
    Transform ownerRoot;
    bool hasHit;
    bool initialized;

    public void Init(Vector2 direction, int newDamage, float newSpeed, float newLifeTime, Transform owner)
    {
        moveDirection = direction;

        if (moveDirection.sqrMagnitude <= 0.01f)
        {
            moveDirection = Vector2.right;
        }

        moveDirection.Normalize();

        damage = newDamage;
        moveSpeed = newSpeed;
        lifeTime = newLifeTime;
        ownerRoot = owner;

        initialized = true;

        SetupReferences();
        ApplyVisualDirection();

        Destroy(gameObject, lifeTime);
    }

    void Awake()
    {
        SetupReferences();
    }

    void Start()
    {
        if (!initialized)
        {
            ApplyVisualDirection();
            Destroy(gameObject, lifeTime);
        }
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            rb.linearVelocity = moveDirection * moveSpeed;
        }
        else
        {
            transform.position += (Vector3)(moveDirection * moveSpeed * Time.fixedDeltaTime);
        }
    }

    void SetupReferences()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (visualRoot == null)
        {
            Transform visual = transform.Find("Visual");

            if (visual != null)
            {
                visualRoot = visual;
            }
        }
    }

    void ApplyVisualDirection()
    {
        if (!flipVisualByDirection) return;
        if (visualRoot == null) return;

        Vector3 scale = visualRoot.localScale;
        scale.x = Mathf.Abs(scale.x);

        if (moveDirection.x < 0f)
        {
            scale.x *= -1f;
        }

        visualRoot.localScale = scale;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    void TryHit(Collider2D other)
    {
        if (hasHit) return;
        if (other == null) return;

        if (ownerRoot != null && other.transform.root == ownerRoot)
        {
            return;
        }

        bool hitSomething = false;

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            hitSomething = true;

            if (enableDebugLog)
            {
                Debug.Log("Enemy4 projectile hit Wukong: " + damage);
            }
        }

        if (!hitSomething)
        {
            PartyMemberHitReceiver partyReceiver = other.GetComponentInParent<PartyMemberHitReceiver>();

            if (partyReceiver != null)
            {
                partyReceiver.TakeDamage(damage);
                hitSomething = true;

                if (enableDebugLog)
                {
                    Debug.Log("Enemy4 projectile hit PartyMemberHitReceiver: " + damage);
                }
            }
        }

        if (!hitSomething)
        {
            PartyHealth partyHealth = other.GetComponentInParent<PartyHealth>();

            if (partyHealth != null)
            {
                partyHealth.TakeDamage(damage);
                hitSomething = true;

                if (enableDebugLog)
                {
                    Debug.Log("Enemy4 projectile hit PartyHealth: " + damage);
                }
            }
        }

        if (hitSomething && destroyOnHit)
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }
}