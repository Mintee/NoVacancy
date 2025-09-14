using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SimpleLookDriver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraRoot;   // PlayerRoot/CameraRoot  (vCam's Tracking Target)
    [SerializeField] private Transform pitchPivot;   // PlayerRoot/CameraRoot/PitchPivot (vCam is a child of this)

    [Header("Mouse Sensitivity (deg per input unit)")]
    [SerializeField] private float mouseSensX = 0.12f;  // was 0.25; lowered to stop spinning
    [SerializeField] private float mouseSensY = 0.20f;

    [Header("Gamepad Sensitivity (deg per second)")]
    [SerializeField] private float gamepadYawSpeed   = 140f; // was 220
    [SerializeField] private float gamepadPitchSpeed = 120f; // was 180

    [Header("Deadzones")]
    [SerializeField] private float mouseYawDeadzoneX = 0.0f;
    [SerializeField] private float gamepadYawDeadzoneX = 0.15f;
    [SerializeField] private float gamepadPitchDeadzoneY = 0.15f;

    [Header("Pitch Limits")]
    [SerializeField] private float minPitch = -40f;
    [SerializeField] private float maxPitch = 60f;
    [SerializeField] private bool invertY = false;

    [Header("Aim Align (when aiming & no horiz input)")]
    [SerializeField] private float rotationSpeed = 540f;

    private float _pitch;

    private void Reset()
    {
        if (cameraRoot == null)
        {
            var t = transform.Find("CameraRoot");
            if (t) cameraRoot = t;
        }
        if (pitchPivot == null && cameraRoot != null)
        {
            var t = cameraRoot.Find("PitchPivot");
            if (t) pitchPivot = t;
        }
    }

    private void Update()
    {
        if (!pitchPivot) return;

        Vector2 look = InputReader.Look;

        // Device sniff
        bool isGamepad = false;
        var pi = InputReader.Instance ? InputReader.Instance.GetComponent<PlayerInput>() : null;
        if (pi && !string.IsNullOrEmpty(pi.currentControlScheme))
            isGamepad = pi.currentControlScheme.ToLower().Contains("gamepad");

        float deltaYawDeg;
        float deltaPitchDeg;

        if (isGamepad)
        {
            float lx = Mathf.Abs(look.x) >= gamepadYawDeadzoneX ? look.x : 0f;
            float ly = Mathf.Abs(look.y) >= gamepadPitchDeadzoneY ? look.y : 0f;

            deltaYawDeg   = lx * gamepadYawSpeed   * Time.deltaTime;
            deltaPitchDeg = ly * gamepadPitchSpeed * (invertY ? -1f : 1f) * Time.deltaTime;
        }
        else
        {
            float lx = Mathf.Abs(look.x) >= mouseYawDeadzoneX ? look.x : 0f;

            deltaYawDeg   = lx * mouseSensX;                          // mouse: per-frame delta
            deltaPitchDeg = look.y * mouseSensY * (invertY ? -1f : 1f);
        }

        // Pitch on pivot (camera-only vertical)
        _pitch = Mathf.Clamp(_pitch + deltaPitchDeg, minPitch, maxPitch);
        pitchPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

        // Yaw on player (body turn)
        if (Mathf.Abs(deltaYawDeg) > 0.0001f)
        {
            transform.Rotate(Vector3.up, deltaYawDeg, Space.Self);
        }
        else if (PlayerController.IsAiming)
        {
            // Align body to camera forward when aiming and no X input
            Vector3 f = GetFlatForward();
            if (f.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(f, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotationSpeed * Time.deltaTime);
            }
        }
    }

    private Vector3 GetFlatForward()
    {
        Transform refT = pitchPivot ? pitchPivot : (cameraRoot ? cameraRoot : transform);
        Vector3 f = refT.forward; f.y = 0f; return f.normalized;
    }

    // ----- Public API (menus) -----
    public void SetLookSensitivity(float s)
    {
        s = Mathf.Max(0.01f, s);
        mouseSensX        = 0.12f * s;
        mouseSensY        = 0.20f * s;
        gamepadYawSpeed   = 140f  * s;
        gamepadPitchSpeed = 120f  * s;
    }
    public void SetMouseSensitivityX(float v)    => mouseSensX = Mathf.Max(0.001f, v);
    public void SetMouseSensitivityY(float v)    => mouseSensY = Mathf.Max(0.001f, v);
    public void SetGamepadYawSpeed(float v)      => gamepadYawSpeed = Mathf.Max(1f, v);
    public void SetGamepadPitchSpeed(float v)    => gamepadPitchSpeed = Mathf.Max(1f, v);
    public void SetInvertY(bool invert)          => invertY = invert;
    public void SetMouseYawDeadzone(float v)     => mouseYawDeadzoneX = Mathf.Max(0f, v);
    public void SetGamepadYawDeadzone(float v)   => gamepadYawDeadzoneX = Mathf.Clamp01(v);
    public void SetGamepadPitchDeadzone(float v) => gamepadPitchDeadzoneY = Mathf.Clamp01(v);
}