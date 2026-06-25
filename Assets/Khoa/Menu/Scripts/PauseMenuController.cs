using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PauseMenuController : MonoBehaviour
{
    public static bool IsPausedGlobal { get; private set; }

    [Header("Root")]
    [Tooltip("Object cha chứa toàn bộ UI Pause. Kéo object PAUSE vào đây.")]
    public GameObject pauseRoot;

    [Tooltip("Ẩn Pause UI khi bắt đầu scene.")]
    public bool hideOnStart = true;

    [Header("Buttons")]
    public Button continueButton;
    public Button exitToMenuButton;

    [Header("Input")]
    public bool useTabKey = true;

    [Header("Scene")]
    public string menuSceneName = "MainMenu";

    [Header("GameOver Check")]
    public GameOverMenuController gameOverMenuController;

    [Header("Freeze Gameplay While Paused")]
    [Tooltip("Tự tìm và khóa PlayerController / FollowerController khi pause.")]
    public bool freezeGameplayScriptsWhenPaused = true;

    [Tooltip("Tên các script gameplay cần khóa khi pause.")]
    public string[] gameplayScriptNamesToFreeze =
    {
        "PlayerController",
        "FollowerController"
    };

    [Tooltip("Dừng velocity của Rigidbody2D khi pause.")]
    public bool stopRigidbodyVelocityWhenPaused = true;

    [Tooltip("Ép Animator Speed = 0 khi pause để tránh nhân vật kẹt trạng thái chạy.")]
    public bool setAnimatorSpeedToZeroWhenPaused = true;

    [Header("Cursor")]
    public bool showCursorWhenPaused = true;
    public bool hideCursorWhenResume = false;

    [Header("Debug")]
    public bool enableDebugLog = true;

    private bool isPaused;

    private readonly Dictionary<Behaviour, bool> cachedBehaviourEnabledState = new Dictionary<Behaviour, bool>();
    private readonly List<Rigidbody2D> cachedRigidbodies = new List<Rigidbody2D>();
    private readonly List<Animator> cachedAnimators = new List<Animator>();

    private void Awake()
    {
        IsPausedGlobal = false;

        BindButtons();

        if (hideOnStart)
        {
            HidePauseImmediate();
        }

        if (enableDebugLog)
        {
            Debug.Log("PauseMenuController: Awake đã chạy trên object = " + gameObject.name);
        }
    }

    private void OnEnable()
    {
        BindButtons();
    }

    private void Update()
    {
        if (!useTabKey)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private void BindButtons()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(ResumeGame);
            continueButton.onClick.AddListener(ResumeGame);
        }

        if (exitToMenuButton != null)
        {
            exitToMenuButton.onClick.RemoveListener(ExitToMenu);
            exitToMenuButton.onClick.AddListener(ExitToMenu);
        }
    }

    public void TogglePause()
    {
        if (IsGameOverShowing())
        {
            if (enableDebugLog)
            {
                Debug.Log("PauseMenuController: GameOver đang hiện, không cho Pause.");
            }

            return;
        }

        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (isPaused)
        {
            return;
        }

        if (IsGameOverShowing())
        {
            return;
        }

        isPaused = true;
        IsPausedGlobal = true;

        if (freezeGameplayScriptsWhenPaused)
        {
            CacheAndFreezeGameplayObjects();
        }

        if (pauseRoot != null)
        {
            pauseRoot.SetActive(true);
        }
        else
        {
            Debug.LogWarning("PauseMenuController: Chưa gán Pause Root.");
        }

        BindButtons();

        Time.timeScale = 0f;

        if (showCursorWhenPaused)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        if (enableDebugLog)
        {
            Debug.Log("PauseMenuController: Đã Pause game.");
        }
    }

    public void ResumeGame()
    {
        if (!isPaused)
        {
            return;
        }

        isPaused = false;
        IsPausedGlobal = false;

        Time.timeScale = 1f;

        RestoreGameplayObjectsAfterPause();

        if (pauseRoot != null)
        {
            pauseRoot.SetActive(false);
        }

        if (hideCursorWhenResume)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (enableDebugLog)
        {
            Debug.Log("PauseMenuController: Đã Continue game.");
        }
    }

    public void ExitToMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        IsPausedGlobal = false;

        if (string.IsNullOrEmpty(menuSceneName))
        {
            Debug.LogWarning("PauseMenuController: Chưa nhập Menu Scene Name.");
            return;
        }

        if (enableDebugLog)
        {
            Debug.Log("PauseMenuController: Thoát về menu scene " + menuSceneName);
        }

        SceneManager.LoadScene(menuSceneName);
    }

    private void HidePauseImmediate()
    {
        isPaused = false;
        IsPausedGlobal = false;

        if (pauseRoot != null)
        {
            pauseRoot.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    private void CacheAndFreezeGameplayObjects()
    {
        cachedBehaviourEnabledState.Clear();
        cachedRigidbodies.Clear();
        cachedAnimators.Clear();

#if UNITY_2023_1_OR_NEWER
        MonoBehaviour[] allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        Rigidbody2D[] allRigidbodies = FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None);
        Animator[] allAnimators = FindObjectsByType<Animator>(FindObjectsSortMode.None);
#else
        MonoBehaviour[] allBehaviours = FindObjectsOfType<MonoBehaviour>();
        Rigidbody2D[] allRigidbodies = FindObjectsOfType<Rigidbody2D>();
        Animator[] allAnimators = FindObjectsOfType<Animator>();
#endif

        for (int i = 0; i < allBehaviours.Length; i++)
        {
            MonoBehaviour behaviour = allBehaviours[i];

            if (behaviour == null)
            {
                continue;
            }

            if (behaviour == this)
            {
                continue;
            }

            if (ShouldFreezeBehaviour(behaviour))
            {
                cachedBehaviourEnabledState[behaviour] = behaviour.enabled;

                if (behaviour.enabled)
                {
                    behaviour.enabled = false;
                }
            }
        }

        if (stopRigidbodyVelocityWhenPaused)
        {
            for (int i = 0; i < allRigidbodies.Length; i++)
            {
                Rigidbody2D rb = allRigidbodies[i];

                if (rb == null)
                {
                    continue;
                }

                cachedRigidbodies.Add(rb);
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        if (setAnimatorSpeedToZeroWhenPaused)
        {
            for (int i = 0; i < allAnimators.Length; i++)
            {
                Animator animator = allAnimators[i];

                if (animator == null)
                {
                    continue;
                }

                cachedAnimators.Add(animator);
                SetAnimatorFloatIfExists(animator, "Speed", 0f);
            }
        }

        if (enableDebugLog)
        {
            Debug.Log("PauseMenuController: Đã khóa gameplay scripts khi Pause. Số script cache = " + cachedBehaviourEnabledState.Count);
        }
    }

    private bool ShouldFreezeBehaviour(MonoBehaviour behaviour)
    {
        if (behaviour == null)
        {
            return false;
        }

        if (gameplayScriptNamesToFreeze == null)
        {
            return false;
        }

        string scriptName = behaviour.GetType().Name;

        for (int i = 0; i < gameplayScriptNamesToFreeze.Length; i++)
        {
            if (scriptName == gameplayScriptNamesToFreeze[i])
            {
                return true;
            }
        }

        return false;
    }

    private void RestoreGameplayObjectsAfterPause()
    {
        foreach (KeyValuePair<Behaviour, bool> pair in cachedBehaviourEnabledState)
        {
            Behaviour behaviour = pair.Key;

            if (behaviour == null)
            {
                continue;
            }

            behaviour.enabled = pair.Value;
        }

        for (int i = 0; i < cachedRigidbodies.Count; i++)
        {
            Rigidbody2D rb = cachedRigidbodies[i];

            if (rb == null)
            {
                continue;
            }

            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        for (int i = 0; i < cachedAnimators.Count; i++)
        {
            Animator animator = cachedAnimators[i];

            if (animator == null)
            {
                continue;
            }

            SetAnimatorFloatIfExists(animator, "Speed", 0f);
        }

        cachedBehaviourEnabledState.Clear();
        cachedRigidbodies.Clear();
        cachedAnimators.Clear();
    }

    private void SetAnimatorFloatIfExists(Animator animator, string parameterName, float value)
    {
        if (animator == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(parameterName))
        {
            return;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName &&
                parameters[i].type == AnimatorControllerParameterType.Float)
            {
                animator.SetFloat(parameterName, value);
                return;
            }
        }
    }

    private bool IsGameOverShowing()
    {
        if (gameOverMenuController == null)
        {
            return false;
        }

        return gameOverMenuController.IsGameOverShown();
    }

    public bool IsPaused()
    {
        return isPaused;
    }
}