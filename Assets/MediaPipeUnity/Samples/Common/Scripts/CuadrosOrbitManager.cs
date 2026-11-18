
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class CuadrosOrbitManager : MonoBehaviour
{
    [Header("Jugador")]
    public Transform jugador;

    [Header("Control de eventos")]
    public EventController eventController;

    [Header("Aparición")]
    public float distanciaSpawn = 8f;
    public float tiempoAparicion = 2f;

    private bool cuadrosIniciados = false;

    [Header("Distribución")]
    public float distancia = 5f;
    public int cantidadCuadros = 12;
    public int cantidadFilas = 2;
    public float alturaEntreFilas = 2f;

    [Header("Altura general")]
public float alturaGeneral = 1.5f;

    [Header("Rotación")]
    public float velocidad = 15f;

    public Vector3 correccionRotacion = Vector3.zero;

    [Header("Prefabs")]
    public GameObject[] cuadrosPrefabs;

    private List<Transform> cuadros = new List<Transform>();

    private float anguloActual = 0f;

    void Start()
    {
        if (jugador == null && Camera.main != null)
            jugador = Camera.main.transform;

    }

    void Update()
    {
        if (jugador == null)
            return;

        anguloActual += velocidad * Time.deltaTime;

        ActualizarPosiciones();
    }

    public void IniciarCuadros()
    {
        if (cuadrosIniciados)
            return;

        cuadrosIniciados = true;

        GenerarCuadros();

        StartCoroutine(AparecerCuadros());
    }

    IEnumerator AparecerCuadros()
{
    Vector3 posicionInicial = jugador.position
                            - jugador.forward * distanciaSpawn;

    foreach (Transform cuadro in cuadros)
    {
        if (cuadro == null)
            continue;

        cuadro.position = posicionInicial;
    }

    float tiempo = 0f;

    while (tiempo < tiempoAparicion)
    {
        tiempo += Time.deltaTime;

        float t = Mathf.Clamp01(tiempo / tiempoAparicion);
        t = Mathf.SmoothStep(0f, 1f, t);

        ActualizarPosiciones();

        yield return null;
    }
}

    void GenerarCuadros()
    {
        if (cuadrosPrefabs == null || cuadrosPrefabs.Length == 0)
        {
            Debug.LogWarning("No hay prefabs asignados.");
            return;
        }

        if (cantidadCuadros <= 0)
            return;

        int filas = Mathf.Clamp(cantidadFilas, 1, cantidadCuadros);

        int cuadrosPorFila = cantidadCuadros / filas;
        int sobrantes = cantidadCuadros % filas;

        for (int fila = 0; fila < filas; fila++)
        {
            int cantidadEnFila = cuadrosPorFila;

            if (fila < sobrantes)
                cantidadEnFila++;

            float altura = (fila - (filas - 1) / 2f)
                           * alturaEntreFilas;

            for (int i = 0; i < cantidadEnFila; i++)
            {
                int indicePrefab = Random.Range(0, cuadrosPrefabs.Length);

                GameObject prefab = cuadrosPrefabs[indicePrefab];

                if (prefab == null)
                    continue;

                GameObject nuevoCuadro = Instantiate(prefab);

                nuevoCuadro.name = "Cuadro_Orbital_" + cuadros.Count;

                cuadros.Add(nuevoCuadro.transform);
            }
        }
    }

    void ActualizarPosiciones()
    {
        int filas = Mathf.Clamp(cantidadFilas, 1, cantidadCuadros);

        int cuadrosPorFila = cantidadCuadros / filas;
        int sobrantes = cantidadCuadros % filas;

        int indiceGlobal = 0;

        for (int fila = 0; fila < filas; fila++)
        {
            int cantidadEnFila = cuadrosPorFila;

            if (fila < sobrantes)
                cantidadEnFila++;

            float altura = (fila - (filas - 1) / 2f)
                           * alturaEntreFilas;

            float separacionAngular = 360f / cantidadEnFila;

            for (int i = 0; i < cantidadEnFila; i++)
            {
                if (indiceGlobal >= cuadros.Count)
                    return;

                Transform cuadro = cuadros[indiceGlobal];

                if (cuadro != null)
                {
                    float angulo = (i * separacionAngular)
                                   + anguloActual
                                   + (fila % 2) * separacionAngular / 2f;

                    float radianes = angulo * Mathf.Deg2Rad;

                    Vector3 direccion = new Vector3(
                        Mathf.Sin(radianes),
                        0f,
                        Mathf.Cos(radianes)
                    );

                    Vector3 posicion = jugador.position
                            + direccion * distancia
                            + Vector3.up * (altura + alturaGeneral);

                    cuadro.position = posicion;

                    Vector3 direccionJugador =
                        jugador.position - cuadro.position;

                    Quaternion rotacionBase = Quaternion.LookRotation(
                        direccionJugador,
                        Vector3.up
                    );

                    cuadro.rotation = rotacionBase * Quaternion.Euler(correccionRotacion);
                }

                indiceGlobal++;
            }
        }
    }
}

