using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class Map3Boss2StoryManager : MonoBehaviour
{
    public enum Map3Boss2StoryState
    {
        Waiting,
        Dialogue,
        NormalEnemyWave,
        BossFight,
        Finished
    }

    [Header("Current State")]
    public Map3Boss2StoryState currentState = Map3Boss2StoryState.Waiting;

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
    [Tooltip("Tên state Idle thật của Ngộ Không.")]
    public string wukongIdleStateName = "Idle";

    [Tooltip("Tên parameter Speed trong Animator của Ngộ Không.")]
    public string wukongSpeedParameterName = "Speed";

    [Tooltip("Các bool Animator của Ngộ Không cần set false khi khóa thoại.")]
    public string[] wukongBoolParametersToFalse;

    [Tooltip("Các trigger Animator của Ngộ Không cần reset khi khóa thoại.")]
    public string[] wukongTriggersToReset;

    [Header("Party Animator")]
    [Tooltip("Tên state Idle thật của đoàn.")]
    public string partyIdleStateName = "Idle";

    [Tooltip("Tên parameter Speed của đoàn.")]
    public string partySpeedParameterName = "Speed";

    [Tooltip("Các bool Animator của đoàn cần set false khi khóa thoại.")]
    public string[] partyBoolParametersToFalse;

    [Tooltip("Các trigger Animator của đoàn cần reset khi khóa thoại.")]
    public string[] partyTriggersToReset;

    [Header("Dialogue")]
    [Tooltip("Dialogue riêng của map bạn.")]
    public Map3DialogueController dialogueController;

    [Tooltip("Hội thoại khi gặp Boss2 Sài Thái Tuế.")]
    public Map3DialogueLine[] boss2IntroLines;

    [Header("Enemy123 Wave")]
    [Tooltip("Spawner Enemy1 / Enemy2 / Enemy3. Dùng trực tiếp giống Map4 của Quân.")]
    public Enemy123RandomSpawner enemy123Spawner;

    [Header("Before Boss Release")]
    [Tooltip("Sau khi diệt hết Enemy123, chờ Ngộ Không về Idle rồi mới mở Boss2.")]
    public bool waitWukongIdleBeforeBossFight = true;

    [Tooltip("Tên state Idle thật của Ngộ Không để chờ trước khi mở Boss2.")]
    public string wukongIdleStateNameForDialogueWait = "Idle";

    [Tooltip("Vận tốc nhỏ hơn số này thì coi như đứng yên.")]
    public float wukongIdleVelocityThreshold = 0.05f;

    [Tooltip("Ngộ Không phải Idle ổn định trong bao lâu mới mở Boss2.")]
    public float wukongIdleStableTime = 0.25f;

    [Tooltip("Thời gian chờ tối đa trước khi tự mở Boss2 để tránh kẹt.")]
    public float maxWaitWukongIdleTime = 5f;

    [Header("Boss2")]
    [Tooltip("Object Boss2 Sài Thái Tuế.")]
    public GameObject boss2Object;

    [Tooltip("Controller của Boss2.")]
    public MonoBehaviour boss2Controller;

    [Tooltip("Rigidbody2D của Boss2.")]
    public Rigidbody2D boss2Rigidbody;

    [Tooltip("Animator của Boss2.")]
    public Animator boss2Animator;

    [Tooltip("Target của Boss2, thường là PF_WukongPlayer.")]
    public Transform boss2Target;

    [Tooltip("Tag của Ngộ Không.")]
    public string playerTag = "Player";

    [Tooltip("Khóa Boss2 từ đầu, chỉ mở sau khi hết quái nhỏ.")]
    public bool lockBossAtStart = true;

    [Header("Debug")]
    public bool enableDebugLog = true;

    private bool boss2IntroStarted;
    private bool enemyWaveStarted;
    private bool waitingBeforeBossFight;
    private bool bossFightStarted;

    void Start()
    {
        currentState = Map3Boss2StoryState.Waiting;

        FindReferencesIfNeeded();

        if (enemy123Spawner != null)
        {
            enemy123Spawner.isSpawning = false;
            enemy123Spawner.spawnOnStart = false;
        }

        if (lockBossAtStart)
        {
            DeactivateBoss2Combat();
        }

        Log("Map3 Boss2 Story bắt đầu.");
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
        if (dialogueController == null)
        {
            dialogueController = FindFirstObjectByType<Map3DialogueController>();
        }

        if (boss2Controller == null && boss2Object != null)
        {
            Boss2Controller typedBoss2 = boss2Object.GetComponent<Boss2Controller>();

            if (typedBoss2 != null)
            {
                boss2Controller = typedBoss2;
            }
            else
            {
                boss2Controller = boss2Object.GetComponent<MonoBehaviour>();
            }
        }

        if (boss2Rigidbody == null && boss2Object != null)
        {
            boss2Rigidbody = boss2Object.GetComponent<Rigidbody2D>();
        }

        if (boss2Animator == null && boss2Object != null)
        {
            boss2Animator = boss2Object.GetComponent<Animator>();
        }

        if (boss2Target == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

            if (playerObject != null)
            {
                boss2Target = playerObject.transform;
            }
        }
    }

    public void StartBoss2Intro()
    {
        if (boss2IntroStarted)
            return;

        boss2IntroStarted = true;
        currentState = Map3Boss2StoryState.Dialogue;

        FindReferencesIfNeeded();

        LockWukongAndParty();
        DeactivateBoss2Combat();

        if (dialogueController != null && boss2IntroLines != null && boss2IntroLines.Length > 0)
        {
            dialogueController.StartDialogue(boss2IntroLines, StartNormalEnemyWave);
        }
        else
        {
            StartNormalEnemyWave();
        }

        Log("Bắt đầu hội thoại Boss2.");
    }

    void StartNormalEnemyWave()
    {
        if (enemyWaveStarted)
            return;

        enemyWaveStarted = true;
        currentState = Map3Boss2StoryState.NormalEnemyWave;

        UnlockWukongAndParty();
        DeactivateBoss2Combat();

        if (enemy123Spawner != null)
        {
            enemy123Spawner.StartSpawn();
        }
        else
        {
            Debug.LogWarning("Map3Boss2StoryManager chưa gán Enemy123RandomSpawner.");
        }

        Log("Bắt đầu wave Enemy123 trước Boss2.");
    }

    void CheckNormalEnemyWaveFinished()
    {
        if (waitingBeforeBossFight)
            return;

        if (bossFightStarted)
            return;

        if (enemy123Spawner == null)
            return;

        if (enemy123Spawner.IsSpawnFinished())
        {
            StartCoroutine(WaitWukongIdleThenStartBossFight());
        }
    }

    IEnumerator WaitWukongIdleThenStartBossFight()
    {
        waitingBeforeBossFight = true;

        if (enemy123Spawner != null)
        {
            enemy123Spawner.StopSpawn();
        }

        float waitTimer = 0f;
        float idleTimer = 0f;

        while (waitWukongIdleBeforeBossFight)
        {
            waitTimer += Time.deltaTime;

            bool isIdleReady = IsWukongIdleReadyForBossFight();

            if (isIdleReady)
            {
                idleTimer += Time.deltaTime;

                if (idleTimer >= wukongIdleStableTime)
                {
                    break;
                }
            }
            else
            {
                idleTimer = 0f;
            }

            if (waitTimer >= maxWaitWukongIdleTime)
            {
                Debug.LogWarning("Map3Boss2StoryManager: chờ Wukong về Idle quá lâu. Tự mở Boss2 để tránh kẹt.");
                break;
            }

            yield return null;
        }

        StartBoss2Fight();
    }

    bool IsWukongIdleReadyForBossFight()
    {
        if (wukongAnimator == null)
        {
            return true;
        }

        if (wukongAnimator.IsInTransition(0))
        {
            return false;
        }

        AnimatorStateInfo stateInfo = wukongAnimator.GetCurrentAnimatorStateInfo(0);

        bool isIdleState = false;

        if (!string.IsNullOrEmpty(wukongIdleStateNameForDialogueWait))
        {
            isIdleState = stateInfo.IsName(wukongIdleStateNameForDialogueWait);
        }

        if (!isIdleState)
        {
            return false;
        }

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

    public void StartBoss2Fight()
    {
        if (bossFightStarted)
            return;

        bossFightStarted = true;
        currentState = Map3Boss2StoryState.BossFight;

        UnlockWukongAndParty();

        if (wukongController != null)
        {
            wukongController.enabled = true;
        }

        if (wukongRigidbody != null)
        {
            wukongRigidbody.linearVelocity = Vector2.zero;
        }

        if (partyFollowScripts != null)
        {
            for (int i = 0; i < partyFollowScripts.Length; i++)
            {
                if (partyFollowScripts[i] != null)
                {
                    partyFollowScripts[i].enabled = true;
                }
            }
        }

        ActivateBoss2Combat();

        Log("Hết Enemy123. Boss2 bắt đầu tấn công Ngộ Không.");
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

            SetBoolFieldOrProperty(boss2Controller, "canMove", true);
            SetBoolFieldOrProperty(boss2Controller, "canAttack", true);
            SetBoolFieldOrProperty(boss2Controller, "isActivated", true);

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
        {
            wukongController.enabled = false;
        }

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
        {
            wukongController.enabled = true;
        }
    }

    void LockParty()
    {
        if (partyFollowScripts != null)
        {
            for (int i = 0; i < partyFollowScripts.Length; i++)
            {
                if (partyFollowScripts[i] != null)
                {
                    partyFollowScripts[i].enabled = false;
                }
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
                {
                    partyFollowScripts[i].enabled = true;
                }
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
            {
                SetAnimatorBoolIfExists(wukongAnimator, wukongBoolParametersToFalse[i], false);
            }
        }

        if (wukongTriggersToReset != null)
        {
            for (int i = 0; i < wukongTriggersToReset.Length; i++)
            {
                ResetAnimatorTriggerIfExists(wukongAnimator, wukongTriggersToReset[i]);
            }
        }

        if (!string.IsNullOrEmpty(wukongIdleStateName))
        {
            wukongAnimator.Play(wukongIdleStateName, 0, 0f);
            wukongAnimator.Update(0f);
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
                {
                    SetAnimatorBoolIfExists(targetAnimator, partyBoolParametersToFalse[j], false);
                }
            }

            if (partyTriggersToReset != null)
            {
                for (int j = 0; j < partyTriggersToReset.Length; j++)
                {
                    ResetAnimatorTriggerIfExists(targetAnimator, partyTriggersToReset[j]);
                }
            }

            if (!string.IsNullOrEmpty(partyIdleStateName))
            {
                targetAnimator.Play(partyIdleStateName, 0, 0f);
                targetAnimator.Update(0f);
            }
        }
    }

    void SetAnimatorFloatIfExists(Animator targetAnimator, string parameterName, float value)
    {
        if (targetAnimator == null)
            return;

        if (string.IsNullOrEmpty(parameterName))
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
        if (targetAnimator == null)
            return;

        if (string.IsNullOrEmpty(parameterName))
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
        if (targetAnimator == null)
            return;

        if (string.IsNullOrEmpty(parameterName))
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

    bool SetTransformFieldOrProperty(object targetObject, string memberName, Transform value)
    {
        if (targetObject == null || value == null)
            return false;

        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        System.Type type = targetObject.GetType();

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
        System.Type type = targetObject.GetType();

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

    void Log(string message)
    {
        if (!enableDebugLog)
            return;

        Debug.Log("[Map3Boss2StoryManager] " + message + " Current State = " + currentState);
    }
}