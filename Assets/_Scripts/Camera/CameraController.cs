using UnityEngine;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{
    [Header("Virtual Cameras")]
    [SerializeField] private CinemachineCamera cmShoulder;   // exploration camera
    [SerializeField] private CinemachineCamera cmAim;        // aiming camera

    [Header("Priorities")]
    [SerializeField] private int shoulderPriority = 10;
    [SerializeField] private int aimPriority = 20;

    private void OnEnable()
    {
        Apply(false);
    }

    private void Update()
    {
        Apply(PlayerController.IsAiming);
    }

    private void Apply(bool aiming)
    {
        if (!cmShoulder || !cmAim) return;

        if (aiming)
        {
            cmAim.Priority = aimPriority;
            cmShoulder.Priority = shoulderPriority - 1;
        }
        else
        {
            cmAim.Priority = shoulderPriority - 1;
            cmShoulder.Priority = shoulderPriority;
        }
    }
}