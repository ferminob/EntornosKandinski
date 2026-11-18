
using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;

public class HandTrackingAnchor : MonoBehaviour
{
    [Header("Referencias")]
    public FingerTipsController fingerTips;
    public Transform modelRoot;

    [Header("Seguimiento")]
    public bool trackRightHand = true;

    [Header("Ajuste de posicion")]
    public Vector3 positionOffset = Vector3.zero;

    private void LateUpdate()
    {
        if (fingerTips == null || modelRoot == null)
            return;

        var handController = fingerTips.handController;

        if (handController == null)
            return;

        var result = handController.CurrentTarget;

        if (result.handLandmarks == null ||
            result.handedness == null)
            return;

        int count = Mathf.Min(
            result.handLandmarks.Count,
            result.handedness.Count
        );

        for (int i = 0; i < count; i++)
        {
            var categories = result.handedness[i].categories;

            if (categories == null || categories.Count == 0)
                continue;

            string side = categories[0].categoryName;

            if (trackRightHand && side != "Right")
                continue;

            if (!trackRightHand && side != "Left")
                continue;

            var landmarks = result.handLandmarks[i].landmarks;

            if (landmarks == null || landmarks.Count < 21)
                continue;

            UpdatePosition(landmarks[0]);
            modelRoot.gameObject.SetActive(true);
            return;
        }

        modelRoot.gameObject.SetActive(false);
    }

    private void UpdatePosition(
        Mediapipe.Tasks.Components.Containers.NormalizedLandmark wrist)
    {
        Transform cam = fingerTips.vrCamera != null
            ? fingerTips.vrCamera
            : Camera.main != null
                ? Camera.main.transform
                : null;

        if (cam == null)
            return;

        Vector3 localPosition = new Vector3(
            (wrist.x - 0.5f) * fingerTips.movementRangeX,
            (wrist.y - 0.5f) * -fingerTips.movementRangeY,
            fingerTips.distanceFromCamera
        );

        transform.position = cam.TransformPoint(
            localPosition + positionOffset
        );
        // Calcular inclinación de la palma
Vector2 indexBase = new Vector2(
    0.5f,
    0.5f
);

Vector2 pinkyBase = new Vector2(
    0.5f,
    0.5f
);

// Se obtienen los landmarks desde MediaPipe
var result = fingerTips.handController.CurrentTarget;

// Buscar la mano derecha
for (int i = 0; i < result.handLandmarks.Count; i++)
{
    if (result.handedness[i].categories[0].categoryName != "Right")
        continue;

    var points = result.handLandmarks[i].landmarks;

    indexBase = new Vector2(
        points[5].x,
        -points[5].y
    );

    pinkyBase = new Vector2(
        points[17].x,
        -points[17].y
    );

    break;
}

Vector2 palmDirection = pinkyBase - indexBase;

float angle = Mathf.Atan2(
    palmDirection.y,
    palmDirection.x
) * Mathf.Rad2Deg;

transform.rotation = cam.rotation *
    Quaternion.Euler(0f, 0f, angle);
    }
}
