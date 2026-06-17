using System;
using System.Collections;
using UnityEngine;

public class Boss2Controller : MonoBehaviour
{
    [Header("Boss Info")]
    [Tooltip("Tên boss hiển thị trong log.")]
    public string bossName = "Sài Thái Tuế";

    [Header("Target")]
    [Tooltip("Mục tiêu chính là Wukong.")]
    public Transform wukongTarget;

    [Tooltip("Tag của Wukong.")]
    public string playerTag = "Player";

    [Tooltip("Tag của đoàn thỉnh kinh.")]
    public string partyTag = "Party";

    [Tooltip("Tự tìm Wukong theo tag nếu chưa kéo target.")]
    public bool autoFindPlayer = true;

    [Header("Activation / Story Combat")]
    [Tooltip("Boss2 đã được kích hoạt đánh thật chưa.")]
    public bool combatActivated = false;

    [Tooltip("Boss2 có được nhận damage từ Wukong không.")]
    public bool canReceiveDamage = false;

    [Tooltip("Boss2 có được hiện UI máu không.")]
    public bool canShowBossUI = false;

    [Tooltip("Tự kích hoạt khi Wukong vào tầm.")]
    public bool autoActivateByRange = false;

    [Tooltip("Tầm phát hiện để Boss2 bắt đầu combat nếu bật autoActivateByRange.")]
    public float activationRange = 10f;

    [Tooltip("Khi ActivateCombat thì Boss2 tự target Wukong.")]
    public bool autoTargetPlayerWhenCombat = true;

    [Tooltip("Khi chưa combat thì ép Boss2 đứng Idle.")]
    public bool forceIdleWhenNotCombat = true;

    [Header("Stop Combat When Dead")]
    [Tooltip("Boss2 dừng đánh khi Wukong chết.")]
    public bool stopBossWhenWukongDead = true;

    [Tooltip("Boss2 dừng đánh khi đoàn thỉnh kinh chết.")]
    public bool stopBossWhenPartyDead = true;

    [Tooltip("Boss2 đã dừng combat vì Wukong hoặc đoàn thỉnh kinh chết.")]
    public bool combatStoppedByDeath = false;

    [Header("References")]
    [Tooltip("Rigidbody2D của Boss2.")]
    public Rigidbody2D rb;

    [Tooltip("Animator của Boss2.")]
    public Animator animator;

    [Tooltip("SpriteRenderer của Boss2.")]
    public SpriteRenderer spriteRenderer;

    [Tooltip("Hitbox đánh cận chiến của Boss2.")]
    public Boss2MeleeHitbox meleeHitbox;

    [Tooltip("HUD máu riêng của Boss2.")]
    public Boss2HUDController boss2HUD;

    [Header("Health")]
    [Tooltip("Máu tối đa của Boss2.")]
    public int maxHealth = 1200;

    [Tooltip("Máu hiện tại của Boss2.")]
    public int currentHealth;

    [Header("Move")]
    [Tooltip("Tốc độ di chuyển của Boss2.")]
    public float moveSpeed = 3.5f;

    [Tooltip("Boss2 chỉ di chuyển theo trục X.")]
    public bool moveOnlyX = true;

    [Tooltip("Khoảng cách Boss2 dừng trước Wukong / Party, không áp sát thêm.")]
    public float stopDistance = 2.0f;

    [Tooltip("Khoảng cách vẫn cho phép tung đòn. Nên lớn hơn hoặc bằng stopDistance.")]
    public float meleeRange = 2.3f;

    [Tooltip("Độ lệch cao thấp cho phép khi đánh.")]
    public float verticalAttackTolerance = 3f;

    [Header("Party Blocking Logic")]
    [Tooltip("Bán kính quét đoàn thỉnh kinh.")]
    public float partyDetectRange = 7f;

    [Tooltip("Chỉ đánh Party khi Party đứng giữa Boss2 và Wukong.")]
    public bool onlyAttackPartyWhenBlockingWukong = true;

    [Header("Melee Attack")]
    [Tooltip("Sát thương đánh cận chiến.")]
    public int meleeDamage = 120;

    [Tooltip("Thời gian hồi đánh sau khi animation đánh kết thúc.")]
    public float meleeCooldown = 1.2f;

    [Tooltip("Thời gian đứng Idle trước khi ra đòn.")]
    public float preMeleeIdleDelay = 0.3f;

    [Tooltip("Thời gian tối đa chờ animation đánh. Nếu Animation Event cuối không chạy, Boss2 tự thoát sau thời gian này.")]
    public float meleeAnimationMaxDuration = 2.5f;

    [Tooltip("Thời gian đứng Idle sau khi đánh xong rồi mới hành động tiếp.")]
    public float postMeleeIdleDelay = 0.25f;

    [Tooltip("Khóa cứng vị trí Boss2 trong lúc đánh để không trượt hình.")]
    public bool freezeWorldPositionWhileAttacking = true;

    [Tooltip("Khi đánh thì chuyển Rigidbody2D sang Kinematic để chặn physics kéo boss đi.")]
    public bool makeRigidbodyKinematicWhileAttacking = true;

    [Header("Death")]
    [Tooltip("Tên trigger animation chết.")]
    public string dieTriggerName = "Die";

    [Tooltip("Nếu true thì Destroy object sau animation Die. Nếu false thì SetActive(false).")]
    public bool destroyAfterDeath = true;

    [Tooltip("Nếu Boss2 đang đánh mà hết máu, đợi đánh xong rồi mới Die.")]
    public bool waitCurrentActionBeforeDie = true;

    [Header("Animator")]
    [Tooltip("Tên parameter tốc độ.")]
    public string speedParameterName = "Speed";

    [Tooltip("Tên trigger đánh cận chiến.")]
    public string meleeTriggerName = "MeleeAttack";

    [Tooltip("Tên state Idle trong Animator.")]
    public string idleStateName = "Boss2_idle";

    [Header("Facing")]
    [Tooltip("Sprite gốc của Boss2 đang quay sang phải.")]
    public bool spriteFacesRightByDefault = true;

    [Tooltip("Lật hướng bằng SpriteRenderer.flipX.")]
    public bool useSpriteRendererFlip = false;

    [Tooltip("Lật hướng bằng localScale X.")]
    public bool useTransformScaleFlip = true;

    [Header("Control")]
    [Tooltip("Cho phép Boss2 di chuyển.")]
    public bool canMove = true;

    [Tooltip("Cho phép Boss2 tấn công.")]
    public bool canAttack = true;

    [Header("Debug")]
    [Tooltip("Bật log debug.")]
    public bool enableDebugLog = false;

    bool isPreparingAttack;
    bool isAttacking;
    bool isDead;
    bool isDying;
    bool pendingDie;
    bool meleeAnimationEnded;
    bool isFacingRight = true;

    float meleeTimer;

    Transform currentCombatTarget;
    Transform lockedMeleeTarget;
    Coroutine actionCoroutine;

    Vector3 lockedWorldPosition;
    bool hasLockedWorldPosition;

    RigidbodyConstraints2D originalConstraints;
    RigidbodyType2D originalBodyType;
    bool originalSimulated;
    bool hasSavedOriginalRigidbodyState;

    public Transform currentTarget
    {
        get { return currentCombatTarget; }
    }

    public Transform target
    {
        get { return currentCombatTarget; }
    }

    void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (meleeHitbox == null)
        {
            meleeHitbox = GetComponentInChildren<Boss2MeleeHitbox>(true);
        }

        if (meleeHitbox != null)
        {
            meleeHitbox.owner = this;
            meleeHitbox.ownerRoot = transform.root;
            meleeHitbox.damage = meleeDamage;
            meleeHitbox.DeactivateHitbox();
        }

        currentHealth = maxHealth;
        isFacingRight = spriteFacesRightByDefault;

        FindPlayerIfNeeded();
        FindBoss2HUDIfNeeded();
        UpdateBossHUD();
        SetBossHUDVisible(false);
    }

    void Start()
    {
        ForceIdleState(true);
    }

    void Update()
    {
        if (isDead || isDying) return;

        // Nếu Wukong hoặc đoàn đã chết thì Boss2 chỉ đứng Idle, không chạy AI, không đánh nữa.
        if (combatStoppedByDeath)
        {
            StopMove();
            CloseMeleeHitbox();
            UnlockWorldPosition();
            ForceBossAnimatorToIdleImmediately();
            return;
        }

        FindPlayerIfNeeded();
        UpdateTimers();

        if (ShouldFreezeBossPosition())
        {
            StopMove();
            MaintainLockedWorldPosition();
        }

        if (!combatActivated)
        {
            if (autoActivateByRange)
            {
                CheckAutoActivation();
            }

            if (!combatActivated)
            {
                StopMove();
                ForceBossIdleForStory();
                return;
            }
        }

        if (autoTargetPlayerWhenCombat && currentCombatTarget == null)
        {
            currentCombatTarget = wukongTarget;
        }

        if (actionCoroutine != null)
        {
            StopMove();
            MaintainLockedWorldPosition();
            return;
        }

        if (pendingDie)
        {
            StartDieNow();
            return;
        }

        RunBossAI();
    }

    void FixedUpdate()
    {
        if (ShouldFreezeBossPosition())
        {
            StopMove();
            MaintainLockedWorldPosition();
        }
    }

    void LateUpdate()
    {
        if (ShouldFreezeBossPosition())
        {
            MaintainLockedWorldPosition();
        }
    }

    bool ShouldFreezeBossPosition()
    {
        return freezeWorldPositionWhileAttacking && hasLockedWorldPosition && actionCoroutine != null;
    }

    void UpdateTimers()
    {
        if (meleeTimer > 0f)
        {
            meleeTimer -= Time.deltaTime;
        }
    }

    void CheckAutoActivation()
    {
        if (combatActivated) return;
        if (wukongTarget == null) return;

        float distance = Mathf.Abs(wukongTarget.position.x - transform.position.x);

        if (distance <= activationRange)
        {
            ActivateCombat();
        }
    }

    void RunBossAI()
    {
        if (!canMove && !canAttack)
        {
            ForceIdleState(false);
            return;
        }

        Transform attackTarget = ChooseAttackTarget();

        if (attackTarget != null)
        {
            currentCombatTarget = attackTarget;

            if (IsTargetInMeleeRange(currentCombatTarget))
            {
                StopMove();
                FaceTarget(currentCombatTarget);

                if (CanUseMelee())
                {
                    StartMeleeAttack(currentCombatTarget);
                }
                else
                {
                    ForceIdleState(false);
                }

                DebugTarget("Đứng im rồi mới đánh");
                return;
            }

            FaceTarget(currentCombatTarget);
        }

        currentCombatTarget = wukongTarget;

        if (currentCombatTarget == null)
        {
            ForceIdleState(false);
            DebugTarget("Không có target");
            return;
        }

        FaceTarget(currentCombatTarget);

        float distanceX = Mathf.Abs(currentCombatTarget.position.x - transform.position.x);

        if (distanceX <= stopDistance)
        {
            StopMove();
            ForceIdleState(false);
            DebugTarget("Đã tới stopDistance, đứng lại");
            return;
        }

        MoveToTarget(currentCombatTarget);
        DebugTarget("Đuổi Wukong");
    }

    Transform ChooseAttackTarget()
    {
        if (wukongTarget != null && IsTargetInMeleeRange(wukongTarget))
        {
            return wukongTarget;
        }

        Transform blockingParty = FindBlockingPartyInMeleeRange();

        if (blockingParty != null)
        {
            return blockingParty;
        }

        return wukongTarget;
    }

    Transform FindBlockingPartyInMeleeRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, partyDetectRange);

        Transform nearestBlockingParty = null;
        float nearestDistance = Mathf.Infinity;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null) continue;

            Transform targetRoot = GetTargetRoot(hit);
            if (targetRoot == null) continue;

            if (!IsPartyTarget(targetRoot, hit)) continue;

            if (onlyAttackPartyWhenBlockingWukong && !IsPartyBetweenBossAndWukong(targetRoot))
            {
                continue;
            }

            if (!IsTargetInMeleeRange(targetRoot))
            {
                continue;
            }

            float distance = Mathf.Abs(targetRoot.position.x - transform.position.x);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestBlockingParty = targetRoot;
            }
        }

        return nearestBlockingParty;
    }

    bool IsPartyBetweenBossAndWukong(Transform partyTarget)
    {
        if (partyTarget == null) return false;
        if (wukongTarget == null) return false;

        float bossX = transform.position.x;
        float wukongX = wukongTarget.position.x;
        float partyX = partyTarget.position.x;

        float minX = Mathf.Min(bossX, wukongX);
        float maxX = Mathf.Max(bossX, wukongX);

        return partyX > minX && partyX < maxX;
    }

    Transform GetTargetRoot(Collider2D hit)
    {
        if (hit.attachedRigidbody != null)
        {
            return hit.attachedRigidbody.transform;
        }

        return hit.transform.root;
    }

    bool IsPartyTarget(Transform targetRoot, Collider2D hit)
    {
        if (targetRoot.CompareTag(partyTag)) return true;
        if (hit.CompareTag(partyTag)) return true;

        Transform current = hit.transform;

        while (current != null)
        {
            if (current.CompareTag(partyTag)) return true;

            if (current == targetRoot) break;

            current = current.parent;
        }

        return false;
    }

    bool IsTargetInMeleeRange(Transform checkTarget)
    {
        if (checkTarget == null) return false;

        float distanceX = Mathf.Abs(checkTarget.position.x - transform.position.x);
        float distanceY = Mathf.Abs(checkTarget.position.y - transform.position.y);

        return distanceX <= meleeRange && distanceY <= verticalAttackTolerance;
    }

    void MoveToTarget(Transform moveTarget)
    {
        if (!canMove)
        {
            StopMove();
            ForceIdleState(false);
            return;
        }

        if (ShouldFreezeBossPosition())
        {
            StopMove();
            MaintainLockedWorldPosition();
            return;
        }

        if (moveTarget == null)
        {
            StopMove();
            ForceIdleState(false);
            return;
        }

        Vector2 direction = moveTarget.position - transform.position;

        if (moveOnlyX)
        {
            direction.y = 0f;
        }

        if (Mathf.Abs(direction.x) <= stopDistance)
        {
            StopMove();
            ForceIdleState(false);
            return;
        }

        float moveDirectionX = Mathf.Sign(direction.x);

        FaceDirection(moveDirectionX);

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(moveDirectionX * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            transform.position += new Vector3(moveDirectionX, 0f, 0f) * moveSpeed * Time.deltaTime;
        }

        SetAnimatorSpeed(Mathf.Abs(moveSpeed));
    }

    void StopMove()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            rb.angularVelocity = 0f;
        }

        SetAnimatorSpeed(0f);
    }

    bool CanUseMelee()
    {
        if (!combatActivated) return false;
        if (combatStoppedByDeath) return false;
        if (!canAttack) return false;
        if (isPreparingAttack) return false;
        if (isAttacking) return false;
        if (isDead) return false;
        if (isDying) return false;
        if (meleeTimer > 0f) return false;
        if (pendingDie) return false;

        return true;
    }

    void StartMeleeAttack(Transform attackTarget)
    {
        if (combatStoppedByDeath) return;
        if (!combatActivated) return;
        if (actionCoroutine != null) return;
        if (isPreparingAttack) return;
        if (isAttacking) return;

        isPreparingAttack = true;
        isAttacking = true;

        StopMove();
        FaceTarget(attackTarget);
        LockWorldPosition();

        actionCoroutine = StartCoroutine(MeleeAttackRoutine(attackTarget));
    }

    IEnumerator MeleeAttackRoutine(Transform attackTarget)
    {
        meleeAnimationEnded = false;
        lockedMeleeTarget = attackTarget;

        StopMove();
        FaceTarget(attackTarget);
        LockWorldPosition();
        ForceIdleState(true);

        float prepareElapsed = 0f;
        float prepareDelay = Mathf.Max(preMeleeIdleDelay, 0.25f);

        while (prepareElapsed < prepareDelay)
        {
            if (combatStoppedByDeath)
            {
                ForceStopBossBecauseTargetDead();
                yield break;
            }

            StopMove();
            MaintainLockedWorldPosition();

            prepareElapsed += Time.deltaTime;
            yield return null;
        }

        if (combatStoppedByDeath)
        {
            ForceStopBossBecauseTargetDead();
            yield break;
        }

        isPreparingAttack = false;

        StopMove();
        MaintainLockedWorldPosition();

        if (animator != null)
        {
            animator.ResetTrigger(meleeTriggerName);
            animator.SetFloat(speedParameterName, 0f);
            animator.SetTrigger(meleeTriggerName);
        }

        float elapsed = 0f;

        while (!meleeAnimationEnded && elapsed < meleeAnimationMaxDuration)
        {
            if (combatStoppedByDeath)
            {
                ForceStopBossBecauseTargetDead();
                yield break;
            }

            StopMove();
            MaintainLockedWorldPosition();

            elapsed += Time.deltaTime;
            yield return null;
        }

        CloseMeleeHitbox();

        meleeTimer = meleeCooldown;
        isAttacking = false;
        lockedMeleeTarget = null;

        ForceIdleState(true);

        float postElapsed = 0f;

        while (postElapsed < postMeleeIdleDelay)
        {
            if (combatStoppedByDeath)
            {
                ForceStopBossBecauseTargetDead();
                yield break;
            }

            StopMove();
            MaintainLockedWorldPosition();

            postElapsed += Time.deltaTime;
            yield return null;
        }

        UnlockWorldPosition();
        actionCoroutine = null;

        if (pendingDie)
        {
            StartDieNow();
            yield break;
        }
    }

    public void OpenMeleeHitbox()
    {
        if (combatStoppedByDeath) return;
        if (!combatActivated) return;
        if (!canAttack) return;
        if (isDead || isDying) return;
        if (!isAttacking) return;
        if (meleeHitbox == null) return;

        StopMove();
        MaintainLockedWorldPosition();

        meleeHitbox.damage = meleeDamage;
        meleeHitbox.owner = this;
        meleeHitbox.ownerRoot = transform.root;

        meleeHitbox.ActivateHitbox(lockedMeleeTarget);
        meleeHitbox.ForceHitTarget(lockedMeleeTarget);

        DebugTarget("Mở hitbox đánh");
    }

    public void CloseMeleeHitbox()
    {
        if (meleeHitbox != null)
        {
            meleeHitbox.DeactivateHitbox();
        }

        StopMove();
        MaintainLockedWorldPosition();
    }

    public void EndMeleeAttackAnimation()
    {
        meleeAnimationEnded = true;
    }

    public void EndMeleeAttack()
    {
        EndMeleeAttackAnimation();
    }

    void LockWorldPosition()
    {
        lockedWorldPosition = transform.position;
        hasLockedWorldPosition = true;

        if (rb != null)
        {
            if (!hasSavedOriginalRigidbodyState)
            {
                originalConstraints = rb.constraints;
                originalBodyType = rb.bodyType;
                originalSimulated = rb.simulated;
                hasSavedOriginalRigidbodyState = true;
            }

            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            if (makeRigidbodyKinematicWhileAttacking)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
            }

            rb.constraints = originalConstraints |
                             RigidbodyConstraints2D.FreezePositionX |
                             RigidbodyConstraints2D.FreezePositionY;

            rb.position = lockedWorldPosition;
        }

        transform.position = lockedWorldPosition;
    }

    void MaintainLockedWorldPosition()
    {
        if (!freezeWorldPositionWhileAttacking) return;
        if (!hasLockedWorldPosition) return;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = lockedWorldPosition;
        }

        transform.position = lockedWorldPosition;
    }

    void UnlockWorldPosition()
    {
        if (rb != null && hasSavedOriginalRigidbodyState)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = originalConstraints;
            rb.bodyType = originalBodyType;
            rb.simulated = originalSimulated;
        }

        hasLockedWorldPosition = false;
    }

    void FaceTarget(Transform faceTarget)
    {
        if (faceTarget == null) return;

        float directionX = faceTarget.position.x - transform.position.x;
        FaceDirection(directionX);
    }

    void FaceDirection(float directionX)
    {
        if (Mathf.Abs(directionX) < 0.05f) return;

        isFacingRight = directionX > 0f;

        if (spriteRenderer != null && useSpriteRendererFlip)
        {
            bool shouldFlip = spriteFacesRightByDefault ? !isFacingRight : isFacingRight;
            spriteRenderer.flipX = shouldFlip;
        }

        if (useTransformScaleFlip)
        {
            Vector3 scale = transform.localScale;
            float absX = Mathf.Abs(scale.x);

            if (spriteFacesRightByDefault)
            {
                scale.x = isFacingRight ? absX : -absX;
            }
            else
            {
                scale.x = isFacingRight ? -absX : absX;
            }

            transform.localScale = scale;
        }
    }

    public Vector2 GetBossFacingDirection()
    {
        return isFacingRight ? Vector2.right : Vector2.left;
    }

    void SetAnimatorSpeed(float speed)
    {
        if (animator != null && !string.IsNullOrEmpty(speedParameterName))
        {
            animator.SetFloat(speedParameterName, speed);
        }
    }

    public void TakeDamage(int damage)
    {
        if (!canReceiveDamage || !combatActivated)
        {
            if (enableDebugLog)
            {
                Debug.Log(gameObject.name + " chưa vào combat nên không nhận damage.");
            }

            return;
        }

        if (isDead || isDying) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateBossHUD();

        if (enableDebugLog)
        {
            Debug.Log(bossName + " nhận damage: -" + damage + " | Máu còn: " + currentHealth + "/" + maxHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void ReceiveDamage(int damage)
    {
        TakeDamage(damage);
    }

    public void ApplyDamage(int damage)
    {
        TakeDamage(damage);
    }

    void Die()
    {
        if (isDead || isDying) return;

        pendingDie = true;
        canReceiveDamage = false;
        canAttack = false;
        canMove = false;

        if (waitCurrentActionBeforeDie && actionCoroutine != null)
        {
            return;
        }

        StartDieNow();
    }

    void StartDieNow()
    {
        if (isDead || isDying) return;

        pendingDie = false;
        isDying = true;
        isPreparingAttack = false;
        isAttacking = false;
        combatActivated = false;
        combatStoppedByDeath = true;

        StopMove();
        CloseMeleeHitbox();

        UnlockWorldPosition();

        lockedMeleeTarget = null;
        currentCombatTarget = null;

        SetBossHUDVisible(false);

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (animator != null)
        {
            animator.ResetTrigger(meleeTriggerName);
            animator.ResetTrigger(dieTriggerName);
            SetAnimatorSpeed(0f);
            animator.SetTrigger(dieTriggerName);
        }

        DebugTarget("Boss2 chuyển sang Die");
    }

    public void DestroyBoss2AfterDieAnimation()
    {
        if (!isDying && !isDead) return;

        isDead = true;

        if (destroyAfterDeath)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void NotifyWukongDead()
    {
        if (!stopBossWhenWukongDead) return;

        ForceStopBossBecauseTargetDead();
    }

    public void NotifyPartyDead()
    {
        if (!stopBossWhenPartyDead) return;

        ForceStopBossBecauseTargetDead();
    }

    public void StopBossCombat()
    {
        ForceStopBossBecauseTargetDead();
    }

    public void StopBossCombatAndReturnIdle()
    {
        ForceStopBossBecauseTargetDead();
    }

    void ForceStopBossBecauseTargetDead()
    {
        combatStoppedByDeath = true;
        combatActivated = false;

        canAttack = false;
        canMove = false;
        canReceiveDamage = false;
        canShowBossUI = false;

        isPreparingAttack = false;
        isAttacking = false;
        meleeAnimationEnded = false;
        pendingDie = false;

        currentCombatTarget = null;
        lockedMeleeTarget = null;

        if (actionCoroutine != null)
        {
            StopCoroutine(actionCoroutine);
            actionCoroutine = null;
        }

        CloseMeleeHitbox();
        UnlockWorldPosition();
        StopMove();

        ForceBossAnimatorToIdleImmediately();

        if (enableDebugLog)
        {
            Debug.Log(bossName + " dừng combat vì Wukong hoặc đoàn thỉnh kinh đã chết. Boss chuyển về Idle.");
        }
    }

    void ForceBossAnimatorToIdleImmediately()
    {
        if (animator == null) return;

        animator.enabled = true;

        if (!string.IsNullOrEmpty(meleeTriggerName))
        {
            animator.ResetTrigger(meleeTriggerName);
        }

        if (!string.IsNullOrEmpty(dieTriggerName))
        {
            animator.ResetTrigger(dieTriggerName);
        }

        if (!string.IsNullOrEmpty(speedParameterName))
        {
            animator.SetFloat(speedParameterName, 0f);
        }

        if (!string.IsNullOrEmpty(idleStateName))
        {
            animator.Play(idleStateName, 0, 0f);
            animator.Update(0f);
        }
    }

    void ForceBossAnimatorToIdleOneTime()
    {
        ForceBossAnimatorToIdleImmediately();
    }

    void KeepIdleAfterCombatStopped()
    {
        StopMove();
        ForceBossAnimatorToIdleImmediately();
    }

    void ForceIdleState(bool restartIdle)
    {
        StopMove();

        if (animator == null) return;

        animator.ResetTrigger(meleeTriggerName);
        animator.ResetTrigger(dieTriggerName);
        SetAnimatorSpeed(0f);

        if (string.IsNullOrEmpty(idleStateName)) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        bool isIdle = stateInfo.IsName(idleStateName);

        if (isIdle && !restartIdle)
        {
            return;
        }

        animator.CrossFade(idleStateName, 0.03f, 0, 0f);
    }

    void ForceBossIdleForStory()
    {
        if (!forceIdleWhenNotCombat) return;

        StopMove();

        if (animator == null) return;

        animator.ResetTrigger(meleeTriggerName);
        animator.ResetTrigger(dieTriggerName);
        SetAnimatorSpeed(0f);

        if (!string.IsNullOrEmpty(idleStateName))
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            if (!stateInfo.IsName(idleStateName))
            {
                animator.Play(idleStateName, 0, 0f);
                animator.Update(0f);
            }
        }
    }

    public void ActivateCombat()
    {
        if (isDead || isDying) return;

        combatActivated = true;
        canReceiveDamage = true;
        canShowBossUI = true;
        combatStoppedByDeath = false;

        canMove = true;
        canAttack = true;

        FindPlayerIfNeeded();

        if (autoTargetPlayerWhenCombat)
        {
            currentCombatTarget = wukongTarget;
        }

        if (currentCombatTarget != null)
        {
            FaceTarget(currentCombatTarget);
        }

        StopMove();
        ForceIdleState(true);

        SetBossHUDVisible(true);
        UpdateBossHUD();

        DebugTarget("ActivateCombat");
    }

    public void DeactivateCombat()
    {
        combatActivated = false;
        canReceiveDamage = false;
        canShowBossUI = false;

        isPreparingAttack = false;
        isAttacking = false;
        currentCombatTarget = null;
        lockedMeleeTarget = null;

        UnlockWorldPosition();

        SetBossHUDVisible(false);
        StopMove();
        ForceBossIdleForStory();
    }

    public void StopCombatAndReturnIdle()
    {
        DeactivateCombat();
    }

    public bool IsCombatState()
    {
        return combatActivated;
    }

    public bool CanReceiveDamage()
    {
        return canReceiveDamage && combatActivated && !IsDead();
    }

    public bool CanShowBossUI()
    {
        return canShowBossUI && combatActivated;
    }

    void FindPlayerIfNeeded()
    {
        if (!autoFindPlayer) return;
        if (wukongTarget != null) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
        {
            wukongTarget = playerObject.transform;
        }
    }

    void FindBoss2HUDIfNeeded()
    {
        if (boss2HUD != null) return;

#if UNITY_2023_1_OR_NEWER
        boss2HUD = FindFirstObjectByType<Boss2HUDController>();
#else
        boss2HUD = FindObjectOfType<Boss2HUDController>();
#endif
    }

    void UpdateBossHUD()
    {
        FindBoss2HUDIfNeeded();

        if (boss2HUD == null) return;

        boss2HUD.SetHealth(currentHealth, maxHealth);
    }

    void SetBossHUDVisible(bool visible)
    {
        FindBoss2HUDIfNeeded();

        if (boss2HUD == null) return;

        boss2HUD.SetVisible(visible);
    }

    public Transform GetLockedMeleeTarget()
    {
        return lockedMeleeTarget;
    }

    public bool IsTargetStillInMeleeRange(Transform checkTarget)
    {
        return IsTargetInMeleeRange(checkTarget);
    }

    public bool IsDead()
    {
        return isDead || isDying || currentHealth <= 0;
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public float GetHealthPercent()
    {
        if (maxHealth <= 0) return 0f;

        return (float)currentHealth / maxHealth;
    }

    void DebugTarget(string state)
    {
        if (!enableDebugLog) return;

        string targetName = currentCombatTarget != null ? currentCombatTarget.name : "NULL";
        float distance = currentCombatTarget != null
            ? Vector2.Distance(transform.position, currentCombatTarget.position)
            : -1f;

        Debug.Log(bossName + " | " + state + " | Target: " + targetName + " | Distance: " + distance.ToString("F2"));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, partyDetectRange);
    }
}