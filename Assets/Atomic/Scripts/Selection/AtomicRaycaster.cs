using Atomic.Input;
using Atomic.Transformation;
using cakeslice;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Atomic
{
    public class AtomicRaycaster : Singleton<AtomicRaycaster>
    {
        #region public vars
        public UnityAction<bool, GameObject> RightHoverChanged, LeftHoverChanged;
        [HideInInspector] public bool rightSelecting, leftSelecting;
        #endregion public vars

        #region private vars
        private LineRenderer _rightRaycastViz, _leftRaycastViz;
        private RaycastHit _rightHit, _leftHit;
        private Transform _rightHand, _leftHand;
        private OVRHand _rightHandPoser, _leftHandPoser;
        private GameObject _currentRightHover, _currentLeftHover;
        private bool _isRightHovering, _isLeftHovering;
        private AtomicInput _input;
        private bool _rightRaycastActive, _leftRaycastActive;
        /// <summary>
        /// True if either right or left raycast is active
        /// </summary>
        private bool _isActive;
        private const float _quickReleaseThreshold = 0.5f;
        //private float _releaseListenerStartTime = 0;


        /// <summary>
        /// Resets to false each time user release raycast. During raycast, this becomes true if any object receives hover.
        /// Used for quick release to determine whether to clear selection.
        /// </summary>
        private bool stateChanged;

        
        [SerializeField] private Material selectionMat, deselectionMat;

        [SerializeField] private Text _rightDebugCanvas, _leftDebugCanvas;
        [SerializeField] private bool _debuggingEnabled;
        
        #endregion private vars

        #region init
        private void Awake()
        {
            _rightRaycastViz = transform.GetChild(0).GetComponent<LineRenderer>();
            _leftRaycastViz = transform.GetChild(1).GetComponent<LineRenderer>();
            _rightHand = RightMarker.Instance.transform;
            _leftHand = LeftMarker.Instance.transform;
            _rightHandPoser = _rightHand.GetComponentInChildren<OVRHand>();
            _leftHandPoser = _leftHand.GetComponentInChildren<OVRHand>();

            _input = AtomicInput.Instance;
        }

        private void OnEnable()
        {
            _input.OnRightStateChanged += OnRightStateChanged;
            _input.OnLeftStateChanged += OnLeftStateChanged;
        }

        private void OnDisable()
        {
            if (_input != null)
            {
                _input.OnRightStateChanged -= OnRightStateChanged;
                _input.OnLeftStateChanged -= OnLeftStateChanged;
            }
        }
        #endregion init

        #region loops and timers
        private void Update()
        {
            //_rightDebugCanvas.text = "_rightRaycastActive: " + _rightRaycastActive + " rightIsSelecting: " + rightSelecting;
            //if (_currentRightHover != null)
            //    _rightDebugCanvas.text += " " + _currentRightHover.name;

            if (_rightRaycastActive && AtomicModeController.Instance.currentRightMode != TransformMode.Text)
                HandleRaycast(ref _rightHandPoser, ref _rightHit, ref _rightHand, ref _rightRaycastViz, ref _isRightHovering, ref _currentRightHover, ref RightHoverChanged);
            else
            {
                if (_rightRaycastViz.GetPosition(0) != Vector3.zero)
                    _rightRaycastViz.SetPositions(new Vector3[] { Vector3.zero, Vector3.zero });

                if (_isRightHovering)
                {
                    _isRightHovering = false;
                    RightHoverChanged?.Invoke(false, _currentRightHover);
                    _currentRightHover = null;
                }

                if (_isActive && !_leftRaycastActive)
                {
                    _isActive = false;
                    //CheckForRelease();
                    stateChanged = false;
                    rightSelecting = false;
                    leftSelecting = false;
                }
            }

            if (_leftRaycastActive)
                HandleRaycast(ref _leftHandPoser, ref _leftHit, ref _leftHand, ref _leftRaycastViz, ref _isLeftHovering, ref _currentLeftHover, ref LeftHoverChanged);
            else
            {
                if (_leftRaycastViz.GetPosition(0) != Vector3.zero)
                    _leftRaycastViz.SetPositions(new Vector3[] { Vector3.zero, Vector3.zero });

                if (_isLeftHovering)
                {
                    _isLeftHovering = false;
                    LeftHoverChanged?.Invoke(false, _currentLeftHover);
                    _currentLeftHover = null;
                }

                if (_isActive && !_rightRaycastActive)
                {
                    _isActive = false;
                    //CheckForRelease();
                    stateChanged = false;
                    rightSelecting = false;
                    leftSelecting = false;
                }
            }
        }

        
        #endregion loops and timers

        #region event handlers
        private void OnRightStateChanged(HandGestureState current, HandGestureState previous)
        {
            _rightRaycastActive = (current == HandGestureState.Selection || current == HandGestureState.Deselection);

            if (current == HandGestureState.Selection)
            {
                _rightRaycastViz.material = selectionMat;
                rightSelecting = true;
            }

            if (current == HandGestureState.Deselection)
            {
                _rightRaycastViz.material = deselectionMat;
                rightSelecting = false;
            }

            if (_rightRaycastActive)
            {
                //_releaseListenerStartTime = Time.time;
                _isActive = true;
                if (_debuggingEnabled)
                    _rightDebugCanvas.text = "Right raycast is active";
            }
        }

        private void OnLeftStateChanged(HandGestureState current, HandGestureState previous)
        {
            _leftRaycastActive = (current == HandGestureState.Selection || current == HandGestureState.Deselection);

            if (current == HandGestureState.Selection)
            {
                _leftRaycastViz.material = selectionMat;
                leftSelecting = true;
            }

            if (current == HandGestureState.Deselection)
            {
                _leftRaycastViz.material = deselectionMat;
                leftSelecting = false;
            }

            if (_leftRaycastActive)
            {
                //_releaseListenerStartTime = Time.time;
                _isActive = true;
                if (_debuggingEnabled)
                    _leftDebugCanvas.text = "Left raycast is active";
            }
                
        }
        #endregion event handlers

        #region helper methods
        void HandleRaycast(ref OVRHand handPoser, ref RaycastHit hit, ref Transform hand, ref LineRenderer vizualizer, ref bool isHovering, ref GameObject hitObject, ref UnityAction<bool, GameObject> HoverChanged)
        {

            if (Physics.Raycast(handPoser.PointerPose.position, handPoser.PointerPose.forward, out hit, 100))
            {
                vizualizer.SetPositions(new Vector3[] {
                        hand.position,
                        hit.point
                    });

                if (hit.transform.GetComponentInChildren<cakeslice.Outline>() != null)
                {    
                    if (hitObject != hit.transform.gameObject)
                    {
                        isHovering = true;
                        stateChanged = true;
                        hitObject = hit.transform.gameObject;
                        HoverChanged?.Invoke(true,hitObject);
                    }
                }
            }
            else
            {
                vizualizer.SetPositions(new Vector3[] {
                        handPoser.PointerPose.position + handPoser.PointerPose.forward * 0.2f,
                        handPoser.PointerPose.position + handPoser.PointerPose.forward * 100
                    });


                if (hitObject != null)
                {
                    isHovering = false;
                    
                    if (!_isLeftHovering && !_isRightHovering)
                    {
                        //CheckForRelease();
                        stateChanged = false;
                    }

                    HoverChanged?.Invoke(false, hitObject);
                    hitObject = null;
                }
            }
        }

        //private void CheckForRelease()
        //{
        //    if (Time.time - _releaseListenerStartTime < _quickReleaseThreshold && Time.time - _releaseListenerStartTime > 0.1f && !stateChanged)
        //        AtomicSelection.Instance.ClearAll();
        //}
        #endregion helper methods
    }
}
