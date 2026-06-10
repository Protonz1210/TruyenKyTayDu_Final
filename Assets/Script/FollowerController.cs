using UnityEngine;

public class FollowerController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public PlayerFacing playerFacing;

    [Header("Follow Settings")]
    public float followDistance = 2f;
    public float moveSpeed = 3.5f;
    public float stopDistance = 0.15f;
    public float catchUpDistance = 5f;
    public float catchUpSpeedMultiplier = 1.5f;

    [Header("Position Settings")]
    public bool lockYPosition = true;

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private float startY;
    private bool isRunning;

    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        startY = transform.position.y;
    }

    void Update()
    {
        if (target == null)
            return;

        bool inCombat = CombatManager.Instance != null && CombatManager.Instance.hasEnemyInCombat;

        if (inCombat)
        {
            SetIdle();
            return;
        }

        FollowTarget();
    }

    void FollowTarget()
    {
        float behindDirection = -1f;

        if (playerFacing != null)
        {
            behindDirection = playerFacing.IsFacingRight ? -1f : 1f;
        }

        Vector3 desiredPosition = target.position;
        desiredPosition.x += behindDirection * followDistance;

        if (lockYPosition)
        {
            desiredPosition.y = startY;
        }

        float distanceX = Mathf.Abs(transform.position.x - desiredPosition.x);

        if (distanceX <= stopDistance)
        {
            SetIdle();
            return;
        }

        float currentMoveSpeed = moveSpeed;

        if (distanceX >= catchUpDistance)
        {
            currentMoveSpeed *= catchUpSpeedMultiplier;
        }

        Vector3 newPosition = transform.position;

        newPosition.x = Mathf.MoveTowards(
            transform.position.x,
            desiredPosition.x,
            currentMoveSpeed * Time.deltaTime
        );

        if (!lockYPosition)
        {
            newPosition.y = Mathf.MoveTowards(
                transform.position.y,
                desiredPosition.y,
                currentMoveSpeed * Time.deltaTime
            );
        }

        float moveDirection = desiredPosition.x - transform.position.x;

        transform.position = newPosition;

        FlipByDirection(moveDirection);
        SetRun();
    }

    void FlipByDirection(float direction)
    {
        if (Mathf.Abs(direction) < 0.01f)
            return;

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction < 0;
        }
        else
        {
            Vector3 scale = transform.localScale;

            if (direction > 0)
                scale.x = Mathf.Abs(scale.x);
            else
                scale.x = -Mathf.Abs(scale.x);

            transform.localScale = scale;
        }
    }

    void SetIdle()
    {
        if (!isRunning)
            return;

        isRunning = false;

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
        }
    }

    void SetRun()
    {
        if (isRunning)
            return;

        isRunning = true;

        if (animator != null)
        {
            animator.SetFloat("Speed", 1f);
        }
    }
}