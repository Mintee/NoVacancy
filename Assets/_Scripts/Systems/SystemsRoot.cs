using UnityEngine;

public class SystemsRoot : MonoBehaviour
{
    private static SystemsRoot _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);        // avoid duplicates when switching scenes
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);  // safe: this is a *root* object
    }
}