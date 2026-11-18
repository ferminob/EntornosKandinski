
using UnityEngine;
using System.Collections.Generic;
using Mediapipe.Unity;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Components.Containers;

public class HandModelController : MonoBehaviour
{
    [Header("MediaPipe")]
    public HandLandmarkerResultAnnotationController handController;

    [Header("Seguimiento de esferas")]
    public FingerTipsController fingerTips;
    public Vector3 wristOffset = Vector3.zero;

    [Header("Objetivos de los dedos")]
    public int ikIterations = 10;
    public float ikTolerance = 0.01f;

    [Header("Huesos del modelo")]
    public Transform[] thumbBones = new Transform[4];
    public Transform[] indexBones = new Transform[4];
    public Transform[] middleBones = new Transform[4];
    public Transform[] ringBones = new Transform[4];
    public Transform[] pinkyBones = new Transform[4];

    [Header("Rotacion")]
    public float rotationMultiplier = 1f;
    public float maxRotation = 80f;
    public float smoothing = 12f;

    [Header("Direccion de rotacion")]
    public float thumbSign = 1f;
    public float indexSign = 1f;
    public float middleSign = -1f;
    public float ringSign = -1f;
    public float pinkySign = -1f;

    private Transform[][] fingers;
    private Quaternion[][] initialRotations;
    private float[][] initialFlexions;

    private bool calibrated = false;

    private void Awake()
    {
        fingers = new Transform[][]
        {
            thumbBones,
            indexBones,
            middleBones,
            ringBones,
            pinkyBones
        };

        SaveInitialRotations();
    }

    private void SaveInitialRotations()
    {
        initialRotations = new Quaternion[fingers.Length][];

        for (int f = 0; f < fingers.Length; f++)
        {
            initialRotations[f] = new Quaternion[fingers[f].Length];

            for (int i = 0; i < fingers[f].Length; i++)
            {
                if (fingers[f][i] != null)
                    initialRotations[f][i] =
                        fingers[f][i].localRotation;
            }
        }
    }

    private void LateUpdate()
    {

        

        if (handController == null)
            return;

        var result = handController.CurrentTarget;

        if (result.handLandmarks == null ||
            result.handedness == null)
            return;

        int count = Mathf.Min(
            result.handLandmarks.Count,
            result.handedness.Count
        );

        for (int h = 0; h < count; h++)
        {
            var categories = result.handedness[h].categories;

            if (categories == null || categories.Count == 0)
                continue;

            if (categories[0].categoryName != "Right")
                continue;

            var landmarks = result.handLandmarks[h].landmarks;

            if (landmarks == null || landmarks.Count < 21)
                return;

            AnimateHand(landmarks);
            return;
        }
    }

    private void AnimateHand(
        IList<NormalizedLandmark> landmarks)
    {
        // Cada dedo utiliza cinco landmarks:
        // muñeca/base, articulación 1, articulación 2,
        // articulación 3 y punta.

        int[][] landmarkMap =
        {
            new int[] { 0, 1, 2, 3, 4 },       // Pulgar
            new int[] { 0, 5, 6, 7, 8 },       // Indice
            new int[] { 0, 9, 10, 11, 12 },    // Medio
            new int[] { 0, 13, 14, 15, 16 },   // Anular
            new int[] { 0, 17, 18, 19, 20 }    // Meñique
        };

        float[] signs =
        {
            thumbSign,
            indexSign,
            middleSign,
            ringSign,
            pinkySign
        };

        float[][] currentFlexions = new float[5][];

        for (int f = 0; f < 5; f++)
        {
            currentFlexions[f] = new float[3];

            for (int j = 0; j < 3; j++)
            {
                Vector3 previous = GetPosition(
                    landmarks[landmarkMap[f][j]]
                );

                Vector3 center = GetPosition(
                    landmarks[landmarkMap[f][j + 1]]
                );

                Vector3 next = GetPosition(
                    landmarks[landmarkMap[f][j + 2]]
                );

                float angle = Vector3.Angle(
                    previous - center,
                    next - center
                );

                currentFlexions[f][j] = 180f - angle;
            }
        }

        // Guardamos la flexion natural de la mano
        // en el primer frame detectado.

        if (!calibrated)
        {
            initialFlexions = new float[5][];

            for (int f = 0; f < 5; f++)
            {
                initialFlexions[f] =
                    (float[])currentFlexions[f].Clone();
            }

            calibrated = true;
            return;
        }

        // Aplicamos las rotaciones a los tres huesos
        // articulados de cada cadena.

        for (int f = 0; f < 5; f++)
        {
            for (int j = 0; j < 3; j++)
            {
                Transform bone = fingers[f][j];

                if (bone == null)
                    continue;

                float difference =
                    currentFlexions[f][j] -
                    initialFlexions[f][j];

                float multiplier = f == 0
                    ? rotationMultiplier * 2f
                    : rotationMultiplier;

                float rotation = Mathf.Clamp(
                    difference * multiplier * signs[f],
                    -maxRotation,
                    maxRotation
                );

                Quaternion rotationOffset;

                    if (f == 0)
                    {
                        // Pulgar: eje X
                        rotationOffset = Quaternion.Euler(rotation, 0f, 0f);
                    }
                    else
                    {
                        // Otros dedos: eje Z
                        rotationOffset = Quaternion.Euler(0f, 0f, rotation);
                    }

                    Quaternion targetRotation =
                        initialRotations[f][j] * rotationOffset;

                float blend = 1f - Mathf.Exp(
                    -smoothing * Time.deltaTime
                );

                bone.localRotation = Quaternion.Slerp(
                    bone.localRotation,
                    targetRotation,
                    blend
                );
            }
        }
    }

    private Vector3 GetPosition(NormalizedLandmark landmark)
    {
        return new Vector3(
            landmark.x,
            landmark.y,
            landmark.z
        );
    }

    private void SolveFingerIK()
    {
        if (fingerTips == null || !fingerTips.IsRightHandTracked)
            return;

        Transform[] targets =
        {
            fingerTips.rightThumb,
            fingerTips.rightIndex,
            fingerTips.rightMiddle,
            fingerTips.rightRing,
            fingerTips.rightPinky
        };

        for (int f = 0; f < fingers.Length; f++)
        {
            if (targets[f] == null)
                continue;

            SolveFinger(fingers[f], targets[f].position);
        }
    }

    private void SolveFinger(Transform[] bones, Vector3 target)
    {
        if (bones == null || bones.Length < 4)
            return;

        Transform tip = bones[3];

        for (int iteration = 0; iteration < ikIterations; iteration++)
        {
            if (Vector3.Distance(tip.position, target) <= ikTolerance)
                break;

            // Rotamos desde la articulación más cercana
            // a la punta hacia la base del dedo.
            for (int j = 2; j >= 0; j--)
            {
                Transform joint = bones[j];

                if (joint == null)
                    continue;

                Vector3 toTip = tip.position - joint.position;
                Vector3 toTarget = target - joint.position;

                if (toTip.sqrMagnitude < 0.000001f ||
                    toTarget.sqrMagnitude < 0.000001f)
                    continue;

                Quaternion correction = Quaternion.FromToRotation(
                    toTip,
                    toTarget
                );

                joint.rotation = correction * joint.rotation;
            }
        }
    }

}
