
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class InteractiveAudio : MonoBehaviour
{
    public enum TrackState
    {
        Inactive,
        Grabbed,
        Placed,
        Floor
    }

    [Header("Audio")]
    public AudioClip audioClip;

    public AudioConductor conductor;

    [Header("Estado")]
    [SerializeField]
    private TrackState currentState = TrackState.Inactive;

    private AudioSource audioSource;

    private TrackState pendingState;
    private double pendingDspTime;
    private bool hasPendingState;

    private float currentMixVolume;

    public TrackState CurrentState => currentState;

    // Métodos temporales para probar desde el Inspector
[ContextMenu("TEST / Grabbed")]
private void TestGrabbed()
{
    OnGrabbed();
}

[ContextMenu("TEST / Placed")]
private void TestPlaced()
{
    OnPlacedOnPedestal();
}

[ContextMenu("TEST / Floor")]
private void TestFloor()
{
    OnReleasedToFloor();
}

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f;
        audioSource.dopplerLevel = 0f;
        audioSource.spread = 0f;

        audioSource.clip = audioClip;
        audioSource.volume = 0f;

        if (audioClip == null)
        {
            Debug.LogWarning(
                "No hay AudioClip asignado en " + gameObject.name
            );
        }

        if (conductor == null)
    conductor = FindFirstObjectByType<AudioConductor>();
    }

    private void OnEnable()
    {
        if (conductor != null)
            conductor.RegisterTrack(this);
    }

    private void OnDisable()
    {
        if (conductor != null)
            conductor.UnregisterTrack(this);

        if (audioSource != null)
            audioSource.Stop();
    }

    // Llamar cuando se agarra el objeto.
    public void OnGrabbed()
    {
        RequestState(TrackState.Grabbed);
    }

    // Llamar cuando se coloca en un pedestal.
    public void OnPlacedOnPedestal()
    {
        RequestState(TrackState.Placed);
    }

    // Llamar cuando se suelta en el piso.
    public void OnReleasedToFloor()
    {
        RequestState(TrackState.Floor);
    }

    private void RequestState(TrackState newState)
    {
        if (conductor == null)
        {
            Debug.LogWarning(
                "Falta asignar AudioConductor en " + gameObject.name
            );
            return;
        }

        conductor.RequestState(this, newState);
    }

    public void QueueState(
        TrackState newState,
        double dspTime)
    {
        pendingState = newState;
        pendingDspTime = dspTime;
        hasPendingState = true;

        // Si se solicita una nueva entrada, preparar el audio
        // para reproducirse desde el principio en el próximo compás.
        if (newState == TrackState.Grabbed ||
            newState == TrackState.Placed)
        {
            audioSource.Stop();

            if (audioClip != null)
            {
                audioSource.clip = audioClip;
                audioSource.volume = 0f;
                audioSource.PlayScheduled(dspTime);
            }
        }
    }

    public void ProcessPendingState(double dspTime)
    {
        if (!hasPendingState)
            return;

        if (dspTime < pendingDspTime)
            return;

        currentState = pendingState;
        hasPendingState = false;
    }

    public void UpdateMix(float targetVolume, float speed)
    {
        currentMixVolume = Mathf.MoveTowards(
            currentMixVolume,
            targetVolume,
            speed * Time.deltaTime
        );

        if (audioSource != null)
            audioSource.volume = currentMixVolume;
    }
}
