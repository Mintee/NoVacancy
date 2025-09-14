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
    [SerializeField] private string paramSpeed = "Speed";
    [SerializeField] private string paramIsAiming = "IsAiming";
    [SerializeField] private string paramIsSprinting = "IsSprinting";
    [SerializeField] private string paramIsCrouching = "IsCrouching";

    [Header("Heading Control")]
    [Tooltip("If true, PlayerController will NOT rotate yaw; another script (SimpleLookDriver) owns rotation.")]
    [SerializeField] private bool externalYawControl = true; // <-- IMPORTANT: default true

    public static bool IsAiming { get; private set; }

    private CharacterController cc;
    private Vector3 planarVelocity;
    private float verticalVelocity;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        // if (!animator) animator = GetComponent<Animator>();
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
        if (!mainCamera) return;
        float dt = Time.deltaTime;

        // Inputs
        Vector2 move = InputReader.Move;
        bool isSprinting = InputReader.IsSprinting;
        bool isCrouching = InputReader.IsCrouching;

        // Camera-relative basis (for movement only)
        Vector3 camF = mainCamera.transform.forward; camF.y = 0f; camF.Normalize();
        Vector3 camR = mainCamera.transform.right;   camR.y = 0f; camR.Normalize();

        float targetSpeed =
            isCrouching ? crouchSpeed :
            IsAiming    ? aimSpeed    :
            isSprinting ? sprintSpeed : walkSpeed;

        Vector3 desiredDir = camF * move.y + camR * move.x;
        Vector3 desiredVel = desiredDir.normalized * targetSpeed;
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

        // ---- NO YAW ROTATION HERE WHEN externalYawControl == true ----
        if (!externalYawControl)
        {
            Vector3 faceDir = Vector3.zero;

            if (IsAiming)
                faceDir = camF; // face camera yaw when aiming
            else
            {
                Vector3 pv = planarVelocity; pv.y = 0f;
                if (pv.sqrMagnitude > 0.001f) faceDir = pv.normalized;
            }

            if (faceDir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(faceDir, Vector3.up), rotationSpeed * dt);
        }

        // Animator
        if (animator)
        {
            float speed = new Vector2(planarVelocity.x, planarVelocity.z).magnitude;
            animator.SetFloat(paramSpeed, speed);
            animator.SetBool(paramIsAiming, IsAiming);
            animator.SetBool(paramIsSprinting, isSprinting && !IsAiming && !isCrouching);
            animator.SetBool(paramIsCrouching, isCrouching);
        }
    }

    private void OnFlashlight() { if (flashlight) flashlight.enabled = !flashlight.enabled; }
    private void OnAimChanged(bool aiming) { IsAiming = aiming; }
    private void OnCrouchChanged(bool crouched) { /* handled in Update */ }

    private void ApplyCrouchImmediate(bool crouched)
    {
        cc.height = crouched ? crouchHeight : standHeight;
        var c = cc.center; c.y = cc.height * 0.5f; cc.center = c;
    }
}