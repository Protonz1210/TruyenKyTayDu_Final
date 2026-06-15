using UnityEngine;
using UnityEngine.InputSystem;

public class WukongSkillCooldown : MonoBehaviour
{
    [Header("HUD")]
    [Tooltip("UI hiển thị hồi chiêu và nội tại.")]
    public MapHUDController hudController;

    [Header("Tutorial Lock")]
    [Tooltip("Bật/tắt hệ thống hồi chiêu. Tắt đi thì skill luôn sẵn sàng và không bị đưa vào hồi chiêu.")]
    public bool cooldownEnabled = true;

    [Header("Skill 1 - Attack1")]
    [Tooltip("Thời gian thực hiện skill 1.")]
    public float skill1ActionDuration = 5f;

    [Tooltip("Thời gian hồi chiêu skill 1.")]
    public float skill1CooldownDuration = 10f;

    [Header("Skill 2 - Attack2")]
    [Tooltip("Thời gian thực hiện skill 2.")]
    public float skill2ActionDuration = 4f;

    [Tooltip("Thời gian hồi chiêu skill 2.")]
    public float skill2CooldownDuration = 12f;

    [Header("Skill 3 - Attack3")]
    [Tooltip("Thời gian thực hiện skill 3.")]
    public float skill3ActionDuration = 5f;

    [Tooltip("Số nội tại cần để dùng skill 3.")]
    public int skill3RequiredPassive = 10;

    [Tooltip("Số nội tại nhận mỗi lần đánh trúng.")]
    public int passiveGainPerHit = 1;

    [Tooltip("Skill 3 sẵn sàng từ đầu.")]
    public bool skill3ReadyAtStart = false;

    [Header("Test")]
    [Tooltip("Bật phím test skill 3: 7_8")]
    public bool enableTestKeys = true;

    private float skill1CooldownTimer;
    private float skill2CooldownTimer;

    private int currentPassiveStack;

    void Start()
    {
        if (skill3ReadyAtStart)
        {
            currentPassiveStack = skill3RequiredPassive;
        }
        else
        {
            currentPassiveStack = 0;
        }

        UpdateHUD();
    }

    void Update()
    {
        UpdateCooldownTimers();
        HandleTestKeys();
        UpdateHUD();
    }

    void UpdateCooldownTimers()
    {
        // Trong tutorial thì không chạy hồi chiêu.
        if (!cooldownEnabled)
        {
            skill1CooldownTimer = 0f;
            skill2CooldownTimer = 0f;
            currentPassiveStack = skill3RequiredPassive;
            return;
        }

        if (skill1CooldownTimer > 0f)
        {
            skill1CooldownTimer -= Time.deltaTime;

            if (skill1CooldownTimer < 0f)
            {
                skill1CooldownTimer = 0f;
            }
        }

        if (skill2CooldownTimer > 0f)
        {
            skill2CooldownTimer -= Time.deltaTime;

            if (skill2CooldownTimer < 0f)
            {
                skill2CooldownTimer = 0f;
            }
        }
    }
    void HandleTestKeys()
    {
        if (!enableTestKeys)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.digit7Key.wasPressedThisFrame)
        {
            AddPassiveStackFromHit();
        }

        if (Keyboard.current.digit8Key.wasPressedThisFrame)
        {
            ResetAllCooldowns();
        }
    }

    public bool TryUseSkill(int skillIndex)
    {
        if (skillIndex == 1)
        {
            return TryUseSkill1();
        }

        if (skillIndex == 2)
        {
            return TryUseSkill2();
        }

        if (skillIndex == 3)
        {
            return TryUseSkill3();
        }

        return false;
    }

    bool TryUseSkill1()
    {
        // Trong tutorial: cho dùng skill nhưng không đưa vào hồi chiêu.
        if (!cooldownEnabled)
        {
            skill1CooldownTimer = 0f;
            Debug.Log("Dùng chiêu 1 trong tutorial. Không hồi chiêu.");
            return true;
        }

        if (skill1CooldownTimer > 0f)
        {
            Debug.Log("Chiêu 1 đang hồi: " + Mathf.CeilToInt(skill1CooldownTimer) + "s");
            return false;
        }

        skill1CooldownTimer = skill1CooldownDuration;

        Debug.Log("Dùng chiêu 1. Hành động: " + skill1ActionDuration + "s | Hồi chiêu: " + skill1CooldownDuration + "s");

        return true;
    }

    bool TryUseSkill2()
    {
        // Trong tutorial: cho dùng skill nhưng không đưa vào hồi chiêu.
        if (!cooldownEnabled)
        {
            skill2CooldownTimer = 0f;
            Debug.Log("Dùng chiêu 2 trong tutorial. Không hồi chiêu.");
            return true;
        }

        if (skill2CooldownTimer > 0f)
        {
            Debug.Log("Chiêu 2 đang hồi: " + Mathf.CeilToInt(skill2CooldownTimer) + "s");
            return false;
        }

        skill2CooldownTimer = skill2CooldownDuration;

        Debug.Log("Dùng chiêu 2. Hành động: " + skill2ActionDuration + "s | Hồi chiêu: " + skill2CooldownDuration + "s");

        return true;
    }

    bool TryUseSkill3()
    {
        // Trong tutorial: cho dùng skill 3 luôn, không cần nội tại.
        if (!cooldownEnabled)
        {
            currentPassiveStack = skill3RequiredPassive;
            UpdateHUD();

            Debug.Log("Dùng chiêu 3 trong tutorial. Không cần nội tại.");
            return true;
        }

        if (currentPassiveStack < skill3RequiredPassive)
        {
            Debug.Log("Chiêu 3 chưa đủ nội tại: " + currentPassiveStack + " / " + skill3RequiredPassive);
            return false;
        }

        currentPassiveStack = 0;

        Debug.Log("Dùng chiêu 3. Nội tại reset về 0.");

        UpdateHUD();

        return true;
    }

    public float GetSkillActionDuration(int skillIndex)
    {
        if (skillIndex == 1)
        {
            return skill1ActionDuration;
        }

        if (skillIndex == 2)
        {
            return skill2ActionDuration;
        }

        if (skillIndex == 3)
        {
            return skill3ActionDuration;
        }

        return 0f;
    }

    public void AddPassiveStackFromHit()
    {
        AddPassiveStackFromHit(passiveGainPerHit);
    }

    public void AddPassiveStackFromHit(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentPassiveStack += amount;

        if (currentPassiveStack > skill3RequiredPassive)
        {
            currentPassiveStack = skill3RequiredPassive;
        }

        Debug.Log("Tích nội tại chiêu 3: " + currentPassiveStack + " / " + skill3RequiredPassive);

        UpdateHUD();
    }

    public void GainPassiveByHit()
    {
        AddPassiveStackFromHit(passiveGainPerHit);
    }

    public void ResetAllCooldowns()
    {
        skill1CooldownTimer = 0f;
        skill2CooldownTimer = 0f;
        currentPassiveStack = skill3RequiredPassive;

        UpdateHUD();

        Debug.Log("Reset toàn bộ hồi chiêu.");
    }

    void UpdateHUD()
    {
        if (hudController == null)
        {
            return;
        }

        float skill1Percent = 1f;

        if (skill1CooldownDuration > 0f)
        {
            skill1Percent = 1f - (skill1CooldownTimer / skill1CooldownDuration);
        }

        skill1Percent = Mathf.Clamp01(skill1Percent);

        float skill2Percent = 1f;

        if (skill2CooldownDuration > 0f)
        {
            skill2Percent = 1f - (skill2CooldownTimer / skill2CooldownDuration);
        }

        skill2Percent = Mathf.Clamp01(skill2Percent);

        float skill3Percent = 0f;

        if (skill3RequiredPassive > 0)
        {
            skill3Percent = (float)currentPassiveStack / skill3RequiredPassive;
        }

        skill3Percent = Mathf.Clamp01(skill3Percent);

        hudController.SetSkillCooldownFill(1, skill1Percent);
        hudController.SetSkillCooldownFill(2, skill2Percent);
        hudController.SetSkillCooldownFill(3, skill3Percent);
    }

    public bool IsSkill1Ready()
    {
        if (!cooldownEnabled)
        {
            return true;
        }

        return skill1CooldownTimer <= 0f;
    }

    public bool IsSkill2Ready()
    {
        if (!cooldownEnabled)
        {
            return true;
        }

        return skill2CooldownTimer <= 0f;
    }

    public bool IsSkill3Ready()
    {
        if (!cooldownEnabled)
        {
            return true;
        }

        return currentPassiveStack >= skill3RequiredPassive;
    }

    public int GetCurrentPassiveStack()
    {
        return currentPassiveStack;
    }
    public void SetCooldownEnabled(bool enabled)
    {
        cooldownEnabled = enabled;

        // Mỗi lần bật/tắt chế độ cooldown đều đưa skill về trạng thái sẵn sàng.
        ResetAllCooldowns();

        if (cooldownEnabled)
        {
            Debug.Log("WukongSkillCooldown: Đã bật lại hồi chiêu.");
        }
        else
        {
            Debug.Log("WukongSkillCooldown: Đã tắt hồi chiêu trong Tutorial.");
        }
    }

    public bool IsCooldownEnabled()
    {
        return cooldownEnabled;
    }
}