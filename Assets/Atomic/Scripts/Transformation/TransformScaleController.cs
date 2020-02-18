using Atomic.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static OVRSkeleton;

namespace Atomic.Transformation
{
    public class TransformScaleController : Singleton<TransformScaleController>
    {
        #region public vars
        public LineRenderer rightLineRenderer, leftLineRenderer;
        public Transform rightStartingPoint, leftStartingPoint;
        public Transform rightCurrentPositionMarker, leftCurrentPositionMarker;
        public bool rightStickActive, leftStickActive;
        public UnityAction<Hand> BeginScaleDrag, EndScaleDrag, BeginScaleHold, EndScaleHold;

        [HideInInspector]
        public Vector3 rightStickDirection
        {
            get
            {
                return (rightCurrentPositionMarker.position - rightStartingPoint.position) * (1 - _stickDeadZone);
            }
            set
            {
                rightStickDirection = value;
            }
        }
        [HideInInspector]
        public Vector3 leftStickDirection
        {
            get
            {
                return (leftCurrentPositionMarker.position - leftStartingPoint.position) * (1 - _stickDeadZone);
            }
            set
            {
                leftStickDirection = value;
            }
        }
        public Transform currentPivot;
        #endregion public vars

        #region private vars
        [SerializeField] private GameObject _rightRefScaleObject, _leftRefScaleObject, _rightNewScaleObject, _leftNewScaleObject;

        private MeshRenderer[] _rightStartingPointRenderer, _leftStartingPointRenderer;
        private MeshRenderer _rightCurrentPositionRenderer, _leftCurrentPositionRenderer;
        private Collider[] _rightColliders, _leftColliders;
        private AtomicInput _input;
        private RightMarker _rightControllerMarker;
        private LeftMarker _leftControllerMarker;
        private float _rightMarkerDistanceMagnitude, _leftMarkerDistanceMagnitude;
        private float _rightTravelPercentage, _leftTravelPercentage;
        private Vector3 _previousRightStickDirection, _previousLeftStickDirection;
        private bool _isRightScaling, _isLeftScaling;
        private const float _defaultRefScale = 0.043f;
        private Vector3 _rightTargetStartingScale, _leftTargetStartingScale;
        private float _rightStartingScaleProportion, _leftStartingScaleProportion;

        /// <summary>
        /// the node that displays the radius at which the hand exited the starting point's disabling influence to engage rotation
        /// this node displays while rotation is engaged and hides while it is not (i.e. while the hand is close to the starting point)
        /// </summary>
        // [SerializeField] private Transform _rightSurfaceLockMarker, _leftSurfaceLockMarker;
        //private MeshRenderer _rightSurfaceMarkerRenderer, _leftSurfaceMarkerRenderer;

        private const float _stickDeadZone = 0.01f;

        // line renderer style
        private const float _startPadding = 0.03f;
        private const float _endPadding = 0.014f;
        private const float _regularRectSize = 0.02f, _grabbedRectSize = 0.015f;

        private OVRSkeleton _rightHandSkeleton, _leftHandSkeleton;
        [SerializeField] private Transform _rightPreMarker, _leftPreMarker;
        private MeshRenderer _rightPreMarkerRenderer, _leftPreMarkerRenderer;
        #endregion private vars

        #region init
        private void Awake()
        {
            _rightColliders = transform.GetChild(0).GetComponentsInChildren<Collider>();
            _leftColliders = transform.GetChild(1).GetComponentsInChildren<Collider>();

            _input = AtomicInput.Instance;

            _rightStartingPointRenderer = rightStartingPoint.GetComponentsInChildren<MeshRenderer>();

            if (rightCurrentPositionMarker == null)
                Debug.LogError("The TranslationToolController couldn't locate its right position marker. Please assign it in Inspector.");
            else _rightCurrentPositionRenderer = rightCurrentPositionMarker.GetComponent<MeshRenderer>();

            rightLineRenderer = transform.GetComponentInChildren<LineRenderer>();

            _leftStartingPointRenderer = leftStartingPoint.GetComponentsInChildren<MeshRenderer>();

            if (leftCurrentPositionMarker == null)
                Debug.LogError("The TranslationToolController couldn't locate its left position marker. Please assign it in Inspector.");
            else _leftCurrentPositionRenderer = leftCurrentPositionMarker.GetComponent<MeshRenderer>();

            _rightControllerMarker = RightMarker.Instance;
            _leftControllerMarker = LeftMarker.Instance;

            _rightHandSkeleton = _rightControllerMarker.transform.GetComponentInChildren<OVRSkeleton>();
            _leftHandSkeleton = _leftControllerMarker.transform.GetComponentInChildren<OVRSkeleton>();

            _rightPreMarkerRenderer = _rightPreMarker.GetChild(0).GetComponent<MeshRenderer>();
            _leftPreMarkerRenderer = _leftPreMarker.GetChild(0).GetComponent<MeshRenderer>();

            ToggleVisibility(false, false);
            ToggleVisibility(false, true);
            ToggleColliders(false, false);
            ToggleColliders(false, true);
        }

        private void OnEnable()
        {
            _input.OnRightStateChanged += OnRightStateChanged;
            _input.OnLeftStateChanged += OnLeftStateChanged;

            AtomicModeController.Instance.RightModeChanged += OnRightModeChanged;
            AtomicModeController.Instance.LeftModeChanged += OnLeftModeChanged;
        }

        private void OnDisable()
        {
            _input.OnRightStateChanged -= OnRightStateChanged;
            _input.OnLeftStateChanged -= OnLeftStateChanged;

            if (AtomicModeController.Instance != null)
            {
                AtomicModeController.Instance.RightModeChanged -= OnRightModeChanged;
                AtomicModeController.Instance.LeftModeChanged -= OnLeftModeChanged;
            }
        }
        #endregion init

        #region loops & timers
        private void Update()
        {
            if (AtomicModeController.Instance.currentRightMode == TransformMode.Scale && !rightStickActive)
            {
                _rightPreMarkerRenderer.enabled = true;
                _rightPreMarker.position = _rightHandSkeleton.Bones[(int)BoneId.Hand_ThumbTip].Transform.position;
                _rightPreMarker.rotation = Quaternion.LookRotation(RightMarker.Instance.palmNormal);
            }
            else _rightPreMarkerRenderer.enabled = false;

            if (AtomicModeController.Instance.currentLeftMode == TransformMode.Scale && !leftStickActive)
            {
                _leftPreMarkerRenderer.enabled = true;
                _leftPreMarker.position = _leftHandSkeleton.Bones[(int)BoneId.Hand_ThumbTip].Transform.position;
                _leftPreMarker.rotation = Quaternion.LookRotation(LeftMarker.Instance.palmNormal);
            }
            else _leftPreMarkerRenderer.enabled = false;

            if (rightStickActive)
            {
                if (!AtomicModeController.Instance.HasFlag(AtomicModeController.Instance.currentRightMode, TransformMode.Scale))
                    return;

                UpdateCurrentMarkerPosition(true);

                if (_previousRightStickDirection == Vector3.zero)
                    _previousRightStickDirection = rightStickDirection;

                UpdateLineRenderer(true);

                if (_isRightScaling)
                {
                    float newScaleModifier = _rightMarkerDistanceMagnitude / _rightStartingScaleProportion;
                    float newRefScale = newScaleModifier * _rightRefScaleObject.transform.localScale.x;
                    _rightNewScaleObject.transform.localScale = new Vector3(newRefScale, newRefScale, newRefScale);
                    currentPivot.localScale = new Vector3(newScaleModifier * _rightTargetStartingScale.x,
                                                            newScaleModifier * _rightTargetStartingScale.y,
                                                            newScaleModifier * _rightTargetStartingScale.z);
                }
            }

            if (leftStickActive)
            {
                if (!AtomicModeController.Instance.HasFlag(AtomicModeController.Instance.currentLeftMode, TransformMode.Scale))
                    return;

                UpdateCurrentMarkerPosition(false);

                if (_previousLeftStickDirection == Vector3.zero)
                    _previousLeftStickDirection = leftStickDirection;

                UpdateLineRenderer(false);

                if (_isLeftScaling)
                {
                    float newScaleModifier = _leftMarkerDistanceMagnitude / _leftStartingScaleProportion;
                    float newRefScale = newScaleModifier * _leftRefScaleObject.transform.localScale.x;
                    _leftNewScaleObject.transform.localScale = new Vector3(newRefScale, newRefScale, newRefScale);
                    currentPivot.localScale = new Vector3(newScaleModifier * _leftTargetStartingScale.x,
                                                            newScaleModifier * _leftTargetStartingScale.y,
                                                            newScaleModifier * _leftTargetStartingScale.z);
                }
            }
        }
        #endregion loops

        #region event handlers
        private void OnRightStateChanged(HandGestureState current, HandGestureState previous)
        {
            if (!(AtomicModeController.Instance.currentRightMode == TransformMode.Scale))
                return;

            if (current == HandGestureState.Insert && !_isRightScaling)
                ToggleScalingState(true, true);
            else if (current != HandGestureState.Insert && _isRightScaling)
                ToggleScalingState(false, true);
            else ToggleTool(current, previous, true);
        }

        private void OnLeftStateChanged(HandGestureState current, HandGestureState previous)
        {
            if (!(AtomicModeController.Instance.currentLeftMode == TransformMode.Scale))
                return;

            if (current == HandGestureState.Insert && !_isLeftScaling)
                ToggleScalingState(true, false);
            else if (current != HandGestureState.Insert && _isLeftScaling)
                ToggleScalingState(false, false);
            else ToggleTool(current, previous, false);
        }

        private void OnRightModeChanged(TransformMode newMode, TransformMode previousMode)
        {
            if (newMode != TransformMode.Scale)
                DisengageScaleTool(true, ref rightStickActive, ref rightLineRenderer);
        }

        private void OnLeftModeChanged(TransformMode newMode, TransformMode previousMode)
        {
            if (newMode != TransformMode.Scale)
                DisengageScaleTool(false, ref leftStickActive, ref leftLineRenderer);
        }
        #endregion event handlers

        #region helper methods
        private void ToggleTool(HandGestureState currentState, HandGestureState previousState, bool right)
        {
            //Debug.Log("ToggleTool - currentstate: " + currentState);
            if (currentState == HandGestureState.DragSelection && 
                (previousState == HandGestureState.Selection || previousState == HandGestureState.Neutral))
            {
                if (right && !rightStickActive)
                {
                    rightCurrentPositionMarker.position = rightStartingPoint.position = _rightHandSkeleton.Bones[(int)BoneId.Hand_ThumbTip].Transform.position;
                    rightStartingPoint.rotation = _rightControllerMarker.transform.rotation;
                    rightCurrentPositionMarker.rotation = currentPivot.rotation;
                    rightStickActive = true;
                    AtomicSelection.Instance.BeginTransformation(true);
                    ToggleVisibility(true, true);
                    ToggleColliders(true, true);
                    BeginScaleDrag?.Invoke(Hand.Right);
                    //Debug.Log("begin drag");
                }
                else if (!right && !leftStickActive)
                {
                    leftCurrentPositionMarker.position = leftStartingPoint.position = _leftHandSkeleton.Bones[(int)BoneId.Hand_ThumbTip].Transform.position;
                    leftStartingPoint.rotation = _leftControllerMarker.transform.rotation;
                    leftCurrentPositionMarker.rotation = currentPivot.rotation;
                    leftStickActive = true;
                    AtomicSelection.Instance.BeginTransformation(false);
                    ToggleVisibility(true, false);
                    ToggleColliders(true, false);
                    BeginScaleDrag?.Invoke(Hand.Left);
                    //Debug.Log("begin drag");
                }

            }
            else if (currentState == HandGestureState.Stop)
            {
                if (right)
                    DisengageScaleTool(true, ref rightStickActive, ref rightLineRenderer);
                else DisengageScaleTool(false, ref leftStickActive, ref leftLineRenderer);
                EndScaleDrag?.Invoke((right) ? Hand.Right : Hand.Left);
                Debug.Log("end drag");
            }
        }

        private void DisengageScaleTool(bool right, ref bool stickActive, ref LineRenderer lineRenderer)
        {
            ToggleVisibility(false, right);
            ToggleColliders(false, right);
            rightLineRenderer.SetPositions(new Vector3[] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero });
            stickActive = false;
            AtomicSelection.Instance.CompleteTransformation(right);
        }

        private void ToggleScalingState(bool on, bool right)
        {
            if (right)
            {
                _isRightScaling = on;
                if (_isRightScaling)
                {
                    _rightTargetStartingScale = currentPivot.localScale;
                    _rightStartingScaleProportion = _rightMarkerDistanceMagnitude;
                    BeginScaleHold?.Invoke(Hand.Right);
                }
                else
                {
                    _rightNewScaleObject.transform.localScale = _rightRefScaleObject.transform.localScale;
                    EndScaleHold?.Invoke(Hand.Right);
                }
            }
            else
            {
                _isLeftScaling = on;
                if (_isLeftScaling)
                {
                    _leftTargetStartingScale = currentPivot.localScale;
                    _leftStartingScaleProportion = _leftMarkerDistanceMagnitude;
                    BeginScaleHold?.Invoke(Hand.Left);
                }
                else
                {
                    _leftNewScaleObject.transform.localScale = _leftRefScaleObject.transform.localScale;
                    EndScaleHold?.Invoke(Hand.Left);
                }
            }
        }

        private void ToggleVisibility(bool on, bool right)
        {
            if (right)
            {
                _rightCurrentPositionRenderer.enabled = on;
                foreach (MeshRenderer renderer in _rightStartingPointRenderer)
                    renderer.enabled = on;
                if (!on)
                {
                    rightCurrentPositionMarker.position = rightStartingPoint.position;
                    //_rightSurfaceMarkerRenderer.enabled = false;
                }
            }
            else
            {
                _leftCurrentPositionRenderer.enabled = on;
                foreach (MeshRenderer renderer in _leftStartingPointRenderer)
                    renderer.enabled = on;
                if (!on)
                {
                    leftCurrentPositionMarker.position = leftStartingPoint.position;
                    // _leftSurfaceMarkerRenderer.enabled = false;
                }
            }
        }

        private void ToggleColliders(bool on, bool right)
        {
            if (right)
            {
                if (_rightColliders.Length > 0)
                    foreach (Collider collider in _rightColliders)
                        collider.enabled = on;
            }
            else
            {
                if (_leftColliders.Length > 0)
                    foreach (Collider collider in _leftColliders)
                        collider.enabled = on;
            }
        }

        private void UpdateCurrentMarkerPosition(bool right)
        {
            if (right)
            {
                Vector3 handTarget = _rightHandSkeleton.Bones[(int)BoneId.Hand_ThumbTip].Transform.position;
                _rightMarkerDistanceMagnitude = Mathf.Abs((handTarget - rightStartingPoint.position).magnitude);
                _rightTravelPercentage = Mathf.Pow(Mathf.Clamp01(_rightMarkerDistanceMagnitude * 100), 10);
                rightCurrentPositionMarker.position = Vector3.Lerp(rightStartingPoint.position, handTarget, _rightTravelPercentage);
                rightCurrentPositionMarker.rotation = Quaternion.Lerp(rightStartingPoint.rotation, _rightControllerMarker.transform.rotation, _rightTravelPercentage);
            }
            else
            {
                Vector3 handTarget = _leftHandSkeleton.Bones[(int)BoneId.Hand_ThumbTip].Transform.position;
                _leftMarkerDistanceMagnitude = Mathf.Abs((handTarget - leftStartingPoint.position).magnitude);
                _leftTravelPercentage = Mathf.Pow(Mathf.Clamp01(_leftMarkerDistanceMagnitude * 100), 10);
                leftCurrentPositionMarker.position = Vector3.Lerp(leftStartingPoint.position, handTarget, _leftTravelPercentage);
                leftCurrentPositionMarker.rotation = Quaternion.Lerp(leftStartingPoint.rotation, _leftControllerMarker.transform.rotation, _leftTravelPercentage);
            }

        }

        private void UpdateLineRenderer(bool right)
        {
            if (right)
            {
                // line rendering prep
                Vector3 directionToCurrent = new Vector3();
                Vector3 startLinePoint = GetStartingArrowPoint(true, ref directionToCurrent);
                Vector3 currentPosLinePoint = rightCurrentPositionMarker.position - directionToCurrent * _endPadding;

                // render the line
                if (_rightMarkerDistanceMagnitude > _startPadding + _endPadding)
                {
                    Vector3 handTarget = _rightControllerMarker.transform.position;
                    Vector3 viewVector = AtomicModeController.Instance.rightCam.transform.position - currentPosLinePoint;
                    float rectSize = (_isRightScaling) ? _grabbedRectSize : _regularRectSize;
                    Vector3 sideRectPoint1 = currentPosLinePoint - directionToCurrent * rectSize + Vector3.Cross(-1 * directionToCurrent, viewVector).normalized * rectSize / 2;
                    Vector3 sideRectPoint2 = currentPosLinePoint - directionToCurrent * rectSize + Vector3.Cross(directionToCurrent, viewVector).normalized * rectSize / 2;
                    Vector3 sideRectPoint3 = currentPosLinePoint + Vector3.Cross(-1 * directionToCurrent, viewVector).normalized * rectSize / 2;
                    Vector3 sideRectPoint4 = currentPosLinePoint + Vector3.Cross(directionToCurrent, viewVector).normalized * rectSize / 2;
                    Vector3 rectStartPoint = currentPosLinePoint - directionToCurrent * rectSize;
                    rightLineRenderer.SetPositions(new Vector3[] { startLinePoint, rectStartPoint, sideRectPoint1, sideRectPoint3, sideRectPoint4, sideRectPoint2, rectStartPoint });
                }
                else rightLineRenderer.SetPositions(new Vector3[] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero });
            }
            else
            {
                // line rendering prep
                Vector3 directionToCurrent = new Vector3();
                Vector3 startLinePoint = GetStartingArrowPoint(false, ref directionToCurrent);
                Vector3 currentPosLinePoint = leftCurrentPositionMarker.position - directionToCurrent * _endPadding;

                // render the line
                if (_leftMarkerDistanceMagnitude > _startPadding + _endPadding)
                {
                    Vector3 handTarget = _leftControllerMarker.transform.position;
                    Vector3 viewVector = AtomicModeController.Instance.rightCam.transform.position - currentPosLinePoint;
                    float rectSize = (_isRightScaling) ? _grabbedRectSize : _regularRectSize;
                    Vector3 sideRectPoint1 = currentPosLinePoint - directionToCurrent * rectSize + Vector3.Cross(-1 * directionToCurrent, viewVector).normalized * rectSize / 2;
                    Vector3 sideRectPoint2 = currentPosLinePoint - directionToCurrent * rectSize + Vector3.Cross(directionToCurrent, viewVector).normalized * rectSize / 2;
                    Vector3 sideRectPoint3 = currentPosLinePoint + Vector3.Cross(-1 * directionToCurrent, viewVector).normalized * rectSize / 2;
                    Vector3 sideRectPoint4 = currentPosLinePoint + Vector3.Cross(directionToCurrent, viewVector).normalized * rectSize / 2;
                    Vector3 rectStartPoint = currentPosLinePoint - directionToCurrent * rectSize;
                    leftLineRenderer.SetPositions(new Vector3[] { startLinePoint, rectStartPoint, sideRectPoint1, sideRectPoint3, sideRectPoint4, sideRectPoint2, rectStartPoint });
                }
                else leftLineRenderer.SetPositions(new Vector3[] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero });
            }
        }

        /// <summary>
        /// A helper arrow begins to draw immediately outside of the gizmo when the user's hand has gone beyond its threshold radius.
        /// Calculate where it should begin drawing.
        /// </summary>
        /// <param name="right">right or left hand</param>
        /// <param name="directionToCurrent">vector that will point from starting point to current hand position</param>
        /// <returns>the point just outside the gizmo where the arrow begins</returns>
        private Vector3 GetStartingArrowPoint(bool right, ref Vector3 directionToCurrent)
        {
            if (right)
            {
                directionToCurrent = (rightCurrentPositionMarker.position - rightStartingPoint.position).normalized;
                return rightStartingPoint.position + directionToCurrent * _startPadding;
            }
            else
            {
                directionToCurrent = (leftCurrentPositionMarker.position - leftStartingPoint.position).normalized;
                return leftStartingPoint.position + directionToCurrent * _startPadding;
            }
        }

        /// <summary>
        /// A helper arrow begins to draw immediately outside of the gizmo when the user's hand has gone beyond its threshold radius.
        /// Calculate where it should begin drawing.
        /// </summary>
        /// <param name="right">right or left hand</param>
        /// <returns>the point just outside the gizmo where the arrow begins</returns>
        private Vector3 GetStartingArrowPoint(bool right)
        {
            if (right)
            {
                Vector3 directionToCurrent = (rightCurrentPositionMarker.position - rightStartingPoint.position).normalized;
                return rightStartingPoint.position + directionToCurrent * _startPadding;
            }
            else
            {
                Vector3 directionToCurrent = (leftCurrentPositionMarker.position - leftStartingPoint.position).normalized;
                return leftStartingPoint.position + directionToCurrent * _startPadding;
            }
        }
        #endregion helper methods
    }
}
