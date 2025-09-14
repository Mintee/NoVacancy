using UnityEngine;

[DisallowMultipleComponent]
public class PlayerStateMachine : MonoBehaviour
{
    [Header("References")]
    public PlayerController Motor;   // assign your PlayerController
    public InputReader Input;        // your input source (optional here)

    [Header("Debug")]
    public BaseState Current;

    // Pre-created states
    public LocomotionState Locomotion { get; private set; }

    private void Awake()
    {
        if (Motor == null) Motor = GetComponent<PlayerController>();

        Locomotion = new LocomotionState(this);
    }

    private void OnEnable()
    {
        SwitchState(Locomotion);
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
}