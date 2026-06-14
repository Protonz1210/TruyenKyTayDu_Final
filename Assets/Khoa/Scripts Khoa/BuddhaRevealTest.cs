using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuddhaRevealTest : MonoBehaviour
{
    [Header("Test Target")]
    [Tooltip("Controller điều khiển Phật Tổ đưa tay và bật bát.")]
    public BuddhaRevealController buddhaRevealController;

    [Header("Input System")]
    [Tooltip("Bật test bằng phím B.")]
    public bool enableTestKey = true;

    private bool isTesting;

    void Update()
    {
        if (!enableTestKey) return;
        if (isTesting) return;

        if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
        {
            StartCoroutine(TestRoutine());
        }
    }

    IEnumerator TestRoutine()
    {
        isTesting = true;

        if (buddhaRevealController != null)
        {
            yield return StartCoroutine(buddhaRevealController.RevealRoutine());
        }
        else
        {
            Debug.LogWarning("BuddhaRevealTest chưa gán BuddhaRevealController.");
        }

        isTesting = false;
    }
}