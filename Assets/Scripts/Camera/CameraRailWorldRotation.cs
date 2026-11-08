using Unity.Mathematics;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>Substitui a orientacao do spline por uma rotacao mundial autorada apos o pipeline do Cinemachine.</summary>
[DisallowMultipleComponent]
public sealed class CameraRailWorldRotation : CinemachineExtension
{
    public Vector3 WorldPositionOffset { get; set; }
    public Vector3 WorldEulerOffset { get; set; }
    public bool OverrideWorldOrientation { get; set; }
    public bool PitchFollowSpline { get; set; }
    public bool YawFollowSpline { get; set; }
    public bool RollFollowSpline { get; set; }
    public SplineContainer Spline { get; set; }
    public CinemachineSplineDolly SplineDolly { get; set; }
    public CinemachineSplineRoll SplineRoll { get; set; }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage == CinemachineCore.Stage.Body)
            state.PositionCorrection += WorldPositionOffset;

        if (stage != CinemachineCore.Stage.Aim || !OverrideWorldOrientation)
            return;

        bool followsAnySplineAxis = PitchFollowSpline || YawFollowSpline ||
            RollFollowSpline;
        Quaternion splineFrame = Quaternion.identity;
        bool hasSplineFrame = followsAnySplineAxis &&
            TryGetSplineFrame(out splineFrame);

        Vector3 pitchAxis = PitchFollowSpline && hasSplineFrame
            ? splineFrame * Vector3.right : Vector3.right;
        Vector3 yawAxis = YawFollowSpline && hasSplineFrame
            ? splineFrame * Vector3.up : Vector3.up;
        Vector3 rollAxis = RollFollowSpline && hasSplineFrame
            ? splineFrame * Vector3.forward : Vector3.forward;

        // With all follow options off, this is exactly the world Euler pose.
        // Enabled axes use the spline frame, including the frame itself, so
        // offsets continue to point down or sideways as the track curves.
        Quaternion baseOrientation = hasSplineFrame
            ? splineFrame : Quaternion.identity;
        Quaternion desired =
            Quaternion.AngleAxis(WorldEulerOffset.y, yawAxis) *
            Quaternion.AngleAxis(WorldEulerOffset.x, pitchAxis) *
            Quaternion.AngleAxis(WorldEulerOffset.z, rollAxis) *
            baseOrientation;
        state.OrientationCorrection = Quaternion.Inverse(state.RawOrientation) *
            desired;
    }

    private bool TryGetSplineFrame(out Quaternion frame)
    {
        frame = Quaternion.identity;
        if (Spline == null || Spline.Spline == null || SplineDolly == null)
            return false;

        float splineT = SplineUtility.ConvertIndexUnit(Spline.Spline,
            SplineDolly.CameraPosition, PathIndexUnit.Distance,
            PathIndexUnit.Normalized);
        if (!SplineUtility.Evaluate(Spline.Spline, splineT, out _,
                out float3 tangent, out float3 up) ||
            math.lengthsq(tangent) < .000001f || math.lengthsq(up) < .000001f)
            return false;

        Quaternion containerRotation = Spline.transform.rotation;
        Vector3 worldForward = containerRotation * (Vector3)tangent;
        Vector3 worldUp = containerRotation * (Vector3)up;
        if (worldForward.sqrMagnitude < .000001f ||
            worldUp.sqrMagnitude < .000001f)
            return false;

        frame = Quaternion.LookRotation(worldForward, worldUp);
        if (SplineRoll != null && SplineRoll.enabled &&
            SplineRoll.Roll != null && SplineRoll.Roll.Count > 0)
        {
            float roll = SplineRoll.Roll.Evaluate(Spline.Spline, splineT,
                PathIndexUnit.Normalized, SplineRoll.GetInterpolator());
            frame *= Quaternion.AngleAxis(roll, Vector3.forward);
        }

        return true;
    }
}
