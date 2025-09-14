using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Animator animator;
    [SerializeField] private Light flashlight;

    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float sprintSpeed = 5.5f;
    [SerializeField] private float aimSpeed = 2.75f;
    [SerializeField] private float crouchSpeed = 1.8f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float rotationSpeed = 540f; // kept for future but not used when external yaw is true

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundStick = -2f;

    [Header("Crouch (CharacterController)")]
    [SerializeField] private float standHeight = 1.8f;
    [SerializeField] private float crouchHeight = 1.2f;
    [SerializeField] private float crouchLerpSpeed = 10f;

    [Header("Animator Params")]
    // [SerializeField] private string paramSpeed = "Speed";
    // [SerializeField] private string paramMoveX = "MoveX";
    // [SerializeField] private string paramMoveZ = "MoveZ";
    // [SerializeField] private string paramIsAiming = "IsAiming";
    // [SerializeField] private string paramIsSprinting = "IsSprinting";
    // [SerializeField] private string paramIsCrouching = "IsCrouching";

    [Header("Heading Control")]
    [Tooltip("If true, PlayerController will NOT rotate yaw; another script (SimpleLookDriver) owns rotation.")]
    [SerializeField] private bool externalYawControl = true; // <-- IMPORTANT: default true

    [Header("Cardinal Lock (Diagonal Resolver)")]
    [Tooltip("Force animation to the dominant axis (forward/back OR strafe) instead of blending diagonally.")]
    [SerializeField] private bool useCardinalLock = true;
    [Tooltip("Ignore tiny inputs to prevent foot shuffles.")]
    [SerializeField] private float lockDeadzone = 0.05f;
    [Tooltip("How much the other axis must exceed the current one to switch dominance. Higher = more stable.")]
    [SerializeField] private float lockHysteresis = 0.15f;
    [Tooltip("Damping used when writing MoveX/MoveZ to Animator.")]
    [SerializeField] private float animatorDamping = 0.12f;

    [Header("State Machine Control")]
    [Tooltip("If true, Update() will not drive movement. A PlayerStateMachine will call ApplyIntent().")]
    [SerializeField] private bool useStateMachine = true;

    public static bool IsAiming { get; private set; }

    private CharacterController cc;
    private Vector3 planarVelocity;
    private float verticalVelocity;

    private enum DominantAxis { None, X, Z }
    private DominantAxis _dominantAxis = DominantAxis.None;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!animator) animator = GetComponent<Animator>();
        if (!mainCamera) mainCamera = Camera.main;

        // Ensure CC starts at stand height
        cc.height = standHeight;
        var c = cc.center; c.y = standHeight * 0.5f; cc.center = c;

        // Input events
        InputReader.FlashlightEvent += OnFlashlight;
        InputReader.AimChangedEvent += OnAimChanged;
        InputReader.CrouchChangedEvent += OnCrouchChanged;

        IsAiming = InputReader.IsAiming;
        ApplyCrouchImmediate(InputReader.IsCrouching);
    }

    private void OnDestroy()
    {
        InputReader.FlashlightEvent -= OnFlashlight;
        InputReader.AimChangedEvent -= OnAimChanged;
        InputReader.CrouchChangedEvent -= OnCrouchChanged;
    }

    private void Update()
    {
        if (useStateMachine)
        {
            return; // SM will call ApplyIntent(...)
        }

        float dt = Time.deltaTime;
        Vector2 move = InputReader.Move;
        bool isSprinting = InputReader.IsSprinting;
        bool isCrouching = InputReader.IsCrouching;
        ApplyIntent(move, isSprinting, isCrouching, IsAiming, dt);
    }

    // ========= PUBLIC API FOR STATES =========
    public void ApplyIntent(Vector2 move, bool isSprinting, bool isCrouching, bool isAiming, float dt)
    {
        if (!mainCamera) return;

        // Camera-relative basis (for movement only)
        Vector3 camF = mainCamera.transform.forward; camF.y = 0f; camF.Normalize();
        Vector3 camR = mainCamera.transform.right; camR.y = 0f; camR.Normalize();

        float targetSpeed =
            isCrouching ? crouchSpeed :
            isAiming ? aimSpeed :
            isSprinting ? sprintSpeed : walkSpeed;

        // Keep stick magnitude so half-tilt = half speed
        Vector3 desiredDir = camF * move.y + camR * move.x;
        float inputMag = Mathf.Clamp01(move.magnitude);
        Vector3 desiredVel = (desiredDir.sqrMagnitude > 0.0001f)
            ? desiredDir.normalized * (targetSpeed * inputMag)
            : Vector3.zero;

        planarVelocity = Vector3.MoveTowards(planarVelocity, desiredVel, acceleration * dt);

        // Gravity
        bool grounded = cc.isGrounded;
        if (grounded && verticalVelocity < 0f) verticalVelocity = groundStick;
        verticalVelocity += gravity * dt;

        // Move
        Vector3 motion = planarVelocity; motion.y = verticalVelocity;
        cc.Move(motion * dt);

        // Crouch height
        float targetHeight = isCrouching ? crouchHeight : standHeight;
        cc.height = Mathf.MoveTowards(cc.height, targetHeight, crouchLerpSpeed * dt);
        var center = cc.center; center.y = cc.height * 0.5f; cc.center = center;

        // Yaw (if we own it)
        if (!externalYawControl)
        {
            Vector3 faceDir = Vector3.zero;
            if (isAiming) faceDir = camF;
            else
            {
                Vector3 pv = planarVelocity; pv.y = 0f;
                if (pv.sqrMagnitude > 0.001f) faceDir = pv.normalized;
            }
            if (faceDir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(faceDir, Vector3.up),
                    rotationSpeed * dt);
        }

        // Animator axes (local, signed)
        Vector3 localDesired = transform.InverseTransformDirection(desiredDir);
        Vector2 moveAxes = new Vector2(localDesired.x, localDesired.z);

        // Deadzone
        if (moveAxes.magnitude < lockDeadzone)
        {
            moveAxes = Vector2.zero;
            _dominantAxis = DominantAxis.None;
        }

        // Cardinal Lock (kept because it feels best for this project; can disable in Inspector)
        if (useCardinalLock && moveAxes != Vector2.zero)
        {
            float ax = Mathf.Abs(moveAxes.x);
            float az = Mathf.Abs(moveAxes.y);

            switch (_dominantAxis)
            {
                case DominantAxis.None: _dominantAxis = (az >= ax) ? DominantAxis.Z : DominantAxis.X; break;
                case DominantAxis.X: if (az > ax + lockHysteresis) _dominantAxis = DominantAxis.Z; break;
                case DominantAxis.Z: if (ax > az + lockHysteresis) _dominantAxis = DominantAxis.X; break;
            }

            if (_dominantAxis == DominantAxis.X) moveAxes = new Vector2(Mathf.Sign(moveAxes.x) * Mathf.Min(1f, ax), 0f);
            else if (_dominantAxis == DominantAxis.Z) moveAxes = new Vector2(0f, Mathf.Sign(moveAxes.y) * Mathf.Min(1f, az));
        }
        else
        {
            float maxAbs = Mathf.Max(Mathf.Abs(moveAxes.x), Mathf.Abs(moveAxes.y), 1f);
            moveAxes /= maxAbs;
        }

        // Normalized speed (0..1) for Animator Speed param
        float maxPlanarSpeed = Mathf.Max(walkSpeed, Mathf.Max(sprintSpeed, Mathf.Max(aimSpeed, crouchSpeed)));
        float normalizedSpeed01 = Mathf.Clamp01(new Vector2(planarVelocity.x, planarVelocity.z).magnitude / maxPlanarSpeed);

        // Animator
        if (animator)
        {
            animator.SetFloat(AnimParams.MoveX, moveAxes.x, animatorDamping, dt);
            animator.SetFloat(AnimParams.MoveZ, moveAxes.y, animatorDamping, dt);
            animator.SetFloat(AnimParams.Speed, normalizedSpeed01, 0.1f, dt);

            animator.SetBool(AnimParams.IsAiming, isAiming);
            animator.SetBool(AnimParams.IsSprinting, isSprinting && !isAiming && !isCrouching);
            animator.SetBool(AnimParams.IsCrouching, isCrouching);
        }
    }

    public void SetAim(bool aiming) { IsAiming = aiming; }

    private void OnFlashlight() { if (flashlight) flashlight.enabled = !flashlight.enabled; }
    private void OnAimChanged(bool aiming) { IsAiming = aiming; }
    private void OnCrouchChanged(bool crouched) { /* handled in ApplyIntent */ }

    private void ApplyCrouchImmediate(bool crouched)
    {
        cc.height = crouched ? crouchHeight : standHeight;
        var c = cc.center; c.y = cc.height * 0.5f; cc.center = c;
    }
    
    // ---------- Animator wrapper API (no magic strings in states) ----------
    public void PlayAttack(float chargeSeconds = 0f)
    {
        if (!animator) return;
        animator.SetFloat(AnimParams.ChargeTime, chargeSeconds);
        animator.ResetTrigger(AnimParams.Attack);
        animator.SetTrigger(AnimParams.Attack);
    }

    public void StopAttack()
    {
        if (!animator) return;
        animator.ResetTrigger(AnimParams.Attack);
    }

    public void StartCharge()
    {
        if (!animator) return;
        animator.ResetTrigger(AnimParams.ChargeStart);
        animator.SetTrigger(AnimParams.ChargeStart);
    }

    public void StopCharge()
    {
        if (!animator) return;
        animator.ResetTrigger(AnimParams.ChargeStart);
    }

    // Generic helpers (ID-based, so still no strings)
    public void SetAnimBool(int id, bool value)    { if (animator) animator.SetBool(id, value); }
    public void SetAnimFloat(int id, float value, float dampTime = 0f, float dt = 0f)
    {
        if (!animator) return;
        if (dampTime > 0f) animator.SetFloat(id, value, dampTime, dt);
        else animator.SetFloat(id, value);
    }
    public void SetAnimTrigger(int id) { if (animator) animator.SetTrigger(id); }
    public void ResetAnimTrigger(int id) { if (animator) animator.ResetTrigger(id); }

}