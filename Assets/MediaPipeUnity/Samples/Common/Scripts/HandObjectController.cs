using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity;

public class HandObjectController : MonoBehaviour
{
    public HandLandmarkerResultAnnotationController handController;
    public float movementRangeX = 5f;
    public float movementRangeY = 5f;


    void Update()
    {
        if (handController == null)
            return;

        var result = handController.CurrentTarget;

        if (result.handLandmarks == null || result.handLandmarks.Count == 0)
            return;

        if (result.handLandmarks[0].landmarks == null ||
            result.handLandmarks[0].landmarks.Count <= 9)
            return;

        var hand = result.handLandmarks[0].landmarks;

        // Punta del dedo índice
        var indexTip = hand[8];

        // Muñeca y base del dedo medio
        var wrist = hand[0];
        var middleMcp = hand[9];

        // Tamaño aparente de la mano
        float handSize = Vector2.Distance(
            new Vector2(wrist.x, wrist.y),
            new Vector2(middleMcp.x, middleMcp.y)
        );

        // Posición X/Y
        float x = indexTip.x;
        float y = indexTip.y;

        // Z aproximado a partir del tamaño de la mano
        float z = Mathf.Lerp(10f, 0f, Mathf.InverseLerp(0.08f, 0.25f, handSize));

        transform.position = new Vector3(
            (x - 0.5f) * movementRangeX,
            (y - 0.5f) * -movementRangeY,
            z
        );
    }
}