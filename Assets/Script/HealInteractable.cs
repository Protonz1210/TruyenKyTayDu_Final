using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Object hồi máu dùng chung.
/// Gắn script này lên bất kỳ vật phẩm / bàn ăn / bình thuốc nào.
/// Người chơi đi vào vùng trigger và bấm phím tương tác thì sẽ hồi đầy máu cho các target được gán.
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
    [Tooltip("Object gợi ý bấm E. Có thể là TextMeshPro, Sprite, UI nhỏ. Nếu không dùng thì bỏ trống.")]
    public GameObject interactHintObject;

    [Tooltip("Ẩn hint khi bắt đầu scene.")]
    public bool hideHintOnStart = true;

    [Header("After Use")]
    [Tooltip("Sau khi hồi máu xong thì ẩn object hồi máu.")]
    public bool hideHealObjectAfterUse = true;

    [Tooltip("Chỉ cho dùng một lần.")]
    public bool useOnlyOnce = true;

    [Header("Optional Notify")]
    [Tooltip("Object nhận thông báo sau khi hồi máu xong. Có thể kéo StoryManager vào nếu map cần đổi phase.")]
    public GameObject notifyObject;

    [Tooltip("Tên hàm sẽ gọi trên Notify Object sau khi hồi máu xong.")]
    public string notifyMessageName = "NotifyHealUsed";

    [Header("Debug")]
    public bool enableDebugLog = true;

    private bool playerInside;
    private bool hasUsed;

    private void Awake()
    {
        if (hideHintOnStart && interactHintObject != null)
        {
            interactHintObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!playerInside)
        {
            return;
        }

        if (hasUsed && useOnlyOnce)
        {
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

        playerInside = true;

        if (interactHintObject != null)
        {
            interactHintObject.SetActive(true);
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

        if (interactHintObject != null)
        {
            interactHintObject.SetActive(false);
        }
    }

    private bool WasKeyPressed(Key key)
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        KeyControl keyControl = Keyboard.current[key];

        return keyControl != null && keyControl.wasPressedThisFrame;
    }

    private void UseHealItem()
    {
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

        if (interactHintObject != null)
        {
            interactHintObject.SetActive(false);
        }

        NotifyAfterHeal();

        if (enableDebugLog)
        {
            Debug.Log(gameObject.name + ": Đã hồi đầy máu cho toàn bộ Heal Targets.");
        }

        if (hideHealObjectAfterUse)
        {
            gameObject.SetActive(false);
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