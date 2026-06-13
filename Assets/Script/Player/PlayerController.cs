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

    [Tooltip("Tag của mặt đất.")]
    public string groundTag = "Ground";

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

    private Rigidbody2D rb;
    private Animator animator;
    private PlayerFacing playerFacing;
    private WukongSkillCooldown skillCooldown;

    private float moveInput;

    private bool isGrounded;
    private bool wasGrounded;
    private bool isAttacking;
    private bool isDead;
    private bool facingRight = true;

    private int jumpCount;

    private Coroutine attackLockCoroutine;
    private bool useTimedAttackLock;
    private float attackUnlockTime;

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
        moveAction.Enable();
        jumpAction.Enable();
        attack0Action.Enable();
        attack1Action.Enable();
        attack2Action.Enable();
        attack3Action.Enable();
        dieTestAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        attack0Action.Disable();
        attack1Action.Disable();
        attack2Action.Disable();
        attack3Action.Disable();
        dieTestAction.Disable();
    }

    void Update()
    {
        if (isDead)
        {
            UpdateAnimation();
            return;
        }

        CheckGroundByTag();
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
            return;

        if (isAttacking && lockMovementWhileAttacking)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        Move();
    }

    // ================= INPUT =================

    void ReadMoveInput()
    {
        moveInput = moveAction.ReadValue<float>();
    }

    // ================= MOVE =================

    void Move()
    {
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
            return;

        if (!jumpAction.WasPressedThisFrame())
            return;

        if (jumpCount >= maxJumpCount)
            return;

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
        isGrounded = false;
    }

    void ResetJumpWhenGrounded()
    {
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
            return;

        // Attack0: đánh thường, không hồi chiêu
        if (attack0Action.WasPressedThisFrame())
        {
            StartAttack("Attack0", 0f);
            return;
        }

        // Attack1: chiêu 1, có hồi chiêu
        if (attack1Action.WasPressedThisFrame())
        {
            if (skillCooldown != null && skillCooldown.TryUseSkill(1))
            {
                StartAttack("Attack1", skillCooldown.GetSkillActionDuration(1));
            }

            return;
        }

        // Attack2: chiêu 2, có hồi chiêu
        if (attack2Action.WasPressedThisFrame())
        {
            if (skillCooldown != null && skillCooldown.TryUseSkill(2))
            {
                StartAttack("Attack2", skillCooldown.GetSkillActionDuration(2));
            }

            return;
        }

        // Attack3: chiêu 3, dùng nội tại
        if (attack3Action.WasPressedThisFrame())
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
            return;

        isAttacking = true;

        if (lockMovementWhileAttacking)
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

    // Gắn hàm này bằng Animation Event ở frame cuối Attack0, Attack1, Attack2, Attack3
    public void EndAttack()
    {
        // Với chiêu có thời gian hành động cố định,
        // nếu Animation Event gọi sớm hơn actionDuration thì bỏ qua.
        if (useTimedAttackLock && Time.time < attackUnlockTime)
            return;

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

        attackHitbox.ActivateHitbox(attackIndex, damage);
    }

    int GetAttackDamage(int attackIndex)
    {
        if (attackIndex == 0)
            return attack0Damage;

        if (attackIndex == 1)
            return attack1Damage;

        if (attackIndex == 2)
            return attack2Damage;

        if (attackIndex == 3)
            return attack3Damage;

        return 0;
    }

    // ================= DIE =================

    void HandleTestDie()
    {
        if (!enableTestDieKey)
            return;

        if (dieTestAction.WasPressedThisFrame())
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead)
            return;

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

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;

        animator.SetBool("IsDead", true);
        animator.SetTrigger("Die");
    }

    // Gắn hàm này bằng Animation Event ở frame cuối animation Die
    public void HideAfterDie()
    {
        gameObject.SetActive(false);
    }

    public void DestroyAfterDie()
    {
        Destroy(gameObject);
    }

    public bool IsDead()
    {
        return isDead;
    }

    // ================= GROUND CHECK BY TAG =================

    void CheckGroundByTag()
    {
        isGrounded = false;

        if (groundCheck == null)
            return;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            groundCheck.position,
            groundCheckRadius
        );

        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag(groundTag))
            {
                isGrounded = true;
                break;
            }
        }
    }

    // ================= ANIMATION =================

    void UpdateAnimation()
    {
        animator.SetFloat("Speed", Mathf.Abs(moveInput));
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsAttacking", isAttacking);
        animator.SetBool("IsDead", isDead);
    }

    // ================= GIZMOS =================

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}