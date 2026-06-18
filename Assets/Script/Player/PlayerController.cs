using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Input Actions")]
    [Tooltip("Input di chuyển trái/phải.")]
    public InputAction moveAction;

    [Tooltip("Input nhảy.")]
    public InputAction jumpAction;

    [Tooltip("Input đánh thường 1.")]
    public InputAction attack0Action;

    [Tooltip("Input đánh thường 2.")]
    public InputAction attack1Action;

    [Tooltip("Input đánh thường 3.")]
    public InputAction attack2Action;

    [Tooltip("Input đánh chiêu mạnh.")]
    public InputAction attack3Action;

    [Tooltip("Input test chết.")]
    public InputAction dieTestAction;

    [Header("Movement")]
    [Tooltip("Tốc độ di chuyển.")]
    public float moveSpeed = 5f;

    [Header("Jump")]
    [Tooltip("Lực nhảy lần đầu.")]
    public float jumpForce = 10f;

    [Tooltip("Lực nhảy lần hai.")]
    public float doubleJumpForce = 13f;

    [Tooltip("Số lần nhảy tối đa.")]
    public int maxJumpCount = 2;

    [Header("Ground Check By Tag")]
    [Tooltip("Điểm kiểm tra mặt đất.")]
    public Transform groundCheck;

    [Tooltip("Bán kính kiểm tra mặt đất.")]
    public float groundCheckRadius = 0.15f;

    [Tooltip("Tag của mặt đất thật. Chỉ tag này mới reset nhảy.")]
    public string groundTag = "Ground";

    [Header("Visual Stand Animation")]
    [Tooltip("Bật cái này để Wukong đứng trên collider không phải Ground vẫn hiện Idle/Run, nhưng không reset nhảy.")]
    public bool idleOnOtherSolidColliders = true;

    [Tooltip("Bỏ qua các collider dạng Trigger khi check đứng để tránh box trigger hội thoại/skill làm sai animation.")]
    public bool ignoreTriggerForVisualStand = true;

    [Header("Attack Settings")]
    [Tooltip("Khóa di chuyển khi đang đánh.")]
    public bool lockMovementWhileAttacking = true;

    [Header("Attack Damage")]
    [Tooltip("Sát thương đánh thường 1.")]
    public int attack0Damage = 50;

    [Tooltip("Sát thương đánh thường 2.")]
    public int attack1Damage = 120;

    [Tooltip("Sát thương đánh thường 3.")]
    public int attack2Damage = 180;

    [Tooltip("Sát thương chiêu mạnh.")]
    public int attack3Damage = 300;

    [Header("Attack Hitbox")]
    [Tooltip("Hitbox tấn công của Wukong.")]
    public WukongAttackHitbox attackHitbox;

    [Header("Test Die")]
    [Tooltip("Bật phím test chết.")]
    public bool enableTestDieKey = true;

    [Header("Death Notify - Auto Set By MapStory")]
    [Tooltip("Không chỉnh ở Inspector Wukong. MapStoryManager sẽ tự gán bằng SetDeathNotifyTarget().")]
    [HideInInspector]
    public GameObject deathNotifyObject;

    [Tooltip("Tên hàm sẽ gọi trên MapStoryManager khi Wukong chết xong animation và biến mất.")]
    [HideInInspector]
    public string deathNotifyMessageName = "NotifyWukongDeathFinished";

    private Rigidbody2D rb;
    private Animator animator;
    private PlayerFacing playerFacing;
    private WukongSkillCooldown skillCooldown;

    private float moveInput;

    // isGrounded: chỉ là Ground thật, dùng cho gameplay như reset jump.
    private bool isGrounded;

    // visualGrounded: dùng riêng cho Animator.
    // Nếu Wukong đứng trên Party/Boss/Enemy/Box có collider thường thì vẫn hiện Idle/Run.
    private bool visualGrounded;

    private bool wasGrounded;
    private bool isAttacking;
    private bool isDead;
    private bool facingRight = true;

    private int jumpCount;

    private Coroutine attackLockCoroutine;
    private bool useTimedAttackLock;
    private float attackUnlockTime;

    private bool hasNotifiedDeathFinished;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerFacing = GetComponent<PlayerFacing>();
        skillCooldown = GetComponent<WukongSkillCooldown>();

        if (playerFacing != null)
        {
            playerFacing.SetFacingRight(facingRight);
        }

        if (attackHitbox != null)
        {
            attackHitbox.DeactivateHitbox();
        }
    }

    void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.Enable();
        }

        if (jumpAction != null)
        {
            jumpAction.Enable();
        }

        if (attack0Action != null)
        {
            attack0Action.Enable();
        }

        if (attack1Action != null)
        {
            attack1Action.Enable();
        }

        if (attack2Action != null)
        {
            attack2Action.Enable();
        }

        if (attack3Action != null)
        {
            attack3Action.Enable();
        }

        if (dieTestAction != null)
        {
            dieTestAction.Enable();
        }
    }

    void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.Disable();
        }

        if (jumpAction != null)
        {
            jumpAction.Disable();
        }

        if (attack0Action != null)
        {
            attack0Action.Disable();
        }

        if (attack1Action != null)
        {
            attack1Action.Disable();
        }

        if (attack2Action != null)
        {
            attack2Action.Disable();
        }

        if (attack3Action != null)
        {
            attack3Action.Disable();
        }

        if (dieTestAction != null)
        {
            dieTestAction.Disable();
        }
    }

    void Update()
    {
        if (isDead)
        {
            UpdateAnimation();
            return;
        }

        CheckGroundAndVisualStand();
        ResetJumpWhenGrounded();

        ReadMoveInput();
        HandleJump();
        HandleAttack();
        HandleTestDie();

        UpdateAnimation();
    }

    void FixedUpdate()
    {
        if (isDead)
        {
            return;
        }

        if (isAttacking && lockMovementWhileAttacking)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        Move();
    }

    // ================= DEATH NOTIFY TARGET =================

    /// <summary>
    /// MapStoryManager sẽ gọi hàm này khi vào map.
    /// Không cần kéo MapStory vào Inspector của Wukong.
    /// </summary>
    public void SetDeathNotifyTarget(GameObject target)
    {
        deathNotifyObject = target;
    }

    /// <summary>
    /// Nếu map nào muốn đổi tên hàm notify thì có thể gọi hàm này.
    /// Bình thường giữ mặc định: NotifyWukongDeathFinished.
    /// </summary>
    public void SetDeathNotifyMessageName(string messageName)
    {
        if (string.IsNullOrEmpty(messageName))
        {
            return;
        }

        deathNotifyMessageName = messageName;
    }

    private void NotifyDeathFinishedToMapStory()
    {
        if (hasNotifiedDeathFinished)
        {
            return;
        }

        hasNotifiedDeathFinished = true;

        if (deathNotifyObject == null)
        {
            Debug.LogWarning("PlayerController: Wukong chết xong nhưng chưa có Death Notify Object. MapStoryManager chưa gán target.");
            return;
        }

        if (string.IsNullOrEmpty(deathNotifyMessageName))
        {
            Debug.LogWarning("PlayerController: Death Notify Message Name đang trống.");
            return;
        }

        deathNotifyObject.SendMessage(
            deathNotifyMessageName,
            SendMessageOptions.DontRequireReceiver
        );

        Debug.Log("PlayerController: Đã báo MapStory rằng Wukong chết xong animation và đã biến mất.");
    }

    // ================= INPUT =================

    void ReadMoveInput()
    {
        if (moveAction == null)
        {
            moveInput = 0f;
            return;
        }

        moveInput = moveAction.ReadValue<float>();
    }

    // ================= MOVE =================

    void Move()
    {
        if (rb == null)
        {
            return;
        }

        rb.linearVelocity = new Vector2(
            moveInput * moveSpeed,
            rb.linearVelocity.y
        );

        if (moveInput > 0 && !facingRight)
        {
            Flip();
        }
        else if (moveInput < 0 && facingRight)
        {
            Flip();
        }
    }

    void Flip()
    {
        facingRight = !facingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;

        if (playerFacing != null)
        {
            playerFacing.SetFacingRight(facingRight);
        }
    }

    // ================= JUMP / DOUBLE JUMP =================

    void HandleJump()
    {
        if (isAttacking)
        {
            return;
        }

        if (jumpAction == null)
        {
            return;
        }

        if (!jumpAction.WasPressedThisFrame())
        {
            return;
        }

        if (jumpCount >= maxJumpCount)
        {
            return;
        }

        if (rb == null)
        {
            return;
        }

        float currentJumpForce;

        if (jumpCount == 0)
        {
            currentJumpForce = jumpForce;
        }
        else
        {
            currentJumpForce = doubleJumpForce;
        }

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            currentJumpForce
        );

        jumpCount++;

        // Sau khi nhảy, Ground thật và Visual Ground đều tắt tạm thời.
        isGrounded = false;
        visualGrounded = false;
    }

    void ResetJumpWhenGrounded()
    {
        // Chỉ Ground thật mới được reset nhảy.
        // Đứng trên Party/Boss/Enemy/Box không reset jump.
        if (isGrounded && !wasGrounded)
        {
            jumpCount = 0;
        }

        wasGrounded = isGrounded;
    }

    // ================= ATTACK =================

    void HandleAttack()
    {
        if (isAttacking)
        {
            return;
        }

        if (attack0Action != null && attack0Action.WasPressedThisFrame())
        {
            StartAttack("Attack0", 0f);
            return;
        }

        if (attack1Action != null && attack1Action.WasPressedThisFrame())
        {
            if (skillCooldown != null && skillCooldown.TryUseSkill(1))
            {
                StartAttack("Attack1", skillCooldown.GetSkillActionDuration(1));
            }

            return;
        }

        if (attack2Action != null && attack2Action.WasPressedThisFrame())
        {
            if (skillCooldown != null && skillCooldown.TryUseSkill(2))
            {
                StartAttack("Attack2", skillCooldown.GetSkillActionDuration(2));
            }

            return;
        }

        if (attack3Action != null && attack3Action.WasPressedThisFrame())
        {
            if (skillCooldown != null && skillCooldown.TryUseSkill(3))
            {
                StartAttack("Attack3", skillCooldown.GetSkillActionDuration(3));
            }

            return;
        }
    }

    void StartAttack(string attackTriggerName, float actionDuration)
    {
        if (isDead)
        {
            return;
        }

        if (animator == null)
        {
            return;
        }

        isAttacking = true;

        if (lockMovementWhileAttacking && rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        animator.SetTrigger(attackTriggerName);

        if (attackLockCoroutine != null)
        {
            StopCoroutine(attackLockCoroutine);
            attackLockCoroutine = null;
        }

        if (actionDuration > 0f)
        {
            useTimedAttackLock = true;
            attackUnlockTime = Time.time + actionDuration;
            attackLockCoroutine = StartCoroutine(EndAttackAfterDuration(actionDuration));
        }
        else
        {
            useTimedAttackLock = false;
        }
    }

    IEnumerator EndAttackAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);

        ForceEndAttack();
    }

    // Gắn hàm này bằng Animation Event ở frame cuối Attack0, Attack1, Attack2, Attack3.
    public void EndAttack()
    {
        // Với chiêu có thời gian hành động cố định,
        // nếu Animation Event gọi sớm hơn actionDuration thì bỏ qua.
        if (useTimedAttackLock && Time.time < attackUnlockTime)
        {
            return;
        }

        ForceEndAttack();
    }

    void ForceEndAttack()
    {
        isAttacking = false;
        useTimedAttackLock = false;

        CloseAttackHitbox();

        if (attackLockCoroutine != null)
        {
            StopCoroutine(attackLockCoroutine);
            attackLockCoroutine = null;
        }
    }

    // ================= ATTACK HITBOX EVENTS =================
    // Các hàm này dùng cho Animation Event.

    public void OpenAttack0Hitbox()
    {
        OpenAttackHitbox(0);
    }

    public void OpenAttack1Hitbox()
    {
        OpenAttackHitbox(1);
    }

    public void OpenAttack2Hitbox()
    {
        OpenAttackHitbox(2);
    }

    public void OpenAttack3Hitbox()
    {
        OpenAttackHitbox(3);
    }

    public void CloseAttackHitbox()
    {
        if (attackHitbox != null)
        {
            attackHitbox.DeactivateHitbox();
        }
    }

    void OpenAttackHitbox(int attackIndex)
    {
        if (attackHitbox == null)
        {
            Debug.LogWarning("PlayerController chưa được gán Attack Hitbox.");
            return;
        }

        int damage = GetAttackDamage(attackIndex);

        int passiveGain = 0;

        if (attackIndex == 0 || attackIndex == 1 || attackIndex == 2)
        {
            passiveGain = 1;
        }

        if (attackIndex == 3)
        {
            passiveGain = 0;
        }

        attackHitbox.ActivateHitbox(damage, passiveGain);
    }

    int GetAttackDamage(int attackIndex)
    {
        if (attackIndex == 0)
        {
            return attack0Damage;
        }

        if (attackIndex == 1)
        {
            return attack1Damage;
        }

        if (attackIndex == 2)
        {
            return attack2Damage;
        }

        if (attackIndex == 3)
        {
            return attack3Damage;
        }

        return 0;
    }

    // ================= DIE =================

    void HandleTestDie()
    {
        if (!enableTestDieKey)
        {
            return;
        }

        if (dieTestAction == null)
        {
            return;
        }

        if (dieTestAction.WasPressedThisFrame())
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        isAttacking = false;
        useTimedAttackLock = false;
        moveInput = 0f;

        CloseAttackHitbox();

        if (attackLockCoroutine != null)
        {
            StopCoroutine(attackLockCoroutine);
            attackLockCoroutine = null;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }

        if (animator != null)
        {
            animator.SetBool("IsDead", true);
            animator.SetTrigger("Die");
        }
        else
        {
            // Nếu không có Animator thì vẫn báo chết xong để tránh kẹt GameOver.
            NotifyDeathFinishedToMapStory();
            gameObject.SetActive(false);
        }
    }

    // Gắn hàm này bằng Animation Event ở frame cuối animation Die.
    // Đây là điểm chuẩn để báo MapStory bật GameOver:
    // Die animation đã chạy xong -> Wukong biến mất -> báo MapStory.
    public void HideAfterDie()
    {
        gameObject.SetActive(false);

        NotifyDeathFinishedToMapStory();
    }

    // Nếu map nào dùng Destroy thay vì Hide thì vẫn báo MapStory trước khi Destroy.
    public void DestroyAfterDie()
    {
        NotifyDeathFinishedToMapStory();

        Destroy(gameObject);
    }

    public bool IsDead()
    {
        return isDead;
    }

    // ================= GROUND / VISUAL STAND CHECK =================

    void CheckGroundAndVisualStand()
    {
        isGrounded = false;
        visualGrounded = false;

        if (groundCheck == null)
        {
            return;
        }

        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            groundCheck.position,
            groundCheckRadius
        );

        foreach (Collider2D col in colliders)
        {
            if (col == null)
            {
                continue;
            }

            // Bỏ qua collider của chính Wukong.
            if (col.transform == transform || col.transform.IsChildOf(transform))
            {
                continue;
            }

            // Ground thật: chỉ nhận đúng tag Ground.
            if (col.CompareTag(groundTag))
            {
                isGrounded = true;
                visualGrounded = true;
                continue;
            }

            // Các collider khác không phải Ground:
            // Không tính là Ground thật, không reset jump.
            // Nhưng vẫn cho Animator hiểu là đang đứng để về Idle/Run.
            if (idleOnOtherSolidColliders)
            {
                if (ignoreTriggerForVisualStand && col.isTrigger)
                {
                    continue;
                }

                visualGrounded = true;
            }
        }
    }

    // ================= ANIMATION =================

    void UpdateAnimation()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetFloat("Speed", Mathf.Abs(moveInput));

        // Animator dùng visualGrounded.
        // Nghĩa là đứng trên Ground thật hoặc đứng trên collider thường khác đều về Idle/Run.
        animator.SetBool("IsGrounded", visualGrounded);

        animator.SetBool("IsAttacking", isAttacking);
        animator.SetBool("IsDead", isDead);
    }

    // ================= GIZMOS =================

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}