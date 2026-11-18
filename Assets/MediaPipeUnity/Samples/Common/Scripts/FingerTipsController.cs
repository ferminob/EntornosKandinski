
using UnityEngine;
using Mediapipe.Unity;
using Mediapipe.Tasks.Vision.HandLandmarker;

public class FingerTipsController : MonoBehaviour
{
    [Header("MediaPipe")]
    public HandLandmarkerResultAnnotationController handController;

    [Header("Orientación de las puntas")]
    public FingerTipsOrientationController orientationController;

    [Header("Mano izquierda")]
    public Transform leftThumb;
    public Transform leftIndex;
    public Transform leftMiddle;
    public Transform leftRing;
    public Transform leftPinky;

    [Header("Mano derecha")]
    public Transform rightThumb;
    public Transform rightIndex;
    public Transform rightMiddle;
    public Transform rightRing;
    public Transform rightPinky;

    [Header("Muñeca")]
    public Transform leftWrist;
    public Transform rightWrist;

    [Header("Pinch")]
    public Transform leftPinchPoint;
    public Transform rightPinchPoint;

    [Header("Modo de movimiento")]
    public bool useRadialMode = false;

    [Header("Modo 2D")]
    public float movementRangeX = 25f;
    public float movementRangeY = 25f;

    [Header("Modo radial")]
    public float radialAngle = 180f;
    public float radialRadius = 5f;
    public float heightRange = 5f;

    [Header("Seguimiento Cardboard")]
    public Transform vrCamera;
    public float distanceFromCamera = 1.5f;

    public bool IsLeftHandTracked { get; private set; }
    public bool IsRightHandTracked { get; private set; }

    private void Update()
    {
        IsLeftHandTracked = false;
        IsRightHandTracked = false;

        SetLeftHandVisible(false);
        SetRightHandVisible(false);

        if (handController == null)
            return;

        var result = handController.CurrentTarget;

        if (result.handLandmarks == null ||
            result.handedness == null)
            return;

        int handCount;

        try
        {
            handCount = Mathf.Min(
                result.handLandmarks.Count,
                result.handedness.Count
            );
        }
        catch
        {
            return;
        }

        for (int i = 0; i < handCount; i++)
        {
            ProcessHand(result, i);
        }
    }

    private void ProcessHand(
        HandLandmarkerResult result,
        int index)
    {
        try
        {
            var handData = result.handLandmarks[index];

            if (handData.landmarks == null ||
                handData.landmarks.Count < 21)
                return;

            var landmarks = handData.landmarks;
            var categories = result.handedness[index].categories;

            if (categories == null || categories.Count == 0)
                return;

            string handedness = categories[0].categoryName;

            var thumb = landmarks[4];
            var indexFinger = landmarks[8];
            var middle = landmarks[12];
            var ring = landmarks[16];
            var pinky = landmarks[20];

            if (handedness == "Left")
            {
                UpdateHand(
                    thumb,
                    indexFinger,
                    middle,
                    ring,
                    pinky,
                    leftThumb,
                    leftIndex,
                    leftMiddle,
                    leftRing,
                    leftPinky,
                    leftWrist,
                    landmarks[0],
                    landmarks[5],
                    landmarks[9],
                    landmarks[17],
                    landmarks[3],
                    landmarks[7],
                    landmarks[11],
                    landmarks[15],
                    landmarks[19]
                );

                SetLeftHandVisible(true);
                IsLeftHandTracked = true;
            }
            else if (handedness == "Right")
            {
                UpdateHand(
                    thumb,
                    indexFinger,
                    middle,
                    ring,
                    pinky,
                    rightThumb,
                    rightIndex,
                    rightMiddle,
                    rightRing,
                    rightPinky,
                    rightWrist,
                    landmarks[0],
                    landmarks[5],
                    landmarks[9],
                    landmarks[17],
                    landmarks[3],
                    landmarks[7],
                    landmarks[11],
                    landmarks[15],
                    landmarks[19]
                );

                SetRightHandVisible(true);
                IsRightHandTracked = true;
            }
        }
        catch (System.ArgumentOutOfRangeException)
        {
            // Descartar el frame si MediaPipe modifica la colección.
        }
        catch (System.InvalidOperationException)
        {
            // Protección ante cambios de la colección.
        }
    }

    private void UpdateHand(
    Mediapipe.Tasks.Components.Containers.NormalizedLandmark thumbLandmark,
    Mediapipe.Tasks.Components.Containers.NormalizedLandmark indexLandmark,
    Mediapipe.Tasks.Components.Containers.NormalizedLandmark middleLandmark,
    Mediapipe.Tasks.Components.Containers.NormalizedLandmark ringLandmark,
    Mediapipe.Tasks.Components.Containers.NormalizedLandmark pinkyLandmark,
    Transform thumb,
    Transform index,
    Transform middle,
    Transform ring,
    Transform pinky,
    Transform wrist,
    Mediapipe.Tasks.Components.Containers.NormalizedLandmark wristLandmark,
    Mediapipe.Tasks.Components.Containers.NormalizedLandmark indexBaseLandmark,
    Mediapipe.Tasks.Components.Containers.NormalizedLandmark middleBaseLandmark,
    Mediapipe.Tasks.Components.Containers.NormalizedLandmark pinkyBaseLandmark,
    Mediapipe.Tasks.Components.Containers.NormalizedLandmark thumbJointLandmark,
    Mediapipe.Tasks.Components.Containers.NormalizedLandmark indexJointLandmark,
    Mediapipe.Tasks.Components.Containers.NormalizedLandmark middleJointLandmark,
    Mediapipe.Tasks.Components.Containers.NormalizedLandmark ringJointLandmark,
    Mediapipe.Tasks.Components.Containers.NormalizedLandmark pinkyJointLandmark
    )
{
    if (wrist != null)
        UpdateFinger(wrist, wristLandmark);

    if (thumb != null)
        UpdateFinger(thumb, thumbLandmark);

    if (index != null)
        UpdateFinger(index, indexLandmark);

    if (middle != null)
        UpdateFinger(middle, middleLandmark);

    if (ring != null)
        UpdateFinger(ring, ringLandmark);

    if (pinky != null)
        UpdateFinger(pinky, pinkyLandmark);

    if (orientationController != null)
{
    orientationController.UpdateHand(
        thumb,
        index,
        middle,
        ring,
        pinky,

        new Vector3(wristLandmark.x, wristLandmark.y, wristLandmark.z),
        new Vector3(indexBaseLandmark.x, indexBaseLandmark.y, indexBaseLandmark.z),
        new Vector3(middleBaseLandmark.x, middleBaseLandmark.y, middleBaseLandmark.z),
        new Vector3(pinkyBaseLandmark.x, pinkyBaseLandmark.y, pinkyBaseLandmark.z),

        new Vector3(
            thumbLandmark.x - thumbJointLandmark.x,
            -(thumbLandmark.y - thumbJointLandmark.y),
            thumbLandmark.z - thumbJointLandmark.z
        ),

        new Vector3(
            indexLandmark.x - indexJointLandmark.x,
            -(indexLandmark.y - indexJointLandmark.y),
            indexLandmark.z - indexJointLandmark.z
        ),

        new Vector3(
            middleLandmark.x - middleJointLandmark.x,
            -(middleLandmark.y - middleJointLandmark.y),
            middleLandmark.z - middleJointLandmark.z
        ),

        new Vector3(
            ringLandmark.x - ringJointLandmark.x,
            -(ringLandmark.y - ringJointLandmark.y),
            ringLandmark.z - ringJointLandmark.z
        ),

        new Vector3(
            pinkyLandmark.x - pinkyJointLandmark.x,
            -(pinkyLandmark.y - pinkyJointLandmark.y),
            pinkyLandmark.z - pinkyJointLandmark.z
        )
    );
  }
}
    // VISIBILIDAD

    private void SetLeftHandVisible(bool visible)
    {
        SetFingerVisible(leftThumb, visible);
        SetFingerVisible(leftIndex, visible);
        SetFingerVisible(leftMiddle, visible);
        SetFingerVisible(leftRing, visible);
        SetFingerVisible(leftPinky, visible);
        SetFingerVisible(leftWrist, visible);

        if (leftPinchPoint != null)
            leftPinchPoint.gameObject.SetActive(visible);
    }

    private void SetRightHandVisible(bool visible)
    {
        SetFingerVisible(rightThumb, visible);
        SetFingerVisible(rightIndex, visible);
        SetFingerVisible(rightMiddle, visible);
        SetFingerVisible(rightRing, visible);
        SetFingerVisible(rightPinky, visible);
        SetFingerVisible(rightWrist, visible);

        if (rightPinchPoint != null)
            rightPinchPoint.gameObject.SetActive(visible);
    }

    private void SetFingerVisible(Transform finger, bool visible)
    {
        if (finger != null)
            finger.gameObject.SetActive(visible);
    }

    // MOVIMIENTO

    private void UpdateFinger(
        Transform finger,
        Mediapipe.Tasks.Components.Containers.NormalizedLandmark landmark)
    {
        if (useRadialMode)
            UpdateRadial(finger, landmark);
        else
            Update2D(finger, landmark);
    }

    private void Update2D(
        Transform finger,
        Mediapipe.Tasks.Components.Containers.NormalizedLandmark landmark)
    {
        Transform cam = vrCamera != null
            ? vrCamera
            : Camera.main != null
                ? Camera.main.transform
                : null;

        if (cam == null)
            return;

        Vector3 localPosition = new Vector3(
            (landmark.x - 0.5f) * movementRangeX,
            (landmark.y - 0.5f) * -movementRangeY,
            distanceFromCamera
        );

        finger.position = cam.TransformPoint(localPosition);
    }

    private void UpdateRadial(
        Transform finger,
        Mediapipe.Tasks.Components.Containers.NormalizedLandmark landmark)
    {
        Transform cam = vrCamera != null
            ? vrCamera
            : Camera.main != null
                ? Camera.main.transform
                : null;

        if (cam == null)
            return;

        float normalizedX = Mathf.Clamp01(landmark.x);

        float angle = Mathf.Lerp(
            -radialAngle / 2f,
            radialAngle / 2f,
            normalizedX
        );

        float angleRad = angle * Mathf.Deg2Rad;
        float height = (landmark.y - 0.5f) * -heightRange;

        Vector3 localPosition = new Vector3(
            Mathf.Sin(angleRad) * radialRadius,
            height,
            Mathf.Cos(angleRad) * radialRadius
        );

        finger.position = cam.TransformPoint(localPosition);
    }
}
