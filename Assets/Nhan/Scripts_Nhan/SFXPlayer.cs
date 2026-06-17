
using UnityEngine;

public class SFXPlayer : MonoBehaviour
{
    [Header("Audio Source")]
    [Tooltip("AudioSource phát sound. Nếu bỏ trống, script tự lấy hoặc tự tạo AudioSource.")]
    public AudioSource audioSource;

    [Header("Footstep")]
    [Tooltip("Danh sách âm bước chân. Tăng Size để thêm nhiều sound.")]
    public AudioClip[] footstepClips;

    [Range(0f, 1f)]
    [Tooltip("Âm lượng bước chân.")]
    public float footstepVolume = 0.45f;

    [Header("Jump / Land")]
    [Tooltip("Danh sách âm nhảy.")]
    public AudioClip[] jumpClips;

    [Tooltip("Danh sách âm tiếp đất.")]
    public AudioClip[] landClips;

    [Range(0f, 1f)]
    [Tooltip("Âm lượng nhảy / tiếp đất.")]
    public float jumpLandVolume = 0.7f;

    [Header("Attack0 Clips")]
    [Tooltip("Danh sách âm trong Attack0. Có thể đặt nhiều event Attack0Index(0), Attack0Index(1), Attack0Index(2)...")]
    public AudioClip[] attack0Clips;

    [Header("Attack1 Clips")]
    [Tooltip("Danh sách âm trong Attack1. Ví dụ Attack1 có 3 âm thì set Size = 3, rồi gọi Attack1Index(0/1/2) bằng Animation Event.")]
    public AudioClip[] attack1Clips;

    [Header("Attack2 Clips")]
    [Tooltip("Danh sách âm trong Attack2.")]
    public AudioClip[] attack2Clips;

    [Header("Attack3 Clips")]
    [Tooltip("Danh sách âm trong Attack3.")]
    public AudioClip[] attack3Clips;

    [Range(0f, 1f)]
    [Tooltip("Âm lượng các đòn đánh.")]
    public float attackVolume = 0.8f;

    [Header("Hit / Die")]
    [Tooltip("Danh sách âm khi bị đánh.")]
    public AudioClip[] hitClips;

    [Tooltip("Danh sách âm khi chết.")]
    public AudioClip[] dieClips;

    [Range(0f, 1f)]
    [Tooltip("Âm lượng hit / die.")]
    public float hitDieVolume = 0.8f;

    [Header("Custom")]
    [Tooltip("Danh sách âm tùy chỉnh cho animation đặc biệt.")]
    public AudioClip[] customClips;

    [Range(0f, 1f)]
    [Tooltip("Âm lượng custom sound.")]
    public float customVolume = 0.8f;

    [Header("Pitch")]
    [Tooltip("Bật random pitch nhẹ để sound đỡ bị lặp.")]
    public bool randomPitch = true;

    [Tooltip("Pitch thấp nhất.")]
    public float minPitch = 0.95f;

    [Tooltip("Pitch cao nhất.")]
    public float maxPitch = 1.05f;

    [Header("Cooldown")]
    [Tooltip("Khoảng cách tối thiểu giữa 2 tiếng bước chân.")]
    public float footstepCooldown = 0.08f;

    [Tooltip("Khoảng cách tối thiểu giữa 2 sound attack. Nếu muốn nhiều âm trong cùng 1 chiêu phát sát nhau, để thấp như 0 hoặc 0.01.")]
    public float attackCooldown = 0.01f;

    [Header("Debug")]
    [Tooltip("Bật để in log khi phát sound.")]
    public bool debugLog = false;

    private float lastFootstepTime = -999f;
    private float lastAttackTime = -999f;

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

    // FOOTSTEP
    public void Footstep()
    {
        if (Time.time - lastFootstepTime < footstepCooldown) return;

        lastFootstepTime = Time.time;
        PlayRandom(footstepClips, footstepVolume, "Footstep");
    }

    public void FootstepIndex(int index)
    {
        if (Time.time - lastFootstepTime < footstepCooldown) return;

        lastFootstepTime = Time.time;
        PlayIndex(footstepClips, index, footstepVolume, "FootstepIndex " + index);
    }

    // JUMP / LAND
    public void Jump()
    {
        PlayRandom(jumpClips, jumpLandVolume, "Jump");
    }

    public void JumpIndex(int index)
    {
        PlayIndex(jumpClips, index, jumpLandVolume, "JumpIndex " + index);
    }

    public void Land()
    {
        PlayRandom(landClips, jumpLandVolume, "Land");
    }

    public void LandIndex(int index)
    {
        PlayIndex(landClips, index, jumpLandVolume, "LandIndex " + index);
    }

    // ATTACK RANDOM THEO NHÓM
    public void Attack0()
    {
        PlayAttackRandom(attack0Clips, "Attack0 Random");
    }

    public void Attack1()
    {
        PlayAttackRandom(attack1Clips, "Attack1 Random");
    }

    public void Attack2()
    {
        PlayAttackRandom(attack2Clips, "Attack2 Random");
    }

    public void Attack3()
    {
        PlayAttackRandom(attack3Clips, "Attack3 Random");
    }

    // ATTACK CHỌN ĐÚNG CLIP THEO INDEX
    public void Attack0Index(int index)
    {
        PlayAttackIndex(attack0Clips, index, "Attack0Index " + index);
    }

    public void Attack1Index(int index)
    {
        PlayAttackIndex(attack1Clips, index, "Attack1Index " + index);
    }

    public void Attack2Index(int index)
    {
        PlayAttackIndex(attack2Clips, index, "Attack2Index " + index);
    }

    public void Attack3Index(int index)
    {
        PlayAttackIndex(attack3Clips, index, "Attack3Index " + index);
    }

    void PlayAttackRandom(AudioClip[] clips, string debugName)
    {
        if (Time.time - lastAttackTime < attackCooldown) return;

        lastAttackTime = Time.time;
        PlayRandom(clips, attackVolume, debugName);
    }

    void PlayAttackIndex(AudioClip[] clips, int index, string debugName)
    {
        if (Time.time - lastAttackTime < attackCooldown) return;

        lastAttackTime = Time.time;
        PlayIndex(clips, index, attackVolume, debugName);
    }

    // HIT / DIE
    public void Hit()
    {
        PlayRandom(hitClips, hitDieVolume, "Hit");
    }

    public void HitIndex(int index)
    {
        PlayIndex(hitClips, index, hitDieVolume, "HitIndex " + index);
    }

    public void Die()
    {
        PlayRandom(dieClips, hitDieVolume, "Die");
    }

    public void DieIndex(int index)
    {
        PlayIndex(dieClips, index, hitDieVolume, "DieIndex " + index);
    }

    // CUSTOM
    public void Custom()
    {
        PlayRandom(customClips, customVolume, "Custom");
    }

    public void CustomIndex(int index)
    {
        PlayIndex(customClips, index, customVolume, "CustomIndex " + index);
    }

    // CORE
    void PlayRandom(AudioClip[] clips, float volume, string debugName)
    {
        if (audioSource == null) return;
        if (clips == null || clips.Length == 0) return;

        AudioClip clip = null;
        int safeCount = 0;

        while (clip == null && safeCount < 20)
        {
            int index = Random.Range(0, clips.Length);
            clip = clips[index];
            safeCount++;
        }

        if (clip == null) return;

        PlayClip(clip, volume, debugName);
    }

    void PlayIndex(AudioClip[] clips, int index, float volume, string debugName)
    {
        if (audioSource == null) return;
        if (clips == null || clips.Length == 0) return;
        if (index < 0 || index >= clips.Length) return;
        if (clips[index] == null) return;

        PlayClip(clips[index], volume, debugName);
    }

    void PlayClip(AudioClip clip, float volume, string debugName)
    {
        if (audioSource == null) return;
        if (clip == null) return;

        audioSource.pitch = randomPitch ? Random.Range(minPitch, maxPitch) : 1f;
        audioSource.PlayOneShot(clip, volume);

        if (debugLog)
        {
            Debug.Log(gameObject.name + " SFX: " + debugName + " / " + clip.name);
        }
    }
}