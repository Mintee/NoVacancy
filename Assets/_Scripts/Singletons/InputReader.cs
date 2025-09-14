using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// Singleton Input Reader using generated interface binding (compile-time safety).
/// Expects an Input Actions asset that generates `InputSystem_Actions` with a `Player` map.
/// Implements IPlayerActions so Unity forces you to wire new actions when added.
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class InputReader : MonoBehaviour, InputSystem_Actions.IPlayerActions
{
    public static InputReader Instance { get; private set; }

    // === Static pollables ===
    public static Vector2 Move { get; private set; }
    public static Vector2 Look { get; private set; }
    public static bool IsSprinting { get; private set; }
    public static bool IsPaused { get; private set; }
    public static bool IsAiming { get; private set; }
    public static bool IsCrouching { get; private set; }  // toggle

    // === Events ===
    public static event Action<bool> AimChangedEvent;
    public static event Action<bool> CrouchChangedEvent;
    public static event Action JumpEvent;
    public static event Action InteractEvent;
    public static event Action FlashlightEvent;
    public static event Action ToggleViewEvent;
    public static event Action<bool> PauseChangedEvent;
    public static event Action<string> ControlSchemeChangedEvent;

    private InputSystem_Actions controls;
    private PlayerInput playerInput;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError($"[InputReader] Multiple instances detected: {Instance.name} and {name}. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        controls = new InputSystem_Actions();
        controls.Player.SetCallbacks(this);

        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.onControlsChanged += HandleControlsChanged;
            if (!string.IsNullOrEmpty(playerInput.currentControlScheme))
                ControlSchemeChangedEvent?.Invoke(playerInput.currentControlScheme);
        }
    }

    private void OnEnable()
    {
        controls.Enable();
        controls.Player.Enable();
    }

    private void OnDisable()
    {
        controls.Player.Disable();
        controls.Disable();
    }

    private void OnDestroy()
    {
        if (playerInput != null)
            playerInput.onControlsChanged -= HandleControlsChanged;

        if (Instance == this) Instance = null;
    }

    private void HandleControlsChanged(PlayerInput pi)
    {
        ControlSchemeChangedEvent?.Invoke(pi.currentControlScheme ?? string.Empty);
    }

    // ====== IPlayerActions callbacks ======
    public void OnMove(InputAction.CallbackContext context) => Move = context.ReadValue<Vector2>();
    public void OnLook(InputAction.CallbackContext context) => Look = context.ReadValue<Vector2>();
    public void OnJump(InputAction.CallbackContext context) { if (context.performed) JumpEvent?.Invoke(); }
    public void OnSprint(InputAction.CallbackContext context) { if (context.performed) IsSprinting = true; else if (context.canceled) IsSprinting = false; }
    public void OnInteract(InputAction.CallbackContext context) { if (context.performed) InteractEvent?.Invoke(); }
    public void OnFlashlight(InputAction.CallbackContext context) { if (context.performed) FlashlightEvent?.Invoke(); }
    public void OnAttack(InputAction.CallbackContext context) { /* implement when ready */ }
    public void OnToggleView(InputAction.CallbackContext context) { if (context.performed) ToggleViewEvent?.Invoke(); }
    
    public void OnAim(InputAction.CallbackContext context)
    {
        // Hold: true while performed/pressed, false on canceled
        if (context.performed) { IsAiming = true; AimChangedEvent?.Invoke(true); }
        if (context.canceled) { IsAiming = false; AimChangedEvent?.Invoke(false); }
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        // Toggle: flip on performed
        if (context.performed)
        {
            IsCrouching = !IsCrouching;
            CrouchChangedEvent?.Invoke(IsCrouching);
        }
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            IsPaused = !IsPaused;
            PauseChangedEvent?.Invoke(IsPaused);
        }
    }
}