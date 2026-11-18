
using Google.XR.Cardboard;
using UnityEngine;

public class CardboardStartup : MonoBehaviour
{
    [Header("Botones de configuración MediaPipe")]
    public GameObject graphConfigButton;
    public GameObject imageSourceConfigButton;

    private void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.brightness = 1.0f;
    }

    public void Update()
    {
        // Engranaje de Cardboard -> Image Source Config
        if (Api.IsGearButtonPressed)
        {
            if (imageSourceConfigButton != null)
                imageSourceConfigButton.SetActive(true);
        }

        // Cruz de Cardboard -> Graph Config
        if (Api.IsCloseButtonPressed)
        {
            if (graphConfigButton != null)
                graphConfigButton.SetActive(true);
        }

        // Mantener el recentrado del visor.
        if (Api.IsTriggerHeldPressed)
        {
            Api.Recenter();
        }
    }
}
