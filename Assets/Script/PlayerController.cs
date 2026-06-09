using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Input Actions")]
    public InputAction moveAction;
    public InputAction jumpAction;
    public InputAction attack0Action;
    public InputAction attack1Action;
    public InputAction attack2Action;
    public InputAction attack3Action;
    public InputAction dieTestAction;

    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Jump")]
    public float jumpForce = 10f;
    public float doubleJumpForce = 13f;
    public int maxJumpCount = 2;

    [Header("Ground Check By Tag")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public string groundTag = "Ground";

    [Header("Attack Settings")]
    public bool lockMovementWhileAttacking = true;

    [Header("Test Die")]
    public bool enableTestDieKey = true;

    private Rigidbody2D rb;
    private Animator animator;
    private PlayerFacing playerFacing;

    private float moveInput;

    private bool isGrounded;
    private bool wasGrounded;
    private bool isAttacking;
    private bool isDead;
    private bool facingRight = true;

    private int jumpCount;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerFacing = GetComponent<PlayerFacing>();

        if (playerFacing != null)
        {
            playerFacing.SetFacingRight(facingRight);
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
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

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

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, currentJumpForce);

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

        if (attack0Action.WasPressedThisFrame())
        {
            StartAttack("Attack0");
        }
        else if (attack1Action.WasPressedThisFrame())
        {
            StartAttack("Attack1");
        }
        else if (attack2Action.WasPressedThisFrame())
        {
            StartAttack("Attack2");
        }
        else if (attack3Action.WasPressedThisFrame())
        {
            StartAttack("Attack3");
        }
    }

    void StartAttack(string attackTriggerName)
    {
        if (isDead)
            return;

        isAttacking = true;

        if (lockMovementWhileAttacking)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        animator.SetTrigger(attackTriggerName);
    }

    // Gắn hàm này bằng Animation Event ở frame cuối Attack0, Attack1, Attack2, Attack3
    public void EndAttack()
    {
        isAttacking = false;
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
        moveInput = 0f;

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

    // Nếu muốn xóa hẳn nhân vật khỏi Scene thì dùng hàm này thay HideAfterDie
    public void DestroyAfterDie()
    {
        Destroy(gameObject);
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