using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Mistura modificadores persistentes sem conhecer as mecanicas que os
/// produziram e envia impactos temporarios ao sistema de Impulse do Cinemachine.
/// </summary>
[DisallowMultipleComponent]
public sealed class CameraEffectStack : MonoBehaviour
{
    public static CameraEffectStack Instance { get; private set; }

    private readonly Dictionary<string, CameraModifier> modifiers = new();
    private CinemachineCamera gameplayCamera;
    private CinemachineImpulseListener impulseListener;

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    internal void Initialize(CinemachineCamera camera)
    {
        gameplayCamera = camera;
        if (gameplayCamera == null)
            return;

        impulseListener = gameplayCamera.GetComponent<CinemachineImpulseListener>();
        if (impulseListener == null)
            impulseListener = gameplayCamera.gameObject.AddComponent<CinemachineImpulseListener>();

        impulseListener.ChannelMask = 1;
        impulseListener.Gain = 1f;
        impulseListener.Use2DDistance = false;
        impulseListener.UseCameraSpace = false;
        impulseListener.SignalCombinationMode =
            CinemachineImpulseListener.SignalCombinationModes.UseLargest;
    }

    public void SetModifier(string id, CameraModifier modifier)
    {
        if (string.IsNullOrEmpty(id))
            return;
        modifiers[id] = modifier;
    }

    public void RemoveModifier(string id)
    {
        if (!string.IsNullOrEmpty(id))
            modifiers.Remove(id);
    }

    public void AddDirectionalImpulse(
        Vector3 direction,
        float distance,
        float duration,
        AnimationCurve intensity)
    {
        direction.y = 0f;
        if (gameplayCamera == null || direction.sqrMagnitude <= .0001f ||
            distance <= 0f || duration <= 0f)
            return;

        CinemachineImpulseDefinition definition = new()
        {
            ImpulseChannel = 1,
            ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Custom,
            CustomImpulseShape = intensity ??
                AnimationCurve.Linear(0f, 1f, 1f, 0f),
            ImpulseDuration = duration,
            ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform
        };
        definition.CreateEvent(
            gameplayCamera.State.GetFinalPosition(),
            direction.normalized * distance
        );
    }

    internal CameraEffectFrame Evaluate()
    {
        CameraEffectFrame frame = CameraEffectFrame.Identity;
        foreach (CameraModifier modifier in modifiers.Values)
        {
            frame.cameraLocalOffset += modifier.cameraLocalOffset;
            frame.distanceOffset += modifier.distanceOffset;
            frame.pitchOffset += modifier.pitchOffset;
            frame.yawOffset += modifier.yawOffset;
            frame.rollOffset += modifier.rollOffset;
            frame.fieldOfViewOffset += modifier.fieldOfViewOffset;
        }
        return frame;
    }
}

[Serializable]
public struct CameraModifier
{
    [Tooltip("Deslocamento local aplicado no rig da camera.")]
    public Vector3 cameraLocalOffset;
    [Tooltip("Metros somados a distancia base da camera.")]
    public float distanceOffset;
    [Tooltip("Graus somados ao Pitch da orbita.")]
    public float pitchOffset;
    [Tooltip("Graus somados ao Yaw da orbita.")]
    public float yawOffset;
    [Tooltip("Graus somados ao Roll da camera.")]
    public float rollOffset;
    [Tooltip("Valor somado ao campo de visao base.")]
    public float fieldOfViewOffset;

    public static CameraModifier Identity => new();
}

internal struct CameraEffectFrame
{
    public Vector3 cameraLocalOffset;
    public float distanceOffset;
    public float pitchOffset;
    public float yawOffset;
    public float rollOffset;
    public float fieldOfViewOffset;

    public static CameraEffectFrame Identity => new();
}
