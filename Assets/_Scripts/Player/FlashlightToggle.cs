using UnityEngine;

public class FlashlightToggle : MonoBehaviour
{
    [SerializeField] private Light flashLight;

    private void Awake()
    {
        if (!flashLight) flashLight = GetComponentInChildren<Light>(true);
        InputReader.FlashlightEvent += Toggle;
    }

    private void OnDestroy()
    {
        InputReader.FlashlightEvent -= Toggle;
    }

    private void Toggle()
    {
        if (flashLight) flashLight.enabled = !flashLight.enabled;
    }
}