using UnityEngine;

public class FollowerController : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Mục tiêu follower đi theo.")]
    public Transform target;

    [Tooltip("Script hướng nhìn của Wukong.")]
    public PlayerFacing playerFacing;

    [Header("Follow Settings")]
    [Tooltip("Khoảng cách đứng sau Wukong.")]
    public float followDistance = 2f;

    [Tooltip("Tốc độ di chuyển.")]
    public float moveSpeed = 3.5f;

    [Tooltip("Khoảng cách dừng khi đến gần vị trí cần đứng.")]
    public float stopDistance = 0.2f;

    [Tooltip("Khoảng cách bắt đầu tăng tốc đuổi theo.")]
    public float catchUpDistance = 5f;

    [Tooltip("Hệ số tăng tốc khi bị tụt lại xa.")]
    public float catchUpSpeedMultiplier = 1.5f;

    [Header("Formation")]
    [Tooltip("Chỉ đổi vị trí đội hình khi Wukong di chuyển.")]
    public bool changeFormationSideOnlyWhenPlayerMoves = true;

    [Tooltip("Ngưỡng xác định Wukong đang đứng yên.")]
    public float playerMoveThreshold = 0.05f;

    [Tooltip("Đoàn đứng sau hướng nhìn ban đầu của Wukong.")]
    public bool startBehindPlayerFacing = true;

    [Header("Physics")]
    [Tooltip("Di chuyển bằng Rigidbody2D.")]
    public bool useRigidbodyMovement = true;

    [Tooltip("Khóa vị trí trục Y.")]
    public bool lockYPosition = false;

    [Header("Facing")]
    [Tooltip("Sprite gốc nhìn sang phải.")]
    public bool spriteFacesRightByDefault = true;

    [Tooltip("Khi đứng yên, follower quay theo hướng Wukong.")]
    public bool facePlayerWhenIdle = true;

    [Tooltip("Khi di chuyển, follower quay theo hướng chạy.")]
    public bool faceMoveDirectionWhenMoving = true;

    private Rigidbody2D rb;
    private Rigidbody2D targetRb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private float startY;
    private bool isRunning;

    // -1 = đứng bên trái Wukong
    // +1 = đứng bên phải Wukong
    private float formationSide = -1f;


void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (target != null)
        {
            targetRb = target.GetComponent<Rigidbody2D>();
        }

        startY = transform.position.y;
    }

    void Start()
    {
        InitFormationSide();
        FacePlayerDirection();
        SetIdle();
    }

    void FixedUpdate()
    {
        if (target == null)
            return;

        bool inCombat = CombatManager.Instance != null &&
                        CombatManager.Instance.hasEnemyInCombat;

        UpdateFormationSideByPlayerMovement();

        if (inCombat)
        {
            StopMove();
            FacePlayerDirection();
            SetIdle();
            return;
        }

        FollowTarget();
    }

    void InitFormationSide()
    {
        if (!startBehindPlayerFacing)
            return;

        if (playerFacing == null)
            return;

        // Wukong nhìn phải -> đoàn đứng bên trái.
        // Wukong nhìn trái -> đoàn đứng bên phải.
        formationSide = playerFacing.IsFacingRight ? -1f : 1f;
    }

    void UpdateFormationSideByPlayerMovement()
    {
        if (!changeFormationSideOnlyWhenPlayerMoves)
        {
            if (playerFacing != null)
            {
                formationSide = playerFacing.IsFacingRight ? -1f : 1f;
            }

            return;
        }

        float playerVelocityX = GetPlayerVelocityX();

        if (Mathf.Abs(playerVelocityX) < playerMoveThreshold)
            return;

        if (playerVelocityX > 0f)
        {
            // Wukong thật sự đi phải -> đoàn đứng bên trái.
            formationSide = -1f;
        }
        else if (playerVelocityX < 0f)
        {
            // Wukong thật sự đi trái -> đoàn đứng bên phải.
            formationSide = 1f;
        }
    }

    float GetPlayerVelocityX()
    {
        if (targetRb != null)
        {
            return targetRb.linearVelocity.x;
        }

        return 0f;
    }

    void FollowTarget()
    {
        Vector2 currentPosition = rb != null
            ? rb.position
            : (Vector2)transform.position;

        Vector2 desiredPosition = target.position;
        desiredPosition.x += formationSide * followDistance;

        if (lockYPosition)
        {
            desiredPosition.y = startY;
        }
        else
        {
            desiredPosition.y = currentPosition.y;
        }

        float distanceX = Mathf.Abs(currentPosition.x - desiredPosition.x);
        float moveDirection = desiredPosition.x - currentPosition.x;

        if (distanceX <= stopDistance)
        {
            StopMove();

            // Đã tới điểm rồi mới quay theo Wukong.
            FacePlayerDirection();

            SetIdle();
            return;
        }

        float currentMoveSpeed = moveSpeed;

        if (distanceX >= catchUpDistance)
        {
            currentMoveSpeed *= catchUpSpeedMultiplier;
        }

        float newX = Mathf.MoveTowards(
            currentPosition.x,
            desiredPosition.x,
            currentMoveSpeed * Time.fixedDeltaTime
        );

        Vector2 newPosition = new Vector2(
            newX,
            currentPosition.y
        );

        if (useRigidbodyMovement && rb != null)
        {
            rb.MovePosition(newPosition);
        }
        else
        {
            transform.position = newPosition;
        }

        // Đang chạy thì quay theo hướng chạy, không quay theo Wukong.
        if (faceMoveDirectionWhenMoving)
        {
            FaceMoveDirection(moveDirection);
        }

        SetRun();
    }

    void FaceMoveDirection(float direction)
    {
        if (Mathf.Abs(direction) < 0.01f)
            return;

        bool faceRight = direction > 0f;
        SetFacing(faceRight);
    }

    void FacePlayerDirection()
    {
        if (!facePlayerWhenIdle)
            return;

        if (playerFacing == null)
            return;

        SetFacing(playerFacing.IsFacingRight);
    }

    void SetFacing(bool faceRight)
    {
        if (spriteRenderer != null)
        {
            if (spriteFacesRightByDefault)
            {
                spriteRenderer.flipX = !faceRight;
            }
            else
            {
                spriteRenderer.flipX = faceRight;
            }
        }
        else
        {
            Vector3 scale = transform.localScale;

            if (faceRight)
                scale.x = Mathf.Abs(scale.x);
            else
                scale.x = -Mathf.Abs(scale.x);

            transform.localScale = scale;
        }
    }

    void StopMove()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
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