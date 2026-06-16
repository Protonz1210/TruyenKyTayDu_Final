using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class Map3HUDController : MonoBehaviour
{
    [Header("UI Document")]
    public UIDocument uiDocument;

    [Header("Element Names")]
    public string bossPanelName = "boss-panel";
    public string boxMaskName = "Box-mask";
    public string boxAvatarName = "box-avarta";
    public string boxImageName = "box-image";
    public string boxTextName = "box_text";

    [Header("Boss UI Fade")]
    public float bossFadeInTime = 0.6f;
    public float bossFadeOutTime = 0.4f;

    [Header("Box Fade")]
    public float boxFadeInTime = 0.35f;
    public float boxFadeOutTime = 0.25f;

    [Header("Location Title")]
    public float locationFadeInTime = 1f;
    public float locationHoldTime = 5f;
    public float locationFadeOutTime = 1f;

    private VisualElement root;
    private VisualElement bossPanel;
    private VisualElement boxMask;
    private VisualElement boxAvatar;
    private VisualElement boxImage;
    private Label boxText;

    void Awake()
    {
        SetupReferences();

        HideBossUIInstant();
        HideBoxInstant();
    }

    void OnEnable()
    {
        SetupReferences();
    }

    void SetupReferences()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogWarning("Map3HUDController chưa có UIDocument.");
            return;
        }

        root = uiDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogWarning("Map3HUDController không tìm thấy rootVisualElement.");
            return;
        }

        bossPanel = root.Q<VisualElement>(bossPanelName);
        boxMask = root.Q<VisualElement>(boxMaskName);
        boxAvatar = root.Q<VisualElement>(boxAvatarName);
        boxImage = root.Q<VisualElement>(boxImageName);
        boxText = root.Q<Label>(boxTextName);
    }

    // =========================
    // BOSS UI
    // =========================

    public void HideBossUIInstant()
    {
        SetupReferences();

        if (bossPanel == null)
            return;

        bossPanel.style.opacity = 0f;
        bossPanel.style.display = DisplayStyle.None;
    }

    public void ShowBossUIInstant()
    {
        SetupReferences();

        if (bossPanel == null)
            return;

        bossPanel.style.display = DisplayStyle.Flex;
        bossPanel.style.opacity = 1f;
    }

    public IEnumerator FadeInBossUI()
    {
        SetupReferences();

        if (bossPanel == null)
            yield break;

        bossPanel.style.display = DisplayStyle.Flex;
        bossPanel.style.opacity = 0f;

        yield return FadeElement(bossPanel, 0f, 1f, bossFadeInTime);
    }

    public IEnumerator FadeOutBossUI()
    {
        SetupReferences();

        if (bossPanel == null)
            yield break;

        bossPanel.style.display = DisplayStyle.Flex;

        yield return FadeElement(bossPanel, 1f, 0f, bossFadeOutTime);

        bossPanel.style.display = DisplayStyle.None;
    }

    // =========================
    // BOX THOẠI / ĐỊA DANH
    // =========================

    public void HideBoxInstant()
    {
        SetupReferences();

        if (boxMask == null)
            return;

        SetBoxOpacity(0f);
        boxMask.style.display = DisplayStyle.None;
    }

    public void ShowBoxInstant()
    {
        SetupReferences();

        if (boxMask == null)
            return;

        boxMask.style.display = DisplayStyle.Flex;
        SetBoxOpacity(1f);
    }

    public IEnumerator FadeInBox()
    {
        SetupReferences();

        if (boxMask == null)
            yield break;

        boxMask.style.display = DisplayStyle.Flex;
        SetBoxOpacity(0f);

        yield return FadeBox(0f, 1f, boxFadeInTime);
    }

    public IEnumerator FadeOutBox()
    {
        SetupReferences();

        if (boxMask == null)
            yield break;

        yield return FadeBox(1f, 0f, boxFadeOutTime);

        boxMask.style.display = DisplayStyle.None;
    }

    public void SetBoxText(string text)
    {
        SetupReferences();

        if (boxText != null)
            boxText.text = text;
    }

    public void SetBoxImage(Sprite sprite)
    {
        SetupReferences();

        if (boxImage == null)
            return;

        if (sprite == null)
        {
            boxImage.style.backgroundImage = null;
            boxImage.style.display = DisplayStyle.None;
        }
        else
        {
            boxImage.style.display = DisplayStyle.Flex;
            boxImage.style.backgroundImage = new StyleBackground(sprite);
        }
    }

    public IEnumerator PlayLocationTitle(string title)
    {
        SetupReferences();

        if (boxMask == null)
            yield break;

        SetBoxText(title);

        boxMask.style.display = DisplayStyle.Flex;
        SetBoxOpacity(0f);

        yield return FadeBox(0f, 1f, locationFadeInTime);

        yield return new WaitForSeconds(locationHoldTime);

        yield return FadeBox(1f, 0f, locationFadeOutTime);

        HideBoxInstant();
    }

    private IEnumerator FadeBox(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetBoxOpacity(to);
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / duration);
            float alpha = Mathf.Lerp(from, to, t);

            SetBoxOpacity(alpha);

            yield return null;
        }

        SetBoxOpacity(to);
    }

    private void SetBoxOpacity(float alpha)
    {
        if (boxMask != null)
            boxMask.style.opacity = alpha;

        if (boxAvatar != null)
            boxAvatar.style.opacity = alpha;

        if (boxImage != null)
            boxImage.style.opacity = alpha;

        if (boxText != null)
            boxText.style.opacity = alpha;
    }

    private IEnumerator FadeElement(VisualElement element, float from, float to, float duration)
    {
        if (element == null)
            yield break;

        if (duration <= 0f)
        {
            element.style.opacity = to;
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / duration);
            float alpha = Mathf.Lerp(from, to, t);

            element.style.opacity = alpha;

            yield return null;
        }

        element.style.opacity = to;
    }
}