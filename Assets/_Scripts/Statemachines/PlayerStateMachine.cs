using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerStateMachine : MonoBehaviour
{
    [Header("References")]
    public PlayerController Motor;   // assign your PlayerController

    [Header("Debug")]
    public BaseState Current;

    // Pre-created states
    public LocomotionState Locomotion { get; private set; }
    public AimState Aim { get; private set; }

    private void Awake()
    {
        if (Motor == null) Motor = GetComponent<PlayerController>();

        Locomotion = new LocomotionState(this);
        Aim = new AimState(this);
    }

    private void OnEnable()
    {
        if (InputReader.Instance != null) InputReader.AimChangedEvent += OnAimChanged;
        SwitchState(Locomotion);
    }

    private void OnDisable()
    {
        if (InputReader.Instance != null) InputReader.AimChangedEvent -= OnAimChanged;
    }

    private void Update()
    {
        Current?.Tick(Time.deltaTime);
    }

    public void SwitchState(BaseState next)
    {
        if (Current == next) return;
        Current?.Exit();
        Current = next;
        Current?.Enter();
    }

    private void OnAimChanged(bool aiming)
    {
        if (aiming) SwitchState(Aim);
        else SwitchState(Locomotion);
    }

}