using UnityEngine;
using UnityEngine.UI;
using Mediapipe.Unity;

public class HandDebugUI : MonoBehaviour
{
    public HandLandmarkerResultAnnotationController handController;
    public Text debugText;
    public bool IsHandOpen { get; private set; }

    void Update()
    {
        if (handController == null)
        {
            debugText.text = "Hand Controller: NONE";
            return;
        }

        var result = handController.CurrentTarget;

        if (result.handLandmarks == null || result.handLandmarks.Count == 0)
        {
            debugText.text = "Hand detected: NO";
            return;
        }

        var hand = result.handLandmarks[0].landmarks;

        // Comprobar que tenemos los 21 landmarks
        if (hand == null || hand.Count <= 20)
        {
            debugText.text = "Hand detected: INCOMPLETE";
            return;
        }

        var indexTip = hand[8];
        var wrist = hand[0];
        var middleMcp = hand[9];

        bool indexExtended = IsFingerExtended(hand[8], hand[6]);
        bool middleExtended = IsFingerExtended(hand[12], hand[10]);
        bool ringExtended = IsFingerExtended(hand[16], hand[14]);
        bool pinkyExtended = IsFingerExtended(hand[20], hand[18]);

        bool handOpen =
            indexExtended &&
            middleExtended &&
            ringExtended &&
            pinkyExtended;
        
        IsHandOpen = handOpen;

        float handSize = Vector2.Distance(
            new Vector2(wrist.x, wrist.y),
            new Vector2(middleMcp.x, middleMcp.y)
        );

        float z = Mathf.Lerp(
            4f,
            1f,
            Mathf.InverseLerp(0.08f, 0.25f, handSize)
        );

        debugText.text =
            "Hand detected: YES\n" +
            $"Hand size: {handSize:F3}\n" +
            $"X: {indexTip.x:F3}\n" +
            $"Y: {indexTip.y:F3}\n" +
            $"Z: {z:F2}\n" +
            $"Hand: {(handOpen ? "OPEN" : "CLOSED")}";
    }

    bool IsFingerExtended(
        Mediapipe.Tasks.Components.Containers.NormalizedLandmark tip,
        Mediapipe.Tasks.Components.Containers.NormalizedLandmark pip)
    {
        return tip.y < pip.y;
    }
}