using UnityEngine;

public abstract class BaseState
{
    protected readonly PlayerStateMachine sm;
    public string Name { get; private set; }

    protected BaseState(PlayerStateMachine stateMachine)
    {
        sm = stateMachine;
        Name = GetType().Name;
    }

    public virtual void Enter() { }
    public virtual void Tick(float deltaTime) { }
    public virtual void Exit() { }
}