
using UnityEngine;

public class SFXboss : MonoBehaviour
{
    [Header("Audio Source")]
    [Tooltip("AudioSource phát âm thanh. Nếu bỏ trống, script tự lấy hoặc tự tạo.")]
    public AudioSource audioSource;

    [Header("Attack")]
    [Tooltip("Danh sách âm thanh tấn công của Boss.")]
    public AudioClip[] attackClips;

    [Range(0f, 1f)]
    [Tooltip("Âm lượng attack.")]
    public float attackVolume = 0.8f;

    [Header("Die")]
    [Tooltip("Danh sách âm thanh chết của Boss")]
    public AudioClip[] dieClips;

    [Range(0f, 1f)]
    [Tooltip("Âm lượng die.")]
    public float dieVolume = 0.85f;

    [Header("Pitch")]
    [Tooltip("Bật random pitch nhẹ.")]
    public bool randomPitch = true;

    [Tooltip("Pitch thấp nhất.")]
    public float minPitch = 0.95f;

    [Tooltip("Pitch cao nhất.")]
    public float maxPitch = 1.05f;

    [Header("Cooldown")]
    [Tooltip("Chống lặp attack sound nếu Animation Event bị gọi gần nhau.")]
    public float attackCooldown = 0.05f;

    [Tooltip("Chỉ cho tiếng chết phát một lần.")]
    public bool dieOnlyOnce = true;

    [Header("Debug")]
    [Tooltip("Bật để xem log phát sound.")]
    public bool debugLog = false;

    private float lastAttackTime = -999f;
    private bool diePlayed;

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

    public void Attack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;

        lastAttackTime = Time.time;
        PlayRandom(attackClips, attackVolume, "Attack");
    }

    public void AttackIndex(int index)
    {
        if (Time.time - lastAttackTime < attackCooldown) return;

        lastAttackTime = Time.time;
        PlayIndex(attackClips, index, attackVolume, "AttackIndex " + index);
    }

    public void Die()
    {
        if (dieOnlyOnce && diePlayed) return;

        diePlayed = true;
        PlayRandom(dieClips, dieVolume, "Die");
    }

    public void DieIndex(int index)
    {
        if (dieOnlyOnce && diePlayed) return;

        diePlayed = true;
        PlayIndex(dieClips, index, dieVolume, "DieIndex " + index);
    }

    public void ResetDie()
    {
        diePlayed = false;
    }

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
            Debug.Log(gameObject.name + " SFXboss: " + debugName + " / " + clip.name);
        }
    }
}