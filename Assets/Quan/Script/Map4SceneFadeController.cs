using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Fade chuyển cảnh cho Map4 bằng UI Toolkit.
/// 
/// Cơ chế:
/// - Khi vào Map4: nếu Auto Fade In On Start bật, màn hình đen rồi rõ dần.
/// - Khi hết Map4: gọi FadeOutThenLoadScene(sceneName), màn hình mờ dần sang đen rồi load scene mới.
/// </summary>
public class Map4SceneFadeController : MonoBehaviour
{
    [Header("UI Toolkit")]
    [Tooltip("UIDocument dùng để tạo lớp fade đen phủ toàn màn hình. Kéo Map4HUD hoặc GlobalHUD có UIDocument vào đây.")]
    public UIDocument uiDocument;

    [Header("Fade Settings")]
    [Tooltip("Thời gian fade vào màn hình đen khi chuyển map.")]
    public float fadeOutDuration = 1f;

    [Tooltip("Thời gian fade từ màn hình đen ra cảnh game khi vào map.")]
    public float fadeInDuration = 1f;

    [Tooltip("Bật để khi scene mới mở ra thì tự fade từ đen sang sáng.")]
    public bool autoFadeInOnStart = true;

    [Tooltip("Bật nếu muốn lúc bắt đầu scene màn hình đang đen hoàn toàn.")]
    public bool startBlack = true;

    [Header("Overlay")]
    [Tooltip("Tên lớp fade được tạo bằng code.")]
    public string fadeOverlayName = "map4-scene-fade-overlay";

    [Header("State")]
    [Tooltip("Đang chạy fade hay không.")]
    public bool isFading;

    [Header("Debug")]
    public bool enableDebugLog = false;

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

    private void SetupOverlay()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument == null)
        {
            uiDocument = GetComponentInParent<UIDocument>();
        }

#if UNITY_2023_1_OR_NEWER
        if (uiDocument == null)
        {
            UIDocument[] documents = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);

            for (int i = 0; i < documents.Length; i++)
            {
                if (documents[i] != null && documents[i].rootVisualElement != null)
                {
                    uiDocument = documents[i];
                    break;
                }
            }
        }
#else
        if (uiDocument == null)
        {
            UIDocument[] documents = FindObjectsOfType<UIDocument>();

            for (int i = 0; i < documents.Length; i++)
            {
                if (documents[i] != null && documents[i].rootVisualElement != null)
                {
                    uiDocument = documents[i];
                    break;
                }
            }
        }
#endif

        if (uiDocument == null)
        {
            Debug.LogError("[Map4SceneFadeController] Chưa có UIDocument.");
            return;
        }

        root = uiDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogError("[Map4SceneFadeController] Không tìm thấy rootVisualElement.");
            return;
        }

        if (fadeOverlay == null)
        {
            fadeOverlay = root.Q<VisualElement>(fadeOverlayName);
        }

        if (fadeOverlay == null)
        {
            fadeOverlay = new VisualElement();
            fadeOverlay.name = fadeOverlayName;

            fadeOverlay.style.position = Position.Absolute;
            fadeOverlay.style.left = 0;
            fadeOverlay.style.right = 0;
            fadeOverlay.style.top = 0;
            fadeOverlay.style.bottom = 0;

            fadeOverlay.style.backgroundColor = new Color(0f, 0f, 0f, 1f);
            fadeOverlay.style.opacity = 0f;
            fadeOverlay.style.display = DisplayStyle.None;

            // Không chặn click/input của UI bên dưới.
            fadeOverlay.pickingMode = PickingMode.Ignore;

            root.Add(fadeOverlay);
        }

        fadeOverlay.BringToFront();
    }

    /// <summary>
    /// Gọi hàm này khi hết Map4 và muốn fade đen rồi chuyển scene.
    /// Ví dụ: FadeOutThenLoadScene("Map5");
    /// </summary>
    public void FadeOutThenLoadScene(string sceneName)
    {
        if (isFading)
        {
            Debug.LogWarning("[Map4SceneFadeController] Đang fade, không gọi lại.");
            return;
        }

        StartCoroutine(FadeOutThenLoadSceneRoutine(sceneName));
    }

    /// <summary>
    /// Gọi hàm này nếu chỉ muốn màn hình mờ dần sang đen, không chuyển scene.
    /// </summary>
    public void FadeOutOnly()
    {
        if (isFading)
        {
            Debug.LogWarning("[Map4SceneFadeController] Đang fade, không gọi lại.");
            return;
        }

        StartCoroutine(FadeOutRoutine());
    }

    /// <summary>
    /// Màn hình rõ dần: đen -> sáng.
    /// </summary>
    public IEnumerator FadeInRoutine()
    {
        SetupOverlay();

        if (fadeOverlay == null)
        {
            yield break;
        }

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

        if (enableDebugLog)
        {
            Debug.Log("[Map4SceneFadeController] Fade In xong.");
        }
    }

    /// <summary>
    /// Màn hình mờ dần: sáng -> đen.
    /// </summary>
    public IEnumerator FadeOutRoutine()
    {
        SetupOverlay();

        if (fadeOverlay == null)
        {
            yield break;
        }

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

        if (enableDebugLog)
        {
            Debug.Log("[Map4SceneFadeController] Fade Out xong.");
        }
    }

    private IEnumerator FadeOutThenLoadSceneRoutine(string sceneName)
    {
        yield return StartCoroutine(FadeOutRoutine());

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[Map4SceneFadeController] Scene Name đang trống.");
            yield break;
        }

        if (enableDebugLog)
        {
            Debug.Log("[Map4SceneFadeController] Load scene: " + sceneName);
        }

        SceneManager.LoadScene(sceneName);
    }

    public void SetBlackImmediate()
    {
        SetupOverlay();

        if (fadeOverlay == null)
        {
            return;
        }

        ShowOverlay();
        SetOverlayAlpha(1f);
        fadeOverlay.BringToFront();
    }

    public void SetClearImmediate()
    {
        SetupOverlay();

        if (fadeOverlay == null)
        {
            return;
        }

        SetOverlayAlpha(0f);
        HideOverlay();
    }

    private void ShowOverlay()
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.style.display = DisplayStyle.Flex;
            fadeOverlay.pickingMode = PickingMode.Ignore;
            fadeOverlay.BringToFront();
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