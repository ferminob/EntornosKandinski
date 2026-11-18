
using System.Collections.Generic;
using UnityEngine;

public class AudioConductor : MonoBehaviour
{
    [Header("Reloj musical")]
    [Min(1f)]
    public double bpm = 130.0;

    [Min(1)]
    public int beatsPerBar = 4;

    [Tooltip("Tiempo de espera antes del primer compás")]
    public float initialDelay = 1f;

    [Header("Mezcla")]
    [Range(0f, 1f)]
    public float grabbedVolume = 1f;

    [Range(0f, 1f)]
    public float duckedVolume = 0.25f;

    [Min(0.1f)]
    public float fadeSpeed = 3f;

    private readonly List<InteractiveAudio> tracks =
        new List<InteractiveAudio>();

    private double musicStartDspTime;

    public double BeatDuration => 60.0 / bpm;

    public double BarDuration => BeatDuration * beatsPerBar;

    private void Start()
    {
        musicStartDspTime =
            AudioSettings.dspTime + initialDelay;

        Debug.Log(
            "AudioConductor iniciado. BPM: " + bpm
        );
    }

    public double GetNextBarDspTime()
    {
        double now = AudioSettings.dspTime;

        if (now < musicStartDspTime)
            return musicStartDspTime;

        double elapsed = now - musicStartDspTime;

        double completedBars =
            System.Math.Floor(elapsed / BarDuration);

        return musicStartDspTime +
               (completedBars + 1) * BarDuration;
    }

    public void RegisterTrack(InteractiveAudio track)
    {
        if (track == null || tracks.Contains(track))
            return;

        tracks.Add(track);
    }

    public void UnregisterTrack(InteractiveAudio track)
    {
        tracks.Remove(track);
    }

    public void RequestState(
        InteractiveAudio track,
        InteractiveAudio.TrackState newState)
    {
        if (track == null)
            return;

        double nextBar = GetNextBarDspTime();

        track.QueueState(newState, nextBar);
    }

    private void Update()
    {
        double now = AudioSettings.dspTime;

        // Aplicar los cambios pendientes al llegar al compás.
        for (int i = tracks.Count - 1; i >= 0; i--)
        {
            if (tracks[i] == null)
            {
                tracks.RemoveAt(i);
                continue;
            }

            tracks[i].ProcessPendingState(now);
        }

        // Comprobar si existe algún objeto agarrado.
        bool hasGrabbedTrack = false;

        foreach (InteractiveAudio track in tracks)
        {
            if (track.CurrentState ==
                InteractiveAudio.TrackState.Grabbed)
            {
                hasGrabbedTrack = true;
                break;
            }
        }

        // Ajustar el volumen de todas las pistas.
        foreach (InteractiveAudio track in tracks)
        {
            float targetVolume = 0f;

            if (track.CurrentState ==
                InteractiveAudio.TrackState.Grabbed)
            {
                targetVolume = grabbedVolume;
            }
            else if (track.CurrentState ==
                     InteractiveAudio.TrackState.Placed)
            {
                targetVolume = hasGrabbedTrack
                    ? duckedVolume
                    : 1f;
            }

            track.UpdateMix(targetVolume, fadeSpeed);
        }
    }
}
