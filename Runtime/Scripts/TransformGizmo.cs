using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    public enum TransformGizmoState
    {
        Waiting,
        MovingAxis,
        MovingPlane,
        RotatingAxis,
        ScalingAxis,
        ScalingWhole,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TransformGizmo : UdonSharpBehaviour
    {
        [SerializeField] private Transform tracked;
        [SerializeField] private Transform gizmo;
        [SerializeField] private float inverseScale = 200f;
        [SerializeField] private float maxAllowedProximity = 5f;
        [Tooltip("Should be less than or equal to the far clipping plane.")]
        [SerializeField] private float maxIntersectionDistance = 800f;
        [Space]
        #region MovingAxis Vars
        [SerializeField] private Transform[] arrows;
        [SerializeField] private Transform[] highlightedArrows;
        [SerializeField] private Transform[] activeArrows;
        [SerializeField] private float arrowLength = 40f;
        #endregion
        [Space]
        #region MovingPlane Vars
        [SerializeField] private Transform[] planes;
        [SerializeField] private Transform highlightedPlane;
        [SerializeField] private Transform activePlane;
        [SerializeField] private float planeSize = 12f;
        #endregion
        [Space]
        #region RotatingAxis Vars
        [SerializeField] private Transform[] halfCircles;
        [SerializeField] private GameObject[] otherHalfCircles;
        [SerializeField] private Transform halfCircleHighlighted;
        [SerializeField] private GameObject otherHalfCircleHighlighted;
        [SerializeField] private Transform activeCircle;
        [SerializeField] private Transform activeSnapCircle;
        [SerializeField] private Transform activeRotationIndicator;
        [SerializeField] private Transform circleLineOne;
        [SerializeField] private Transform circleLineTwo;
        [SerializeField] private MeshRenderer activeRotationIndicatorRenderer;
        private Material activeRotationIndicatorMat; // Set in Start.
        [SerializeField] private float circleRadius = 44f;
        #endregion
        [Space]
        #region ScalingAxis Vars
        [SerializeField] private Transform[] scalers;
        [SerializeField] private Transform[] highlightedScalers;
        [SerializeField] private Transform[] activeScalers;
        [SerializeField] private float axisScalerSize = 3f;
        [SerializeField] private float axisScalerPosition = 55f;
        #endregion
        [Space]
        #region ScalingWhole Vars
        [SerializeField] private Transform wholeScaler;
        [SerializeField] private Transform highlightedWholeScaler;
        [SerializeField] private Transform activeWholeScaler;
        [SerializeField] private float wholeScalerSize = 5f;
        #endregion
        [Space] // DEBUG
        [SerializeField] private Transform[] debugIntersects;
        [SerializeField] private Transform debugIndicatorOne;
        [SerializeField] private Transform debugIndicatorTwo;

        private Quaternion[] tangentRotations = new Quaternion[]
        {
            Quaternion.Euler(90f, 0f, 0f),
            Quaternion.Euler(0f, 90f, 0f),
            Quaternion.Euler(0f, 0f, 90f),
        };

        private Vector3[] axisDirs = new Vector3[]
        {
            Vector3.right,
            Vector3.up,
            Vector3.forward,
        };

        private VRCPlayerApi localPlayer;

        private TransformGizmoState state;
        private VRCPlayerApi.TrackingData headTrackingData;
        private float gizmoScale;
        private Vector3 headToTargetDir;
        private Vector3 headDir;
        private Vector3 headLocal;

        private Vector3 planeOriginIntersection;
        private Vector3 planeStartPosition;
        private Vector3 planeTotalMovement;
        private float originGizmoScale;

        private TransformGizmoState highlightedState;
        private float highlightedProximity;
        private int highlightedAxis;

        // Waiting.
        private bool[] showFullCircle = new bool[3];

        // Rotating Axis.
        private Quaternion prevRotation;
        private Vector3 localRotationDirection;
        private Quaternion prevOffset;

        #region Unity Events

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            activeRotationIndicatorMat = activeRotationIndicatorRenderer.material;
            EnterState(TransformGizmoState.Waiting);
        }

        private void Update()
        {
            CalculateSharedVariables();

            for (int i = 0; i < 3; i++)
                debugIntersects[i].gameObject.SetActive(false);

            if (state != TransformGizmoState.Waiting && Input.GetMouseButtonUp(0))
                EnterState(TransformGizmoState.Waiting); // Also calls UpdateCurrentState().
            else
                UpdateCurrentState();

            // Prevent the gizmo highlights and such from jumping around due to using old position and rotation.
            gizmo.localScale = Vector3.one * gizmoScale;
            gizmo.SetPositionAndRotation(tracked.position, tracked.rotation);
        }

        #endregion

        #region State Entering

        private void EnterState(TransformGizmoState newState)
        {
            DisableEverything();
            switch (newState)
            {
                case TransformGizmoState.Waiting:
                    EnterWaitingState();
                    break;
                case TransformGizmoState.MovingAxis:
                    EnterMovingAxisState();
                    break;
                case TransformGizmoState.MovingPlane:
                    EnterMovingPlaneState();
                    break;
                case TransformGizmoState.RotatingAxis:
                    EnterRotatingAxisState();
                    break;
                case TransformGizmoState.ScalingAxis:
                    EnterScalingAxisState();
                    break;
                case TransformGizmoState.ScalingWhole:
                    EnterScalingWholeState();
                    break;
            }
            state = newState;
            UpdateCurrentState();
        }

        private void DisableEverything()
        {
            for (int i = 0; i < 3; i++)
            {
                arrows[i].gameObject.SetActive(false);
                activeArrows[i].gameObject.SetActive(false);
                planes[i].gameObject.SetActive(false);
                halfCircles[i].gameObject.SetActive(false);
                scalers[i].gameObject.SetActive(false);
                activeScalers[i].gameObject.SetActive(false);
            }
            activePlane.gameObject.SetActive(false);
            activeCircle.gameObject.SetActive(false);
            activeSnapCircle.gameObject.SetActive(false);
            activeRotationIndicator.gameObject.SetActive(false);
            circleLineOne.gameObject.SetActive(false);
            circleLineTwo.gameObject.SetActive(false);
            wholeScaler.gameObject.SetActive(false);
            activeWholeScaler.gameObject.SetActive(false);
        }

        private void EnterWaitingState()
        {
            for (int i = 0; i < 3; i++)
            {
                arrows[i].gameObject.SetActive(true);
                planes[i].gameObject.SetActive(true);
                halfCircles[i].gameObject.SetActive(true);
                scalers[i].gameObject.SetActive(true);
            }
            wholeScaler.gameObject.SetActive(true);
        }

        private void EnterMovingAxisState()
        {
            for (int i = 0; i < 3; i++)
                arrows[i].gameObject.SetActive(i != highlightedAxis);
            activeArrows[highlightedAxis].gameObject.SetActive(true);
        }

        private void EnterMovingPlaneState()
        {
            for (int i = 0; i < 3; i++)
            {
                planes[i].gameObject.SetActive(i != highlightedAxis);
                activeArrows[i].gameObject.SetActive(i != highlightedAxis);
            }
            arrows[highlightedAxis].gameObject.SetActive(true);
            activePlane.localPosition = planes[highlightedAxis].localPosition;
            activePlane.localRotation = planes[highlightedAxis].localRotation;
            activePlane.gameObject.SetActive(true);
        }

        private void EnterRotatingAxisState()
        {
            for (int i = 0; i < 3; i++)
                halfCircles[i].gameObject.SetActive(i != highlightedAxis);
            activeCircle.localRotation = GetOriginRotation();
            activeCircle.gameObject.SetActive(true);
            activeRotationIndicator.gameObject.SetActive(true);
            circleLineOne.gameObject.SetActive(true);
            circleLineTwo.gameObject.SetActive(true);
        }

        private void EnterScalingAxisState()
        {
            for (int i = 0; i < 3; i++)
                scalers[i].gameObject.SetActive(i != highlightedAxis);
            activeScalers[highlightedAxis].gameObject.SetActive(true);
            wholeScaler.gameObject.SetActive(true);
        }

        private void EnterScalingWholeState()
        {
            for (int i = 0; i < 3; i++)
                activeScalers[i].gameObject.SetActive(true);
            activeWholeScaler.gameObject.SetActive(true);
        }

        #endregion

        #region State Update

        private void UpdateCurrentState()
        {
            switch (state)
            {
                case TransformGizmoState.Waiting:
                    Waiting();
                    break;
                case TransformGizmoState.MovingAxis:
                    MovingAxis();
                    break;
                case TransformGizmoState.MovingPlane:
                    MovingPlane();
                    break;
                case TransformGizmoState.RotatingAxis:
                    RotatingAxis();
                    break;
                case TransformGizmoState.ScalingAxis:
                    ScalingAxis();
                    break;
                case TransformGizmoState.ScalingWhole:
                    ScalingWhole();
                    break;
            }
        }

        private void Waiting()
        {
            for (int i = 0; i < 3; i++)
            {
                FacePlaneTowardsHead(i);
                FaceCircleTowardsHead(i);
            }

            highlightedState = TransformGizmoState.Waiting;
            highlightedProximity = 1f;
            for (int i = 0; i < 3; i++)
                CheckProximity(i);

            DisableAllHighlights();

            if (highlightedState == TransformGizmoState.Waiting)
                return;

            if (Input.GetMouseButtonDown(0))
            {
                EnterState(highlightedState);
                return;
            }

            UpdateCurrentHighlight();
        }

        private void MovingAxis()
        {

        }

        private void MovingPlane()
        {
            if (!TryGetIntersection(highlightedAxis, out Vector3 intersection))
                return;

            bool snapping = Input.GetKey(KeyCode.LeftControl);

            // Shifted to prevent bobbing caused by scale differences at different positions.
            Vector3 shiftedOrigin = planeOriginIntersection * (originGizmoScale / gizmoScale);
            Vector3 movementToApply = tracked.rotation * ((intersection - shiftedOrigin) * gizmoScale);
            if (tracked.parent != null)
                movementToApply = tracked.parent.InverseTransformVector(movementToApply);
            planeTotalMovement += movementToApply;
            if (snapping)
            {
                planeTotalMovement = Quaternion.Inverse(tracked.localRotation) * planeTotalMovement;
                planeTotalMovement.x = Mathf.Round(planeTotalMovement.x / 0.25f) * 0.25f;
                planeTotalMovement.y = Mathf.Round(planeTotalMovement.y / 0.25f) * 0.25f;
                planeTotalMovement.z = Mathf.Round(planeTotalMovement.z / 0.25f) * 0.25f;
                planeTotalMovement = tracked.localRotation * planeTotalMovement;
            }
            tracked.localPosition = planeStartPosition + planeTotalMovement;

            CalculateSharedVariables(); // TODO: only recalculate the scale, the rest isn't needed.
        }

        private void RotatingAxis()
        {
            bool snapping = Input.GetKey(KeyCode.LeftControl);
            activeSnapCircle.gameObject.SetActive(snapping);

            if (!TryGetIntersection(highlightedAxis, out Vector3 intersection))
            {
                for (int i = 0; i < 3; i++)
                    if (i != highlightedAxis)
                        FaceCircleTowardsHead(i);
                if (snapping)
                    UpdateSnappingCircleHighlight();
                return;
            }

            debugIndicatorOne.localPosition = localRotationDirection * 20f;
            debugIndicatorTwo.localPosition = Vector3.Project(intersection, localRotationDirection);

            Vector3 projected = Vector3.Project(intersection, localRotationDirection);
            float totalMovement = projected.magnitude / (circleRadius * Mathf.PI * 2f) * 360f;
            if (snapping)
                totalMovement = Mathf.Round(totalMovement / 15f) * 15f;
            totalMovement *= (Vector3.Dot(projected, localRotationDirection) < 0f ? -1f : 1f);

            Vector3 euler = Vector3.zero;
            euler[highlightedAxis] = totalMovement;
            Quaternion offset = Quaternion.Euler(euler);
            Quaternion rotationToApply = Quaternion.Inverse(prevOffset) * offset;

            prevOffset = offset;
            prevRotation *= rotationToApply;
            localRotationDirection = Quaternion.Inverse(rotationToApply) * localRotationDirection;

            tracked.rotation = prevRotation;
            Quaternion originRotation = GetOriginRotation();
            activeRotationIndicator.localRotation = originRotation;
            circleLineOne.localRotation = originRotation;
            circleLineTwo.localRotation = originRotation * Quaternion.Euler(0f, totalMovement, 0f);
            activeRotationIndicatorMat.SetFloat("_Angle", totalMovement);

            // TODO: recalculate stuff since the tracked rotation changed and the gizmo has also been rotated.
            for (int i = 0; i < 3; i++)
                if (i != highlightedAxis)
                    FaceCircleTowardsHead(i);
            if (snapping)
                UpdateSnappingCircleHighlight();
        }

        private void ScalingAxis()
        {

        }

        private void ScalingWhole()
        {

        }

        #endregion

        #region Highlights

        private void DisableAllHighlights()
        {
            for (int i = 0; i < 3; i++)
            {
                arrows[i].gameObject.SetActive(true);
                highlightedArrows[i].gameObject.SetActive(false);
                planes[i].gameObject.SetActive(true);
                halfCircles[i].gameObject.SetActive(true);
                halfCircleHighlighted.gameObject.SetActive(false);
                scalers[i].gameObject.SetActive(true);
                highlightedScalers[i].gameObject.SetActive(false);
            }
            highlightedPlane.gameObject.SetActive(false);
            wholeScaler.gameObject.SetActive(true);
            highlightedWholeScaler.gameObject.SetActive(false);
        }

        private void UpdateCurrentHighlight()
        {
            switch (highlightedState)
            {
                case TransformGizmoState.Waiting:
                    return;
                case TransformGizmoState.MovingAxis:
                    UpdateMovingAxisHighlight();
                    break;
                case TransformGizmoState.MovingPlane:
                    UpdateMovingPlaneHighlight();
                    break;
                case TransformGizmoState.RotatingAxis:
                    UpdateRotatingAxisHighlight();
                    break;
                case TransformGizmoState.ScalingAxis:
                    UpdateScalingAxisHighlight();
                    break;
                case TransformGizmoState.ScalingWhole:
                    UpdateScalingWholeHighlight();
                    break;
            }
        }

        private void UpdateMovingAxisHighlight()
        {
            arrows[highlightedAxis].gameObject.SetActive(false);
            highlightedArrows[highlightedAxis].gameObject.SetActive(true);
        }

        private void UpdateMovingPlaneHighlight()
        {
            planes[highlightedAxis].gameObject.SetActive(false);
            highlightedPlane.localPosition = planes[highlightedAxis].localPosition;
            highlightedPlane.localRotation = planes[highlightedAxis].localRotation;
            highlightedPlane.gameObject.SetActive(true);
        }

        private void UpdateRotatingAxisHighlight()
        {
            halfCircles[highlightedAxis].gameObject.SetActive(false);
            halfCircleHighlighted.localRotation = halfCircles[highlightedAxis].localRotation;
            halfCircleHighlighted.gameObject.SetActive(true);
            otherHalfCircleHighlighted.gameObject.SetActive(showFullCircle[highlightedAxis]);
        }

        private void UpdateScalingAxisHighlight()
        {
            scalers[highlightedAxis].gameObject.SetActive(false);
            highlightedScalers[highlightedAxis].gameObject.SetActive(true);
        }

        private void UpdateScalingWholeHighlight()
        {
            wholeScaler.gameObject.SetActive(false);
            highlightedWholeScaler.gameObject.SetActive(true);
        }

        #endregion

        #region Util/Other

        private void CalculateSharedVariables()
        {
            headTrackingData = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            gizmoScale = Vector3.Distance(tracked.position, headTrackingData.position) / inverseScale;

            headToTargetDir = (tracked.position - headTrackingData.position).normalized;
            if (headToTargetDir == Vector3.zero)
                headToTargetDir = Vector3.forward;
            headToTargetDir = Quaternion.Inverse(tracked.rotation) * headToTargetDir;

            headDir = Quaternion.Inverse(tracked.rotation) * headTrackingData.rotation * Vector3.forward;
            headLocal = Quaternion.Inverse(tracked.rotation) * (headTrackingData.position - tracked.position) / gizmoScale;
        }

        private void FacePlaneTowardsHead(int axisIndex)
        {
            Vector3 localPos = new Vector3(
                headLocal.x < 0f ? -planeSize / 2f : planeSize / 2f,
                headLocal.y < 0f ? -planeSize / 2f : planeSize / 2f,
                headLocal.z < 0f ? -planeSize / 2f : planeSize / 2f);
            localPos[axisIndex] = 0f;
            planes[axisIndex].localPosition = localPos;
        }

        private Quaternion GetOriginRotation()
        {
            return Quaternion.LookRotation(localRotationDirection, axisDirs[highlightedAxis])
                * Quaternion.Euler(0f, -90f, 0f);
        }

        private void UpdateSnappingCircleHighlight()
        {
            activeSnapCircle.localRotation = GetOriginRotation();
        }

        private void FaceCircleTowardsHead(int axisIndex)
        {
            // TODO: maybe if the angle is too shallow, hide it entirely.

            Vector3 axis = axisDirs[axisIndex];

            halfCircles[axisIndex].localRotation = Quaternion.LookRotation(
                Vector3.ProjectOnPlane(headToTargetDir, axis).normalized, axis);

            bool isSteep = Mathf.Abs(Vector3.Dot(headToTargetDir, axis)) > 0.9625f;
            showFullCircle[axisIndex] = isSteep;
            otherHalfCircles[axisIndex].gameObject.SetActive(isSteep);
        }

        private bool IsLookingTowardsPlane(int axisIndex)
        {
            Vector3 headToPlaneDir = new Vector3();
            // Inverted since headLocal is effectively from plane to head.
            headToPlaneDir[axisIndex] = -headLocal[axisIndex];
            return Vector3.Dot(headToPlaneDir, headDir) > 0f;
        }

        private bool TryGetIntersection(int axisIndex, out Vector3 intersection)
        {
            if (!IsLookingTowardsPlane(axisIndex))
            {
                intersection = new Vector3();
                return false;
            }
            intersection = GetIntersection(axisIndex);
            return (intersection - headLocal).magnitude * gizmoScale <= maxIntersectionDistance;
        }

        private Vector3 GetIntersection(int axisIndex)
        {
            Vector3 intersection = headLocal + (headDir * -(headLocal[axisIndex] / headDir[axisIndex]));
            intersection[axisIndex] = 0f;
            debugIntersects[axisIndex].gameObject.SetActive(true);
            debugIntersects[axisIndex].localPosition = intersection;
            return intersection;
        }

        private bool IsNearHalfCircle(int axisIndex, Vector3 intersection)
        {
            return Vector3.Dot(intersection, headToTargetDir) <= 0f;
        }

        #endregion

        #region Proximity Checks

        private float GetProximityMultiplier(int axisIndex)
        {
            // TODO: This thing is so whacky, especially when you're looking parallel to an arrow for example.
            return 1f / ((headDir / headDir[axisIndex]).magnitude * maxAllowedProximity);
        }

        private void CheckProximity(int axisIndex)
        {
            if (!TryGetIntersection(axisIndex, out Vector3 intersection))
                return;

            float proximityMultiplier = GetProximityMultiplier(axisIndex);
            float depthProximity = (intersection - headLocal).magnitude / 10_000f;

            { // RotatingAxis
                float proximity = depthProximity + Mathf.Abs(intersection.magnitude - circleRadius) * proximityMultiplier;
                if (proximity < highlightedProximity && (showFullCircle[axisIndex] || IsNearHalfCircle(axisIndex, intersection)))
                    SetHighlightedStateToRotatingAxis(proximity, axisIndex, intersection);
            }

            { // ScalingWhole
                float proximity = depthProximity + Mathf.Max(0f, intersection.magnitude - wholeScalerSize / 2f) / maxAllowedProximity;
                if (proximity < highlightedProximity)
                    SetHighlightedStateToScalingWhole(depthProximity, intersection);
            }

            { // MovingPlane
                Vector3 shifted = intersection - planes[axisIndex].localPosition;
                float maxDistance = Mathf.Max(Mathf.Abs(shifted.x), Mathf.Abs(shifted.y), Mathf.Abs(shifted.z));
                if (depthProximity < highlightedProximity && maxDistance <= planeSize / 2f)
                    SetHighlightedStateToMovingPlane(depthProximity, axisIndex, intersection);
            }

            for (int i = 0; i < 3; i++)
            {
                if (i == axisIndex || intersection[i] < 0f)
                    continue;

                { // MovingAxis
                    Vector3 semiProjected = intersection;
                    semiProjected[i] = Mathf.Max(0f, semiProjected[i] - arrowLength) / proximityMultiplier;
                    float proximity = depthProximity + semiProjected.magnitude * proximityMultiplier;
                    if (proximity < highlightedProximity)
                        SetHighlightedStateToMovingAxis(depthProximity, i, intersection);
                }

                { // ScalingAxis
                    Vector3 shifted = intersection - axisDirs[i] * axisScalerPosition;
                    // axisScalerSize should be used as a radius here, which means it should be divided by 2,
                    // but for more proximity it's also getting multiplied by 2 so the cancel each other out.
                    float proximity = depthProximity + Mathf.Max(0f, shifted.magnitude - axisScalerSize / 2f) / maxAllowedProximity;
                    if (proximity < highlightedProximity)
                        SetHighlightedStateToScalingAxis(depthProximity, i, intersection);
                }
            }
        }

        private void SetHighlightedStateToMovingAxis(float proximity, int axisIndex, Vector3 intersection)
        {
            highlightedState = TransformGizmoState.MovingAxis;
            highlightedProximity = proximity;
            highlightedAxis = axisIndex;
        }

        private void SetHighlightedStateToMovingPlane(float proximity, int axisIndex, Vector3 intersection)
        {
            highlightedState = TransformGizmoState.MovingPlane;
            highlightedProximity = proximity;
            highlightedAxis = axisIndex;

            planeStartPosition = tracked.localPosition;
            planeTotalMovement = new Vector3();
            planeOriginIntersection = intersection;
            originGizmoScale = gizmoScale;
        }

        private void SetHighlightedStateToRotatingAxis(float proximity, int axisIndex, Vector3 intersection)
        {
            highlightedState = TransformGizmoState.RotatingAxis;
            highlightedProximity = proximity;
            highlightedAxis = axisIndex;

            prevRotation = tracked.rotation;
            localRotationDirection = tangentRotations[axisIndex] * intersection.normalized;
            prevOffset = Quaternion.identity;
        }

        private void SetHighlightedStateToScalingAxis(float proximity, int axisIndex, Vector3 intersection)
        {
            highlightedState = TransformGizmoState.ScalingAxis;
            highlightedProximity = proximity;
            highlightedAxis = axisIndex;
        }

        private void SetHighlightedStateToScalingWhole(float proximity, Vector3 intersection)
        {
            highlightedState = TransformGizmoState.ScalingWhole;
            highlightedProximity = proximity;
        }

        #endregion
    }
}
