using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class Map3Boss2StoryManager : MonoBehaviour
{
    public enum Map3Boss2StoryState
    {
        Waiting,
        PreEnemyDialogue,
        NormalEnemyWave,
        PreBossDialogue,
        BossFight,
        PostBossDialogue,
        Finished
    }

    [Header("Current State")]
    public Map3Boss2StoryState currentState = Map3Boss2StoryState.Waiting;

    [Header("UI")]
    [Tooltip("GlobalHUD chứa máu Wukong, máu đoàn, 3 nút chiêu, cooldown.")]
    public GameObject globalHUD;

    [Tooltip("Map3HUDController nằm trên Map3BossHUD.")]
    public Map3HUDController hudController;

    [Header("Player / Party")]
    [Tooltip("Script điều khiển Ngộ Không.")]
    public PlayerController wukongController;

    [Tooltip("Rigidbody2D của Ngộ Không.")]
    public Rigidbody2D wukongRigidbody;

    [Tooltip("Animator của Ngộ Không.")]
    public Animator wukongAnimator;

    [Tooltip("Các script follow của Đường Tăng / Trư Bát Giới / Sa Tăng.")]
    public Behaviour[] partyFollowScripts;

    [Tooltip("Animator của Đường Tăng / Trư Bát Giới / Sa Tăng.")]
    public Animator[] partyAnimators;

    [Header("Wukong Animator")]
    [Tooltip("Tên state Idle thật của Wukong. Theo ảnh Animator của bạn là Wukong1Idle.")]
    public string wukongIdleStateName = "Wukong1Idle";

    [Tooltip("Tên parameter Speed trong Animator của Wukong.")]
    public string wukongSpeedParameterName = "Speed";

    [Tooltip("Các bool Animator của Wukong cần set false khi khóa thoại.")]
    public string[] wukongBoolParametersToFalse;

    [Tooltip("Các trigger Animator của Wukong cần reset khi khóa thoại.")]
    public string[] wukongTriggersToReset;

    [Header("Party Animator")]
    [Tooltip("Tên state Idle thật của đoàn. Nếu không chắc, để trống để tránh lỗi.")]
    public string partyIdleStateName = "";

    [Tooltip("Tên parameter Speed của đoàn.")]
    public string partySpeedParameterName = "Speed";

    [Tooltip("Các bool Animator của đoàn cần set false khi khóa thoại.")]
    public string[] partyBoolParametersToFalse;

    [Tooltip("Các trigger Animator của đoàn cần reset khi khóa thoại.")]
    public string[] partyTriggersToReset;

    [Header("Dialogue Controller")]
    public Map3DialogueController dialogueController;

    [Header("Phase 2 - Thoại trước enemy")]
    [Tooltip("Thoại khi Wukong chạm trigger đầu tiên.")]
    public Map3DialogueLine[] preEnemyDialogueLines;

    [Header("Enemy123 Wave")]
    [Tooltip("Spawner Enemy1 / Enemy2 / Enemy3.")]
    public Enemy123RandomSpawner enemy123Spawner;

    [Header("Phase 4 - Thoại trước boss")]
    [Tooltip("Sau khi diệt hết Enemy123, chờ Ngộ Không về Idle rồi mới hiện thoại trước boss.")]
    public bool waitWukongIdleBeforePreBossDialogue = true;

    [Tooltip("Tên state Idle thật của Wukong để chờ trước thoại boss.")]
    public string wukongIdleStateNameForDialogueWait = "Wukong1Idle";

    [Tooltip("Vận tốc nhỏ hơn số này thì coi là đứng yên.")]
    public float wukongIdleVelocityThreshold = 0.05f;

    [Tooltip("Ngộ Không phải Idle ổn định trong bao lâu.")]
    public float wukongIdleStableTime = 0.25f;

    [Tooltip("Thời gian chờ tối đa trước khi tự mở thoại để tránh kẹt.")]
    public float maxWaitWukongIdleTime = 5f;

    [Tooltip("Thoại sau khi enemy chết hết, trước khi boss đánh.")]
    public Map3DialogueLine[] preBossDialogueLines;

    [Header("Boss2")]
    public GameObject boss2Object;
    public MonoBehaviour boss2Controller;
    public Rigidbody2D boss2Rigidbody;
    public Animator boss2Animator;
    public Transform boss2Target;
    public string playerTag = "Player";

    [Tooltip("Khóa boss từ đầu, chỉ mở sau thoại trước boss.")]
    public bool lockBossAtStart = true;

    [Header("Phase 6 - Thoại sau boss chết")]
    [Tooltip("Thoại sau khi Boss2 chết. Không khóa Wukong khi hiện thoại này.")]
    public Map3DialogueLine[] postBossDialogueLines;

    [Tooltip("Fade out UI máu boss khi boss chết.")]
    public bool fadeOutBossUIOnBossDead = true;

    [Tooltip("Không ép tắt object boss. Nên bật để code boss tự xử lý chết.")]
    public bool doNotForceHideBossObject = true;

    [Header("Boss Death Check")]
    [Tooltip("Tên biến máu hiện tại trong Boss2Controller. Nếu không đúng, code sẽ thử thêm vài tên phổ biến.")]
    public string bossCurrentHealthFieldName = "currentHealth";

    public float bossDeathCheckInterval = 0.25f;

    [Header("Debug")]
    public bool enableDebugLog = true;

    private bool boss2IntroStarted;
    private bool enemyWaveStarted;
    private bool waitingBeforePreBossDialogue;
    private bool preBossDialogueStarted;
    private bool bossFightStarted;
    private bool postBossDialogueStarted;

    private Coroutine bossDeathWatchCoroutine;

    void Start()
    {
        currentState = Map3Boss2StoryState.Waiting;

        FindReferencesIfNeeded();

        if (globalHUD != null)
            globalHUD.SetActive(true);

        if (hudController != null)
        {
            hudController.HideBossUIInstant();
            hudController.HideBoxInstant();
        }

        if (dialogueController != null)
            dialogueController.HideDialogue();

        if (enemy123Spawner != null)
        {
            enemy123Spawner.isSpawning = false;
            enemy123Spawner.spawnOnStart = false;
        }

        if (lockBossAtStart)
        {
            DeactivateBoss2Combat();
        }

        Log("Map3 Boss2 Story bắt đầu. Boss UI ẩn, boss bị khóa.");
    }

    void Update()
    {
        if (currentState == Map3Boss2StoryState.NormalEnemyWave)
        {
            CheckNormalEnemyWaveFinished();
        }
    }

    void FindReferencesIfNeeded()
    {
        if (hudController == null)
            hudController = FindFirstObjectByType<Map3HUDController>();

        if (dialogueController == null)
            dialogueController = FindFirstObjectByType<Map3DialogueController>();

        if (boss2Controller == null && boss2Object != null)
        {
            Boss2Controller typedBoss2 = boss2Object.GetComponent<Boss2Controller>();

            if (typedBoss2 != null)
                boss2Controller = typedBoss2;
            else
                boss2Controller = boss2Object.GetComponent<MonoBehaviour>();
        }

        if (boss2Rigidbody == null && boss2Object != null)
            boss2Rigidbody = boss2Object.GetComponent<Rigidbody2D>();

        if (boss2Animator == null && boss2Object != null)
            boss2Animator = boss2Object.GetComponent<Animator>();

        if (boss2Target == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

            if (playerObject != null)
                boss2Target = playerObject.transform;
        }
    }

    // ======================================================
    // PHASE 2: PRE ENEMY DIALOGUE
    // Trigger hiện tại gọi hàm này.
    // ======================================================

    public void StartBoss2Intro()
    {
        if (boss2IntroStarted)
            return;

        boss2IntroStarted = true;
        currentState = Map3Boss2StoryState.PreEnemyDialogue;

        FindReferencesIfNeeded();

        LockWukongAndParty();
        DeactivateBoss2Combat();

        if (hudController != null)
            hudController.HideBossUIInstant();

        if (dialogueController != null && preEnemyDialogueLines != null && preEnemyDialogueLines.Length > 0)
        {
            dialogueController.StartDialogue(preEnemyDialogueLines, StartNormalEnemyWave);
        }
        else
        {
            StartNormalEnemyWave();
        }

        Log("Phase 2: Bắt đầu thoại trước Enemy123.");
    }

    public void StartPreEnemyDialogue()
    {
        StartBoss2Intro();
    }

    // ======================================================
    // PHASE 3: ENEMY WAVE
    // ======================================================

    void StartNormalEnemyWave()
    {
        if (enemyWaveStarted)
            return;

        enemyWaveStarted = true;
        currentState = Map3Boss2StoryState.NormalEnemyWave;

        UnlockWukongAndParty();
        DeactivateBoss2Combat();

        if (hudController != null)
        {
            hudController.HideBossUIInstant();
            hudController.HideBoxInstant();
        }

        if (enemy123Spawner != null)
        {
            enemy123Spawner.StartSpawn();
        }
        else
        {
            Debug.LogWarning("Map3Boss2StoryManager chưa gán Enemy123RandomSpawner.");
        }

        Log("Phase 3: Bắt đầu wave Enemy123.");
    }

    void CheckNormalEnemyWaveFinished()
    {
        if (waitingBeforePreBossDialogue)
            return;

        if (preBossDialogueStarted)
            return;

        if (enemy123Spawner == null)
            return;

        if (enemy123Spawner.IsSpawnFinished())
        {
            StartCoroutine(WaitWukongIdleThenStartPreBossDialogue());
        }
    }

    IEnumerator WaitWukongIdleThenStartPreBossDialogue()
    {
        waitingBeforePreBossDialogue = true;

        if (enemy123Spawner != null)
            enemy123Spawner.StopSpawn();

        float waitTimer = 0f;
        float idleTimer = 0f;

        while (waitWukongIdleBeforePreBossDialogue)
        {
            waitTimer += Time.deltaTime;

            bool isIdleReady = IsWukongIdleReadyForDialogue();

            if (isIdleReady)
            {
                idleTimer += Time.deltaTime;

                if (idleTimer >= wukongIdleStableTime)
                    break;
            }
            else
            {
                idleTimer = 0f;
            }

            if (waitTimer >= maxWaitWukongIdleTime)
            {
                Debug.LogWarning("Map3Boss2StoryManager: chờ Wukong Idle quá lâu. Tự mở thoại trước boss để tránh kẹt.");
                break;
            }

            yield return null;
        }

        StartPreBossDialogue();
    }

    bool IsWukongIdleReadyForDialogue()
    {
        if (wukongAnimator == null)
            return true;

        if (wukongAnimator.IsInTransition(0))
            return false;

        AnimatorStateInfo stateInfo = wukongAnimator.GetCurrentAnimatorStateInfo(0);

        bool isIdleState = false;

        if (!string.IsNullOrEmpty(wukongIdleStateNameForDialogueWait))
            isIdleState = stateInfo.IsName(wukongIdleStateNameForDialogueWait);
        else
            isIdleState = true;

        if (!isIdleState)
            return false;

        if (wukongRigidbody != null)
        {
            float velocityX = Mathf.Abs(wukongRigidbody.linearVelocity.x);
            float velocityY = Mathf.Abs(wukongRigidbody.linearVelocity.y);

            if (velocityX > wukongIdleVelocityThreshold)
                return false;

            if (velocityY > wukongIdleVelocityThreshold)
                return false;
        }

        return true;
    }

    // ======================================================
    // PHASE 4: PRE BOSS DIALOGUE
    // ======================================================

    public void StartPreBossDialogue()
    {
        if (preBossDialogueStarted)
            return;

        preBossDialogueStarted = true;
        currentState = Map3Boss2StoryState.PreBossDialogue;

        LockWukongAndParty();
        DeactivateBoss2Combat();

        if (hudController != null)
            hudController.HideBossUIInstant();

        if (dialogueController != null && preBossDialogueLines != null && preBossDialogueLines.Length > 0)
        {
            dialogueController.StartDialogue(preBossDialogueLines, StartBoss2Fight);
        }
        else
        {
            StartBoss2Fight();
        }

        Log("Phase 4: Bắt đầu thoại trước Boss2.");
    }

    // ======================================================
    // PHASE 5: BOSS FIGHT
    // ======================================================

    public void StartBoss2Fight()
    {
        if (bossFightStarted)
            return;

        bossFightStarted = true;
        currentState = Map3Boss2StoryState.BossFight;

        StartCoroutine(StartBoss2FightRoutine());
    }

    IEnumerator StartBoss2FightRoutine()
    {
        UnlockWukongAndParty();

        if (wukongController != null)
            wukongController.enabled = true;

        if (wukongRigidbody != null)
            wukongRigidbody.linearVelocity = Vector2.zero;

        if (partyFollowScripts != null)
        {
            for (int i = 0; i < partyFollowScripts.Length; i++)
            {
                if (partyFollowScripts[i] != null)
                    partyFollowScripts[i].enabled = true;
            }
        }

        if (hudController != null)
        {
            hudController.HideBoxInstant();
            yield return StartCoroutine(hudController.FadeInBossUI());
        }

        ActivateBoss2Combat();

        if (bossDeathWatchCoroutine != null)
            StopCoroutine(bossDeathWatchCoroutine);

        bossDeathWatchCoroutine = StartCoroutine(WatchBoss2Death());

        Log("Phase 5: Boss UI fade in xong. Boss2 bắt đầu tấn công.");
    }

    void DeactivateBoss2Combat()
    {
        if (boss2Controller != null)
        {
            boss2Controller.SendMessage("DeactivateCombat", SendMessageOptions.DontRequireReceiver);
            boss2Controller.enabled = false;
        }

        if (boss2Rigidbody != null)
        {
            boss2Rigidbody.linearVelocity = Vector2.zero;
            boss2Rigidbody.angularVelocity = 0f;
        }

        if (boss2Animator != null)
        {
            SetAnimatorFloatIfExists(boss2Animator, "Speed", 0f);
        }

        Log("Boss2 đang bị khóa.");
    }

    void ActivateBoss2Combat()
    {
        FindReferencesIfNeeded();

        if (boss2Controller != null)
        {
            boss2Controller.enabled = true;

            SetTransformFieldOrProperty(boss2Controller, "target", boss2Target);
            SetTransformFieldOrProperty(boss2Controller, "playerTarget", boss2Target);
            SetTransformFieldOrProperty(boss2Controller, "wukongTarget", boss2Target);

            SetBoolFieldOrProperty(boss2Controller, "canMove", true);
            SetBoolFieldOrProperty(boss2Controller, "canAttack", true);
            SetBoolFieldOrProperty(boss2Controller, "isActivated", true);
            SetBoolFieldOrProperty(boss2Controller, "combatActivated", true);
            SetBoolFieldOrProperty(boss2Controller, "canReceiveDamage", true);
            SetBoolFieldOrProperty(boss2Controller, "canShowBossUI", true);
            SetBoolFieldOrProperty(boss2Controller, "autoActivateByRange", false);

            boss2Controller.SendMessage("ActivateCombat", SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            Debug.LogWarning("Map3Boss2StoryManager chưa gán Boss2Controller.");
        }

        if (boss2Rigidbody != null)
        {
            boss2Rigidbody.linearVelocity = Vector2.zero;
            boss2Rigidbody.angularVelocity = 0f;
        }

        Log("Boss2 đã được mở combat.");
    }

    // ======================================================
    // PHASE 6: POST BOSS DIALOGUE
    // ======================================================

    IEnumerator WatchBoss2Death()
    {
        while (currentState == Map3Boss2StoryState.BossFight)
        {
            if (IsBoss2Dead())
            {
                StartPostBossDialogue();
                yield break;
            }

            yield return new WaitForSeconds(bossDeathCheckInterval);
        }
    }

    public void StartPostBossDialogue()
    {
        if (postBossDialogueStarted)
            return;

        postBossDialogueStarted = true;
        currentState = Map3Boss2StoryState.PostBossDialogue;

        StartCoroutine(PostBossDialogueRoutine());
    }

    IEnumerator PostBossDialogueRoutine()
    {
        // Theo yêu cầu: boss chết thì KHÔNG khóa Wukong + đoàn.
        UnlockWukongAndParty();

        if (fadeOutBossUIOnBossDead && hudController != null)
        {
            yield return StartCoroutine(hudController.FadeOutBossUI());
        }

        if (!doNotForceHideBossObject && boss2Object != null)
            boss2Object.SetActive(false);

        if (dialogueController != null && postBossDialogueLines != null && postBossDialogueLines.Length > 0)
        {
            dialogueController.StartDialogue(postBossDialogueLines, FinishStory);
        }
        else
        {
            FinishStory();
        }

        Log("Phase 6: Boss2 chết. Không khóa Wukong. Hiện thoại sau boss.");
    }

    void FinishStory()
    {
        currentState = Map3Boss2StoryState.Finished;
        UnlockWukongAndParty();

        if (hudController != null)
            hudController.HideBossUIInstant();

        Log("Map3 Boss2 Story Finished.");
    }

    bool IsBoss2Dead()
    {
        if (boss2Object == null && boss2Controller == null)
            return false;

        if (boss2Object != null && !boss2Object.activeInHierarchy)
            return true;

        if (boss2Controller == null)
            return false;

        float hp;

        if (TryGetNumberFieldOrProperty(boss2Controller, bossCurrentHealthFieldName, out hp))
            return hp <= 0f;

        if (TryGetNumberFieldOrProperty(boss2Controller, "currentHealth", out hp))
            return hp <= 0f;

        if (TryGetNumberFieldOrProperty(boss2Controller, "CurrentHealth", out hp))
            return hp <= 0f;

        if (TryGetNumberFieldOrProperty(boss2Controller, "health", out hp))
            return hp <= 0f;

        if (TryGetNumberFieldOrProperty(boss2Controller, "Health", out hp))
            return hp <= 0f;

        return false;
    }

    // ======================================================
    // LOCK / UNLOCK
    // ======================================================

    void LockWukongAndParty()
    {
        LockWukong();
        LockParty();
    }

    void UnlockWukongAndParty()
    {
        UnlockWukong();
        UnlockParty();
    }

    void LockWukong()
    {
        if (wukongRigidbody != null)
        {
            wukongRigidbody.linearVelocity = Vector2.zero;
            wukongRigidbody.angularVelocity = 0f;
        }

        if (wukongController != null)
            wukongController.enabled = false;

        ForceWukongIdle();
    }

    void UnlockWukong()
    {
        if (wukongRigidbody != null)
        {
            wukongRigidbody.linearVelocity = Vector2.zero;
            wukongRigidbody.angularVelocity = 0f;
        }

        if (wukongController != null)
            wukongController.enabled = true;
    }

    void LockParty()
    {
        if (partyFollowScripts != null)
        {
            for (int i = 0; i < partyFollowScripts.Length; i++)
            {
                if (partyFollowScripts[i] != null)
                    partyFollowScripts[i].enabled = false;
            }
        }

        ForcePartyIdle();
    }

    void UnlockParty()
    {
        if (partyFollowScripts != null)
        {
            for (int i = 0; i < partyFollowScripts.Length; i++)
            {
                if (partyFollowScripts[i] != null)
                    partyFollowScripts[i].enabled = true;
            }
        }
    }

    void ForceWukongIdle()
    {
        if (wukongAnimator == null)
            return;

        SetAnimatorFloatIfExists(wukongAnimator, wukongSpeedParameterName, 0f);

        if (wukongBoolParametersToFalse != null)
        {
            for (int i = 0; i < wukongBoolParametersToFalse.Length; i++)
                SetAnimatorBoolIfExists(wukongAnimator, wukongBoolParametersToFalse[i], false);
        }

        if (wukongTriggersToReset != null)
        {
            for (int i = 0; i < wukongTriggersToReset.Length; i++)
                ResetAnimatorTriggerIfExists(wukongAnimator, wukongTriggersToReset[i]);
        }

        if (string.IsNullOrEmpty(wukongIdleStateName))
            return;

        if (HasAnimatorState(wukongAnimator, wukongIdleStateName))
        {
            wukongAnimator.Play(wukongIdleStateName, 0, 0f);
            wukongAnimator.Update(0f);
        }
        else
        {
            Debug.LogWarning("Map3Boss2StoryManager: Wukong Animator không có state: " + wukongIdleStateName);
        }
    }

    void ForcePartyIdle()
    {
        if (partyAnimators == null)
            return;

        for (int i = 0; i < partyAnimators.Length; i++)
        {
            Animator targetAnimator = partyAnimators[i];

            if (targetAnimator == null)
                continue;

            SetAnimatorFloatIfExists(targetAnimator, partySpeedParameterName, 0f);

            if (partyBoolParametersToFalse != null)
            {
                for (int j = 0; j < partyBoolParametersToFalse.Length; j++)
                    SetAnimatorBoolIfExists(targetAnimator, partyBoolParametersToFalse[j], false);
            }

            if (partyTriggersToReset != null)
            {
                for (int j = 0; j < partyTriggersToReset.Length; j++)
                    ResetAnimatorTriggerIfExists(targetAnimator, partyTriggersToReset[j]);
            }

            if (string.IsNullOrEmpty(partyIdleStateName))
                continue;

            if (HasAnimatorState(targetAnimator, partyIdleStateName))
            {
                targetAnimator.Play(partyIdleStateName, 0, 0f);
                targetAnimator.Update(0f);
            }
            else
            {
                Debug.LogWarning("Map3Boss2StoryManager: Party Animator không có state: " + partyIdleStateName);
            }
        }
    }

    // ======================================================
    // ANIMATOR HELPERS
    // ======================================================

    void SetAnimatorFloatIfExists(Animator targetAnimator, string parameterName, float value)
    {
        if (targetAnimator == null || string.IsNullOrEmpty(parameterName))
            return;

        AnimatorControllerParameter[] parameters = targetAnimator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName && parameters[i].type == AnimatorControllerParameterType.Float)
            {
                targetAnimator.SetFloat(parameterName, value);
                return;
            }
        }
    }

    void SetAnimatorBoolIfExists(Animator targetAnimator, string parameterName, bool value)
    {
        if (targetAnimator == null || string.IsNullOrEmpty(parameterName))
            return;

        AnimatorControllerParameter[] parameters = targetAnimator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName && parameters[i].type == AnimatorControllerParameterType.Bool)
            {
                targetAnimator.SetBool(parameterName, value);
                return;
            }
        }
    }

    void ResetAnimatorTriggerIfExists(Animator targetAnimator, string parameterName)
    {
        if (targetAnimator == null || string.IsNullOrEmpty(parameterName))
            return;

        AnimatorControllerParameter[] parameters = targetAnimator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName && parameters[i].type == AnimatorControllerParameterType.Trigger)
            {
                targetAnimator.ResetTrigger(parameterName);
                return;
            }
        }
    }

    bool HasAnimatorState(Animator anim, string stateName)
    {
        if (anim == null || string.IsNullOrEmpty(stateName))
            return false;

        return anim.HasState(0, Animator.StringToHash(stateName));
    }

    // ======================================================
    // REFLECTION HELPERS
    // ======================================================

    bool SetTransformFieldOrProperty(object targetObject, string memberName, Transform value)
    {
        if (targetObject == null || value == null)
            return false;

        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        Type type = targetObject.GetType();

        FieldInfo field = type.GetField(memberName, flags);

        if (field != null && field.FieldType == typeof(Transform))
        {
            field.SetValue(targetObject, value);
            return true;
        }

        PropertyInfo property = type.GetProperty(memberName, flags);

        if (property != null && property.CanWrite && property.PropertyType == typeof(Transform))
        {
            property.SetValue(targetObject, value);
            return true;
        }

        return false;
    }

    bool SetBoolFieldOrProperty(object targetObject, string memberName, bool value)
    {
        if (targetObject == null)
            return false;

        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        Type type = targetObject.GetType();

        FieldInfo field = type.GetField(memberName, flags);

        if (field != null && field.FieldType == typeof(bool))
        {
            field.SetValue(targetObject, value);
            return true;
        }

        PropertyInfo property = type.GetProperty(memberName, flags);

        if (property != null && property.CanWrite && property.PropertyType == typeof(bool))
        {
            property.SetValue(targetObject, value);
            return true;
        }

        return false;
    }

    bool TryGetNumberFieldOrProperty(object targetObject, string memberName, out float value)
    {
        value = 0f;

        if (targetObject == null || string.IsNullOrEmpty(memberName))
            return false;

        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        Type type = targetObject.GetType();

        FieldInfo field = type.GetField(memberName, flags);

        if (field != null)
        {
            object raw = field.GetValue(targetObject);

            if (raw is int intValue)
            {
                value = intValue;
                return true;
            }

            if (raw is float floatValue)
            {
                value = floatValue;
                return true;
            }

            if (raw is double doubleValue)
            {
                value = (float)doubleValue;
                return true;
            }
        }

        PropertyInfo property = type.GetProperty(memberName, flags);

        if (property != null && property.CanRead)
        {
            object raw = property.GetValue(targetObject);

            if (raw is int intValue)
            {
                value = intValue;
                return true;
            }

            if (raw is float floatValue)
            {
                value = floatValue;
                return true;
            }

            if (raw is double doubleValue)
            {
                value = (float)doubleValue;
                return true;
            }
        }

        return false;
    }

    void Log(string message)
    {
        if (!enableDebugLog)
            return;

        Debug.Log("[Map3Boss2StoryManager] " + message + " Current State = " + currentState);
    }
}