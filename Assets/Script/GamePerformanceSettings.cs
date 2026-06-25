using UnityEngine;

public class GamePerformanceSettings : MonoBehaviour
{
    [Header("FPS")]
    [Tooltip("FPS mục tiêu. 60 là ổn nhất cho game 2D.")]
    public int targetFrameRate = 60;

    [Tooltip("Tắt VSync để Application.targetFrameRate có tác dụng.")]
    public bool disableVSync = true;

    [Header("Physics 2D")]
    [Tooltip("Fixed timestep mặc định Unity là 0.02 = 50 lần/giây. Game 2D nên giữ mức này.")]
    public float fixedDeltaTime = 0.02f;

    private void Awake()
    {
        if (disableVSync)
        {
            QualitySettings.vSyncCount = 0;
        }

        Application.targetFrameRate = targetFrameRate;
        Time.fixedDeltaTime = fixedDeltaTime;

        Debug.Log(
            "GamePerformanceSettings: Target FPS = " + targetFrameRate +
            " | VSync = " + QualitySettings.vSyncCount +
            " | FixedDeltaTime = " + Time.fixedDeltaTime
        );
    }
}