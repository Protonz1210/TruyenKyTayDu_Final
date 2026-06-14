
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class Map5SceneFadeController : MonoBehaviour
{
    [Header("UI Toolkit")]
    [Tooltip("UIDocument dùng để tạo lớp fade đen phủ toàn màn hình. Có thể kéo UIDocument của Map5DialogueController vào đây.")]
    public UIDocument uiDocument;

    [Header("Fade Settings")]
    [Tooltip("Thời gian fade vào màn hình đen.")]
    public float fadeOutDuration = 1f;

    [Tooltip("Thời gian fade từ màn hình đen ra cảnh game.")]
    public float fadeInDuration = 1f;

    [Tooltip("Bật ở MAP 5.1 để khi scene mới mở ra thì tự fade từ đen sang sáng.")]
    public bool autoFadeInOnStart = false;

    [Tooltip("Bật nếu muốn lúc bắt đầu scene màn hình đang đen hoàn toàn.")]
    public bool startBlack = false;

    [Header("Test")]
    [Tooltip("Bật để bấm F test fade out rồi fade in, chưa chuyển scene.")]
    public bool enableTestKey = true;

    [Header("State")]
    [Tooltip("Đang chạy fade hay không.")]
    public bool isFading;

    private VisualElement root;
    private VisualElement fadeOverlay;

    private void Awake()
    {
        SetupOverlay();
    }

    private void Start()
    {
        SetupOverlay();

        if (startBlack)
        {
            SetOverlayAlpha(1f);
            ShowOverlay();
        }
        else
        {
            SetOverlayAlpha(0f);
            HideOverlay();
        }

        if (autoFadeInOnStart)
        {
            StartCoroutine(FadeInRoutine());
        }
    }

    private void Update()
    {
        if (!enableTestKey)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (!isFading)
            {
                StartCoroutine(TestFadeRoutine());
            }
        }
    }

    private void SetupOverlay()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument == null)
        {
            Debug.LogError("[Map5SceneFadeController] Chưa có UIDocument.");
            return;
        }

        root = uiDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogError("[Map5SceneFadeController] Không tìm thấy rootVisualElement.");
            return;
        }

        if (fadeOverlay != null)
        {
            return;
        }

        fadeOverlay = new VisualElement();
        fadeOverlay.name = "map5-scene-fade-overlay";

        fadeOverlay.style.position = Position.Absolute;
        fadeOverlay.style.left = 0;
        fadeOverlay.style.right = 0;
        fadeOverlay.style.top = 0;
        fadeOverlay.style.bottom = 0;
        fadeOverlay.style.backgroundColor = new Color(0f, 0f, 0f, 1f);
        fadeOverlay.style.opacity = 0f;
        fadeOverlay.style.display = DisplayStyle.None;

        root.Add(fadeOverlay);
        fadeOverlay.BringToFront();
    }

    public void FadeOutThenLoadScene(string sceneName)
    {
        if (isFading)
        {
            Debug.LogWarning("[Map5SceneFadeController] Đang fade, không gọi lại.");
            return;
        }

        StartCoroutine(FadeOutThenLoadSceneRoutine(sceneName));
    }

    public IEnumerator FadeOutRoutine()
    {
        SetupOverlay();

        isFading = true;
        ShowOverlay();
        fadeOverlay.BringToFront();

        float timer = 0f;

        while (timer < fadeOutDuration)
        {
            float t = timer / fadeOutDuration;
            SetOverlayAlpha(t);

            timer += Time.deltaTime;
            yield return null;
        }

        SetOverlayAlpha(1f);
        isFading = false;
    }

    public IEnumerator FadeInRoutine()
    {
        SetupOverlay();

        isFading = true;
        ShowOverlay();
        fadeOverlay.BringToFront();
        SetOverlayAlpha(1f);

        float timer = 0f;

        while (timer < fadeInDuration)
        {
            float t = timer / fadeInDuration;
            SetOverlayAlpha(1f - t);

            timer += Time.deltaTime;
            yield return null;
        }

        SetOverlayAlpha(0f);
        HideOverlay();

        isFading = false;
    }

    private IEnumerator FadeOutThenLoadSceneRoutine(string sceneName)
    {
        yield return StartCoroutine(FadeOutRoutine());

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[Map5SceneFadeController] Scene Name đang trống.");
            yield break;
        }

        Debug.Log("[Map5SceneFadeController] Load scene: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator TestFadeRoutine()
    {
        yield return StartCoroutine(FadeOutRoutine());
        yield return new WaitForSeconds(0.35f);
        yield return StartCoroutine(FadeInRoutine());
    }

    private void ShowOverlay()
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.style.display = DisplayStyle.Flex;
        }
    }

    private void HideOverlay()
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.style.display = DisplayStyle.None;
        }
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.style.opacity = Mathf.Clamp01(alpha);
        }
    }
}