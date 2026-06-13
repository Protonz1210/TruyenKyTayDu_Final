using UnityEngine;

public class PlayerFacing : MonoBehaviour
{
    public bool IsFacingRight { get; private set; } = true;

    public void SetFacingRight(bool value)
    {
        IsFacingRight = value;
    }
}