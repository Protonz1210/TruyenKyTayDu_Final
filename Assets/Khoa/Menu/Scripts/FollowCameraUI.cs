using UnityEngine;

/// <summary>
/// Giúp object UI world-space đi theo camera.
/// Dùng cho GameOver object dạng đặt trong Scene.
/// Gắn script này vào object cha OVER.
/// </summary>
public class FollowCameraUI : MonoBehaviour
{
    [Header("Camera")]
    [Tooltip("Camera mà UI sẽ đi theo. Nếu bỏ trống sẽ tự lấy Camera.main.")]
    public Camera targetCamera;

    [Header("Follow Offset")]
    [Tooltip("Khoảng cách X so với camera.")]
    public float offsetX = 0f;

    [Tooltip("Khoảng cách Y so với camera.")]
    public float offsetY = 0f;

    [Tooltip("Z cố định của object UI. Với game 2D thường để 0 hoặc gần camera tùy setup.")]
    public float fixedZ = 0f;

    [Header("Options")]
    [Tooltip("Bật để object luôn đi theo camera mỗi frame.")]
    public bool followEveryFrame = true;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        FollowCameraNow();
    }

    private void LateUpdate()
    {
        if (!followEveryFrame)
        {
            return;
        }

        FollowCameraNow();
    }

    public void FollowCameraNow()
    {
        if (targetCamera == null)
        {
            return;
        }

        Vector3 camPos = targetCamera.transform.position;

        transform.position = new Vector3(
            camPos.x + offsetX,
            camPos.y + offsetY,
            fixedZ
        );
    }
}