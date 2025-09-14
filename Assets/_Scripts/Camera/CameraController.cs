using UnityEngine;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{
    [Header("Virtual Cameras")]
    [SerializeField] private CinemachineCamera LocoCam;  // exploration camera
    [SerializeField] private CinemachineCamera AimCam;   // aiming camera

    [Header("Priorities")]
    [SerializeField] private int LocoPriority = 10;
    [SerializeField] private int AimPriority = 11;

    private void OnEnable()
    {
        if (InputReader.Instance != null) InputReader.AimChangedEvent += SetAim;
    }

    private void OnDisable()
    {
        if (InputReader.Instance != null) InputReader.AimChangedEvent -= SetAim;
    }

    
    private void SetAim(bool aiming)
    {
        if (LocoCam != null) LocoCam.Priority = aiming ? LocoPriority : Mathf.Max(LocoPriority, AimPriority - 1);
        if (AimCam != null) AimCam.Priority = aiming ? Mathf.Max(AimPriority, LocoPriority + 1) : AimPriority - 2;
    }
}