using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MapSelectMenu : MonoBehaviour
{
    [Header("Map Buttons")]
    [Tooltip("Nút chọn Màn 1.")]
    public Button map1Button;

    [Tooltip("Nút chọn Màn 2.")]
    public Button map2Button;

    [Tooltip("Nút chọn Màn 3.")]
    public Button map3Button;

    [Tooltip("Nút chọn Màn 4.")]
    public Button map4Button;

    [Tooltip("Nút chọn Màn 5.")]
    public Button map5Button;

    [Header("Scene Names")]
    [Tooltip("Tên Scene của Màn 1. Phải đúng y hệt tên Scene trong Project.")]
    public string map1SceneName = "Map1";

    [Tooltip("Tên Scene của Màn 2. Phải đúng y hệt tên Scene trong Project.")]
    public string map2SceneName = "Map2";

    [Tooltip("Tên Scene của Màn 3. Phải đúng y hệt tên Scene trong Project.")]
    public string map3SceneName = "Map3";

    [Tooltip("Tên Scene của Màn 4. Phải đúng y hệt tên Scene trong Project.")]
    public string map4SceneName = "Map4";

    [Tooltip("Tên Scene của Màn 5. Phải đúng y hệt tên Scene trong Project.")]
    public string map5SceneName = "Map5";

    private void Awake()
    {
        SetupButtons();
    }

    private void SetupButtons()
    {
        if (map1Button != null)
        {
            map1Button.onClick.RemoveAllListeners();
            map1Button.onClick.AddListener(() => LoadMap(map1SceneName));
        }

        if (map2Button != null)
        {
            map2Button.onClick.RemoveAllListeners();
            map2Button.onClick.AddListener(() => LoadMap(map2SceneName));
        }

        if (map3Button != null)
        {
            map3Button.onClick.RemoveAllListeners();
            map3Button.onClick.AddListener(() => LoadMap(map3SceneName));
        }

        if (map4Button != null)
        {
            map4Button.onClick.RemoveAllListeners();
            map4Button.onClick.AddListener(() => LoadMap(map4SceneName));
        }

        if (map5Button != null)
        {
            map5Button.onClick.RemoveAllListeners();
            map5Button.onClick.AddListener(() => LoadMap(map5SceneName));
        }
    }

    private void LoadMap(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Tên Scene đang bị trống. Kiểm tra lại trong Inspector của MapSelectMenu.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}