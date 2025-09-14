using UnityEngine;

public class AimState : BaseState
{
    public AimState(PlayerStateMachine sm) : base(sm) { }

    public override void Enter()
    {
        // Tell Motor we’re aiming
        sm.Motor.SetAim(true);
    }

    public override void Tick(float dt)
    {
        Vector2 move  = InputReader.Move;
        bool crouch   = InputReader.IsCrouching;

        // No sprint when aiming; pass aim=true for speed/rotation rules already in Motor
        sm.Motor.ApplyIntent(move, isSprinting:false, isCrouching:crouch, isAiming:true, dt);
    }

    public override void Exit()
    {
        sm.Motor.SetAim(false);
    }
}