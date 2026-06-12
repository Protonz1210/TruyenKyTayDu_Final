using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FixedAspectRatio : MonoBehaviour
{
    [Header("Aspect Ratio")]
    [Tooltip("Tỷ lệ chiều ngang.")]
    public float targetWidth = 16f;

    [Tooltip("Tỷ lệ chiều cao.")]
    public float targetHeight = 9f;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        ApplyAspectRatio();
    }

    void Start()
    {
        ApplyAspectRatio();
    }

    void Update()
    {
        ApplyAspectRatio();
    }

    void ApplyAspectRatio()
    {
        float targetAspect = targetWidth / targetHeight;
        float windowAspect = (float)Screen.width / Screen.height;

        float scaleHeight = windowAspect / targetAspect;

        if (scaleHeight < 1.0f)
        {
            Rect rect = cam.rect;

            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0f;
            rect.y = (1.0f - scaleHeight) / 2.0f;

            cam.rect = rect;
        }
        else
        {
            float scaleWidth = 1.0f / scaleHeight;

            Rect rect = cam.rect;

            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0f;

            cam.rect = rect;
        }
    }
}
