using Atomic.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static OVRSkeleton;

namespace Atomic.Transformation
{
    public class TransformTranslationController : Singleton<TransformTranslationController>
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
        private OVRHand _rightHandPoser, _leftHandPoser;
        private OVRSkeleton _rightHandSkeleton, _leftHandSkeleton;
        [SerializeField] private Vector3 _hand2HUDOffset;
        [SerializeField] private Transform _rightPreMarker, _leftPreMarker;
        private MeshRenderer _rightPreMarkerRenderer, _leftPreMarkerRenderer;
        
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

            _rightHandPoser = _rightControllerMarker.transform.GetComponentInChildren<OVRHand>();
            _leftHandPoser = _leftControllerMarker.transform.GetComponentInChildren<OVRHand>();
            _rightHandSkeleton = _rightControllerMarker.transform.GetComponentInChildren<OVRSkeleton>();
            _leftHandSkeleton = _leftControllerMarker.transform.GetComponentInChildren<OVRSkeleton>();

            _rightPreMarkerRenderer = _rightPreMarker.GetChild(0).GetComponent<MeshRenderer>();
            _leftPreMarkerRenderer = _leftPreMarker.GetChild(0).GetComponent<MeshRenderer>();

            ToggleTranslateToolVisibility(false, false);
            ToggleTranslateToolVisibility(false, true);
            ToggleTranslateToolColliders(false, false);
            ToggleTranslateToolColliders(false, true);
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
            UpdatePreMarkers();
            UpdateRight();
            UpdateLeft();
        }

        private void UpdatePreMarkers()
        {
            if (AtomicModeController.Instance.currentRightMode == TransformMode.Translate && !rightStickActive)
            {
                UpdatePreMarkersHelper(true, ref _rightPreMarkerRenderer, ref _rightPreMarker, _rightHandSkeleton);
                //_rightPreMarkerRenderer.enabled = true;
                //_rightPreMarker.position = _rightHandSkeleton.Bones[(int)BoneId.Hand_ThumbTip].Transform.position;
                //_rightPreMarker.rotation = Quaternion.LookRotation(RightMarker.Instance.palmNormal);
            }
            else _rightPreMarkerRenderer.enabled = false;

            if (AtomicModeController.Instance.currentLeftMode == TransformMode.Translate && !leftStickActive)
            {
                UpdatePreMarkersHelper(false, ref _leftPreMarkerRenderer, ref _leftPreMarker, _leftHandSkeleton);
                //_leftPreMarkerRenderer.enabled = true;
                //_leftPreMarker.position = _leftHandSkeleton.Bones[(int)BoneId.Hand_ThumbTip].Transform.position;
                //_leftPreMarker.rotation = Quaternion.LookRotation(LeftMarker.Instance.palmNormal);
            }
            else _leftPreMarkerRenderer.enabled = false;
        }

        /// <summary>
        /// Refactors code duplicated for each hand in UpdatePreMarkers method
        /// </summary>
        private void UpdatePreMarkersHelper(bool right, ref MeshRenderer renderer, ref Transform preMarker, OVRSkeleton skeleton)
        {
            renderer.enabled = true;
            preMarker.position = skeleton.Bones[(int)BoneId.Hand_ThumbTip].Transform.position;
            preMarker.rotation = Quaternion.LookRotation((right) ? RightMarker.Instance.palmNormal : LeftMarker.Instance.palmNormal);
        }

        void UpdateRight()
        {
            if (rightStickActive)
            {
                HandUpdateHelper(Hand.Right, ref rightStickVelocity, rightCurrentPositionMarker, rightStartingPoint);
                //if (!IsModeActive(Hand.Right, TransformMode.Translate))
                //    return;

                //rightStickVelocity = rightCurrentPositionMarker.position - rightStartingPoint.position;
                //rightStickVelocity = rightStickVelocity * (1 - _stickDeadZone);

                //UpdateCurrentMarkerPosition(true);
                //UpdateLineRenderer(true);
            }
        }

        void UpdateLeft()
        {
            if (leftStickActive)
            {
                HandUpdateHelper(Hand.Left, ref leftStickVelocity, leftCurrentPositionMarker, leftStartingPoint);
                //if (!IsModeActive(Hand.Left, TransformMode.Translate))
                //    return;

                //leftStickVelocity = leftCurrentPositionMarker.position - leftStartingPoint.position;
                //leftStickVelocity = leftStickVelocity * (1 - _stickDeadZone);

                //UpdateCurrentMarkerPosition(false);
                //UpdateLineRenderer(false);
            }
        }

        private void HandUpdateHelper(Hand hand,ref Vector3 velocity, Transform currentPositionMarker, Transform startingPoint)
        {
            if (!IsModeActive(hand, TransformMode.Translate))
                return;

            velocity = currentPositionMarker.position - startingPoint.position;
            velocity = velocity * (1 - _stickDeadZone);

            UpdateCurrentMarkerPosition(hand == Hand.Right);
            UpdateLineRenderer(hand == Hand.Right);
        }
        #endregion loops

        #region event handlers
        private void OnRightStateChanged(HandGestureState current, HandGestureState previous)
        {
            if (IsModeActive(Hand.Right, TransformMode.Translate))
                ToggleTool(current, previous, true);
        }

        private void OnLeftStateChanged(HandGestureState current, HandGestureState previous)
        {
            if (IsModeActive(Hand.Left, TransformMode.Translate))
                ToggleTool(current, previous, false);
        }

        private void OnRightModeChanged(TransformMode newMode, TransformMode previousMode)
        {
            if (newMode != TransformMode.Translate)
                DisengageTranslationTool(true);
        }

        private void OnLeftModeChanged(TransformMode newMode, TransformMode previousMode)
        {
            if (newMode != TransformMode.Translate)
                DisengageTranslationTool(false);
        }
        #endregion event handlers

        #region helper methods
        private void ToggleTool(HandGestureState currentState, HandGestureState previousState, bool right)
        {
            if (currentState == HandGestureState.DragSelection)
            {
                if (right)
                {
                    DragToggleHelper(true, ref rightCurrentPositionMarker, ref rightStartingPoint, _rightHandSkeleton, ref rightStickActive);
                    //rightCurrentPositionMarker.position = rightStartingPoint.position = _rightHandSkeleton.Bones[(int)BoneId.Hand_ThumbTip].Transform.position;
                    //rightCurrentPositionMarker.rotation = rightStartingPoint.rotation = _rightControllerMarker.transform.rotation;
                    //rightStickActive = true;
                    //AtomicSelection.Instance.BeginTransformation(true);
                }
                else
                {
                    DragToggleHelper(true, ref leftCurrentPositionMarker, ref leftStartingPoint, _leftHandSkeleton, ref leftStickActive);
                    //leftCurrentPositionMarker.position = leftStartingPoint.position = _leftHandSkeleton.Bones[(int)BoneId.Hand_ThumbTip].Transform.position;
                    //leftCurrentPositionMarker.rotation = leftStartingPoint.rotation = _leftControllerMarker.transform.rotation;
                    //leftStickActive = true;
                    //AtomicSelection.Instance.BeginTransformation(false);
                }
                ToggleTranslateToolVisibility(true, right);
                ToggleTranslateToolColliders(true, right);
                BeginTranslationDrag?.Invoke((right) ? Hand.Right : Hand.Left);
                ADM.QLog("begin drag");
            }
            else if (previousState == HandGestureState.DragSelection && currentState != HandGestureState.DragSelection)
                DisengageTranslationTool(right);
        }

        /// <summary>
        /// Refactors drag selection toggling code duplicated for each hand in ToggleTool method
        /// </summary>
        private void DragToggleHelper(bool right, ref Transform currentMarkerPosition, ref Transform startingPoint, OVRSkeleton skeleton, ref bool stickActive)
        {
            currentMarkerPosition.position = startingPoint.position = skeleton.Bones[(int)BoneId.Hand_ThumbTip].Transform.position;
            currentMarkerPosition.rotation = startingPoint.rotation = (right) ? _rightControllerMarker.transform.rotation : _leftControllerMarker.transform.rotation;
            stickActive = true;
            AtomicSelection.Instance.BeginTransformation(right);
        }

        private void ToggleTranslateToolVisibility(bool on, bool right)
        {
            if (right)
            {
                ToggleVisibilityHelper(on, true, ref _rightCurrentPositionRenderer, ref _rightStartingPointRenderer, ref rightCurrentPositionMarker, rightStartingPoint);
                //_rightCurrentPositionRenderer.enabled = on;
                //foreach (MeshRenderer renderer in _rightStartingPointRenderer)
                //    renderer.enabled = on;
                //if (!on)
                //    rightCurrentPositionMarker.position = rightStartingPoint.position;
            }
            else
            {
                ToggleVisibilityHelper(on, false, ref _leftCurrentPositionRenderer, ref _leftStartingPointRenderer, ref leftCurrentPositionMarker, leftStartingPoint);
                //_leftCurrentPositionRenderer.enabled = on;
                //foreach (MeshRenderer renderer in _leftStartingPointRenderer)
                //    renderer.enabled = on;
                //if (!on)
                //    leftCurrentPositionMarker.position = leftStartingPoint.position;
            }
        }

        /// <summary>
        /// Refactors code duplicated for each hand in ToggleTranslateToolVisibility method
        /// </summary>
        private void ToggleVisibilityHelper(bool on, bool right, ref MeshRenderer currentPosRenderer, ref MeshRenderer[] startingPointRenderer, ref Transform currentPositionMarker, Transform startingPoint)
        {
            currentPosRenderer.enabled = on;
            foreach (MeshRenderer renderer in startingPointRenderer)
                renderer.enabled = on;
            if (!on)
                currentPositionMarker.position = startingPoint.position;
        }

        private void ToggleTranslateToolColliders(bool on, bool right)
        {
            if (right)
            {
                ToggleColliderHelper(on, true, ref _rightColliders);
                //if (_rightColliders.Length > 0)
                //    foreach (Collider collider in _rightColliders)
                //        collider.enabled = on;
            }
            else
            {
                ToggleColliderHelper(on, false, ref _leftColliders);
                //if (_leftColliders.Length > 0)
                //    foreach (Collider collider in _leftColliders)
                //        collider.enabled = on;
            }
        }

        /// <summary>
        /// Refactors code duplicated for each hand in ToggleTranslateToolColliders method
        /// </summary>
        private void ToggleColliderHelper(bool on, bool right, ref Collider[] colliders)
        {
            if (colliders.Length > 0)
                foreach (Collider collider in colliders)
                    collider.enabled = on;
        }

        private void UpdateCurrentMarkerPosition(bool right)
        {
            if (right)
            {
                UpdateMarkerHelper(true,_rightHandSkeleton, ref _rightMarkerDistanceMagnitude,rightStartingPoint,ref _rightTravelPercentage, ref rightCurrentPositionMarker);
                //Vector3 handTarget = _rightHandSkeleton.Bones[(int)BoneId.Hand_ThumbTip].Transform.position;
                //_rightMarkerDistanceMagnitude = Mathf.Abs((handTarget - rightStartingPoint.position).magnitude);
                //_rightTravelPercentage = Mathf.Pow(Mathf.Clamp01(_rightMarkerDistanceMagnitude * 100), 10);
                //rightCurrentPositionMarker.position = Vector3.Lerp(rightStartingPoint.position, handTarget, _rightTravelPercentage);
                //rightCurrentPositionMarker.rotation = Quaternion.Lerp(rightStartingPoint.rotation, _rightControllerMarker.transform.rotation, _rightTravelPercentage);
            }
            else
            {
                UpdateMarkerHelper(false, _leftHandSkeleton, ref _leftMarkerDistanceMagnitude, leftStartingPoint, ref _leftTravelPercentage, ref leftCurrentPositionMarker);
                //Vector3 handTarget = _leftHandSkeleton.Bones[(int)BoneId.Hand_ThumbTip].Transform.position;
                //_leftMarkerDistanceMagnitude = Mathf.Abs((handTarget - leftStartingPoint.position).magnitude);
                //_leftTravelPercentage = Mathf.Pow(Mathf.Clamp01(_leftMarkerDistanceMagnitude * 100), 10);
                //leftCurrentPositionMarker.position = Vector3.Lerp(leftStartingPoint.position, handTarget, _leftTravelPercentage);
                //leftCurrentPositionMarker.rotation = Quaternion.Lerp(leftStartingPoint.rotation, _leftControllerMarker.transform.rotation, _leftTravelPercentage);
            }
        }

        /// <summary>
        /// Refactor code duplicated for each hand in UpdateCurrentMarkerPosition method
        /// </summary>
        private void UpdateMarkerHelper(bool right, OVRSkeleton skeleton, ref float markerDistanceMagnitude, Transform startingPoint, ref float travelPercentage, ref Transform currentPositionMarker)
        {
            Vector3 handTarget = skeleton.Bones[(int)BoneId.Hand_ThumbTip].Transform.position;
            markerDistanceMagnitude = Mathf.Abs((handTarget - startingPoint.position).magnitude);
            travelPercentage = Mathf.Pow(Mathf.Clamp01(markerDistanceMagnitude * 100), 10);
            currentPositionMarker.position = Vector3.Lerp(startingPoint.position, handTarget, travelPercentage);
            currentPositionMarker.rotation = Quaternion.Lerp(
                                                                startingPoint.rotation, 
                                                                (right) ? _rightControllerMarker.transform.rotation : _leftControllerMarker.transform.rotation,
                                                                travelPercentage
                                                            );
        }

        private void UpdateLineRenderer(bool right)
        {
            if (right)
            {
                UpdateLineRendererHelper(true, rightCurrentPositionMarker, rightStartingPoint, _rightMarkerDistanceMagnitude, ref rightLineRenderer);
                //// line rendering prep
                //Vector3 directionToCurrent = (rightCurrentPositionMarker.position - rightStartingPoint.position).normalized;
                //Vector3 startLinePoint = rightStartingPoint.position + directionToCurrent * _startPadding;
                //Vector3 currentPosLinePoint = rightCurrentPositionMarker.position - directionToCurrent * _endPadding;

                //// render the line
                //if (_rightMarkerDistanceMagnitude > _startPadding + _endPadding)
                //{
                //    Vector3 handTarget = _rightControllerMarker.transform.position + _hand2HUDOffset;
                //    Vector3 viewVector = AtomicModeController.Instance.rightCam.transform.position - handTarget;
                //    Vector3 sideArrowPoint1 = handTarget - directionToCurrent * _arrowheadHeight + Vector3.Cross(-1 * directionToCurrent, viewVector).normalized * _arrowheadWidth / 2;
                //    Vector3 sideArrowPoint2 = handTarget - directionToCurrent * _arrowheadHeight + Vector3.Cross(directionToCurrent, viewVector).normalized * _arrowheadWidth / 2;
                //    rightLineRenderer.SetPositions(new Vector3[] { startLinePoint ,
                //                                                currentPosLinePoint ,
                //                                                sideArrowPoint1,
                //                                                sideArrowPoint2,
                //                                                currentPosLinePoint });
                //}
                //else rightLineRenderer.SetPositions(new Vector3[] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero });
            }
            else
            {
                UpdateLineRendererHelper(false, leftCurrentPositionMarker, leftStartingPoint, _leftMarkerDistanceMagnitude, ref leftLineRenderer);
                //// line rendering prep
                //Vector3 directionToCurrent = (leftCurrentPositionMarker.position - leftStartingPoint.position).normalized;
                //Vector3 startLinePoint = leftStartingPoint.position + directionToCurrent * _startPadding;
                //Vector3 currentPosLinePoint = leftCurrentPositionMarker.position - directionToCurrent * _endPadding;

                //// render the line
                //if (_leftMarkerDistanceMagnitude > _startPadding + _endPadding)
                //{
                //    Vector3 handTarget = _leftControllerMarker.transform.position + _hand2HUDOffset;
                //    Vector3 viewVector = AtomicModeController.Instance.rightCam.transform.position - handTarget;
                //    Vector3 sideArrowPoint1 = handTarget - directionToCurrent * _arrowheadHeight + Vector3.Cross(-1 * directionToCurrent, viewVector).normalized * _arrowheadWidth / 2;
                //    Vector3 sideArrowPoint2 = handTarget - directionToCurrent * _arrowheadHeight + Vector3.Cross(directionToCurrent, viewVector).normalized * _arrowheadWidth / 2;

                //    leftLineRenderer.SetPositions(new Vector3[] { startLinePoint, currentPosLinePoint, sideArrowPoint1, sideArrowPoint2, currentPosLinePoint });
                //}
                //else leftLineRenderer.SetPositions(new Vector3[] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero });
            }
        }

        /// <summary>
        /// Refactors code duplicated for each hand in UpdateLineRenderer method
        /// </summary>
        private void UpdateLineRendererHelper(bool right, Transform currentPositionMarker, Transform startingPoint, float markerDistanceMagnitude, ref LineRenderer lineRenderer)
        {
            // line rendering prep
            Vector3 directionToCurrent = (currentPositionMarker.position - startingPoint.position).normalized;
            Vector3 startLinePoint = startingPoint.position + directionToCurrent * _startPadding;
            Vector3 currentPosLinePoint = currentPositionMarker.position - directionToCurrent * _endPadding;

            // render the line
            if (markerDistanceMagnitude > _startPadding + _endPadding)
            {
                Vector3 handTarget = ((right) ? _rightControllerMarker.transform.position : _leftControllerMarker.transform.position) + _hand2HUDOffset;
                Vector3 viewVector = AtomicModeController.Instance.rightCam.transform.position - handTarget;
                Vector3 sideArrowPoint1 = handTarget - directionToCurrent * _arrowheadHeight + Vector3.Cross(-1 * directionToCurrent, viewVector).normalized * _arrowheadWidth / 2;
                Vector3 sideArrowPoint2 = handTarget - directionToCurrent * _arrowheadHeight + Vector3.Cross(directionToCurrent, viewVector).normalized * _arrowheadWidth / 2;
                lineRenderer.SetPositions(new Vector3[] { startLinePoint ,
                                                                currentPosLinePoint ,
                                                                sideArrowPoint1,
                                                                sideArrowPoint2,
                                                                currentPosLinePoint });
            }
            else lineRenderer.SetPositions(new Vector3[] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero });
        }

        private bool IsModeActive(Hand hand, TransformMode mode)
        {
            return (hand == Hand.Right) 
                ? (AtomicModeController.Instance.currentRightMode == mode) 
                : (AtomicModeController.Instance.currentRightMode == mode);
        }

        /// <summary>
        /// If we're in Translate mode, determine whether any objects are selected
        /// </summary>
        private bool IsThereACurrentSelection(Hand hand)
        {
            return
                IsModeActive(hand,TransformMode.Translate) && AtomicSelection.Instance.selectedObjects.Count == 0;
        }

        private void DisengageTranslationTool(bool right)
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
            ADM.QLog("end drag");
        }
        #endregion helper methods
    }
}

