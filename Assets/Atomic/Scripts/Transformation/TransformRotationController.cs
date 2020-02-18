using Atomic.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static OVRSkeleton;

namespace Atomic.Transformation
{
    public class TransformRotationController : Singleton<TransformRotationController>
    {
        #region public vars
        /// <summary>
        /// if true, controller affects only one axis of rotation instead of all three
        /// </summary>
        [SerializeField] public bool singleAxisMode;
        public Transform rightPreMarker, leftPreMarker;
        public LineRenderer rightLineRenderer, leftLineRenderer;
        public Transform rightStartingPoint, leftStartingPoint;
        public float rightStartingLocalRoll, leftStartingLocalRoll;
        public Transform rightCurrentPositionMarker, leftCurrentPositionMarker;
        public bool rightStickActive, leftStickActive;
        public UnityAction<Hand> BeginRotationDrag, EndRotationDrag;
        public Vector3 currentRightUpAxis, currentLeftUpAxis;
        public bool rightRotationGearEngaged, leftRotationGearEngaged;
        public float currentAngularVelocity;
        [Range(0, 1)] public float angularVelocityStrength;
        public const float angularInertialRateOfDecay = 0.02f;
        [HideInInspector] public int rightAngularVelocitySign, leftAngularVelocitySign;
        [HideInInspector] public Vector3 rightAngularVelocityAxis, leftAngularVelocityAxis;

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
        /*[HideInInspector]*/ public Transform currentPivot;

        /// <summary>
        /// The rotational difference between selected objects current forward facing and the starting facing vector
        /// of the rotation gizmo. This is used to prevent the object from snapping to the gizmo's forward facing when you begin rotation.
        /// </summary>
        [HideInInspector] public Quaternion currentRotationOffset;
        #endregion public vars

        #region private vars
        private MeshRenderer _rightPreMarkerRenderer, _leftPreMarkerRenderer;
        private MeshRenderer[] _rightStartingPointRenderer, _leftStartingPointRenderer;
        private MeshRenderer _rightCurrentPositionRenderer, _leftCurrentPositionRenderer;
        private Collider[] _rightColliders, _leftColliders;
        private AtomicInput _input;
        private RightMarker _rightControllerMarker;
        private LeftMarker _leftControllerMarker;
        private float _rightMarkerDistanceMagnitude, _leftMarkerDistanceMagnitude;
        private float _rightTravelPercentage, _leftTravelPercentage;
        private const float _gearThreshold = 0.03f;
        private Vector3 _previousRightStickDirection, _previousLeftStickDirection;
        private Vector3 _previousRightOrthoDirection, _previousLeftOrthoDirection;


        private IEnumerator _rightRotationGearToggle, _leftRotationGearToggle;
        [SerializeField] private Vector3 _hand2HUDOffset;

        /// <summary>
        /// the node that displays the radius at which the hand exited the starting point's disabling influence to engage rotation
        /// this node displays while rotation is engaged and hides while it is not (i.e. while the hand is close to the starting point)
        /// </summary>
        [SerializeField] private Transform _rightSurfaceLockMarker, _leftSurfaceLockMarker;
        private MeshRenderer _rightSurfaceMarkerRenderer, _leftSurfaceMarkerRenderer;

        // velocity properties
        private float _velocityStrengthFactor;
        private const float _stickDeadZone = 0.01f;

        // line renderer style
        private const float _startPadding = 0.03f;
        private const float _endPadding = 0.014f;
        private const float _arrowheadHeight = 0.03f;
        private const float _arrowheadWidth = 0.02f;

        OVRSkeleton _rightHandSkeleton, _leftHandSkeleton;
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

            if (_rightSurfaceLockMarker != null)
                _rightSurfaceMarkerRenderer = _rightSurfaceLockMarker.GetComponent<MeshRenderer>();
            else Debug.LogError("RotationToolController couldn't locate the _rightSurfaceLockMarker. Is it assigned in the Inspector?");

            if (_leftSurfaceLockMarker != null)
                _leftSurfaceMarkerRenderer = _leftSurfaceLockMarker.GetComponent<MeshRenderer>();
            else Debug.LogError("RotationToolController couldn't locate the _leftSurfaceLockMarker. Is it assigned in the Inspector?");

            _rightHandSkeleton = _rightControllerMarker.transform.GetComponentInChildren<OVRSkeleton>();
            _leftHandSkeleton = _leftControllerMarker.transform.GetComponentInChildren<OVRSkeleton>();

            _rightPreMarkerRenderer = rightPreMarker.GetChild(0).GetComponent<MeshRenderer>();
            _leftPreMarkerRenderer = leftPreMarker.GetChild(0).GetComponent<MeshRenderer>();
            
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
            UpdatePreMarkers();

            if (rightStickActive)
            {
                if (!AtomicModeController.Instance.HasFlag(AtomicModeController.Instance.currentRightMode, TransformMode.Rotate))
                    return;

                UpdateCurrentMarkerPosition(true);

                if (rightRotationGearEngaged)
                {
                    if (_previousRightStickDirection == Vector3.zero)
                        _previousRightStickDirection = rightStickDirection;

                    rightAngularVelocityAxis = Vector3.Cross(_previousRightStickDirection, rightStickDirection);
                    _rightSurfaceLockMarker.position = GetStartingArrowPoint(true);
                    _rightSurfaceLockMarker.rotation = Quaternion.LookRotation(rightStickDirection);
                    UpdateLineRenderer(true);
                }
                else if (_previousRightStickDirection != Vector3.zero)
                    _previousRightStickDirection = Vector3.zero;
            }

            if (leftStickActive)
            {
                if (!AtomicModeController.Instance.HasFlag(AtomicModeController.Instance.currentLeftMode, TransformMode.Rotate))
                    return;

                UpdateCurrentMarkerPosition(false);

                if (leftRotationGearEngaged)
                {
                    if (_previousLeftStickDirection == Vector3.zero)
                        _previousLeftStickDirection = leftStickDirection;

                    leftAngularVelocityAxis = Vector3.Cross(_previousLeftStickDirection, leftStickDirection);
                    _leftSurfaceLockMarker.position = GetStartingArrowPoint(false);
                    _leftSurfaceLockMarker.rotation = Quaternion.LookRotation(leftStickDirection);
                    UpdateLineRenderer(false);
                }
                else if (_previousLeftStickDirection != Vector3.zero)
                    _previousLeftStickDirection = Vector3.zero;
            }
        }

        /// <summary>
        /// Ratchet loop that handles state when the hand snaps on/off the root position of the gizmo during drag
        /// </summary>
        private IEnumerator ToggleRotationGear(bool right)
        {


            bool firstTimeOnly = true;
            if (right)
            {
                if (!AtomicModeController.Instance.HasFlag(AtomicModeController.Instance.currentRightMode, TransformMode.Rotate))
                    yield break;
                while (rightStickActive)
                {
                    yield return new WaitUntil(() => (_rightMarkerDistanceMagnitude > _gearThreshold || !rightStickActive));

                    if (!rightStickActive ||
                        !AtomicModeController.Instance.HasFlag(AtomicModeController.Instance.currentRightMode, TransformMode.Rotate))
                        yield break;
                    _rightSurfaceLockMarker.position = GetStartingArrowPoint(true);
                    rightRotationGearEngaged = true;

                    if (firstTimeOnly)
                    {
                        CalculateRotationOffset(true);
                        firstTimeOnly = false;
                    }

                    rightStartingLocalRoll = RightMarker.Instance.transform.parent.localEulerAngles.z;

                    ADM.QLog("right gear on");

                    yield return new WaitUntil(() => (_rightMarkerDistanceMagnitude < _gearThreshold || !rightStickActive));


                    rightStartingLocalRoll = 0;
                    rightRotationGearEngaged = false;
                    _rightSurfaceLockMarker.position = rightStartingPoint.position;
                    _rightSurfaceLockMarker.rotation = rightStartingPoint.rotation;
                    ADM.QLog("right gear off");
                }
                _rightSurfaceLockMarker.position = rightStartingPoint.position;
                _rightSurfaceLockMarker.rotation = rightStartingPoint.rotation;
            }
            else
            {
                if (!AtomicModeController.Instance.HasFlag(AtomicModeController.Instance.currentLeftMode, TransformMode.Rotate))
                    yield break;

                while (leftStickActive)
                {
                    yield return new WaitUntil(() => (_leftMarkerDistanceMagnitude > _gearThreshold || !leftStickActive));

                    if (!leftStickActive ||
                        !AtomicModeController.Instance.HasFlag(AtomicModeController.Instance.currentLeftMode, TransformMode.Rotate))
                        yield break;
                    _leftSurfaceLockMarker.position = GetStartingArrowPoint(false);
                    leftRotationGearEngaged = true;

                    if (firstTimeOnly)
                    {
                        CalculateRotationOffset(false);
                        firstTimeOnly = false;
                    }

                    leftStartingLocalRoll = LeftMarker.Instance.transform.parent.localEulerAngles.z;

                    ADM.QLog("left gear on");

                    yield return new WaitUntil(() => (_leftMarkerDistanceMagnitude < _gearThreshold || !leftStickActive));


                    leftStartingLocalRoll = 0;
                    leftRotationGearEngaged = false;
                    _leftSurfaceLockMarker.position = leftStartingPoint.position;
                    _leftSurfaceLockMarker.rotation = leftStartingPoint.rotation;
                    ADM.QLog("left gear off");
                }
                _leftSurfaceLockMarker.position = leftStartingPoint.position;
                _leftSurfaceLockMarker.rotation = leftStartingPoint.rotation;
            }
        }
        #endregion loops

        #region event handlers
        private void OnRightStateChanged(HandGestureState current, HandGestureState previous)
        {
            if (AtomicModeController.Instance.currentRightMode == TransformMode.Rotate)
                ToggleTool(current, previous, true);
        }

        private void OnLeftStateChanged(HandGestureState current, HandGestureState previous)
        {
            if (AtomicModeController.Instance.currentLeftMode == TransformMode.Rotate)
                ToggleTool(current, previous, false);
        }

        private void OnRightModeChanged(TransformMode newMode, TransformMode previousMode)
        {
            if (newMode != TransformMode.Rotate)
                DisengageRotationTool(true, ref rightStickActive, ref rightLineRenderer);
        }

        private void OnLeftModeChanged(TransformMode newMode, TransformMode previousMode)
        {
            if (newMode != TransformMode.Rotate)
                DisengageRotationTool(false, ref leftStickActive, ref leftLineRenderer);
        }
        #endregion event handlers

        #region helper methods
        private void UpdatePreMarkers()
        {
            if (AtomicModeController.Instance.currentRightMode == TransformMode.Rotate && !rightStickActive)
            {
                _rightPreMarkerRenderer.enabled = true;
                rightPreMarker.position = _rightHandSkeleton.Bones[(int)BoneId.Hand_ThumbTip].Transform.position;
                rightPreMarker.rotation = Quaternion.LookRotation(RightMarker.Instance.palmNormal);
            }
            else _rightPreMarkerRenderer.enabled = false;

            if (AtomicModeController.Instance.currentLeftMode == TransformMode.Rotate && !leftStickActive)
            {
                _leftPreMarkerRenderer.enabled = true;
                leftPreMarker.position = _leftHandSkeleton.Bones[(int)BoneId.Hand_ThumbTip].Transform.position;
                leftPreMarker.rotation = Quaternion.LookRotation(LeftMarker.Instance.palmNormal);
            }
            else _leftPreMarkerRenderer.enabled = false;
        }

        private void ToggleTool(HandGestureState currentState, HandGestureState previousState, bool right)
        {
            if (currentState == HandGestureState.DragSelection &&
                (previousState == HandGestureState.Selection || previousState == HandGestureState.Neutral))
            {
                if (right && !rightStickActive)
                {
                    rightCurrentPositionMarker.position = rightStartingPoint.position = rightStartingPoint.position = _rightHandSkeleton.Bones[(int)BoneId.Hand_ThumbTip].Transform.position;
                    rightStartingPoint.rotation = _rightControllerMarker.transform.rotation;
                    currentRightUpAxis = rightStartingPoint.up;
                    rightCurrentPositionMarker.rotation = currentPivot.rotation;
                    rightStickActive = true;
                    AtomicSelection.Instance.BeginTransformation(true);
                    BeginRotationGearToggler(true);
                    ToggleVisibility(true, true);
                    ToggleColliders(true, true);
                    BeginRotationDrag?.Invoke(Hand.Right);
                    //ADM.QLog("begin drag");
                }
                else if (!right && !leftStickActive)
                {
                    leftCurrentPositionMarker.position = leftStartingPoint.position = _leftHandSkeleton.Bones[(int)BoneId.Hand_ThumbTip].Transform.position;
                    leftStartingPoint.rotation = _leftControllerMarker.transform.rotation;
                    currentLeftUpAxis = leftStartingPoint.up;
                    leftCurrentPositionMarker.rotation = currentPivot.rotation;
                    leftStickActive = true;
                    AtomicSelection.Instance.BeginTransformation(false);
                    BeginRotationGearToggler(false);
                    ToggleVisibility(true, false);
                    ToggleColliders(true, false);
                    BeginRotationDrag?.Invoke(Hand.Left);
                    //ADM.QLog("begin drag");
                }
            }
            else if (currentState == HandGestureState.Stop)
            {
                if (right)
                    DisengageRotationTool(true, ref rightStickActive, ref rightLineRenderer);
                else DisengageRotationTool(false, ref leftStickActive, ref leftLineRenderer);
                
                EndRotationDrag?.Invoke((right) ? Hand.Right : Hand.Left);
                ADM.QLog("end drag");
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
                    _rightSurfaceMarkerRenderer.enabled = false;
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
                    _leftSurfaceMarkerRenderer.enabled = on;
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
                    Vector3 handTarget = _rightControllerMarker.transform.position + _hand2HUDOffset;
                    Vector3 viewVector = AtomicModeController.Instance.rightCam.transform.position - handTarget;
                    Vector3 sideArrowPoint1 = handTarget - directionToCurrent * _arrowheadHeight + Vector3.Cross(-1 * directionToCurrent, viewVector).normalized * _arrowheadWidth / 2;
                    Vector3 sideArrowPoint2 = handTarget - directionToCurrent * _arrowheadHeight + Vector3.Cross(directionToCurrent, viewVector).normalized * _arrowheadWidth / 2;
                    rightLineRenderer.SetPositions(new Vector3[] { startLinePoint ,
                                                                currentPosLinePoint ,
                                                                sideArrowPoint1,
                                                                sideArrowPoint2,
                                                                currentPosLinePoint });

                }
                else rightLineRenderer.SetPositions(new Vector3[] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero });
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
                    Vector3 handTarget = _leftControllerMarker.transform.position + _hand2HUDOffset;
                    Vector3 viewVector = AtomicModeController.Instance.rightCam.transform.position - handTarget;
                    Vector3 sideArrowPoint1 = handTarget - directionToCurrent * _arrowheadHeight + Vector3.Cross(-1 * directionToCurrent, viewVector).normalized * _arrowheadWidth / 2;
                    Vector3 sideArrowPoint2 = handTarget - directionToCurrent * _arrowheadHeight + Vector3.Cross(directionToCurrent, viewVector).normalized * _arrowheadWidth / 2;

                    leftLineRenderer.SetPositions(new Vector3[] { startLinePoint, currentPosLinePoint, sideArrowPoint1, sideArrowPoint2, currentPosLinePoint });
                }
                else leftLineRenderer.SetPositions(new Vector3[] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero });
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

        private void BeginRotationGearToggler(bool right)
        {
            if (right)
            {
                InterruptRotationGearToggler(true);
                _rightRotationGearToggle = ToggleRotationGear(true);
                StartCoroutine(_rightRotationGearToggle);
            }
            else
            {
                InterruptRotationGearToggler(false);
                _leftRotationGearToggle = ToggleRotationGear(false);
                StartCoroutine(_leftRotationGearToggle);
            }
        }

        private void InterruptRotationGearToggler(bool right)
        {
            if (right)
            {
                if (_rightRotationGearToggle != null)
                    StopCoroutine(_rightRotationGearToggle);
            }
            else
            {
                if (_leftRotationGearToggle != null)
                    StopCoroutine(_leftRotationGearToggle);
            }
        }

        private void CalculateRotationOffset(bool right)
        {
            float angleHand2ObjPivot = Vector3.Angle((right) ? rightStartingPoint.transform.forward : leftStartingPoint.transform.forward, currentPivot.forward);
            Vector3 axisHand2ObjPivot = Vector3.Cross((right) ? rightStartingPoint.transform.forward : leftStartingPoint.transform.forward, currentPivot.forward);
            currentRotationOffset = Quaternion.AngleAxis(angleHand2ObjPivot, axisHand2ObjPivot);
        }

        private void DisengageRotationTool(bool right, ref bool stickActive, ref LineRenderer lineRenderer)
        {
            ToggleVisibility(false, right);
            ToggleColliders(false, right);
            lineRenderer.SetPositions(new Vector3[] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero });
            stickActive = false;
            AtomicSelection.Instance.CompleteTransformation(right);
        }
        #endregion helper methods
    }
}
