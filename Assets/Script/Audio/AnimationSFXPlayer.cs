using UnityEngine;

public class AnimationSFXPlayer : MonoBehaviour
{
    [Header("Audio Source")]
    [Tooltip("AudioSource phát sound. Nếu bỏ trống, script tự lấy hoặc tự tạo AudioSource.")]
    public AudioSource audioSource;

    [Header("Move SFX")]
    [Tooltip("Âm thanh bước chân khi chạy.")]
    public AudioClip footstepSFX;

    [Tooltip("Âm thanh khi nhảy.")]
    public AudioClip jumpSFX;

    [Tooltip("Âm thanh khi tiếp đất.")]
    public AudioClip landSFX;

    [Header("Attack SFX")]
    [Tooltip("Âm thanh Attack0.")]
    public AudioClip attack0SFX;

    [Tooltip("Âm thanh Attack1.")]
    public AudioClip attack1SFX;

    [Tooltip("Âm thanh Attack2.")]
    public AudioClip attack2SFX;

    [Tooltip("Âm thanh Attack3.")]
    public AudioClip attack3SFX;

    [Header("Other SFX")]
    [Tooltip("Âm thanh khi bị đánh.")]
    public AudioClip hitSFX;

    [Tooltip("Âm thanh khi chết.")]
    public AudioClip dieSFX;

    [Header("Volume")]
    [Range(0f, 1f)]
    [Tooltip("Âm lượng bước chân.")]
    public float footstepVolume = 0.45f;

    [Range(0f, 1f)]
    [Tooltip("Âm lượng nhảy.")]
    public float jumpVolume = 0.7f;

    [Range(0f, 1f)]
    [Tooltip("Âm lượng attack.")]
    public float attackVolume = 0.8f;

    [Range(0f, 1f)]
    [Tooltip("Âm lượng hit/die.")]
    public float otherVolume = 0.8f;

    [Header("Pitch Random")]
    [Tooltip("Bật random pitch nhẹ để âm thanh đỡ bị lặp máy móc.")]
    public bool randomizePitch = true;

    [Tooltip("Pitch thấp nhất.")]
    public float minPitch = 0.95f;

    [Tooltip("Pitch cao nhất.")]
    public float maxPitch = 1.05f;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    public void PlayFootstepSFX()
    {
        PlayClip(footstepSFX, footstepVolume);
    }

    public void PlayJumpSFX()
    {
        PlayClip(jumpSFX, jumpVolume);
    }

    public void PlayLandSFX()
    {
        PlayClip(landSFX, jumpVolume);
    }

    public void PlayAttack0SFX()
    {
        PlayClip(attack0SFX, attackVolume);
    }

    public void PlayAttack1SFX()
    {
        PlayClip(attack1SFX, attackVolume);
    }

    public void PlayAttack2SFX()
    {
        PlayClip(attack2SFX, attackVolume);
    }

    public void PlayAttack3SFX()
    {
        PlayClip(attack3SFX, attackVolume);
    }

    public void PlayHitSFX()
    {
        PlayClip(hitSFX, otherVolume);
    }

    public void PlayDieSFX()
    {
        PlayClip(dieSFX, otherVolume);
    }

    void PlayClip(AudioClip clip, float volume)
    {
        if (audioSource == null) return;
        if (clip == null) return;

        if (randomizePitch)
        {
            audioSource.pitch = Random.Range(minPitch, maxPitch);
        }
        else
        {
            audioSource.pitch = 1f;
        }

        audioSource.PlayOneShot(clip, volume);
    }
}