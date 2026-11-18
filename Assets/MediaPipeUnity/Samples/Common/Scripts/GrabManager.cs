
using UnityEngine;

public class GrabManager : MonoBehaviour
{
    [Header("Manos")]
    public HandPinchController leftHand;
    public HandPinchController rightHand;

    [Header("Cardboard")]
    public Transform vrCamera;

    [Header("Raycast")]
    public float selectionDistance = 5f;
    public LayerMask selectableLayers = ~0;

    [Header("Seguimiento de manos")]
    public FingerTipsController fingerTipsController;

    [Header("Rayos visibles en VR")]
    public LineRenderer leftRay;
    public LineRenderer rightRay;

    private InteractiveShape[] shapes;
    private PillarSurface[] pillars;

    private InteractiveShape leftGrabbedObject;
    private InteractiveShape rightGrabbedObject;

    private InteractiveShape leftSelectedObject;
    private InteractiveShape rightSelectedObject;

    private PillarSurface leftSelectedPillar;
    private PillarSurface rightSelectedPillar;

    private bool leftWasPinching;
    private bool rightWasPinching;

    private void Start()
    {
        shapes = FindObjectsByType<InteractiveShape>(
            FindObjectsSortMode.None
        );

        pillars = FindObjectsByType<PillarSurface>(
            FindObjectsSortMode.None
        );

        ClearAllSelections();

        if (leftRay != null)
            leftRay.gameObject.SetActive(false);

        if (rightRay != null)
            rightRay.gameObject.SetActive(false);
    }

    private void Update()
    {
        UpdateVisibleRay(
            leftHand,
            leftRay,
            fingerTipsController != null &&
            fingerTipsController.IsLeftHandTracked
        );

        UpdateVisibleRay(
            rightHand,
            rightRay,
            fingerTipsController != null &&
            fingerTipsController.IsRightHandTracked
        );

        UpdatePillarSelection(leftHand, ref leftSelectedPillar);
        UpdatePillarSelection(rightHand, ref rightSelectedPillar);

        bool leftPinchStarted =
            leftHand != null &&
            leftHand.IsPinching &&
            !leftWasPinching;

        bool rightPinchStarted =
            rightHand != null &&
            rightHand.IsPinching &&
            !rightWasPinching;

        leftWasPinching =
            leftHand != null && leftHand.IsPinching;

        rightWasPinching =
            rightHand != null && rightHand.IsPinching;  

        bool leftTracked =
            fingerTipsController != null &&
            fingerTipsController.IsLeftHandTracked;

        bool rightTracked =
            fingerTipsController != null &&
            fingerTipsController.IsRightHandTracked;

        HandleHand(
            leftHand,
            ref leftGrabbedObject,
            ref leftSelectedObject,
            leftSelectedPillar,
            leftTracked
        );

        HandleHand(
            rightHand,
            ref rightGrabbedObject,
            ref rightSelectedObject,
            rightSelectedPillar,
            rightTracked
        );
    }

    
private void HandleHand(
    HandPinchController hand,
    ref InteractiveShape grabbedObject,
    ref InteractiveShape selectedObject,
    PillarSurface selectedPillar,
    bool handTracked)
{
    if (hand == null)
        return;

    // No seleccionar ni agarrar sin una mano detectada.
    if (!handTracked)
    {
        if (selectedObject != null)
        {
            selectedObject.SetSelected(false);
            selectedObject = null;
        }

        return;
    }

    // Mover el objeto mientras se mantiene el pinch.
    if (grabbedObject != null)
    {
        if (hand.IsPinching)
        {
            grabbedObject.MoveTo(
                hand.PinchMidpoint,
                GetCameraTransform()
            );
        }
        else
        {
            InteractiveShape objectToRelease = grabbedObject;

            bool placed = false;

            if (selectedPillar != null &&
                selectedPillar.placementPoint != null)
            {
                placed = selectedPillar.TryPlaceObject(
                    objectToRelease
                );
            }

            InteractiveAudio audio = GetAudio(objectToRelease);

if (placed)
{
    objectToRelease.Release(
        selectedPillar.placementPoint
    );

    if (audio != null)
        audio.OnPlacedOnPedestal();
}
else
{
    objectToRelease.Release();

    if (audio != null)
        audio.OnReleasedToFloor();
}

            grabbedObject = null;
        }

        return;
    }

    // Seleccionar el objeto apuntado.
    InteractiveShape target = FindTarget(hand);

    if (target != selectedObject)
    {
        if (selectedObject != null)
            selectedObject.SetSelected(false);

        selectedObject = target;

        if (selectedObject != null)
            selectedObject.SetSelected(true);
    }

    // Agarrar mientras el pinch está cerrado.
    if (hand.IsPinching && selectedObject != null)
    {
        InteractiveShape objectToGrab = selectedObject;

// Buscar el cuadro al que pertenece actualmente.
PillarSurface currentPillar =
    objectToGrab.GetComponentInParent<PillarSurface>();

if (currentPillar != null)
    currentPillar.RemoveObject(objectToGrab);

// Mantener la limpieza del resto de los pilares.
RemoveObjectFromPillars(objectToGrab);

        objectToGrab.GrabAt(
            hand.PinchMidpoint,
            GetCameraTransform()
        );  

        InteractiveAudio audio = GetAudio(objectToGrab);

        if (audio != null)
            audio.OnGrabbed();  

        grabbedObject = objectToGrab;

        selectedObject.SetSelected(false);
        selectedObject = null;
    }
}
    private InteractiveAudio GetAudio(InteractiveShape obj)
    {
        if (obj == null)
            return null;

        return obj.GetComponent<InteractiveAudio>();
    }

    private void RemoveObjectFromPillars(InteractiveShape obj)
    {
        if (pillars == null || obj == null)
            return;

        foreach (PillarSurface pillar in pillars)
        {
            if (pillar != null)
                pillar.RemoveObject(obj);
        }
    }

    private Transform GetCameraTransform()
    {
        if (vrCamera != null)
            return vrCamera;

        return Camera.main != null
            ? Camera.main.transform
            : null;
    }

    // Todos los raycasts parten del punto medio del pinch
    // y siguen la dirección de la cámara VR.
    private Ray GetHandRay(HandPinchController hand)
    {
        Transform cam = GetCameraTransform();

        Vector3 direction = cam != null
            ? cam.forward
            : Vector3.forward;

        return new Ray(hand.PinchMidpoint, direction);
    }

    private InteractiveShape FindTarget(HandPinchController hand)
    {
        if (hand == null || GetCameraTransform() == null)
            return null;

        RaycastHit[] hits = Physics.RaycastAll(
            GetHandRay(hand),
            selectionDistance,
            selectableLayers,
            QueryTriggerInteraction.Collide
        );

        InteractiveShape closest = null;
        float closestDistance = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            InteractiveShape shape =
                hit.collider.GetComponentInParent<InteractiveShape>();

            if (shape == null)
                continue;

            if (shape == leftGrabbedObject ||
                shape == rightGrabbedObject)
                continue;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closest = shape;
            }
        }

        return closest;
    }

    private Collider FindPillar(HandPinchController hand)
    {
        if (hand == null)
            return null;

        RaycastHit[] hits = Physics.RaycastAll(
            GetHandRay(hand),
            selectionDistance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        System.Array.Sort(
            hits,
            (a, b) => a.distance.CompareTo(b.distance)
        );

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<InteractiveShape>()
                != null)
                continue;

            PillarSurface pillar =
                hit.collider.GetComponentInParent<PillarSurface>();

            if (pillar == null)
                continue;

            Collider top = pillar.TopCollider;

            if (top != null && !top.isTrigger)
                return top;
        }

        return null;
    }

    private PillarSurface FindPillarTarget(
        HandPinchController hand)
    {
        if (hand == null)
            return null;

        RaycastHit[] hits = Physics.RaycastAll(
            GetHandRay(hand),
            selectionDistance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        System.Array.Sort(
            hits,
            (a, b) => a.distance.CompareTo(b.distance)
        );

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<InteractiveShape>()
                != null)
                continue;

            PillarSurface pillar =
                hit.collider.GetComponentInParent<PillarSurface>();

            if (pillar != null)
                return pillar;
        }

        return null;
    }

    private PillarSurface FindPillarSurface(
        HandPinchController hand)
    {
        if (hand == null)
            return null;

        RaycastHit[] hits = Physics.RaycastAll(
            GetHandRay(hand),
            selectionDistance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        System.Array.Sort(
            hits,
            (a, b) => a.distance.CompareTo(b.distance)
        );

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<InteractiveShape>()
                != null)
                continue;

            PillarSurface pillar =
                hit.collider.GetComponentInParent<PillarSurface>();

            if (pillar != null && pillar.placementPoint != null)
                return pillar;
        }

        return null;
    }

    private void UpdatePillarSelection(
        HandPinchController hand,
        ref PillarSurface selectedPillar)
    {
        if (hand == null)
            return;

        // Conservar el último pilar durante el frame
        // en que se abre el pinch.
        if (hand.PinchMidpoint == Vector3.zero)
            return;

        PillarSurface target = FindPillarTarget(hand);

        if (target == selectedPillar)
            return;

        if (selectedPillar != null)
            selectedPillar.SetSelected(false);

        selectedPillar = target;

        if (selectedPillar != null)
            selectedPillar.SetSelected(true);
    }

    private void ClearAllSelections()
    {
        if (shapes != null)
        {
            foreach (InteractiveShape shape in shapes)
            {
                if (shape != null)
                    shape.SetSelected(false);
            }
        }

        if (pillars != null)
        {
            foreach (PillarSurface pillar in pillars)
            {
                if (pillar != null)
                    pillar.SetSelected(false);
            }
        }
    }

    private void UpdateVisibleRay(
        HandPinchController hand,
        LineRenderer line,
        bool handTracked)
    {
        if (line == null)
            return;

        if (!handTracked || hand == null)
        {
            line.gameObject.SetActive(false);
            return;
        }

        Ray ray = GetHandRay(hand);

        line.gameObject.SetActive(true);
        line.positionCount = 2;
        line.useWorldSpace = true;

        line.SetPosition(0, ray.origin);
        line.SetPosition(
            1,
            ray.origin + ray.direction * selectionDistance
        );
    }

    private void OnDrawGizmos()
    {
        Transform cam = vrCamera != null
            ? vrCamera
            : Camera.main != null
                ? Camera.main.transform
                : null;

        if (cam == null)
            return;

        Gizmos.color = Color.cyan;

        if (leftHand != null)
        {
            Gizmos.DrawRay(
                leftHand.PinchMidpoint,
                cam.forward * selectionDistance
            );
        }

        Gizmos.color = Color.magenta;

        if (rightHand != null)
        {
            Gizmos.DrawRay(
                rightHand.PinchMidpoint,
                cam.forward * selectionDistance
            );
        }
    }
}
