
using System.Collections;
using UnityEngine;

public class VRRecenter : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private Transform vrCamera;
    [SerializeField] private KeyCode recenterKey = KeyCode.R;

    private IEnumerator Start()
    {
        // Esperar a que OpenXR inicialice el seguimiento.
        yield return new WaitForSeconds(1f);

        Recenter();
    }

    private void Update()
    {
        if (Input.GetKeyDown(recenterKey))
        {
            Recenter();
        }
    }

    public void Recenter()
    {
        if (vrCamera == null)
        {
            Debug.LogWarning("VRRecenter: falta asignar la cámara.");
            return;
        }

        // Dirección horizontal hacia donde mira la cámara.
        Vector3 cameraForward = vrCamera.forward;
        cameraForward.y = 0f;

        if (cameraForward.sqrMagnitude < 0.001f)
            return;

        cameraForward.Normalize();

        // Queremos que esa dirección pase a ser el frente (Z+).
        float angle = Vector3.SignedAngle(
            cameraForward,
            Vector3.forward,
            Vector3.up
        );

        // Rotar el origen VR, no la cámara directamente.
        transform.RotateAround(
            vrCamera.position,
            Vector3.up,
            angle
        );

        Debug.Log("VR: vista recentrada. Corrección: " + angle);
    }
}
