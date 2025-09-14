using UnityEngine;

public class LocomotionState : BaseState
{
    public LocomotionState(PlayerStateMachine sm) : base(sm) { }

    public override void Enter()
    {
        // Ensure we're not in aim rules for basic locomotion
        sm.Motor.SetAim(false);
    }

    public override void Tick(float dt)
    {
        // Read your existing input values
        Vector2 move   = InputReader.Move;
        bool isSprint  = InputReader.IsSprinting && !InputReader.IsCrouching;
        bool isCrouch  = InputReader.IsCrouching;

        // Drive the motor
        sm.Motor.ApplyIntent(move, isSprint, isCrouch, isAiming:false, dt);
    }
}