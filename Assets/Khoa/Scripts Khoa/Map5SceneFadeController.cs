
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

    [Header("Cinematic Text")]
    [Tooltip("Bật: khi chuyển scene sẽ hiện chữ cinematic trên nền đen trước khi load scene mới.")]
    public bool useCinematicTextBeforeLoad = true;

    [Tooltip("Dòng chữ cinematic hiện giữa màn hình khi chuyển map.")]
    public string cinematicText = "Linh Sơn";

    [Tooltip("Thời gian chữ mờ dần hiện lên.")]
    public float textFadeInDuration = 0.6f;

    [Tooltip("Thời gian giữ chữ rõ trên màn hình.")]
    public float textHoldDuration = 1.2f;

    [Tooltip("Thời gian chữ mờ dần biến mất.")]
    public float textFadeOutDuration = 0.6f;

    [Tooltip("Cỡ chữ cinematic.")]
    public int cinematicTextFontSize = 42;

    [Tooltip("Vị trí chữ theo trục Y. Số âm là thấp xuống, số dương là cao lên.")]
    public float cinematicTextOffsetY = 0f;

    [Header("Test")]
    [Tooltip("Bật để bấm F test fade out, hiện text, fade in, chưa chuyển scene.")]
    public bool enableTestKey = true;

    [Header("State")]
    [Tooltip("Đang chạy fade hay không.")]
    public bool isFading;

    private VisualElement root;
    private VisualElement fadeOverlay;
    private Label cinematicTextLabel;

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

        HideCinematicText();

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
                StartCoroutine(TestFadeWithTextRoutine());
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

        if (fadeOverlay == null)
        {
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
        }

        if (cinematicTextLabel == null)
        {
            cinematicTextLabel = new Label();
            cinematicTextLabel.name = "map5-cinematic-text";
            cinematicTextLabel.text = cinematicText;

            cinematicTextLabel.style.position = Position.Absolute;
            cinematicTextLabel.style.left = 0;
            cinematicTextLabel.style.right = 0;
            cinematicTextLabel.style.top = Length.Percent(50);
            cinematicTextLabel.style.translate = new Translate(0, cinematicTextOffsetY, 0);

            cinematicTextLabel.style.height = 80;
            cinematicTextLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            cinematicTextLabel.style.fontSize = cinematicTextFontSize;
            cinematicTextLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            cinematicTextLabel.style.color = new Color(1f, 0.82f, 0.36f, 1f);
            cinematicTextLabel.style.opacity = 0f;
            cinematicTextLabel.style.display = DisplayStyle.None;

            root.Add(cinematicTextLabel);
        }

        fadeOverlay.BringToFront();
        cinematicTextLabel.BringToFront();
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
        cinematicTextLabel.BringToFront();

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
        cinematicTextLabel.BringToFront();
        SetOverlayAlpha(1f);
        HideCinematicText();

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

    public IEnumerator PlayCinematicTextRoutine()
    {
        SetupOverlay();

        if (!useCinematicTextBeforeLoad)
        {
            yield break;
        }

        ShowOverlay();
        SetOverlayAlpha(1f);
        ShowCinematicText();
        SetCinematicTextAlpha(0f);

        cinematicTextLabel.text = cinematicText;
        cinematicTextLabel.style.fontSize = cinematicTextFontSize;
        cinematicTextLabel.style.translate = new Translate(0, cinematicTextOffsetY, 0);

        float timer = 0f;

        while (timer < textFadeInDuration)
        {
            float t = timer / textFadeInDuration;
            SetCinematicTextAlpha(t);

            timer += Time.deltaTime;
            yield return null;
        }

        SetCinematicTextAlpha(1f);

        yield return new WaitForSeconds(textHoldDuration);

        timer = 0f;

        while (timer < textFadeOutDuration)
        {
            float t = timer / textFadeOutDuration;
            SetCinematicTextAlpha(1f - t);

            timer += Time.deltaTime;
            yield return null;
        }

        SetCinematicTextAlpha(0f);
        HideCinematicText();
    }

    private IEnumerator FadeOutThenLoadSceneRoutine(string sceneName)
    {
        yield return StartCoroutine(FadeOutRoutine());

        if (useCinematicTextBeforeLoad)
        {
            yield return StartCoroutine(PlayCinematicTextRoutine());
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[Map5SceneFadeController] Scene Name đang trống.");
            yield break;
        }

        Debug.Log("[Map5SceneFadeController] Load scene: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator TestFadeWithTextRoutine()
    {
        yield return StartCoroutine(FadeOutRoutine());
        yield return StartCoroutine(PlayCinematicTextRoutine());
        yield return new WaitForSeconds(0.2f);
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

    private void ShowCinematicText()
    {
        if (cinematicTextLabel != null)
        {
            cinematicTextLabel.style.display = DisplayStyle.Flex;
        }
    }

    private void HideCinematicText()
    {
        if (cinematicTextLabel != null)
        {
            cinematicTextLabel.style.display = DisplayStyle.None;
        }
    }

    private void SetCinematicTextAlpha(float alpha)
    {
        if (cinematicTextLabel != null)
        {
            cinematicTextLabel.style.opacity = Mathf.Clamp01(alpha);
        }
    }
}