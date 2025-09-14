using UnityEngine;

public static class AnimParams
{
    // Floats
    public static readonly int Speed      = Animator.StringToHash("Speed");
    public static readonly int MoveX      = Animator.StringToHash("MoveX");
    public static readonly int MoveZ      = Animator.StringToHash("MoveZ");
    public static readonly int ChargeTime = Animator.StringToHash("ChargeTime");

    // Bools
    public static readonly int IsAiming    = Animator.StringToHash("IsAiming");
    public static readonly int IsSprinting = Animator.StringToHash("IsSprinting");
    public static readonly int IsCrouching = Animator.StringToHash("IsCrouching");

    // Triggers
    public static readonly int Attack      = Animator.StringToHash("Attack");
    public static readonly int ChargeStart = Animator.StringToHash("ChargeStart");

    // (Add more here as controller grows)
}