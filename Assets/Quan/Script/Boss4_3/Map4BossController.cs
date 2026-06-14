using System.Collections;
using UnityEngine;

public class Map4BossController : MonoBehaviour
{
    public enum BossHealthUISlot
    {
        Boss3,
        Boss4
    }
   

    [Header("Boss Info")]
    [Tooltip("ID của boss. Boss3 = 3, Boss4 = 4.")]
    public int bossId = 4;

    [Tooltip("Tên boss hiển thị trong log.")]
    public string bossName = "Thanh Sư Tinh";

    [Tooltip("Thanh máu UI mà boss này sử dụng.")]
    public BossHealthUISlot healthUISlot = BossHealthUISlot.Boss4;

    [Header("Health")]
    [Tooltip("Máu tối đa.")]
    public int maxHealth = 1000;

    [Tooltip("Máu hiện tại.")]
    public int currentHealth;

    [Header("Story Combat State")]
    [Tooltip("Boss đã được kích hoạt đánh thật chưa.")]
    public bool combatActivated = false;

    [Tooltip("Boss có được nhận damage từ Wukong không.")]
    public bool canReceiveDamage = false;

    [Tooltip("Boss có được hiện UI máu không.")]
    public bool canShowBossUI = false;

    [Tooltip("Ép boss đứng Idle khi chưa combat.")]
    public bool forceIdleWhenNotCombat = true;

    [Tooltip("Boss chết thì ẩn object.")]
    public bool hideWhenDefeated = false;

    [Header("References")]
    [Tooltip("Rigidbody2D của boss.")]
    public Rigidbody2D rb;

    [Tooltip("Animator của boss.")]
    public Animator animator;

    [Tooltip("SpriteRenderer của boss.")]
    public SpriteRenderer spriteRenderer;

    [Tooltip("Target chính là Wukong.")]
    public Transform wukongTarget;

    [Tooltip("HUD thanh máu boss.")]
    public Map4BossHUDController map4BossHUD;

    [Tooltip("Hitbox đánh gần.")]
    public Boss4MeleeHitbox meleeHitbox;

    [Tooltip("Điểm sinh projectile.")]
    public Transform ultimateFirePoint;

    [Tooltip("Prefab projectile ulti.")]
    public GameObject ultimateProjectilePrefab;

    [Header("Stop Combat When Dead")]
    [Tooltip("Boss dừng đánh khi Wukong chết.")]
    public bool stopBossWhenWukongDead = true;

    [Tooltip("Boss dừng đánh khi đoàn thỉnh kinh chết.")]
    public bool stopBossWhenPartyDead = true;

    [Tooltip("Boss đã dừng combat vì Wukong hoặc đoàn thỉnh kinh chết.")]
    public bool combatStoppedByDeath = false;

    [Header("Tags")]
    [Tooltip("Tag của Wukong.")]
    public string playerTag = "Player";

    [Tooltip("Tag của đoàn thỉnh kinh.")]
    public string partyTag = "Party";

    [Header("Activation")]
    [Tooltip("Boss tự kích hoạt khi Wukong vào tầm.")]
    public bool autoActivate = true;

    [Tooltip("Tầm phát hiện để boss bắt đầu hoạt động.")]
    public float activationRange = 12f;

    [Tooltip("Boss bắt đầu active ngay khi vào scene.")]
    public bool activeOnStart = true;

    [Tooltip("Boss phải đánh cận chiến trúng lần đầu mới bắt đầu hồi chiêu ulti.")]
    public bool requireFirstMeleeHitBeforeUltimate = true;

    [Tooltip("Boss đã đánh cận chiến trúng lần đầu.")]
    public bool hasFirstMeleeHit = false;

    [Header("Move")]
    [Tooltip("Tốc độ di chuyển.")]
    public float moveSpeed = 2.5f;

    [Tooltip("Khoảng cách dừng khi áp sát target.")]
    public float stopDistanceToWukong = 1.4f;

    [Tooltip("Boss chỉ di chuyển theo trục X.")]
    public bool moveOnlyX = true;

    [Header("Facing")]
    [Tooltip("Sprite gốc của boss đang quay sang phải.")]
    public bool spriteFacesRightByDefault = true;

    [Tooltip("Lật hướng bằng SpriteRenderer.flipX.")]
    public bool useSpriteRendererFlip = true;

    [Tooltip("Lật hướng bằng localScale X.")]
    public bool useTransformScaleFlip = false;

    [Header("Animator")]
    [Tooltip("Tên parameter tốc độ trong Animator.")]
    public string speedParameterName = "Speed";

    [Tooltip("Tên trigger đánh gần.")]
    public string meleeTriggerName = "MeleeAttack";

    [Tooltip("Tên trigger ulti.")]
    public string ultimateTriggerName = "Ultimate";

    [Tooltip("Tên state idle trong Animator.")]
    public string idleStateName = "Boss4_idle";

    [Header("Melee")]
    [Tooltip("Tầm đánh gần.")]
    public float meleeRange = 1.5f;

    [Tooltip("Sát thương đánh gần.")]
    public int meleeDamage = 100;

    [Tooltip("Thời gian hồi đánh gần.")]
    public float meleeCooldown = 1.5f;

    [Tooltip("Thời gian tối đa chờ animation đánh gần.")]
    public float meleeAnimationMaxDuration = 1.2f;

    [Tooltip("Thời gian đứng idle sau khi đánh gần.")]
    public float postMeleeIdleDelay = 0.15f;

    [Header("Party Detect")]
    [Tooltip("Bán kính quét Party.")]
    public float partyDetectRange = 6f;

    [Header("Ultimate")]
    [Tooltip("Sát thương ulti.")]
    public int ultimateDamage = 100;

    [Tooltip("Thời gian hồi ulti.")]
    public float ultimateCooldown = 4f;

    [Tooltip("Thời gian đứng idle trước khi dùng ulti.")]
    public float preUltimateIdleDelay = 0.2f;

    [Tooltip("Thời gian tối đa chờ animation ulti.")]
    public float ultimateAnimationMaxDuration = 2.5f;

    [Tooltip("Thời gian đứng idle sau khi dùng ulti.")]
    public float postUltimateIdleDelay = 0.35f;

    [Tooltip("Chỉ dùng ulti khi đúng khoảng cách.")]
    public bool useUltimateDistanceCondition = true;

    [Tooltip("Khoảng cách tối thiểu để dùng ulti.")]
    public float minUltimateDistanceToWukong = 2f;

    [Tooltip("Khoảng cách tối đa để dùng ulti.")]
    public float maxUltimateDistanceToWukong = 7f;

    [Header("Ultimate Fire Point")]
    [Tooltip("Tự cập nhật vị trí điểm sinh projectile theo hướng boss.")]
    public bool autoUpdateUltimateFirePoint = true;

    [Tooltip("Khoảng cách sinh projectile tính từ tâm sprite boss.")]
    public Vector2 ultimateFirePointLocalOffset = new Vector2(1.3f, 0.5f);

    [Header("Skill Lock")]
    [Tooltip("Khóa vị trí boss khi đang dùng skill.")]
    public bool freezeWorldPositionWhileUsingSkill = true;

    [Header("Debug")]
    [Tooltip("Bật log debug target.")]
    public bool enableDebugLog = false;

    bool isActive;
    bool isDefeated;
    bool isAttacking;
    bool isUsingUltimate;
    bool isMovementLocked;
    bool isFacingRight = true;

    bool hasForcedIdleAfterCombatStop;

    float meleeTimer;
    float ultimateTimer;

    bool meleeAnimationEnded;
    bool ultimateProjectileFired;
    bool ultimateAnimationEnded;

    Transform currentTarget;
    Transform lockedMeleeTarget;
    Coroutine actionCoroutine;

    Vector3 lockedWorldPosition;
    bool hasLockedWorldPosition;

    public Transform currentCombatTarget
    {
        get { return currentTarget; }
    }

    public Transform target
    {
        get { return currentTarget; }
    }

    public Transform GetLockedMeleeTarget()
    {
        return lockedMeleeTarget;
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
            meleeHitbox = GetComponentInChildren<Boss4MeleeHitbox>(true);
        }

        currentHealth = maxHealth;

        isFacingRight = spriteFacesRightByDefault;

        SyncHealthSlotByBossId();
        FindWukongIfNeeded();
        FindBossHUDIfNeeded();
    }

    void Start()
    {
        isActive = activeOnStart && !autoActivate;

        if (activeOnStart && autoActivate)
        {
            isActive = true;
        }

        UpdateBossHUD();
        ForceIdleState(true);

        if (requireFirstMeleeHitBeforeUltimate)
        {
            hasFirstMeleeHit = false;
            ultimateTimer = 999999f;
        }
        else
        {
            hasFirstMeleeHit = true;
            ultimateTimer = 0f;
        }
    }

    void Update()
    {
        if (!combatActivated)
        {
            StopMove();
            ForceBossIdleForStory();
            return;
        }
        if (isDefeated) return;

        if (combatStoppedByDeath)
        {
            StopMove();

            if (!hasForcedIdleAfterCombatStop)
            {
                StopBossCombatAndReturnIdle();
            }
            else
            {
                KeepIdleAfterCombatStopped();
            }

            return;
        }

        FindWukongIfNeeded();
        UpdateTimers();

        if (autoUpdateUltimateFirePoint)
        {
            UpdateUltimateFirePointPosition();
        }

        if (!isActive)
        {
            CheckActivation();
            StopMove();
            return;
        }

        if (actionCoroutine != null)
        {
            MaintainLockedWorldPosition();
            return;
        }

        RunBossAI();
    }

    void UpdateTimers()
    {
        if (meleeTimer > 0f)
        {
            meleeTimer -= Time.deltaTime;
        }

        if (ultimateTimer > 0f)
        {
            ultimateTimer -= Time.deltaTime;
        }
    }

    void CheckActivation()
    {
        if (wukongTarget == null) return;

        float distance = Vector2.Distance(transform.position, wukongTarget.position);

        if (distance <= activationRange)
        {
            isActive = true;
        }
    }
    void RunBossAI()
    {
        if (combatStoppedByDeath)
        {
            StopBossCombatAndReturnIdle();
            return;
        }

        if (isMovementLocked)
        {
            StopMove();
            return;
        }

        if (wukongTarget != null)
        {
            currentTarget = wukongTarget;
            FaceTarget(currentTarget);

            float distanceToWukong = Vector2.Distance(transform.position, wukongTarget.position);

            if (distanceToWukong <= meleeRange)
            {
                if (CanUseMelee())
                {
                    StartMeleeAttack(wukongTarget);
                }
                else
                {
                    ForceIdleState(false);
                }

                DebugTarget("Ưu tiên đánh Wukong trong melee range");
                return;
            }

            Transform blockingParty = FindBlockingPartyInMeleeRange();

            if (blockingParty != null)
            {
                currentTarget = blockingParty;
                FaceTarget(currentTarget);

                if (CanUseMelee())
                {
                    StartMeleeAttack(currentTarget);
                }
                else
                {
                    ForceIdleState(false);
                }

                DebugTarget("Đánh Party vì Party đang chắn giữa boss và Wukong");
                return;
            }

            currentTarget = wukongTarget;
            FaceTarget(currentTarget);

            if (CanUseUltimate(distanceToWukong))
            {
                StartUltimateAttack(currentTarget);
                DebugTarget("Dùng ulti theo Wukong");
                return;
            }

            MoveToTarget(currentTarget);
            DebugTarget("Đuổi Wukong");
            return;
        }

        Transform partyInMeleeRange = FindPartyInMeleeRange();

        if (partyInMeleeRange != null)
        {
            currentTarget = partyInMeleeRange;
            FaceTarget(currentTarget);

            if (CanUseMelee())
            {
                StartMeleeAttack(currentTarget);
            }
            else
            {
                ForceIdleState(false);
            }

            DebugTarget("Đánh Party vì không có Wukong target");
            return;
        }

        currentTarget = FindNearestPartyTarget();

        if (currentTarget == null)
        {
            ForceIdleState(false);
            DebugTarget("Không có target");
            return;
        }

        FaceTarget(currentTarget);
        MoveToTarget(currentTarget);
        DebugTarget("Đuổi Party vì không có Wukong");
    }
    Transform FindBlockingPartyInMeleeRange()
    {
        if (wukongTarget == null) return null;

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

            float distanceToParty = Vector2.Distance(transform.position, targetRoot.position);

            if (distanceToParty > meleeRange) continue;

            if (!IsPartyBetweenBossAndWukong(targetRoot)) continue;

            if (distanceToParty < nearestDistance)
            {
                nearestDistance = distanceToParty;
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
    Transform FindWukongInMeleeRange()
    {
        if (wukongTarget == null) return null;

        float distanceToWukong = Vector2.Distance(transform.position, wukongTarget.position);

        if (distanceToWukong <= meleeRange)
        {
            return wukongTarget;
        }

        return null;
    }
    Transform FindPartyInMeleeRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, partyDetectRange);

        Transform nearestParty = null;
        float nearestDistance = Mathf.Infinity;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null) continue;

            Transform targetRoot = GetTargetRoot(hit);
            if (targetRoot == null) continue;

            if (!IsPartyTarget(targetRoot, hit)) continue;

            float distance = Vector2.Distance(transform.position, targetRoot.position);

            if (distance > meleeRange) continue;

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestParty = targetRoot;
            }
        }

        return nearestParty;
    }

    Transform FindBestCombatTarget()
    {
        Transform bestTarget = null;
        float bestDistance = Mathf.Infinity;

        if (wukongTarget != null)
        {
            float distanceToWukong = Vector2.Distance(transform.position, wukongTarget.position);

            bestTarget = wukongTarget;
            bestDistance = distanceToWukong;
        }

        Transform nearestParty = FindNearestPartyTarget();

        if (nearestParty != null)
        {
            float distanceToParty = Vector2.Distance(transform.position, nearestParty.position);

            if (distanceToParty < bestDistance)
            {
                bestTarget = nearestParty;
                bestDistance = distanceToParty;
            }
        }

        return bestTarget;
    }

    Transform FindNearestPartyTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, partyDetectRange);

        Transform nearestParty = null;
        float nearestDistance = Mathf.Infinity;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null) continue;

            Transform targetRoot = GetTargetRoot(hit);
            if (targetRoot == null) continue;

            if (!IsPartyTarget(targetRoot, hit)) continue;

            float distance = Vector2.Distance(transform.position, targetRoot.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestParty = targetRoot;
            }
        }

        return nearestParty;
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

    void MoveToTarget(Transform targetTransform)
    {
        if (combatStoppedByDeath)
        {
            StopBossCombatAndReturnIdle();
            return;
        }

        if (targetTransform == null)
        {
            ForceIdleState(false);
            return;
        }

        Vector2 direction = targetTransform.position - transform.position;

        if (moveOnlyX)
        {
            direction.y = 0f;
        }

        if (Mathf.Abs(direction.x) <= stopDistanceToWukong)
        {
            ForceIdleState(false);
            return;
        }

        float moveDirectionX = Mathf.Sign(direction.x);

        FaceDirection(moveDirectionX);

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(moveDirectionX * moveSpeed, rb.linearVelocity.y);
        }

        if (animator != null)
        {
            animator.SetFloat(speedParameterName, Mathf.Abs(moveSpeed));
        }
    }

    void StopMove()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        if (animator != null)
        {
            animator.SetFloat(speedParameterName, 0f);
        }
    }

    bool CanUseMelee()
    {
        if (combatStoppedByDeath) return false;
        if (isAttacking) return false;
        if (isUsingUltimate) return false;
        if (meleeTimer > 0f) return false;

        return true;
    }

    bool CanUseUltimate(float distanceToTarget)
    {
        if (combatStoppedByDeath) return false;
        if (requireFirstMeleeHitBeforeUltimate && !hasFirstMeleeHit) return false;
        if (isAttacking) return false;
        if (isUsingUltimate) return false;
        if (ultimateTimer > 0f) return false;
        if (ultimateProjectilePrefab == null) return false;
        if (ultimateFirePoint == null) return false;

        if (useUltimateDistanceCondition)
        {
            if (distanceToTarget < minUltimateDistanceToWukong) return false;
            if (distanceToTarget > maxUltimateDistanceToWukong) return false;
        }

        return true;
    }

    void StartMeleeAttack(Transform attackTarget)
    {
        if (combatStoppedByDeath)
        {
            StopBossCombatAndReturnIdle();
            return;
        }

        if (actionCoroutine != null) return;

        actionCoroutine = StartCoroutine(MeleeAttackRoutine(attackTarget));
    }

    IEnumerator MeleeAttackRoutine(Transform attackTarget)
    {
        isAttacking = true;
        meleeAnimationEnded = false;
        lockedMeleeTarget = attackTarget;

        SetMovementLock(true);
        LockWorldPosition();

        StopMove();
        FaceTarget(attackTarget);
        ForceIdleState(false);

        if (animator != null)
        {
            animator.ResetTrigger(ultimateTriggerName);
            animator.SetTrigger(meleeTriggerName);
        }

        float elapsed = 0f;

        while (!meleeAnimationEnded && elapsed < meleeAnimationMaxDuration)
        {
            if (combatStoppedByDeath)
            {
                StopBossCombatAndReturnIdle();
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

        float idleElapsed = 0f;

        while (idleElapsed < postMeleeIdleDelay)
        {
            if (combatStoppedByDeath)
            {
                StopBossCombatAndReturnIdle();
                yield break;
            }

            StopMove();
            MaintainLockedWorldPosition();

            idleElapsed += Time.deltaTime;
            yield return null;
        }

        SetMovementLock(false);

        actionCoroutine = null;
    }

    void StartUltimateAttack(Transform attackTarget)
    {
        if (combatStoppedByDeath)
        {
            StopBossCombatAndReturnIdle();
            return;
        }

        if (actionCoroutine != null) return;

        actionCoroutine = StartCoroutine(UltimateAttackRoutine(attackTarget));
    }

    IEnumerator UltimateAttackRoutine(Transform attackTarget)
    {
        isUsingUltimate = true;
        ultimateProjectileFired = false;
        ultimateAnimationEnded = false;

        SetMovementLock(true);
        LockWorldPosition();

        StopMove();
        FaceTarget(attackTarget);
        ForceIdleState(true);

        float preElapsed = 0f;

        while (preElapsed < preUltimateIdleDelay)
        {
            if (combatStoppedByDeath)
            {
                StopBossCombatAndReturnIdle();
                yield break;
            }

            StopMove();
            MaintainLockedWorldPosition();

            preElapsed += Time.deltaTime;
            yield return null;
        }

        if (animator != null)
        {
            animator.ResetTrigger(meleeTriggerName);
            animator.SetTrigger(ultimateTriggerName);
        }

        float elapsed = 0f;

        while (!ultimateAnimationEnded && elapsed < ultimateAnimationMaxDuration)
        {
            if (combatStoppedByDeath)
            {
                StopBossCombatAndReturnIdle();
                yield break;
            }

            StopMove();
            MaintainLockedWorldPosition();

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!ultimateProjectileFired && !combatStoppedByDeath)
        {
            FireUltimateProjectile();
        }

        ultimateTimer = ultimateCooldown;

        ForceIdleAfterUltimate();

        float postElapsed = 0f;

        while (postElapsed < postUltimateIdleDelay)
        {
            if (combatStoppedByDeath)
            {
                StopBossCombatAndReturnIdle();
                yield break;
            }

            StopMove();
            MaintainLockedWorldPosition();

            postElapsed += Time.deltaTime;
            yield return null;
        }

        isUsingUltimate = false;
        ultimateProjectileFired = false;
        ultimateAnimationEnded = false;

        SetMovementLock(false);

        actionCoroutine = null;
    }

    public void OpenMeleeHitbox()
    {
        if (combatStoppedByDeath) return;
        if (meleeHitbox == null) return;

        GameObject hitboxObject = meleeHitbox.gameObject;
        hitboxObject.SetActive(true);

        Collider2D[] colliders = hitboxObject.GetComponentsInChildren<Collider2D>(true);
        for (int c = 0; c < colliders.Length; c++)
        {
            if (colliders[c] != null)
            {
                colliders[c].enabled = true;
            }
        }

        SpriteRenderer[] renderers = hitboxObject.GetComponentsInChildren<SpriteRenderer>(true);
        for (int r = 0; r < renderers.Length; r++)
        {
            if (renderers[r] != null)
            {
                renderers[r].enabled = true;
            }
        }

        Animator[] animators = hitboxObject.GetComponentsInChildren<Animator>(true);
        for (int a = 0; a < animators.Length; a++)
        {
            if (animators[a] != null)
            {
                animators[a].enabled = true;
            }
        }

        hitboxObject.SendMessage("SetDamage", meleeDamage, SendMessageOptions.DontRequireReceiver);
        hitboxObject.SendMessage("SetOwner", transform, SendMessageOptions.DontRequireReceiver);
        hitboxObject.SendMessage("ActivateHitbox", SendMessageOptions.DontRequireReceiver);
        hitboxObject.SendMessage("OpenHitbox", SendMessageOptions.DontRequireReceiver);
    }

    public void CloseMeleeHitbox()
    {
        if (meleeHitbox == null) return;

        GameObject hitboxObject = meleeHitbox.gameObject;

        hitboxObject.SendMessage("DeactivateHitbox", SendMessageOptions.DontRequireReceiver);
        hitboxObject.SendMessage("CloseHitbox", SendMessageOptions.DontRequireReceiver);
    }

    public void EndMeleeAttack()
    {
        meleeAnimationEnded = true;
    }

    public void FireUltimateProjectile()
    {
        if (isDefeated) return;
        if (combatStoppedByDeath) return;
        if (ultimateProjectileFired) return;
        if (ultimateProjectilePrefab == null) return;
        if (ultimateFirePoint == null) return;

        UpdateUltimateFirePointPosition();

        ultimateProjectileFired = true;

        Vector2 shootDirection = GetBossFacingDirection();

        GameObject projectileObject = Instantiate(
            ultimateProjectilePrefab,
            ultimateFirePoint.position,
            Quaternion.identity
        );

        Boss4UltimateProjectile projectile = projectileObject.GetComponent<Boss4UltimateProjectile>();

        if (projectile != null)
        {
            projectile.Init(shootDirection, ultimateDamage, transform, ultimateFirePoint);
        }
    }

    public void EndUltimateAnimation()
    {
        ultimateAnimationEnded = true;
    }

    public void NotifyWukongDead()
    {
        if (!stopBossWhenWukongDead) return;

        combatStoppedByDeath = true;
        StopBossCombatAndReturnIdle();
    }
    public void NotifyFirstMeleeHit()
    {
        if (hasFirstMeleeHit) return;

        hasFirstMeleeHit = true;
        ultimateTimer = ultimateCooldown;

        if (enableDebugLog)
        {
            Debug.Log(bossName + " đã đánh cận chiến trúng lần đầu. Bắt đầu hồi chiêu ulti.");
        }
    }
    public void NotifyPartyDead()
    {
        if (!stopBossWhenPartyDead) return;

        combatStoppedByDeath = true;
        StopBossCombatAndReturnIdle();
    }

    public void StopBossCombat()
    {
        combatStoppedByDeath = true;
        StopBossCombatAndReturnIdle();
    }

    public void ResumeBossCombat()
    {
        combatStoppedByDeath = false;
        isActive = activeOnStart;
        ForceIdleState(true);
    }
    public void StopBossCombatAndReturnIdle()
    {
        combatStoppedByDeath = true;

        if (actionCoroutine != null)
        {
            StopCoroutine(actionCoroutine);
            actionCoroutine = null;
        }

        isAttacking = false;
        isUsingUltimate = false;
        isMovementLocked = false;

        meleeAnimationEnded = false;
        ultimateProjectileFired = false;
        ultimateAnimationEnded = false;

        currentTarget = null;
        lockedMeleeTarget = null;
        hasLockedWorldPosition = false;

        ForceCloseAllBossHitbox();

        StopMove();

        ForceBossAnimatorToIdleOneTime();

        hasForcedIdleAfterCombatStop = true;
    }
    void ForceBossAnimatorToIdleOneTime()
    {
        if (animator == null) return;

        animator.enabled = true;

        animator.ResetTrigger(meleeTriggerName);
        animator.ResetTrigger(ultimateTriggerName);
        animator.SetFloat(speedParameterName, 0f);

        if (string.IsNullOrEmpty(idleStateName)) return;

        animator.Play(idleStateName, 0, 0f);
        animator.Update(0f);
    }
    void ForceCloseAllBossHitbox()
    {
        CloseMeleeHitbox();

        Boss4MeleeHitbox[] allMeleeHitboxes = GetComponentsInChildren<Boss4MeleeHitbox>(true);

        for (int i = 0; i < allMeleeHitboxes.Length; i++)
        {
            if (allMeleeHitboxes[i] == null) continue;

            GameObject hitboxObject = allMeleeHitboxes[i].gameObject;

            hitboxObject.SendMessage("DeactivateHitbox", SendMessageOptions.DontRequireReceiver);
            hitboxObject.SendMessage("CloseHitbox", SendMessageOptions.DontRequireReceiver);

            Collider2D[] colliders = hitboxObject.GetComponentsInChildren<Collider2D>(true);
            for (int c = 0; c < colliders.Length; c++)
            {
                if (colliders[c] != null)
                {
                    colliders[c].enabled = false;
                }
            }

            SpriteRenderer[] renderers = hitboxObject.GetComponentsInChildren<SpriteRenderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                if (renderers[r] != null)
                {
                    renderers[r].enabled = false;
                }
            }

            Animator[] animators = hitboxObject.GetComponentsInChildren<Animator>(true);
            for (int a = 0; a < animators.Length; a++)
            {
                if (animators[a] != null)
                {
                    animators[a].enabled = false;
                }
            }

            ParticleSystem[] particles = hitboxObject.GetComponentsInChildren<ParticleSystem>(true);
            for (int p = 0; p < particles.Length; p++)
            {
                if (particles[p] != null)
                {
                    particles[p].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            hitboxObject.SetActive(false);
        }
    }
    void KeepIdleAfterCombatStopped()
    {
        StopMove();

        if (animator == null) return;
        if (string.IsNullOrEmpty(idleStateName)) return;

        animator.ResetTrigger(meleeTriggerName);
        animator.ResetTrigger(ultimateTriggerName);
        animator.SetFloat(speedParameterName, 0f);

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (!stateInfo.IsName(idleStateName))
        {
            animator.CrossFade(idleStateName, 0.05f, 0, 0f);
        }
    }


    void ForceIdleState(bool restartIdle)
    {
        StopMove();

        if (animator == null) return;

        animator.ResetTrigger(meleeTriggerName);
        animator.ResetTrigger(ultimateTriggerName);
        animator.SetFloat(speedParameterName, 0f);

        if (string.IsNullOrEmpty(idleStateName)) return;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        bool isIdle = currentState.IsName(idleStateName);

        if (isIdle && !restartIdle)
        {
            return;
        }

        animator.CrossFade(idleStateName, 0.03f, 0, 0f);
    }

    void ForceIdleAfterUltimate()
    {
        StopMove();

        if (animator == null) return;
        if (string.IsNullOrEmpty(idleStateName)) return;

        animator.ResetTrigger(meleeTriggerName);
        animator.ResetTrigger(ultimateTriggerName);
        animator.SetFloat(speedParameterName, 0f);

        animator.Play(idleStateName, 0, 0f);
    }

    void SetMovementLock(bool locked)
    {
        isMovementLocked = locked;

        if (!locked)
        {
            hasLockedWorldPosition = false;
        }
    }

    void LockWorldPosition()
    {
        lockedWorldPosition = transform.position;
        hasLockedWorldPosition = true;
    }

    void MaintainLockedWorldPosition()
    {
        if (!freezeWorldPositionWhileUsingSkill) return;
        if (!hasLockedWorldPosition) return;

        transform.position = lockedWorldPosition;
    }

    void FaceTarget(Transform targetTransform)
    {
        if (targetTransform == null) return;

        float directionX = targetTransform.position.x - transform.position.x;

        FaceDirection(directionX);
    }

    void FaceDirection(float directionX)
    {
        if (Mathf.Abs(directionX) < 0.01f) return;

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

    void UpdateUltimateFirePointPosition()
    {
        if (!autoUpdateUltimateFirePoint) return;
        if (ultimateFirePoint == null) return;

        Vector2 facingDirection = GetBossFacingDirection();

        float xOffset = Mathf.Abs(ultimateFirePointLocalOffset.x) * facingDirection.x;
        float yOffset = ultimateFirePointLocalOffset.y;

        Vector3 basePosition = transform.position;

        if (spriteRenderer != null)
        {
            basePosition = spriteRenderer.bounds.center;
        }

        Vector3 spawnPosition = basePosition + new Vector3(xOffset, yOffset, 0f);

        ultimateFirePoint.position = spawnPosition;
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
        if (isDefeated) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateBossHUD();

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
        if (isDefeated) return;

        isDefeated = true;

        StopMove();

        if (actionCoroutine != null)
        {
            StopCoroutine(actionCoroutine);
            actionCoroutine = null;
        }

        CloseMeleeHitbox();

        if (animator != null)
        {
            animator.SetFloat(speedParameterName, 0f);
        }

        if (hideWhenDefeated)
        {
            gameObject.SetActive(false);
        }
    }

    void UpdateBossHUD()
    {
        FindBossHUDIfNeeded();

        if (map4BossHUD == null) return;

        if (bossId == 3 || healthUISlot == BossHealthUISlot.Boss3)
        {
            map4BossHUD.SetBoss3Health(currentHealth, maxHealth);
        }
        else if (bossId == 4 || healthUISlot == BossHealthUISlot.Boss4)
        {
            map4BossHUD.SetBoss4Health(currentHealth, maxHealth);
        }
    }

    void SyncHealthSlotByBossId()
    {
        if (bossId == 3)
        {
            healthUISlot = BossHealthUISlot.Boss3;
        }
        else if (bossId == 4)
        {
            healthUISlot = BossHealthUISlot.Boss4;
        }
    }

    void FindWukongIfNeeded()
    {
        if (wukongTarget != null) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
        {
            wukongTarget = playerObject.transform;
        }
    }

    void FindBossHUDIfNeeded()
    {
        if (map4BossHUD != null) return;

#if UNITY_2023_1_OR_NEWER
        map4BossHUD = FindFirstObjectByType<Map4BossHUDController>();
#else
        map4BossHUD = FindObjectOfType<Map4BossHUDController>();
#endif
    }

    void DebugTarget(string state)
    {
        if (!enableDebugLog) return;

        string targetName = currentTarget != null ? currentTarget.name : "NULL";
        float distance = currentTarget != null ? Vector2.Distance(transform.position, currentTarget.position) : -1f;

        Debug.Log(bossName + " | " + state + " | Target: " + targetName + " | Distance: " + distance.ToString("F2"));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, partyDetectRange);

        if (useUltimateDistanceCondition)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, minUltimateDistanceToWukong);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, maxUltimateDistanceToWukong);
        }

        DrawUltimateFirePointGizmo();
    }

    void DrawUltimateFirePointGizmo()
    {
        Vector3 basePosition = transform.position;

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();

        if (sr != null)
        {
            basePosition = sr.bounds.center;
        }

        Vector2 facingDirection = isFacingRight ? Vector2.right : Vector2.left;

        float xOffset = Mathf.Abs(ultimateFirePointLocalOffset.x) * facingDirection.x;
        float yOffset = ultimateFirePointLocalOffset.y;

        Vector3 spawnPosition = basePosition + new Vector3(xOffset, yOffset, 0f);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(spawnPosition, 0.12f);

        Gizmos.color = Color.white;
        Gizmos.DrawLine(basePosition, spawnPosition);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            spawnPosition + Vector3.up * 0.25f,
            "Projectile Spawn\nX: " + ultimateFirePointLocalOffset.x + " | Y: " + ultimateFirePointLocalOffset.y
        );
#endif
    }
    public bool IsDead()
    {
        return currentHealth <= 0;
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }
    public void ActivateCombat()
    {
        combatActivated = true;
        canReceiveDamage = true;
        canShowBossUI = true;

        if (enableDebugLog)
        {
            Debug.Log(gameObject.name + " đã được kích hoạt COMBAT.");
        }
    }

    public void DeactivateCombat()
    {
        combatActivated = false;
        canReceiveDamage = false;
        canShowBossUI = false;

        ForceBossIdleForStory();

        if (enableDebugLog)
        {
            Debug.Log(gameObject.name + " đã tắt combat, trở về Idle.");
        }
    }

    public void StopCombatAndReturnIdle()
    {
        combatActivated = false;
        canReceiveDamage = false;
        canShowBossUI = false;

        ForceBossIdleForStory();
    }

    public bool IsCombatState()
    {
        return combatActivated;
    }

    public bool CanReceiveDamage()
    {
        return canReceiveDamage;
    }

    public bool CanShowBossUI()
    {
        return canShowBossUI && combatActivated && !IsDead();
    }

    void ForceBossIdleForStory()
    {
        if (!forceIdleWhenNotCombat) return;
        if (animator == null) return;

        animator.SetFloat(speedParameterName, 0f);

        if (!string.IsNullOrEmpty(idleStateName))
        {
            animator.Play(idleStateName, 0, 0f);
            animator.Update(0f);
        }
    }
}