using UnityEngine;
using UnityEngine.SceneManagement;

public class MapSelect : MonoBehaviour
{
    [Header("Scene Names")]
    [Tooltip("Tên scene Map 1 đúng như trong Build Settings.")]
    public string map1SceneName = "Map1";

    [Tooltip("Tên scene Map 2 đúng như trong Build Settings.")]
    public string map2SceneName = "Map2";

    [Tooltip("Tên scene Map 2.5 đúng như trong Build Settings nếu có.")]
    public string map25SceneName = "Map2.5";

    [Tooltip("Tên scene Map 3 đúng như trong Build Settings.")]
    public string map3SceneName = "Map3";

    [Tooltip("Tên scene Map 4 đúng như trong Build Settings.")]
    public string map4SceneName = "Map4";

    [Tooltip("Tên scene Map 5 đúng như trong Build Settings.")]
    public string map5SceneName = "Map5";

    public void Map1()
    {
        LoadMapByName(map1SceneName);
    }

    public void Map2()
    {
        LoadMapByName(map2SceneName);
    }

    public void Map25()
    {
        LoadMapByName(map25SceneName);
    }

    public void Map3()
    {
        LoadMapByName(map3SceneName);
    }

    public void Map4()
    {
        LoadMapByName(map4SceneName);
    }

    public void Map5()
    {
        LoadMapByName(map5SceneName);
    }

    private void LoadMapByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("MapSelect: Scene name đang bị trống, không thể load map.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                "MapSelect: Không tìm thấy scene tên '" + sceneName +
                "'. Hãy kiểm tra tên scene trong File > Build Settings > Scenes In Build."
            );
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}