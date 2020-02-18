using Atomic.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Atomic.Transformation
{
    public class TransformPivotController : MonoBehaviour
    {
        #region public vars
        public LineRenderer rightLineRenderer, leftLineRenderer;
        public Transform rightStartingPoint, leftStartingPoint;
        public Transform rightCurrentPositionMarker, leftCurrentPositionMarker;
        public bool rightStickActive, leftStickActive;
        [HideInInspector] public Vector3 rightStickVelocity, leftStickVelocity;
        public UnityAction<Hand> BeginTranslationDrag, EndTranslationDrag;
        #endregion public vars

        #region private vars
        private MeshRenderer[] _rightStartingPointRenderer, _leftStartingPointRenderer;
        private MeshRenderer _rightCurrentPositionRenderer, _leftCurrentPositionRenderer;
        private Collider[] _rightColliders, _leftColliders;
        private AtomicInput _input;
        private RightMarker _rightControllerMarker;
        private LeftMarker _leftControllerMarker;
        private float _rightMarkerDistanceMagnitude, _leftMarkerDistanceMagnitude;
        private float _rightTravelPercentage, _leftTravelPercentage;
        [SerializeField] private Vector3 _hand2HUDOffset;

        // velocity properties
        private float _velocityStrengthFactor;
        private const float _stickDeadZone = 0.01f;

        // line renderer style
        private const float _startPadding = 0.03f;
        private const float _endPadding = 0.014f;
        private const float _arrowheadHeight = 0.03f;
        private const float _arrowheadWidth = 0.02f;
        #endregion private vars

        #region init
        private void Awake()
        {
            if (!gameObject.activeInHierarchy) return;
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

        #region loops
        private void Update()
        {
            if (rightStickActive)
            {
                if (!AtomicModeController.Instance.HasFlag(AtomicModeController.Instance.currentRightMode, TransformMode.Translate))
                    return;

                rightStickVelocity = rightCurrentPositionMarker.position - rightStartingPoint.position;
                rightStickVelocity = rightStickVelocity * (1 - _stickDeadZone);

                UpdateCurrentMarkerPosition(true);
                UpdateLineRenderer(true);
            }

            if (leftStickActive)
            {
                if (!AtomicModeController.Instance.HasFlag(AtomicModeController.Instance.currentLeftMode, TransformMode.Translate))
                    return;

                leftStickVelocity = leftCurrentPositionMarker.position - leftStartingPoint.position;
                leftStickVelocity = leftStickVelocity * (1 - _stickDeadZone);

                UpdateCurrentMarkerPosition(false);
                UpdateLineRenderer(false);
            }
        }
        #endregion loops

        #region event handlers
        private void OnRightStateChanged(HandGestureState current, HandGestureState previous)
        {
            if (!AtomicModeController.Instance.HasFlag(AtomicModeController.Instance.currentRightMode, TransformMode.Translate))
                return;

            if (current == HandGestureState.DragSelection || (current != HandGestureState.DragSelection))
                ToggleTool(current, previous, true);
        }

        private void OnLeftStateChanged(HandGestureState current, HandGestureState previous)
        {
            if (!AtomicModeController.Instance.HasFlag(AtomicModeController.Instance.currentLeftMode, TransformMode.Translate))
                return;

            if (current == HandGestureState.DragSelection || (current != HandGestureState.DragSelection))
                ToggleTool(current, previous, false);
        }

        private void OnRightModeChanged(TransformMode newMode, TransformMode previousMode)
        {
            if (newMode != TransformMode.Translate)
            {
                ToggleTranslateToolVisibility(false, true);
                ToggleTranslateToolColliders(false, true);
            }
        }

        private void OnLeftModeChanged(TransformMode newMode, TransformMode previousMode)
        {
            if (newMode != TransformMode.Translate)
            {
                ToggleTranslateToolVisibility(false, false);
                ToggleTranslateToolColliders(false, false);
            }
        }
        #endregion event handlers

        #region helper methods
        private void ToggleTool(HandGestureState currentState, HandGestureState previousState, bool right)
        {
            if (currentState == HandGestureState.DragSelection)
            {
                if (right)
                {
                    rightStartingPoint.position = _rightControllerMarker.transform.position + _hand2HUDOffset;
                    rightStartingPoint.rotation = _rightControllerMarker.transform.rotation;
                    rightCurrentPositionMarker.position = _rightControllerMarker.transform.position + _hand2HUDOffset;
                    rightCurrentPositionMarker.rotation = _rightControllerMarker.transform.rotation;
                    rightStickActive = true;
                    AtomicSelection.Instance.BeginTransformation(true);
                }
                else
                {
                    leftStartingPoint.position = _leftControllerMarker.transform.position + _hand2HUDOffset;
                    leftStartingPoint.rotation = _leftControllerMarker.transform.rotation;
                    leftCurrentPositionMarker.position = _leftControllerMarker.transform.position + _hand2HUDOffset;
                    leftCurrentPositionMarker.rotation = _leftControllerMarker.transform.rotation;
                    leftStickActive = true;
                    AtomicSelection.Instance.BeginTransformation(false);
                }
                ToggleTranslateToolVisibility(true, right);
                ToggleTranslateToolColliders(true, right);
                BeginTranslationDrag?.Invoke((right) ? Hand.Right : Hand.Left);
                Debug.Log("begin drag");
            }
            else if (previousState == HandGestureState.DragSelection && currentState != HandGestureState.DragSelection)
            {
                ToggleTranslateToolVisibility(false, right);
                ToggleTranslateToolColliders(false, right);
                if (right)
                {
                    rightLineRenderer.SetPositions(new Vector3[] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero });
                    rightStickActive = false;
                    AtomicSelection.Instance.CompleteTransformation(true);
                }
                else
                {
                    leftLineRenderer.SetPositions(new Vector3[] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero });
                    leftStickActive = false;
                    AtomicSelection.Instance.CompleteTransformation(false);
                }
                EndTranslationDrag?.Invoke((right) ? Hand.Right : Hand.Left);
                Debug.Log("end drag");
            }
        }

        private void ToggleTranslateToolVisibility(bool on, bool right)
        {
            if (right)
            {
                _rightCurrentPositionRenderer.enabled = on;
                foreach (MeshRenderer renderer in _rightStartingPointRenderer)
                    renderer.enabled = on;
                if (!on)
                    rightCurrentPositionMarker.position = rightStartingPoint.position;
            }
            else
            {
                _leftCurrentPositionRenderer.enabled = on;
                foreach (MeshRenderer renderer in _leftStartingPointRenderer)
                    renderer.enabled = on;
                if (!on)
                    leftCurrentPositionMarker.position = leftStartingPoint.position;
            }
        }

        private void ToggleTranslateToolColliders(bool on, bool right)
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
                Vector3 handTarget = _rightControllerMarker.transform.position + _hand2HUDOffset;
                _rightMarkerDistanceMagnitude = Mathf.Abs((handTarget - rightStartingPoint.position).magnitude);
                _rightTravelPercentage = Mathf.Pow(Mathf.Clamp01(_rightMarkerDistanceMagnitude * 100), 10);
                rightCurrentPositionMarker.position = Vector3.Lerp(rightStartingPoint.position, handTarget, _rightTravelPercentage);
                rightCurrentPositionMarker.rotation = Quaternion.Lerp(rightStartingPoint.rotation, _rightControllerMarker.transform.rotation, _rightTravelPercentage);
            }
            else
            {
                Vector3 handTarget = _leftControllerMarker.transform.position + _hand2HUDOffset;
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
                //Debug.Log("right line updating");
                // line rendering prep
                Vector3 directionToCurrent = (rightCurrentPositionMarker.position - rightStartingPoint.position).normalized;
                Vector3 startLinePoint = rightStartingPoint.position + directionToCurrent * _startPadding;
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
                    Debug.Log("right line magnitude: " + _rightMarkerDistanceMagnitude);
                }
                else rightLineRenderer.SetPositions(new Vector3[] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero });
            }
            else
            {
                //Debug.Log("left line updating");

                // line rendering prep
                Vector3 directionToCurrent = (leftCurrentPositionMarker.position - leftStartingPoint.position).normalized;
                Vector3 startLinePoint = leftStartingPoint.position + directionToCurrent * _startPadding;
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
        #endregion helper methods
    }
}

