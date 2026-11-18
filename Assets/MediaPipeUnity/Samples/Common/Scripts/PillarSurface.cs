
using UnityEngine;

public class PillarSurface : MonoBehaviour
{
    [Header("Punto de colocación")]
    public Transform placementPoint;

    [Header("Superficie de apoyo")]
    public Collider topCollider;

    [Header("Contorno visual")]
    public GameObject selectionOutline;

    private InteractiveShape placedObject;

    public Collider TopCollider
    {
        get
        {
            return topCollider != null
                ? topCollider
                : GetComponentInChildren<Collider>();
        }
    }

    private void Awake()
    {
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectionOutline != null)
            selectionOutline.SetActive(selected);
    }

    public bool IsOccupied()
    {
        return placedObject != null;
    }

    public bool TryPlaceObject(InteractiveShape obj)
    {
        if (obj == null)
            return false;

        // El pilar no admite un segundo objeto.
        if (placedObject != null && placedObject != obj)
            return false;

        placedObject = obj;
        return true;
    }

    public void RemoveObject(InteractiveShape obj)
    {
        if (placedObject == obj)
            placedObject = null;
    }
}
