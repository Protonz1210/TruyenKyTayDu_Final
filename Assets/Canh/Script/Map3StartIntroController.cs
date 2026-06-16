using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Map3StartIntroController : MonoBehaviour
{
    [Header("UI tổng")]
    [Tooltip("GlobalHUD chứa máu Wukong, máu đoàn, skill, hộp thoại thật.")]
    public GameObject globalHUD;

    [Header("HUD tên map")]
    [Tooltip("Map3HUDController nằm trên Map3BossHUD, dùng để hiện ĐỘNG KỲ LÂN.")]
    public Map3HUDController map3HUD;

    [Header("Tên map đầu màn")]
    [TextArea]
    public string mapTitle = "ĐỘNG\nKỲ\nLÂN";

    [Header("Timing")]
    public float delayBeforeTitle = 0.2f;

    [Header("Wukong")]
    public PlayerController wukongController;
    public Rigidbody2D wukongRigidbody;
    public Animator wukongAnimator;
    public PlayerInput wukongPlayerInput;

    [Header("Animator Idle")]
    [Tooltip("Tên state Idle thật trong Animator của Ngộ Không.")]
    public string wukongIdleStateName = "Wukong1Idle";

    [Tooltip("Tên parameter Speed trong Animator.")]
    public string speedParameterName = "Speed";

    [Tooltip("Tên parameter vận tốc dọc nếu Animator có.")]
    public string verticalVelocityParameterName = "VerticalVelocity";

    [Header("Các bool cần ép khi đứng Idle")]
    [Tooltip("Những bool này sẽ bị ép TRUE để Animator không chuyển sang Jump/Fall.")]
    public string[] boolParametersToTrue =
    {
        "IsGrounded",
        "Grounded",
        "isGrounded"
    };

    [Tooltip("Những bool này sẽ bị ép FALSE để Animator không chuyển sang Jump/Attack.")]
    public string[] boolParametersToFalse =
    {
        "IsJumping",
        "Jump",
        "isJumping",
        "IsAttacking",
        "Attacking",
        "Attack"
    };

    private float cachedGravityScale;
    private RigidbodyConstraints2D cachedConstraints;
    private bool hasCachedPhysics;

    private IEnumerator Start()
    {
        // 1. Ẩn UI tổng khi vừa vào map.
        if (globalHUD != null)
            globalHUD.SetActive(false);

        // 2. Ẩn boss UI + box tên map trước.
        if (map3HUD != null)
        {
            map3HUD.HideBossUIInstant();
            map3HUD.HideBoxInstant();
        }

        // 3. Khóa Ngộ Không, nhưng Animator vẫn chạy Idle bình thường.
        LockWukongButKeepIdleAnimation();

        yield return new WaitForSeconds(delayBeforeTitle);

        // 4. Hiện tên map: fade in -> giữ 5s -> fade out.
        if (map3HUD != null)
            yield return StartCoroutine(map3HUD.PlayLocationTitle(mapTitle));

        // 5. Sau khi tên map biến mất, hiện lại UI tổng.
        if (globalHUD != null)
            globalHUD.SetActive(true);

        // 6. Boss UI vẫn ẩn.
        if (map3HUD != null)
        {
            map3HUD.HideBossUIInstant();
            map3HUD.HideBoxInstant();
        }

        // 7. Mở lại điều khiển.
        UnlockWukong();
    }

    private void LockWukongButKeepIdleAnimation()
    {
        // Khóa input.
        if (wukongPlayerInput != null)
            wukongPlayerInput.enabled = false;

        // Tắt PlayerController để không nhận move/jump/attack.
        if (wukongController != null)
            wukongController.enabled = false;

        // Khóa vật lý để không rơi nhẹ rồi bị Animator hiểu là Jump/Fall.
        if (wukongRigidbody != null)
        {
            cachedGravityScale = wukongRigidbody.gravityScale;
            cachedConstraints = wukongRigidbody.constraints;
            hasCachedPhysics = true;

            wukongRigidbody.linearVelocity = Vector2.zero;
            wukongRigidbody.angularVelocity = 0f;
            wukongRigidbody.gravityScale = 0f;

            wukongRigidbody.constraints =
                RigidbodyConstraints2D.FreezePositionX |
                RigidbodyConstraints2D.FreezePositionY |
                RigidbodyConstraints2D.FreezeRotation;
        }

        // Ép Animator về Idle nhưng KHÔNG freeze Animator.
        ForceWukongIdleLoop();
    }

    private void UnlockWukong()
    {
        // Mở lại vật lý.
        if (wukongRigidbody != null && hasCachedPhysics)
        {
            wukongRigidbody.linearVelocity = Vector2.zero;
            wukongRigidbody.angularVelocity = 0f;
            wukongRigidbody.gravityScale = cachedGravityScale;
            wukongRigidbody.constraints = cachedConstraints;
        }

        // Mở lại controller.
        if (wukongController != null)
            wukongController.enabled = true;

        // Mở lại input.
        if (wukongPlayerInput != null)
            wukongPlayerInput.enabled = true;
    }

    private void ForceWukongIdleLoop()
    {
        if (wukongAnimator == null)
            return;

        // Animator vẫn chạy để Idle loop hoạt động.
        wukongAnimator.speed = 1f;

        // Reset toàn bộ trigger cũ: Jump, Attack...
        ResetAllTriggers(wukongAnimator);

        // Ép parameter về trạng thái đứng yên.
        SetAnimatorFloatIfExists(wukongAnimator, speedParameterName, 0f);
        SetAnimatorFloatIfExists(wukongAnimator, verticalVelocityParameterName, 0f);

        if (boolParametersToTrue != null)
        {
            foreach (string param in boolParametersToTrue)
                SetAnimatorBoolIfExists(wukongAnimator, param, true);
        }

        if (boolParametersToFalse != null)
        {
            foreach (string param in boolParametersToFalse)
                SetAnimatorBoolIfExists(wukongAnimator, param, false);
        }

        // Ép về đúng state Idle thật.
        if (!string.IsNullOrEmpty(wukongIdleStateName) &&
            wukongAnimator.HasState(0, Animator.StringToHash(wukongIdleStateName)))
        {
            wukongAnimator.Play(wukongIdleStateName, 0, 0f);
            wukongAnimator.Update(0f);
        }
        else
        {
            Debug.LogWarning("Map3StartIntroController: Không tìm thấy state Idle: " + wukongIdleStateName);
        }
    }

    private void ResetAllTriggers(Animator anim)
    {
        if (anim == null)
            return;

        foreach (AnimatorControllerParameter p in anim.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger)
                anim.ResetTrigger(p.name);
        }
    }

    private void SetAnimatorFloatIfExists(Animator anim, string paramName, float value)
    {
        if (anim == null || string.IsNullOrEmpty(paramName))
            return;

        foreach (AnimatorControllerParameter p in anim.parameters)
        {
            if (p.name == paramName && p.type == AnimatorControllerParameterType.Float)
            {
                anim.SetFloat(paramName, value);
                return;
            }
        }
    }

    private void SetAnimatorBoolIfExists(Animator anim, string paramName, bool value)
    {
        if (anim == null || string.IsNullOrEmpty(paramName))
            return;

        foreach (AnimatorControllerParameter p in anim.parameters)
        {
            if (p.name == paramName && p.type == AnimatorControllerParameterType.Bool)
            {
                anim.SetBool(paramName, value);
                return;
            }
        }
    }
}