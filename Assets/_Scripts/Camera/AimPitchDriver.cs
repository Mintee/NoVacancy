using UnityEngine;

/// <summary>
/// Adds an *additive* local X (pitch) to a chest bone so the body follows camera pitch while aiming,
/// and smoothly blends that additive back to 0 when aim is released.
/// Place on Player (or any manager). Runs in LateUpdate so it applies *after* Animator poses.
/// </summary>
public class AimPitchDriver : MonoBehaviour
{
    [Header("References")]
    public Transform PlayerRoot;   // character root that already owns yaw
    public Transform ChestBone;    // the bone to pitch (UpperChest/Spine2)
    public Camera AimCamera;       // usually the main camera

    [Header("Pitch Limits (deg)")]
    public float MinPitchDeg = -35f;   // down
    public float MaxPitchDeg =  40f;   // up

    [Header("Blending (deg/sec)")]
    public float BlendInSpeedDegPerSec  = 720f;   // how fast we match camera while aiming
    public float BlendOutSpeedDegPerSec = 540f;   // how fast we return to 0 when not aiming

    // runtime
    private float _additivePitchDeg = 0f;  // additive offset we apply on top of the animated pose

    private void Reset()
    {
        if (AimCamera == null)  AimCamera  = Camera.main;
        if (PlayerRoot == null) PlayerRoot = GetComponentInParent<Transform>();
    }

    private void LateUpdate()
    {
        if (!PlayerRoot || !ChestBone || !AimCamera) return;

        // Cache the *animated* local rotation before we modify it,
        // so we can apply our additive on top (preserves Y/Z from animation).
        Quaternion animatedLocal = ChestBone.localRotation;

        // Compute desired camera-relative pitch in player-local space
        Vector3 localCamFwd = Quaternion.Inverse(PlayerRoot.rotation) * AimCamera.transform.forward;
        float horiz = Mathf.Max(0.0001f, new Vector2(localCamFwd.z, localCamFwd.x).magnitude);
        float desiredPitchDeg = Mathf.Atan2(localCamFwd.y, horiz) * Mathf.Rad2Deg;
        float clampedDesired  = Mathf.Clamp(desiredPitchDeg, MinPitchDeg, MaxPitchDeg);

        // Choose target additive based on aim state: follow camera when aiming, otherwise zero it out
        bool aiming = PlayerController.IsAiming; // your existing flag
        float targetAdditive = aiming ? clampedDesired : 0f;

        // Blend additive toward target
        float speed = aiming ? BlendInSpeedDegPerSec : BlendOutSpeedDegPerSec;
        _additivePitchDeg = Mathf.MoveTowards(_additivePitchDeg, targetAdditive, speed * Time.deltaTime);

        // Apply additive pitch around local X on top of the animated pose
        Quaternion add = Quaternion.AngleAxis(-_additivePitchDeg, Vector3.right);
        ChestBone.localRotation = add * animatedLocal;
    }
}