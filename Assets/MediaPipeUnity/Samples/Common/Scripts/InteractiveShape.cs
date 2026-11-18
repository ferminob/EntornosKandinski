
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class InteractiveShape : MonoBehaviour
{
    [Header("Visual")]
    public GameObject selectionOutline;

    [Header("Agarre")]
    public float pinchThreshold = 1.0f;
    public Transform grabPoint;

    private bool isSelected;
    private bool isGrabbed;
    private Rigidbody rb;

    // Punto al que está vinculado cuando se coloca.
    private Transform currentPlacementPoint;

    public bool IsSelected => isSelected;
    public bool IsGrabbed => isGrabbed;
    public float PinchThreshold => pinchThreshold;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (grabPoint == null)
        {
            Transform found = transform.Find("GrabPoint");

            if (found != null)
                grabPoint = found;
        }
    }

    private void Start()
    {
        if (selectionOutline != null)
            selectionOutline.SetActive(false);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectionOutline != null)
            selectionOutline.SetActive(selected);
    }

    public void Grab()
    {
        GrabAt(transform.position);
    }

    public void GrabAt(Vector3 pinchPosition)
    {
        GrabAt(pinchPosition, null);
    }

    public void GrabAt(
        Vector3 pinchPosition,
        Transform cameraTransform)
    {
        if (isGrabbed)
            return;

        // Separar el objeto del cuadro al agarrarlo.
        if (currentPlacementPoint != null)
        {
            transform.SetParent(null, true);
            currentPlacementPoint = null;
        }

        isGrabbed = true;

        // Limpiar velocidades antes de convertirlo en cinemático.
        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        rb.useGravity = false;
        rb.isKinematic = true;

        AlignGrabPoint(pinchPosition, cameraTransform);

        SetSelected(true);
    }

    public void MoveTo(Vector3 position)
    {
        MoveTo(position, null);
    }

    public void MoveTo(
        Vector3 position,
        Transform cameraTransform)
    {
        if (!isGrabbed)
            return;

        AlignGrabPoint(position, cameraTransform);
    }

    private void AlignGrabPoint(
        Vector3 desiredPosition,
        Transform cameraTransform)
    {
        if (grabPoint == null)
        {
            transform.position = desiredPosition;
            return;
        }

        if (cameraTransform != null)
        {
            Quaternion desiredPointRotation =
                Quaternion.LookRotation(
                    -cameraTransform.forward,
                    cameraTransform.up
                );

            transform.rotation =
                desiredPointRotation *
                Quaternion.Inverse(grabPoint.localRotation);
        }

        Vector3 pointOffset =
            grabPoint.position - transform.position;

        transform.position =
            desiredPosition - pointOffset;
    }

    public void Release(Transform placementPoint = null)
    {
        if (!isGrabbed)
            return;

        isGrabbed = false;

        if (placementPoint != null)
        {
            Vector3 pointOffset =
                grabPoint != null
                    ? grabPoint.position - transform.position
                    : Vector3.zero;

            transform.position =
                placementPoint.position - pointOffset;

            // Vincular el objeto al cuadro.
            transform.SetParent(placementPoint, true);

            currentPlacementPoint = placementPoint;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }
        else
        {
            // Asegurar independencia al soltar en el piso.
            transform.SetParent(null, true);
            currentPlacementPoint = null;

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        SetSelected(false);
    }
}
