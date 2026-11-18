using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla la secuencia inicial y el ciclo de tres etapas de Entornos Kandinski.
/// Los prefabs orbitales deben tener InteractiveShape, Rigidbody y Collider.
/// </summary>
public class EventController : MonoBehaviour
{
    [System.Serializable]

    private class OrbitingObject
{
    public InteractiveShape shape;
    public Transform grabPoint;
    public Quaternion grabPointRelativeRotation;

    public float angle;
    public float radius;
    public float heightOffset;
    public float angularSpeed;
    public float phase;
    public float lifeTimer;
}
    public CuadrosOrbitManager cuadrosOrbitManager;

    [Header("Iluminación")]

        public Light luzInicial;
        public Light luzSecundaria;

        [SerializeField] private float intensidadMaxima = 1f;
        [SerializeField] private float duracionFadeLuces = 2f;

        private Coroutine transicionLuces;

    [Header("Cámara y centro orbital")]
        [SerializeField] private Camera targetCamera;
        [Tooltip("Idealmente, el Transform raíz del jugador. Si queda vacío, se usa la cámara.")]
        [SerializeField] private Transform orbitCenter;
        [SerializeField] private Vector3 orbitCenterOffset = new Vector3(0f, 0f, 0f);

    [Header("Secuencia inicial (se ejecuta una sola vez)")]
        [SerializeField] private InteractiveShape[] initialShapes;
        [SerializeField] private GameObject[] initialWalls;
        [SerializeField] private GameObject[] initialPaintings;
        [SerializeField] private float wallFallDistance = 12f;
        [SerializeField] private float wallDownDistance = 5f;
        [SerializeField] private float wallRotationDegrees = 70f;
        [SerializeField] private float initialElementsDisappearAfter = 4f;
        [SerializeField] private float delayBeforeFirstStage = 3f;

    [Header("Spawn de objetos")]
        [SerializeField] private GameObject[] objectPrefabs;
        [SerializeField] private float spawnInterval = 3f;
        [SerializeField] private float objectLifetime = 60f;

        [SerializeField] private float orbitRadiusMin = 2f;
        [SerializeField] private float orbitRadiusMax = 4f;
        [SerializeField] private float orbitHeightMin = -0.5f;
        [SerializeField] private float orbitHeightMax = 1.5f;
        [SerializeField] private float orbitSpeedMin = 12f;
        [SerializeField] private float orbitSpeedMax = 25f;
        [SerializeField] private float orbitBobAmplitude = 0.2f;
        [SerializeField] private float orbitBobSpeed = 1f;

        [Header("Skybox")]
        [SerializeField] private float skyboxFadeDuration = 3f;

        public Material skyboxMaterial;
        private float skyboxExposureOriginal;
        private Color skyboxTintOriginal;
        private bool usaExposure;
        private bool usaTint;

    private readonly List<OrbitingObject> orbitingObjects = new List<OrbitingObject>();
    private bool initialSequenceStarted;
    private bool spawningStarted;
    private float spawnTimer;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

            if (luzInicial != null)
                luzInicial.intensity = 0f;

            if (luzSecundaria != null)
                luzSecundaria.intensity = 0f;

        PrepararSkybox();
            
    }

    private void Start()
    {

        if (targetCamera == null)
        {
            Debug.LogError("EventController: no se encontró una cámara. Asigná Main Camera.", this);
            enabled = false;
            return;
        }

        CambiarLuces(intensidadMaxima, 0f);

        // Las formas iniciales comienzan suspendidas, sin gravedad.
        if (initialShapes != null) foreach (InteractiveShape shape in initialShapes)
        {
            if (shape == null) continue;
            Rigidbody rb = shape.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;
                rb.isKinematic = true;
            }
        }
    }

    private void Update()
{
    if (!initialSequenceStarted)
    {
        CheckInitialGrab();
        return;
    }

    UpdateOrbitingObjects();

    if (!spawningStarted)
        return;

    spawnTimer += Time.deltaTime;

    if (spawnTimer >= Mathf.Max(0.1f, spawnInterval))
    {
        spawnTimer = 0f;
        SpawnObject();
    }
}

    private void CheckInitialGrab()
    {
        if (initialShapes == null) return;

        foreach (InteractiveShape shape in initialShapes)
        {
            if (shape != null && shape.IsGrabbed)
            {
                initialSequenceStarted = true;

                CambiarLuces(0f, intensidadMaxima);

                if (cuadrosOrbitManager != null)
                    cuadrosOrbitManager.IniciarCuadros();

                StartCoroutine(PlayInitialSequence(shape));
                return;
            }
        }
    }

    private IEnumerator PlayInitialSequence(InteractiveShape grabbedInitialShape)
    {
        Vector3 awayDirection = targetCamera != null ? -targetCamera.transform.forward : Vector3.back;
        awayDirection.y = Mathf.Min(awayDirection.y, -0.15f);
        awayDirection.Normalize();

        // La forma agarrada no debe recibir gravedad mientras está en la mano.
        foreach (InteractiveShape shape in initialShapes)
        {
            if (shape == null || shape == grabbedInitialShape || shape.IsGrabbed) continue;
            Rigidbody rb = shape.GetComponent<Rigidbody>();
            if (rb == null) continue;
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (initialWalls != null)
{
    for (int i = 0; i < initialWalls.Length; i++)
    {
        GameObject wall = initialWalls[i];

        if (wall == null)
            continue;

        Vector3 rotationAxis;
        float rotationDirection;

        switch (i)
        {
            case 0: // Frente: +X
                rotationAxis = Vector3.right;
                rotationDirection = 1f;
                break;

            case 1: // Atrás: -X
                rotationAxis = Vector3.right;
                rotationDirection = -1f;
                break;

            case 2: // Izquierda: +Z
                rotationAxis = Vector3.forward;
                rotationDirection = 1f;
                break;

            case 3: // Derecha: -Z
                rotationAxis = Vector3.forward;
                rotationDirection = -1f;
                break;

            default:
                rotationAxis = Vector3.right;
                rotationDirection = 1f;
                break;
        }

        StartCoroutine(
            AnimateWallFall(wall, rotationAxis, rotationDirection)
        );
    }
}

if (initialPaintings != null)
{
    foreach (GameObject painting in initialPaintings)
    {
        if (painting != null)
            StartCoroutine(AnimatePaintingFall(painting));
    }
}

        yield return new WaitForSeconds(initialElementsDisappearAfter);

        if (initialWalls != null)
        {
            foreach (GameObject wall in initialWalls)
            {
                if (wall != null) Destroy(wall);
            }
        }
        if (initialPaintings != null)
        {
            foreach (GameObject painting in initialPaintings)
            {
                if (painting != null)
                    Destroy(painting);
            }
        }

        yield return new WaitForSeconds(Mathf.Max(0f, delayBeforeFirstStage));

        yield return StartCoroutine(FadeSkybox());

        spawningStarted = true;
        spawnTimer = 0f;

        SpawnObject();
    }

    // Animación por Transform para evitar problemas con MeshCollider cóncavos
    // al convertir paredes estáticas en Rigidbody dinámicos.
private IEnumerator AnimateWallFall(
    GameObject wall,
    Vector3 rotationAxis,
    float rotationDirection)
{
    if (wall == null)
        yield break;

    Vector3 startPosition = wall.transform.position;
    Quaternion startRotation = wall.transform.rotation;

    float duration = Mathf.Max(0.1f, initialElementsDisappearAfter);
    float elapsed = 0f;

    while (elapsed < duration && wall != null)
    {
        elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(elapsed / duration);
        float eased = t * t;

        // Todas las paredes descienden por el eje -Y.
        wall.transform.position =
            startPosition + Vector3.down * (wallDownDistance * eased);

        // Cada pared rota sobre su eje correspondiente.
        wall.transform.rotation =
            startRotation * Quaternion.AngleAxis(
                wallRotationDegrees * eased * rotationDirection,
                rotationAxis
            );

        yield return null;
    }
}

    private void SpawnObject()
{
    if (!spawningStarted || objectPrefabs == null || objectPrefabs.Length == 0)
        return;

    GameObject prefab = objectPrefabs[Random.Range(0, objectPrefabs.Length)];

    if (prefab == null)
        return;

    Transform center = orbitCenter != null ? orbitCenter : targetCamera.transform;

    float angle = Random.Range(0f, 360f);
    float radius = Random.Range(orbitRadiusMin, orbitRadiusMax);
    float height = Random.Range(orbitHeightMin, orbitHeightMax);

    Vector3 spawnPosition = GetOrbitPosition(center, angle, radius, height, 0f);

    GameObject instance = Instantiate(prefab, spawnPosition, prefab.transform.rotation);

    InteractiveShape shape = instance.GetComponent<InteractiveShape>();

    if (shape == null)
    {
        Debug.LogError($"El prefab '{prefab.name}' no tiene InteractiveShape.", prefab);
        Destroy(instance);
        return;
    }

    Rigidbody rb = instance.GetComponent<Rigidbody>();

    if (rb == null)
    {
        Debug.LogError($"El prefab '{prefab.name}' no tiene Rigidbody.", prefab);
        Destroy(instance);
        return;
    }

    Transform grabPoint = null;

// Buscamos GrabPoint incluso si está dentro de otros hijos.
Transform[] children = instance.GetComponentsInChildren<Transform>(true);

foreach (Transform child in children)
{
    if (child.name == "GrabPoint")
    {
        grabPoint = child;
        break;
    }
}

if (grabPoint == null)
{
    Debug.LogError(
        $"El prefab '{prefab.name}' no tiene un hijo llamado GrabPoint.",
        prefab
    );

    Destroy(instance);
    return;
}

// Guardamos la rotación del GrabPoint relativa a la raíz.
Quaternion grabPointRelativeRotation =
    Quaternion.Inverse(instance.transform.rotation) * grabPoint.rotation;

    rb.useGravity = false;
    rb.isKinematic = true;

    orbitingObjects.Add(new OrbitingObject
{
    shape = shape,
    grabPoint = grabPoint,
    grabPointRelativeRotation = grabPointRelativeRotation,

    angle = angle,
    radius = radius,
    heightOffset = height,
    angularSpeed = Random.Range(orbitSpeedMin, orbitSpeedMax)
        * (Random.value < 0.5f ? -1f : 1f),
    phase = Random.Range(0f, 100f),
    lifeTimer = 0f
});
}
    private void UpdateOrbitingObjects()
{
    Transform center = orbitCenter != null
        ? orbitCenter
        : targetCamera != null ? targetCamera.transform : null;

    if (center == null)
        return;

    for (int i = orbitingObjects.Count - 1; i >= 0; i--)
    {
        OrbitingObject orbital = orbitingObjects[i];

        if (orbital.shape == null)
        {
            orbitingObjects.RemoveAt(i);
            continue;
        }

        // Si fue agarrado, deja de pertenecer al sistema orbital.
        if (orbital.shape.IsGrabbed)
        {
            orbitingObjects.RemoveAt(i);
            continue;
        }

        // Cada objeto tiene su propio tiempo de vida.
        orbital.lifeTimer += Time.deltaTime;

        if (orbital.lifeTimer >= Mathf.Max(0.1f, objectLifetime))
        {
            Destroy(orbital.shape.gameObject);
            orbitingObjects.RemoveAt(i);
            continue;
        }

        // Movimiento orbital.
        orbital.angle += orbital.angularSpeed * Time.deltaTime;

        float bob = Mathf.Sin(
            (Time.time + orbital.phase) * orbitBobSpeed
        ) * orbitBobAmplitude;

        orbital.shape.transform.position = GetOrbitPosition(
            center,
            orbital.angle,
            orbital.radius,
            orbital.heightOffset,
            bob
        );

        // Orientación hacia el jugador usando GrabPoint como referencia.
        if (targetCamera != null && orbital.grabPoint != null)
        {
            Vector3 direction =
                targetCamera.transform.position -
                orbital.grabPoint.position;

            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion desiredGrabPointRotation =
                    Quaternion.LookRotation(direction, Vector3.up);

                orbital.shape.transform.rotation =
                    desiredGrabPointRotation *
                    Quaternion.Inverse(orbital.grabPointRelativeRotation);
            }
        }
    }
}
    private Vector3 GetOrbitPosition(Transform center, float angleDegrees, float radius, float height, float bob)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;
        Vector3 localOffset = new Vector3(Mathf.Cos(radians) * radius, height + bob, Mathf.Sin(radians) * radius);
        return center.position + orbitCenterOffset + center.rotation * localOffset;
    }

private IEnumerator AnimatePaintingFall(GameObject painting)
{
    if (painting == null)
        yield break;

    Vector3 startPosition = painting.transform.position;

    float duration = Mathf.Max(0.1f, initialElementsDisappearAfter);
    float elapsed = 0f;

    while (elapsed < duration && painting != null)
    {
        elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(elapsed / duration);
        float eased = t * t;

        painting.transform.position =
            startPosition + Vector3.down * (wallDownDistance * eased);

        yield return null;
    }
}

private void CambiarLuces(float intensidadInicial, float intensidadSecundaria)
{
    if (transicionLuces != null)
        StopCoroutine(transicionLuces);

    transicionLuces = StartCoroutine(
        FadeLuces(intensidadInicial, intensidadSecundaria)
    );
}

private IEnumerator FadeLuces(float destinoInicial, float destinoSecundaria)
{
    float inicioInicial = luzInicial != null
        ? luzInicial.intensity : 0f;

    float inicioSecundaria = luzSecundaria != null
        ? luzSecundaria.intensity : 0f;

    float tiempo = 0f;
    float duracion = Mathf.Max(0.01f, duracionFadeLuces);

    while (tiempo < duracion)
    {
        tiempo += Time.deltaTime;

        float t = Mathf.Clamp01(tiempo / duracion);

        if (luzInicial != null)
        {
            luzInicial.intensity = Mathf.Lerp(
                inicioInicial,
                destinoInicial,
                t
            );
        }

        if (luzSecundaria != null)
        {
            luzSecundaria.intensity = Mathf.Lerp(
                inicioSecundaria,
                destinoSecundaria,
                t
            );
        }

        yield return null;
    }

    if (luzInicial != null)
        luzInicial.intensity = destinoInicial;

    if (luzSecundaria != null)
        luzSecundaria.intensity = destinoSecundaria;

    transicionLuces = null;
}

private void PrepararSkybox()
{
    if (skyboxMaterial == null)
{
    Debug.LogError("No asignaste el material del Skybox.", this);
    return;
}
skyboxMaterial = new Material(skyboxMaterial);
RenderSettings.skybox = skyboxMaterial;
    usaTint = skyboxMaterial.HasProperty("_Tint");
    usaExposure = skyboxMaterial.HasProperty("_Exposure");

    if (usaTint)
    {
        skyboxTintOriginal = skyboxMaterial.GetColor("_Tint");
        skyboxMaterial.SetColor("_Tint", Color.black);
    }

    if (usaExposure)
    {
        skyboxExposureOriginal = skyboxMaterial.GetFloat("_Exposure");
        skyboxMaterial.SetFloat("_Exposure", 0f);
    }
}

private IEnumerator FadeSkybox()
{
    if (skyboxMaterial == null)
        yield break;

    float tiempo = 0f;
    float duracion = Mathf.Max(0.01f, skyboxFadeDuration);

    while (tiempo < duracion)
    {
        tiempo += Time.deltaTime;
        float t = Mathf.Clamp01(tiempo / duracion);

        if (usaExposure)
        {
            skyboxMaterial.SetFloat(
                "_Exposure",
                Mathf.Lerp(0f, skyboxExposureOriginal, t)
            );
        }

        if (usaTint)
        {
            skyboxMaterial.SetColor(
                "_Tint",
                Color.Lerp(Color.black, skyboxTintOriginal, t)
            );
        }

        yield return null;
    }

    if (usaExposure)
        skyboxMaterial.SetFloat("_Exposure", skyboxExposureOriginal);

    if (usaTint)
        skyboxMaterial.SetColor("_Tint", skyboxTintOriginal);
}

}
