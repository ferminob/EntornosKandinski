using UnityEngine;

public class HandPinchController : MonoBehaviour
{
    [Header("Dedos")]
    public Transform thumb;
    public Transform index;
    public Transform middle;
    public Transform ring;
    public Transform pinky;

    [Header("Pinza")]
    public float pinchThreshold = 1.0f;

    public bool IsPinching { get; private set; }

    public Transform ActiveFinger { get; private set; }

    public Vector3 PinchMidpoint { get; private set; }

    [Header("Seguimiento")]
    public FingerTipsController fingerTipsController;

    [Tooltip("True para la mano izquierda, false para la derecha.")]
    public bool isLeftHand;

    [Header("Debug")]
    public Transform pinchPointVisual;

    void Update()
{
    if (fingerTipsController == null)
    {
        ResetPinch();
        return;
    }

    bool handTracked = isLeftHand
        ? fingerTipsController.IsLeftHandTracked
        : fingerTipsController.IsRightHandTracked;

    if (!handTracked)
    {
        ResetPinch();

        if (pinchPointVisual != null)
            pinchPointVisual.gameObject.SetActive(false);

        return;
    }


    if (thumb == null ||
        index == null ||
        middle == null ||
        ring == null ||
        pinky == null)
    {
        ResetPinch();
        return;
    }

    // =====================================================
    // PUNTO DE SELECCIÓN
    // =====================================================

    Transform closestFinger = GetClosestFinger();

    PinchMidpoint =
        (thumb.position + closestFinger.position) / 2f;

    // =====================================================
    // PINZA
    // =====================================================

    if (!IsPinching)
    {
        float distance = Vector3.Distance(
            thumb.position,
            closestFinger.position
        );

        if (distance <= pinchThreshold)
        {
            IsPinching = true;
            ActiveFinger = closestFinger;
        }
    }
    else
    {
        // Una vez iniciada la pinza,
        // SOLO importa el dedo que la inició.
        float distance = Vector3.Distance(
            thumb.position,
            ActiveFinger.position
        );

        if (distance > pinchThreshold)
        {
            ResetPinch();
            return;
        }

        // Mientras está agarrando,
        // el punto pasa a depender del dedo activo.
        PinchMidpoint =
            (thumb.position + ActiveFinger.position) / 2f;
    }

    // =====================================================
    // VISUAL DEL PUNTO
    // =====================================================

    if (pinchPointVisual != null)
    {
        pinchPointVisual.position = PinchMidpoint;
        pinchPointVisual.gameObject.SetActive(true);
    }
}

    void ResetPinch()
    {
        IsPinching = false;
        ActiveFinger = null;
        PinchMidpoint = Vector3.zero;
    }

    Transform GetClosestFinger()
    {
        Transform closest = index;

        float closestDistance = Vector3.Distance(
            thumb.position,
            index.position
        );

        CheckFinger(
            middle,
            ref closest,
            ref closestDistance
        );

        CheckFinger(
            ring,
            ref closest,
            ref closestDistance
        );

        CheckFinger(
            pinky,
            ref closest,
            ref closestDistance
        );

        return closest;
    }

    void CheckFinger(
        Transform finger,
        ref Transform closest,
        ref float closestDistance)
    {
        float distance = Vector3.Distance(
            thumb.position,
            finger.position
        );

        if (distance < closestDistance)
        {
            closestDistance = distance;
            closest = finger;
        }
    }
}