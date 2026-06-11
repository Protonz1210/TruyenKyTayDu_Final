using UnityEngine;
using UnityEngine.InputSystem;

public class WukongSkillCooldown : MonoBehaviour
{
    [Header("HUD")]
    public MapHUDController hudController;

    [Header("Skill 1 - Attack1")]
    public float skill1ActionDuration = 5f;
    public float skill1CooldownDuration = 10f;

    [Header("Skill 2 - Attack2")]
    public float skill2ActionDuration = 4f;
    public float skill2CooldownDuration = 12f;

    [Header("Skill 3 - Attack3")]
    public float skill3ActionDuration = 5f;
    public int skill3RequiredPassive = 10;
    public int passiveGainPerHit = 1;
    public bool skill3ReadyAtStart = false;

    [Header("Test")]
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
        if (skill1CooldownTimer > 0f)
        {
            skill1CooldownTimer -= Time.deltaTime;

            if (skill1CooldownTimer < 0f)
                skill1CooldownTimer = 0f;
        }

        if (skill2CooldownTimer > 0f)
        {
            skill2CooldownTimer -= Time.deltaTime;

            if (skill2CooldownTimer < 0f)
                skill2CooldownTimer = 0f;
        }
    }

    void HandleTestKeys()
    {
        if (!enableTestKeys)
            return;

        // Test cộng nội tại cho chiêu 3 bằng phím 7
        if (Keyboard.current.digit7Key.wasPressedThisFrame)
        {
            AddPassiveStackFromHit();
        }

        // Test reset toàn bộ hồi chiêu bằng phím 8
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
        if (skill1CooldownTimer > 0f)
        {
            Debug.Log("Chiêu 1 đang hồi: " + Mathf.CeilToInt(skill1CooldownTimer) + "s");
            return false;
        }

        // Hồi chiêu bắt đầu ngay khi bấm chiêu
        skill1CooldownTimer = skill1CooldownDuration;

        Debug.Log("Dùng chiêu 1. Hành động: " + skill1ActionDuration + "s | Hồi chiêu: " + skill1CooldownDuration + "s");

        return true;
    }

    bool TryUseSkill2()
    {
        if (skill2CooldownTimer > 0f)
        {
            Debug.Log("Chiêu 2 đang hồi: " + Mathf.CeilToInt(skill2CooldownTimer) + "s");
            return false;
        }

        // Hồi chiêu bắt đầu ngay khi bấm chiêu
        skill2CooldownTimer = skill2CooldownDuration;

        Debug.Log("Dùng chiêu 2. Hành động: " + skill2ActionDuration + "s | Hồi chiêu: " + skill2CooldownDuration + "s");

        return true;
    }

    bool TryUseSkill3()
    {
        if (currentPassiveStack < skill3RequiredPassive)
        {
            Debug.Log("Chiêu 3 chưa đủ nội tại: " + currentPassiveStack + " / " + skill3RequiredPassive);
            return false;
        }

        currentPassiveStack = 0;

        Debug.Log("Dùng chiêu 3. Nội tại reset về 0.");

        return true;
    }

    public float GetSkillActionDuration(int skillIndex)
    {
        if (skillIndex == 1)
            return skill1ActionDuration;

        if (skillIndex == 2)
            return skill2ActionDuration;

        if (skillIndex == 3)
            return skill3ActionDuration;

        return 0f;
    }

    public void AddPassiveStackFromHit()
    {
        AddPassiveStackFromHit(passiveGainPerHit);
    }

    public void AddPassiveStackFromHit(int amount)
    {
        if (amount <= 0)
            return;

        currentPassiveStack += amount;

        if (currentPassiveStack > skill3RequiredPassive)
            currentPassiveStack = skill3RequiredPassive;

        Debug.Log("Tích nội tại chiêu 3: " + currentPassiveStack + " / " + skill3RequiredPassive);

        UpdateHUD();
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
            return;

        // Skill 1: 100% khi sẵn sàng, 0% khi vừa dùng
        float skill1Percent = 1f;

        if (skill1CooldownDuration > 0f)
        {
            skill1Percent = 1f - (skill1CooldownTimer / skill1CooldownDuration);
        }

        // Skill 2
        float skill2Percent = 1f;

        if (skill2CooldownDuration > 0f)
        {
            skill2Percent = 1f - (skill2CooldownTimer / skill2CooldownDuration);
        }

        // Skill 3: fill theo nội tại
        float skill3Percent = 0f;

        if (skill3RequiredPassive > 0)
        {
            skill3Percent = (float)currentPassiveStack / skill3RequiredPassive;
        }

        hudController.SetSkillCooldownFill(1, skill1Percent);
        hudController.SetSkillCooldownFill(2, skill2Percent);
        hudController.SetSkillCooldownFill(3, skill3Percent);
    }

    public bool IsSkill1Ready()
    {
        return skill1CooldownTimer <= 0f;
    }

    public bool IsSkill2Ready()
    {
        return skill2CooldownTimer <= 0f;
    }

    public bool IsSkill3Ready()
    {
        return currentPassiveStack >= skill3RequiredPassive;
    }

    public int GetCurrentPassiveStack()
    {
        return currentPassiveStack;
    }
}