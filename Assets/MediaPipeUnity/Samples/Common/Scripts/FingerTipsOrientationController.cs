
using UnityEngine;
using System.Collections.Generic;

public class FingerTipsOrientationController : MonoBehaviour
{
    [Header("Cámara")]
    public Transform vrCamera;

    [Header("Orientación")]
    public float zDepthScale = 1f;
    public float rotationSmoothness = 15f;

    [Header("Escala")]
    public float referencePalmWidth = 0.18f;
    public float minScale = 0.65f;
    public float maxScale = 1.5f;
    public float scaleSmoothness = 10f;

    private class FingerCalibration
    {
        public Vector3 initialDirection;
        public Quaternion initialRotation;
    }

    private readonly Dictionary<Transform, FingerCalibration>
        calibrations = new Dictionary<Transform, FingerCalibration>();

    private readonly Dictionary<Transform, Vector3>
        initialScales = new Dictionary<Transform, Vector3>();

    public void UpdateHand(
        Transform thumb,
        Transform index,
        Transform middle,
        Transform ring,
        Transform pinky,
        Vector3 wristLandmark,
        Vector3 indexBaseLandmark,
        Vector3 middleBaseLandmark,
        Vector3 pinkyBaseLandmark,
        Vector3 thumbDirection,
        Vector3 indexDirection,
        Vector3 middleDirection,
        Vector3 ringDirection,
        Vector3 pinkyDirection)
    {
        Transform cam = vrCamera != null
            ? vrCamera
            : Camera.main != null
                ? Camera.main.transform
                : null;

        if (cam == null)
            return;

        // Cada dedo conserva su orientación inicial
        // y solamente recibe el cambio de dirección.

        ApplyFingerRotation(thumb, thumbDirection, cam);
ApplyFingerRotation(index, indexDirection, cam);
ApplyFingerRotation(middle, middleDirection, cam);
ApplyFingerRotation(ring, ringDirection, cam);
ApplyFingerRotation(pinky, pinkyDirection, cam);

        // Escala según el ancho aparente de la palma.

        Vector3 indexBase = LandmarkToLocal(indexBaseLandmark);
        Vector3 pinkyBase = LandmarkToLocal(pinkyBaseLandmark);

        float palmWidth = Vector3.Distance(
            indexBase,
            pinkyBase
        );

        if (referencePalmWidth <= 0f)
            return;

        float scaleFactor = Mathf.Clamp(
            palmWidth / referencePalmWidth,
            minScale,
            maxScale
        );

        ApplyScale(thumb, scaleFactor);
        ApplyScale(index, scaleFactor);
        ApplyScale(middle, scaleFactor);
        ApplyScale(ring, scaleFactor);
        ApplyScale(pinky, scaleFactor);
    }

    
private void ApplyFingerRotation(
    Transform finger,
    Vector3 direction,
    Transform cam)
{
    if (finger == null || direction.sqrMagnitude < 0.000001f)
        return;

    // Convertir la dirección del dedo al espacio mundial.
    Vector3 localDirection = new Vector3(
        direction.x,
        direction.y,
        direction.z * zDepthScale
    ).normalized;

    Vector3 worldDirection =
        cam.TransformDirection(localDirection).normalized;

    // La uña (+Z local) debe mirar hacia la cámara.
    Vector3 nailDirection = -cam.forward;

    // Evitar que la orientación de la uña interfiera
    // con la dirección longitudinal del dedo.
    nailDirection = Vector3.ProjectOnPlane(
        nailDirection,
        worldDirection
    ).normalized;

    // Protección si ambos vectores quedan casi paralelos.
    if (nailDirection.sqrMagnitude < 0.000001f)
    {
        nailDirection = Vector3.ProjectOnPlane(
            cam.up,
            worldDirection
        ).normalized;
    }

    // +Y local sigue el dedo.
    // +Z local mantiene la orientación de la uña.
    Quaternion targetRotation = Quaternion.LookRotation(
        nailDirection,
        worldDirection
    );

    float t = 1f - Mathf.Exp(
        -rotationSmoothness * Time.deltaTime
    );

    finger.rotation = Quaternion.Slerp(
        finger.rotation,
        targetRotation,
        t
    );
}


    private Vector3 LandmarkToLocal(Vector3 landmark)
    {
        return new Vector3(
            landmark.x,
            -landmark.y,
            landmark.z * zDepthScale
        );
    }

    private void ApplyScale(
        Transform finger,
        float scaleFactor)
    {
        if (finger == null)
            return;

        if (!initialScales.ContainsKey(finger))
            initialScales.Add(finger, finger.localScale);

        Vector3 targetScale =
            initialScales[finger] * scaleFactor;

        float t = 1f - Mathf.Exp(
            -scaleSmoothness * Time.deltaTime
        );

        finger.localScale = Vector3.Lerp(
            finger.localScale,
            targetScale,
            t
        );
    }
}
