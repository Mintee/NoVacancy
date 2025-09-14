using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CameraDeviceSelector : MonoBehaviour
{
    [Header("Assign your two vCams")]
    // [SerializeField] private CinemachineCamera cmMouse;
    // [SerializeField] private CinemachineCamera cmGamepad;
    [SerializeField] private GameObject cmMouse;
    [SerializeField] private GameObject cmGamepad;

    private PlayerInput _pi;

    private void OnEnable()
    {
        InputReader.ControlSchemeChangedEvent += OnSchemeChanged;
    }

    private void Start()
    {
        _pi = InputReader.Instance ? InputReader.Instance.GetComponent<PlayerInput>() : null;
        Apply(_pi != null ? _pi.currentControlScheme : null);
    }

    private void OnDisable()
    {
        InputReader.ControlSchemeChangedEvent -= OnSchemeChanged;
    }

    private void OnSchemeChanged(string scheme)
    {
        Apply(scheme);
    }

    private void Apply(string scheme)
    {
        bool useGamepad = !string.IsNullOrEmpty(scheme) && scheme.ToLower().Contains("gamepad");

        // if (cmMouse)   cmMouse.Priority   = useGamepad ? inactivePriority : activePriority;
        // if (cmGamepad) cmGamepad.Priority = useGamepad ? activePriority   : inactivePriority;
        if (cmMouse)   cmMouse.SetActive(!useGamepad);
        if (cmGamepad) cmGamepad.SetActive(useGamepad);
    }
}