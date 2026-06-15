using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FireLightFlicker : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Light 2D của đốm lửa. Nếu bỏ trống, script tự lấy Light2D trên object này.")]
    public Light2D targetLight;

    [Header("Intensity")]
    [Tooltip("Độ sáng thấp nhất của lửa.")]
    public float minIntensity = 0.7f;

    [Tooltip("Độ sáng cao nhất của lửa.")]
    public float maxIntensity = 1.2f;

    [Tooltip("Tốc độ nhấp nháy của lửa.")]
    public float flickerSpeed = 5f;

    [Header("Radius")]
    [Tooltip("Bán kính nhỏ nhất của vùng sáng.")]
    public float minOuterRadius = 2.0f;

    [Tooltip("Bán kính lớn nhất của vùng sáng.")]
    public float maxOuterRadius = 2.8f;

    private float randomOffset;

    private void Awake()
    {
        if (targetLight == null)
        {
            targetLight = GetComponent<Light2D>();
        }

        randomOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        if (targetLight == null) return;

        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, randomOffset);

        targetLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
        targetLight.pointLightOuterRadius = Mathf.Lerp(minOuterRadius, maxOuterRadius, noise);
    }
}