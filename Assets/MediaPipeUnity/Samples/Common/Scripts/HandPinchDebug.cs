using UnityEngine;
using UnityEngine.UI;

public class HandPinchDebug : MonoBehaviour
{
    public HandPinchController leftHand;
    public HandPinchController rightHand;

    public Text debugText;

    void Update()
    {
        if (debugText == null)
            return;

        string leftStatus = "LEFT: ";

        if (leftHand != null && leftHand.IsPinching)
        {
            leftStatus += "PINCH";

            if (leftHand.ActiveFinger != null)
            {
                leftStatus +=
                    " (" + leftHand.ActiveFinger.name + ")";
            }
        }
        else
        {
            leftStatus += "OPEN";
        }

        string rightStatus = "RIGHT: ";

        if (rightHand != null && rightHand.IsPinching)
        {
            rightStatus += "PINCH";

            if (rightHand.ActiveFinger != null)
            {
                rightStatus +=
                    " (" + rightHand.ActiveFinger.name + ")";
            }
        }
        else
        {
            rightStatus += "OPEN";
        }

        debugText.text =
            leftStatus + "\n" +
            rightStatus;
    }
}