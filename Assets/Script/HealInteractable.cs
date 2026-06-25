using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Object hồi máu dùng chung.
/// Script này chỉ xử lý:
/// - Người chơi vào vùng trigger.
/// - Hiện/tắt hint nếu có.
/// - Nhấn phím tương tác.
/// - Hồi đầy máu cho các target.
/// - Báo Notify về object khác nếu cần.
///
/// Script KHÔNG tự ẩn object hồi máu.
/// Việc ẩn/hiện object hồi máu để StoryManager hoặc cốt truyện xử lý.
/// </summary>
public class HealInteractable : MonoBehaviour
{
    [Header("Player Check")]
    [Tooltip("Tag của nhân vật có quyền tương tác.")]
    public string playerTag = "Player";

    [Tooltip("Phím tương tác để dùng vật phẩm hồi máu.")]
    public Key interactKey = Key.E;

    [Header("Heal Targets")]
    [Tooltip("Các object sẽ được hồi máu. Kéo Wukong, Đường Tăng, Bát Giới, Sa Tăng hoặc object cha chứa Health vào đây.")]
    public GameObject[] healTargets;

    [Header("Hint")]
    [Tooltip("Object gợi ý bấm E. Nên kéo HealHintCanvas vào đây. Không kéo chính object hồi máu.")]
    public GameObject interactHintObject;

    [Tooltip("Tự tắt hint khi bắt đầu scene. Chỉ tắt hint, không tắt object hồi máu.")]
    public bool hideHintOnStart = true;

    [Tooltip("Khi Pause Game thì ẩn hint bấm E.")]
    public bool hideHintWhenPaused = true;

    [Header("Use Control")]
    [Tooltip("Chỉ cho dùng một lần.")]
    public bool useOnlyOnce = true;

    [Tooltip("Sau khi dùng xong thì khóa script tương tác, nhưng không ẩn object hồi máu.")]
    public bool disableInteractAfterUse = true;

    [Header("Pause Control")]
    [Tooltip("Bật lên để khi Pause Game thì không cho nhấn E hồi máu.")]
    public bool blockInputWhenGamePaused = true;

    [Tooltip("Sau khi Resume, bắt người chơi nhả phím E rồi mới cho nhận E tiếp. Tránh vừa Resume đã dùng vật phẩm ngay.")]
    public bool waitKeyReleaseAfterResume = true;

    [Header("Optional Notify")]
    [Tooltip("Object nhận thông báo sau khi hồi máu xong. Có thể kéo StoryManager vào nếu map cần đổi phase.")]
    public GameObject notifyObject;

    [Tooltip("Tên hàm sẽ gọi trên Notify Object sau khi hồi máu xong.")]
    public string notifyMessageName = "NotifyHealUsed";

    [Header("Debug")]
    public bool enableDebugLog = true;

    private bool playerInside;
    private bool hasUsed;

    private bool wasPausedLastFrame;
    private bool waitingKeyReleaseAfterPause;

    private void Awake()
    {
        SetupHintOnStart();
    }

    private void Update()
    {
        if (IsGamePausedAndBlocked())
        {
            HandlePausedState();
            return;
        }

        HandleResumeFromPauseState();

        if (!playerInside)
        {
            return;
        }

        if (hasUsed && useOnlyOnce)
        {
            return;
        }

        if (waitingKeyReleaseAfterPause)
        {
            if (!IsKeyPressed(interactKey))
            {
                waitingKeyReleaseAfterPause = false;
            }

            return;
        }

        if (WasKeyPressed(interactKey))
        {
            UseHealItem();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        if (!other.CompareTag(playerTag))
        {
            return;
        }

        if (hasUsed && useOnlyOnce)
        {
            return;
        }

        playerInside = true;

        if (!IsGamePausedAndBlocked())
        {
            SetHintActive(true);
        }
        else if (hideHintWhenPaused)
        {
            SetHintActive(false);
        }

        if (enableDebugLog)
        {
            Debug.Log(gameObject.name + ": Người chơi đã vào vùng hồi máu. Nhấn " + interactKey + " để hồi máu.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInside = false;
        waitingKeyReleaseAfterPause = false;
        SetHintActive(false);
    }

    private void HandlePausedState()
    {
        wasPausedLastFrame = true;

        if (hideHintWhenPaused)
        {
            SetHintActive(false);
        }
    }

    private void HandleResumeFromPauseState()
    {
        if (!wasPausedLastFrame)
        {
            return;
        }

        wasPausedLastFrame = false;

        if (waitKeyReleaseAfterResume)
        {
            waitingKeyReleaseAfterPause = IsKeyPressed(interactKey);
        }
        else
        {
            waitingKeyReleaseAfterPause = false;
        }

        if (playerInside && !(hasUsed && useOnlyOnce))
        {
            SetHintActive(true);
        }

        if (enableDebugLog)
        {
            Debug.Log(gameObject.name + ": Đã Resume khỏi Pause. Chờ nhả phím tương tác = " + waitingKeyReleaseAfterPause);
        }
    }

    private void SetupHintOnStart()
    {
        if (!hideHintOnStart)
        {
            return;
        }

        SetHintActive(false);
    }

    private void SetHintActive(bool active)
    {
        if (interactHintObject == null)
        {
            return;
        }

        // Chống gán nhầm chính object hồi máu vào ô Hint.
        // Nếu gán nhầm như vậy, không được tắt chính object này.
        if (interactHintObject == gameObject)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning(
                    gameObject.name + ": Interact Hint Object đang gán nhầm chính object hồi máu. " +
                    "Hãy kéo HealHintCanvas vào ô Interact Hint Object, không kéo chính object hồi máu."
                );
            }

            return;
        }

        interactHintObject.SetActive(active);
    }

    private bool WasKeyPressed(Key key)
    {
        if (IsGamePausedAndBlocked())
        {
            return false;
        }

        if (Keyboard.current == null)
        {
            return false;
        }

        KeyControl keyControl = Keyboard.current[key];

        return keyControl != null && keyControl.wasPressedThisFrame;
    }

    private bool IsKeyPressed(Key key)
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        KeyControl keyControl = Keyboard.current[key];

        return keyControl != null && keyControl.isPressed;
    }

    private bool IsGamePausedAndBlocked()
    {
        if (!blockInputWhenGamePaused)
        {
            return false;
        }

        return PauseMenuController.IsPausedGlobal;
    }

    private void UseHealItem()
    {
        if (IsGamePausedAndBlocked())
        {
            return;
        }

        if (hasUsed && useOnlyOnce)
        {
            return;
        }

        hasUsed = true;

        if (healTargets != null)
        {
            for (int i = 0; i < healTargets.Length; i++)
            {
                HealTargetToFull(healTargets[i]);
            }
        }

        SetHintActive(false);

        if (enableDebugLog)
        {
            Debug.Log(gameObject.name + ": Đã hồi đầy máu cho toàn bộ Heal Targets.");
        }

        NotifyAfterHeal();

        if (disableInteractAfterUse)
        {
            enabled = false;
        }
    }

    private void NotifyAfterHeal()
    {
        if (notifyObject == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(notifyMessageName))
        {
            return;
        }

        notifyObject.SendMessage(
            notifyMessageName,
            SendMessageOptions.DontRequireReceiver
        );
    }

    private void HealTargetToFull(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return;
        }

        Component[] components = targetObject.GetComponentsInChildren<Component>(true);

        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];

            if (component == null)
            {
                continue;
            }

            string typeName = component.GetType().Name;

            if (!typeName.ToLower().Contains("health"))
            {
                continue;
            }

            bool healedByMethod = TryCallFullHealMethod(component);

            if (!healedByMethod)
            {
                TrySetHealthFieldsToMax(component);
            }

            TryRefreshHealthUI(component);

            if (enableDebugLog)
            {
                Debug.Log("HealInteractable: Đã xử lý hồi máu cho " + typeName + " trên " + targetObject.name);
            }
        }
    }

    private bool TryCallFullHealMethod(Component healthComponent)
    {
        string[] methodNames =
        {
            "HealFull",
            "FullHeal",
            "HealToFull",
            "RestoreFullHealth",
            "RestoreToFullHealth",
            "SetFullHealth",
            "ResetHealth",
            "RecoverFullHealth"
        };

        Type type = healthComponent.GetType();

        for (int i = 0; i < methodNames.Length; i++)
        {
            MethodInfo method = type.GetMethod(
                methodNames[i],
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (method == null)
            {
                continue;
            }

            ParameterInfo[] parameters = method.GetParameters();

            if (parameters.Length == 0)
            {
                method.Invoke(healthComponent, null);
                return true;
            }
        }

        return false;
    }

    private void TrySetHealthFieldsToMax(Component healthComponent)
    {
        Type type = healthComponent.GetType();

        FieldInfo currentField = FindField(type, "currentHealth", "currentHp", "currentHP", "health", "hp");
        FieldInfo maxField = FindField(type, "maxHealth", "maxHp", "maxHP");

        if (currentField != null && maxField != null)
        {
            object maxValue = maxField.GetValue(healthComponent);
            currentField.SetValue(healthComponent, maxValue);
            return;
        }

        PropertyInfo currentProperty = FindProperty(type, "CurrentHealth", "CurrentHp", "CurrentHP", "Health", "HP");
        PropertyInfo maxProperty = FindProperty(type, "MaxHealth", "MaxHp", "MaxHP");

        if (currentProperty != null && maxProperty != null && currentProperty.CanWrite)
        {
            object maxValue = maxProperty.GetValue(healthComponent);
            currentProperty.SetValue(healthComponent, maxValue);
        }
    }

    private FieldInfo FindField(Type type, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            FieldInfo field = type.GetField(
                names[i],
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (field != null)
            {
                return field;
            }
        }

        return null;
    }

    private PropertyInfo FindProperty(Type type, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            PropertyInfo property = type.GetProperty(
                names[i],
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (property != null)
            {
                return property;
            }
        }

        return null;
    }

    private void TryRefreshHealthUI(Component healthComponent)
    {
        string[] methodNames =
        {
            "UpdateHealthUI",
            "UpdateUI",
            "RefreshUI",
            "RefreshHealthUI",
            "UpdateHUD",
            "RefreshHUD"
        };

        Type type = healthComponent.GetType();

        for (int i = 0; i < methodNames.Length; i++)
        {
            MethodInfo method = type.GetMethod(
                methodNames[i],
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (method == null)
            {
                continue;
            }

            ParameterInfo[] parameters = method.GetParameters();

            if (parameters.Length == 0)
            {
                method.Invoke(healthComponent, null);
            }
        }
    }
}