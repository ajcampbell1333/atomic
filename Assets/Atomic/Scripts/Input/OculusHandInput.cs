using Atomic.Transformation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Atomic.Input.InputTouchStates;
using static OVRSkeleton;

namespace Atomic.Input
{
    public class OculusHandInput : Singleton<OculusHandInput>
    {
        #region debug vars
        [SerializeField] public Text rightDebugOutput, leftDebugOutput;
        private int debugIterator;
        [SerializeField]
        private bool _debuggingEnabled;
        #endregion debug vars

        #region public vars
        [SerializeField] public Transform headTransform;
        [SerializeField] public float holdActionThreshold;
        [HideInInspector] public Action<HandGestureState, HandGestureState> LeftStateChanged, RightStateChanged;
        [HideInInspector] public Action<RotationState, RotationState> LeftRotationStateChanged, RightRotationStateChanged;
        [HideInInspector] public Action<bool, TouchStateIndex> HoldAction;
        [HideInInspector] public TouchStates rightHandState, leftHandState;
        [SerializeField] public HandGestureState currentRightHandGestureState, currentLeftHandGestureState;
        [HideInInspector] public RotationState currentRightRotationState, currentLeftRotationState;
        public bool[] rightHeldLong, leftHeldLong;
        public bool modeActive;
        [HideInInspector] public Action<bool> ModeActiveStateChanged;
        #endregion public vars

        #region private vars
        private GameObject _selectionRightStartingMarker, _selectionLeftStartingMarker;
        [SerializeField] private OVRHand _rightHandState, _leftHandState;
        [SerializeField] private OVRSkeleton _rightHandSkeleton, _leftHandSkeleton;
        private HandGestureState previousRightState;
        private HandGestureState previousLeftState;
        private bool _rightReady, _leftReady;
        private bool _rightDragSelectToggle, _leftDragSelectToggle;
        //private Vector3 _camForwardHorizontal;
        #endregion private vars

        #region init
        private void Awake()
        {
            rightHandState = new TouchStates();
            leftHandState = new TouchStates();
            currentRightHandGestureState = HandGestureState.Neutral;
            currentLeftHandGestureState = HandGestureState.Neutral;
            currentRightRotationState = RotationState.lowerCase;
            currentLeftRotationState = RotationState.lowerCase;

            // track whether a currently held finger has passed the hold action threshold
            // index values match the order of the TouchStates enum
            rightHeldLong = new bool[3] { false, false, false };
            leftHeldLong = new bool[3] { false, false, false };

            _selectionRightStartingMarker = new GameObject("RightStartMarker");
            _selectionLeftStartingMarker = new GameObject("LeftStartMarker");
        }

        private void OnEnable()
        {
            AtomicModeController.Instance.RightModeChanged += OnRightModeChanged;
            AtomicModeController.Instance.LeftModeChanged += OnLeftModeChanged;
        }

        private void OnDisable()
        {
            if (AtomicModeController.Instance != null)
            {
                AtomicModeController.Instance.RightModeChanged -= OnRightModeChanged;
                AtomicModeController.Instance.LeftModeChanged -= OnLeftModeChanged;
            }
        }
        #endregion init

        #region loops and timers
        private void Update()
        {
            //// right
            //CheckForUpdates(true, ref currentRightHandGestureState, ref previousRightState, ref _rightHandState, ref _rightHandSkeleton);
            previousRightState = currentRightHandGestureState;
            if (!_rightHandState.IsTracked)
                UpdateHandInput(true, "Right not found", HandGestureState.Neutral);
            else if (IsFistClosedThumbOut(_rightHandSkeleton))
                UpdateHandInput(true, "Right fist closed thumb out", HandGestureState.Stop);
            else if (IsFistClosed(_rightHandSkeleton))
                UpdateHandInput(true, "Right fist closed", HandGestureState.SqueezeAll);
            else if (_rightHandState.GetFingerIsPinching(OVRHand.HandFinger.Index))
                UpdateHandInput(true, "Right Index Finger is pinching", HandGestureState.Insert);
            else if (_rightHandState.GetFingerIsPinching(OVRHand.HandFinger.Middle))
                UpdateHandInput(true, "Right Middle Finger is pinching", HandGestureState.DragSelection);
            else if (IsPointingUpward(_rightHandSkeleton, true))
                UpdateHandInput(true, "Right pointing upward", HandGestureState.DeselectAll);
            else if (IsPointingWithPalmDown(_rightHandSkeleton, true))
                UpdateHandInput(true, "Right hand pointing", HandGestureState.Deselection);
            else if (IsPalmFlatDownThumbOut(_rightHandSkeleton, true))
                UpdateHandInput(true, "Right palm is flat and thumb is out", HandGestureState.Selection);
            else UpdateHandInput(true, "Right neutral", HandGestureState.Neutral);

            //// left
            //CheckForUpdates(false, ref currentLeftHandGestureState, ref previousLeftState, ref _leftHandState, ref _leftHandSkeleton);

            previousLeftState = currentLeftHandGestureState;
            if (!_leftHandState.IsTracked)
                UpdateHandInput(false, "Left not found", HandGestureState.Neutral);
            else if (IsFistClosedThumbOut(_leftHandSkeleton))
            {
                UpdateHandInput(false, "Left fist closed thumb out", HandGestureState.Stop);
                string output = "Left fist closed thumb out ";
                ILog(output + (_leftHandSkeleton.Bones[(int)BoneId.Hand_ThumbTip].Transform.position - _leftHandSkeleton.Bones[(int)BoneId.Hand_Index2].Transform.position).magnitude.ToString(), false);
            }
            else if (IsFistClosed(_leftHandSkeleton))
                UpdateHandInput(false, "Left fist closed", HandGestureState.SqueezeAll);
            else if (_leftHandState.GetFingerIsPinching(OVRHand.HandFinger.Index))
                UpdateHandInput(false, "Left Index Finger is pinching", HandGestureState.Insert);
            else if (_leftHandState.GetFingerIsPinching(OVRHand.HandFinger.Middle))
                UpdateHandInput(false, "Left Middle Finger is pinching", HandGestureState.DragSelection);
            else if (IsPointingUpward(_leftHandSkeleton, false))
                UpdateHandInput(false, "Left pointing upward", HandGestureState.DeselectAll);
            else if (IsPointingWithPalmDown(_leftHandSkeleton, false))
                UpdateHandInput(false, "Left hand pointing", HandGestureState.Deselection);
            else if (IsPalmFlatDownThumbOut(_leftHandSkeleton, false))
            {
                UpdateHandInput(false, "Left palm is flat and thumb is out", HandGestureState.Selection);
                string output = "Left palm is flat and thumb is out ";
                ILog(output +
                    (_leftHandSkeleton.Bones[(int)BoneId.Hand_IndexTip].Transform.position - _leftHandSkeleton.Bones[(int)BoneId.Hand_WristRoot].Transform.position).magnitude.ToString() + " " +
                (_leftHandSkeleton.Bones[(int)BoneId.Hand_MiddleTip].Transform.position - _leftHandSkeleton.Bones[(int)BoneId.Hand_WristRoot].Transform.position).magnitude.ToString() + " " +
                (_leftHandSkeleton.Bones[(int)BoneId.Hand_RingTip].Transform.position - _leftHandSkeleton.Bones[(int)BoneId.Hand_WristRoot].Transform.position).magnitude.ToString() + " " +
                (_leftHandSkeleton.Bones[(int)BoneId.Hand_PinkyTip].Transform.position - _leftHandSkeleton.Bones[(int)BoneId.Hand_WristRoot].Transform.position).magnitude.ToString() + " " +
                (_leftHandSkeleton.Bones[(int)BoneId.Hand_ThumbTip].Transform.position - _leftHandSkeleton.Bones[(int)BoneId.Hand_Index2].Transform.position).magnitude.ToString(), false);
            }
            else UpdateHandInput(false, "Left neutral", HandGestureState.Neutral);

            UpdateRotationState(true);
            UpdateRotationState(false);
        }
        #endregion loops and timers

        #region event handlers
        private void UpdateHandInput(bool right, string logText, HandGestureState newState)
        {
            //string gestureStateOutput = "current: " + newState.ToString() + " previous: " +
            //    ((right) ? previousRightState.ToString() : previousLeftState.ToString()) + " rightdragselecttoggle: " + _leftDragSelectToggle + " ";
            //ILog(gestureStateOutput + logText, right);


            if (right)
            {
                //Debug.Log("_rightDragSelectToggle: " + _rightDragSelectToggle +
                //    " current: " + currentRightHandGestureState + 
                //    " new state: " + newState + 
                //    " previous state: " + previousRightState);
                //UpdateHandInputHelper(true, ref newState, ref _rightDragSelectToggle, ref currentRightHandGestureState, ref previousRightState);
                if (newState == HandGestureState.DragSelection &&
                    (previousRightState != HandGestureState.DragSelection && previousRightState != HandGestureState.Stop))
                {
                    if (_rightDragSelectToggle && !CheckForTextDragLock(true))
                    {
                        currentRightHandGestureState = newState = HandGestureState.Stop;
                        previousRightState = HandGestureState.DragSelection;
                    }
                    _rightDragSelectToggle = !_rightDragSelectToggle;
                    modeActive = _rightDragSelectToggle;
                    ModeActiveStateChanged?.Invoke(modeActive);
                    ActivateNewState(newState, true);
                }
                else if (newState == HandGestureState.Stop)
                {
                    _rightDragSelectToggle = false;
                    ActivateNewState(newState, true);
                }
                else if (_rightDragSelectToggle)
                {
                    if (newState == HandGestureState.Insert || AtomicModeController.Instance.currentRightMode == TransformMode.Scale)
                        ActivateNewState(newState, true);
                    else currentRightHandGestureState = newState;
                }
                else ActivateNewState(newState, true);
            }
            else
            {
                //UpdateHandInputHelper(false, ref newState, ref _leftDragSelectToggle, ref currentLeftHandGestureState, ref previousLeftState);
                if (newState == HandGestureState.DragSelection &&
                    (previousLeftState != HandGestureState.DragSelection && previousLeftState != HandGestureState.Stop))
                //(previousLeftState == HandGestureState.Selection || previousLeftState == HandGestureState.Neutral)) // made no dif
                {
                    if (_leftDragSelectToggle && !CheckForTextDragLock(false))
                    {
                        currentLeftHandGestureState = newState = HandGestureState.Stop;
                        previousLeftState = HandGestureState.DragSelection;
                    }
                    _leftDragSelectToggle = !_leftDragSelectToggle;
                    modeActive = _leftDragSelectToggle;
                    ModeActiveStateChanged?.Invoke(modeActive);
                    ActivateNewState(newState, false);
                }
                else if (newState == HandGestureState.Stop)
                {
                    _leftDragSelectToggle = false;
                    ActivateNewState(newState, false);
                }
                else if (_leftDragSelectToggle)
                {
                    if (newState == HandGestureState.Insert || AtomicModeController.Instance.currentLeftMode == TransformMode.Scale)
                        ActivateNewState(newState, false); // scale mode current: neutral prev: drag doesn't change to current: neutral prev: neutral - why not?
                    else currentLeftHandGestureState = newState;
                }
                else ActivateNewState(newState, false);
            }
        }

        void UpdateRotationState(bool right)
        {
            RotationState previousRotationState = (right) ? currentRightRotationState : currentLeftRotationState;
            if (right)
                UpdateRotationStateHelper(true, ref _rightHandState, ref currentRightRotationState, previousRotationState);
            else UpdateRotationStateHelper(false, ref _leftHandState, ref currentLeftRotationState, previousRotationState);
        }

        private IEnumerator ResetLeftForModeChange()
        {
            yield return new WaitForEndOfFrame();
            _leftDragSelectToggle = false;
            if (modeActive)
                ModeActiveStateChanged?.Invoke(false);
            modeActive = false;
            ActivateNewState(HandGestureState.Stop, false);
        }

        private void OnLeftModeChanged(TransformMode current, TransformMode previous)
        {
            StartCoroutine(ResetLeftForModeChange());
        }

        private IEnumerator ResetRightForModeChange()
        {
            yield return new WaitForEndOfFrame();
            _rightDragSelectToggle = false;
            if (modeActive)
                ModeActiveStateChanged?.Invoke(false);
            modeActive = false;            
            ActivateNewState(HandGestureState.Stop, true);
        }

        private void OnRightModeChanged(TransformMode current, TransformMode previous)
        {
            StartCoroutine(ResetRightForModeChange());
        }
        #endregion event handlers

        #region helper methods
        /// <summary>
        /// Refactors code duplicated for each hand in the UpdatHandInput Method
        /// </summary>
        private void UpdateHandInputHelper(bool right, ref HandGestureState newState, ref bool dragSelectToggle, ref HandGestureState current, ref HandGestureState previous)
        {
            if (newState == HandGestureState.DragSelection &&
                    (previous != HandGestureState.DragSelection && previous != HandGestureState.Stop))
            {
                if (dragSelectToggle && !CheckForTextDragLock(right))
                {
                    current = newState = HandGestureState.Stop;
                    previous = HandGestureState.DragSelection;
                }
                dragSelectToggle = !dragSelectToggle;
                modeActive = dragSelectToggle;
                ModeActiveStateChanged?.Invoke(modeActive);
                ActivateNewState(newState, right);
            }
            else if (newState == HandGestureState.Stop)
            {
                dragSelectToggle = false;
                ActivateNewState(newState, right);
            }
            else if (dragSelectToggle)
            {
                TransformMode currentMode = (right) ? AtomicModeController.Instance.currentRightMode : AtomicModeController.Instance.currentLeftMode;
                if (newState == HandGestureState.Insert || currentMode == TransformMode.Scale)
                    ActivateNewState(newState, right);
                else current = newState;
            }
            else ActivateNewState(newState, right);
        }

        /// <summary>
        /// Refactors code duplicated from each hand in the UpdateRotationState method
        /// </summary>
        private void UpdateRotationStateHelper(bool right, ref OVRHand hand, ref RotationState current, RotationState previous)
        {
            // calculate the lateral-toward-the-body horizontal vector for the current hand for comparison to the palm normal
            Vector3 inward = ((right)?-1:1) * Vector3.Cross(Vector3.up, hand.PointerPose.forward);
            Vector3 palmNormal = (right) ? RightMarker.Instance.palmNormal : LeftMarker.Instance.palmNormal;
            if (Vector3.Dot(inward, palmNormal) >= 0.75f && current != RotationState.lowerCase)
            {
                current = RotationState.lowerCase;
                BroadcastRotationStateChange(right, current, previous);
            }

            if (Vector3.Dot(Vector3.down, palmNormal) >= 0.75f && current != RotationState.upperCase)
            {
                current = RotationState.upperCase;
                BroadcastRotationStateChange(right, current, previous);
            }

            if (Vector3.Dot(Vector3.up, palmNormal) >= 0.75f && current != RotationState.specialCharacters)
            { 
                current = RotationState.specialCharacters;
                BroadcastRotationStateChange(right, current, previous);
            }

            //_camForwardHorizontal = AtomicHeadMarker.Instance.transform.position + AtomicHeadMarker.Instance.transform.forward;
            //_camForwardHorizontal = new Vector3(_camForwardHorizontal.x, AtomicHeadMarker.Instance.transform.position.y, _camForwardHorizontal.z);

            //Vector3 _indexFingerDirection = 

            if (IsPointingWholeHandUpward((right) ? _rightHandSkeleton : _leftHandSkeleton, right) && current != RotationState.numbers)
            //if (Vector3.Dot(palmNormal, _camForwardHorizontal) > 0.75 && current != RotationState.numbers)
            { 
                current = RotationState.numbers;
                BroadcastRotationStateChange(right, current, previous);
            }
        }

        private void BroadcastRotationStateChange(bool right, RotationState current, RotationState previous)
        {
            if (current == previous) return;
            if (right)
                RightRotationStateChanged?.Invoke(current, previous);
            else LeftRotationStateChanged?.Invoke(current, previous);
        }

        /// <summary>
        /// Refactors duplicate code for each hand in the update loop used to check
        /// whether a state change is needed this frame
        /// </summary>
        private void CheckForUpdates(bool right, ref HandGestureState current, ref HandGestureState previous, ref OVRHand hand, ref OVRSkeleton skeleton)
        {
            string handName = (right) ? "Right" : "Left";
            previous = current;
            if (!hand.IsTracked)
                UpdateHandInput(true, handName + " not found", HandGestureState.Neutral);
            else if (IsFistClosedThumbOut(skeleton))
                UpdateHandInput(true, handName + " fist closed thumb out", HandGestureState.Stop);
            else if (IsFistClosed(skeleton))
                UpdateHandInput(true, handName + " fist closed", HandGestureState.SqueezeAll);
            else if (hand.GetFingerIsPinching(OVRHand.HandFinger.Index))
                UpdateHandInput(true, handName + " Index Finger is pinching", HandGestureState.Insert);
            else if (hand.GetFingerIsPinching(OVRHand.HandFinger.Middle))
                UpdateHandInput(true, handName + " Middle Finger is pinching", HandGestureState.DragSelection);
            else if (IsPointingUpward(skeleton, true))
                UpdateHandInput(true, handName + " pointing upward", HandGestureState.DeselectAll);
            else if (IsPointingWithPalmDown(skeleton, true))
                UpdateHandInput(true, handName + " hand pointing", HandGestureState.Deselection);
            else if (IsPalmFlatDownThumbOut(skeleton, true))
                UpdateHandInput(true, handName + " palm is flat and thumb is out", HandGestureState.Selection);
            else UpdateHandInput(true, handName + " neutral", HandGestureState.Neutral);
        }

        private void ActivateNewState(HandGestureState newState, bool right)
        {
            if (right)
            {
                currentRightHandGestureState = newState;
                if (currentRightHandGestureState != previousRightState)
                {
                    RightStateChanged?.Invoke(currentRightHandGestureState, previousRightState);
                    Debug.Log("OculusHandInput: RightStateChange invoked - newState: " + newState);
                }

                    //StartCoroutine(ClearFalsePositives(true));
                    
            }
            else {
                currentLeftHandGestureState = newState;
                if (currentLeftHandGestureState != previousLeftState)
                    //StartCoroutine(ClearFalsePositives(false));
                    LeftStateChanged?.Invoke(currentLeftHandGestureState, previousLeftState);
            }
        }

        /// <summary>
        /// prevents this class from invoking a system-wide state change until
        /// the new state is confirmed to stick across a certain number of frames
        /// -- this stops single-frame hand-tracking noise (which is rampant on Quest)
        /// from causing strobe-flickering state changes
        /// </summary>
        private IEnumerator ClearFalsePositives(bool right)
        {
            int stickThreshold = 5;
            
            HandGestureState startingState = (right) ? currentRightHandGestureState : currentLeftHandGestureState;
            HandGestureState previousState = (right) ? previousRightState : previousLeftState;

            if (startingState == HandGestureState.DragSelection)
                stickThreshold *= 2;

            for (int i = 0; i < stickThreshold; i++)
            {
                yield return new WaitForEndOfFrame();
                if (startingState != ((right) ? currentRightHandGestureState : currentLeftHandGestureState))
                    yield break;
            }
            if (right) RightStateChanged?.Invoke(startingState, previousState);
            else LeftStateChanged?.Invoke(startingState, previousState);
        }

        private IEnumerator ResetRight()
        {
            yield return new WaitForSeconds(0.1f);
            _rightReady = true;
        }

        private IEnumerator ResetLeft()
        {
            yield return new WaitForSeconds(0.1f);
            _leftReady = true;
        }

        private bool IsFistClosed(OVRSkeleton hand)
        {
            float indexDistanceThreshold = 0.11f;
            float middleDistanceThreshold = 0.11f;
            float ringDistanceThreshold = 0.08f;
            float pinkyDistanceThreshold = 0.08f;
            float thumbDistanceThreshold = 0.05f;
            return
                (hand.Bones[(int)BoneId.Hand_IndexTip].Transform.position - hand.Bones[(int)BoneId.Hand_WristRoot].Transform.position).magnitude < indexDistanceThreshold &&
                (hand.Bones[(int)BoneId.Hand_MiddleTip].Transform.position - hand.Bones[(int)BoneId.Hand_WristRoot].Transform.position).magnitude < middleDistanceThreshold &&
                (hand.Bones[(int)BoneId.Hand_RingTip].Transform.position - hand.Bones[(int)BoneId.Hand_WristRoot].Transform.position).magnitude < ringDistanceThreshold &&
                (hand.Bones[(int)BoneId.Hand_PinkyTip].Transform.position - hand.Bones[(int)BoneId.Hand_WristRoot].Transform.position).magnitude < pinkyDistanceThreshold &&
                (hand.Bones[(int)BoneId.Hand_ThumbTip].Transform.position - hand.Bones[(int)BoneId.Hand_Index2].Transform.position).magnitude < thumbDistanceThreshold;
        }

        private bool IsFistClosedThumbOut(OVRSkeleton hand)
        {
            float indexDistanceThreshold = 0.11f;
            float middleDistanceThreshold = 0.11f;
            float ringDistanceThreshold = 0.08f;
            float pinkyDistanceThreshold = 0.08f;
            float thumbDistanceThreshold = 0.05f;
            return
                (hand.Bones[(int)BoneId.Hand_IndexTip].Transform.position - hand.Bones[(int)BoneId.Hand_WristRoot].Transform.position).magnitude < indexDistanceThreshold &&
                (hand.Bones[(int)BoneId.Hand_MiddleTip].Transform.position - hand.Bones[(int)BoneId.Hand_WristRoot].Transform.position).magnitude < middleDistanceThreshold &&
                (hand.Bones[(int)BoneId.Hand_RingTip].Transform.position - hand.Bones[(int)BoneId.Hand_WristRoot].Transform.position).magnitude < ringDistanceThreshold &&
                (hand.Bones[(int)BoneId.Hand_PinkyTip].Transform.position - hand.Bones[(int)BoneId.Hand_WristRoot].Transform.position).magnitude < pinkyDistanceThreshold &&
                (hand.Bones[(int)BoneId.Hand_ThumbTip].Transform.position - hand.Bones[(int)BoneId.Hand_Index2].Transform.position).magnitude > thumbDistanceThreshold;
        }

        private bool IsPointingWhileThumbIsUp(OVRSkeleton hand)
        {
            Vector3 thumbDirection = (hand.Bones[(int)BoneId.Hand_ThumbTip].Transform.position - hand.Bones[(int)BoneId.Hand_Thumb1].Transform.position).normalized;
            Vector3 indexDirection = (hand.Bones[(int)BoneId.Hand_IndexTip].Transform.position - hand.Bones[(int)BoneId.Hand_Index1].Transform.position).normalized;
            Vector3 middleDirection = (hand.Bones[(int)BoneId.Hand_MiddleTip].Transform.position - hand.Bones[(int)BoneId.Hand_Middle1].Transform.position).normalized;
            float thumbIndexComparison = Vector3.Dot(thumbDirection, indexDirection);
            float indexMiddleComparison = Vector3.Dot(indexDirection, middleDirection);
            return thumbIndexComparison > 0.1f &&
                thumbIndexComparison < 0.5f &&
                indexMiddleComparison < -0.3f &&
                Vector3.Dot(thumbDirection, Vector3.up) > 0.6f;
        }

        private bool IsPointingWithPalmDown(OVRSkeleton hand, bool right)
        {
            Vector3 palmDownTest = (hand.Bones[(int)BoneId.Hand_Index1].Transform.position - hand.Bones[(int)BoneId.Hand_Pinky1].Transform.position).normalized;
            Vector3 indexDirection = (hand.Bones[(int)BoneId.Hand_IndexTip].Transform.position - hand.Bones[(int)BoneId.Hand_Index1].Transform.position).normalized;
            Vector3 middleDirection = (hand.Bones[(int)BoneId.Hand_MiddleTip].Transform.position - hand.Bones[(int)BoneId.Hand_Middle1].Transform.position).normalized;
            float indexMiddleComparison = Vector3.Dot(indexDirection, middleDirection);
            float palmDownTestComparison = Vector3.Distance(palmDownTest, Vector3.up);
            return 
                indexMiddleComparison < 0 &&
                (palmDownTestComparison > 0.5f || palmDownTestComparison < -0.5f);
        }

        private bool IsPointingWholeHandUpward(OVRSkeleton hand, bool right)
        {
            Vector3 middleDirection = (hand.Bones[(int)BoneId.Hand_MiddleTip].Transform.position - hand.Bones[(int)BoneId.Hand_Middle1].Transform.position).normalized;
            Vector3 ringDirection = (hand.Bones[(int)BoneId.Hand_RingTip].Transform.position - hand.Bones[(int)BoneId.Hand_Ring1].Transform.position).normalized;
            return Vector3.Dot(Vector3.up, middleDirection) > 0.7f && Vector3.Dot(Vector3.up, ringDirection) > 0.7f;
        }

        private bool IsPointingUpward(OVRSkeleton hand, bool right)
        {
            Vector3 indexDirection = (hand.Bones[(int)BoneId.Hand_IndexTip].Transform.position - hand.Bones[(int)BoneId.Hand_Index1].Transform.position).normalized;
            Vector3 middleDirection = (hand.Bones[(int)BoneId.Hand_MiddleTip].Transform.position - hand.Bones[(int)BoneId.Hand_Middle1].Transform.position).normalized;
            float indexMiddleComparison = Vector3.Dot(indexDirection, middleDirection);
            
            return indexMiddleComparison < 0 && Vector3.Dot(indexDirection,Vector3.up) > 0.7f;
        }

        private bool IsPalmFlatWhileThumbIsUp(OVRSkeleton hand)
        {
            Vector3 thumbDirection = (hand.Bones[(int)BoneId.Hand_ThumbTip].Transform.position - hand.Bones[(int)BoneId.Hand_Thumb1].Transform.position).normalized;
            Vector3 indexDirection = (hand.Bones[(int)BoneId.Hand_IndexTip].Transform.position - hand.Bones[(int)BoneId.Hand_Index1].Transform.position).normalized;
            Vector3 middleDirection = (hand.Bones[(int)BoneId.Hand_MiddleTip].Transform.position - hand.Bones[(int)BoneId.Hand_Middle1].Transform.position).normalized;
            float thumbIndexComparison = Vector3.Dot(thumbDirection, indexDirection);
            float indexMiddleComparison = Vector3.Dot(indexDirection, middleDirection);
            return thumbIndexComparison > 0.1f && 
                thumbIndexComparison < 0.5f &&
                indexMiddleComparison > 0.7f &&
                Vector3.Dot(thumbDirection, Vector3.up) > 0.6f;
        }

        private bool IsPalmFlatDownThumbOut(OVRSkeleton hand, bool right)
        {
            Vector3 palmDownTest = (hand.Bones[(int)BoneId.Hand_Index1].Transform.position - hand.Bones[(int)BoneId.Hand_Pinky1].Transform.position).normalized;
            Vector3 indexDirection = (hand.Bones[(int)BoneId.Hand_IndexTip].Transform.position - hand.Bones[(int)BoneId.Hand_Index1].Transform.position).normalized;
            Vector3 middleDirection = (hand.Bones[(int)BoneId.Hand_MiddleTip].Transform.position - hand.Bones[(int)BoneId.Hand_Middle1].Transform.position).normalized;
            float indexMiddleComparison = Vector3.Dot(indexDirection, middleDirection);
            float palmDownTestComparison = Vector3.Distance(palmDownTest, Vector3.up);
            return 
                indexMiddleComparison > 0.7f &&
                (palmDownTestComparison > 0.5f || palmDownTestComparison < -0.5f);
        }

        private bool IsNeutral(OVRHand hand, OVRSkeleton handSkeleton)
        {
            return !hand.GetFingerIsPinching(OVRHand.HandFinger.Index) &&
                !hand.GetFingerIsPinching(OVRHand.HandFinger.Middle) &&
                !IsFistClosed(handSkeleton) &&
                !IsPalmFlatWhileThumbIsUp(handSkeleton);
        }
        #endregion helper methods

        #region debugging
        private void LogAllBoneIds()
        {
            string boneList = "R: ";
            int count = 0;
            foreach (OVRBone bone in _rightHandSkeleton.Bones)
            {
                boneList += count + " " + bone.Id + ", ";
                count++;
            }
            ILog(boneList, true);
            boneList = "L: ";
            count = 0;
            foreach (OVRBone bone in _leftHandSkeleton.Bones)
            {
                boneList += count + " " + bone.Id + ", ";
                count++;
            }
            ILog(boneList, false);
        }

        /// <summary>
        /// output potential threshold values to the console
        /// and then set the thresholds in the IsFistClosed method accordingly
        /// </summary>
        private void CalibrateFistClosureThresholds()
        {
            int firstBone = 0;
            int secondBone = 0;
            switch (debugIterator % 5)
            {
                case 0:
                    firstBone = (int)BoneId.Hand_IndexTip;
                    secondBone = (int)BoneId.Hand_WristRoot;
                    break;
                case 1:
                    firstBone = (int)BoneId.Hand_MiddleTip;
                    secondBone = (int)BoneId.Hand_WristRoot;
                    break;
                case 2:
                    firstBone = (int)BoneId.Hand_RingTip;
                    secondBone = (int)BoneId.Hand_WristRoot;
                    break;
                case 3:
                    firstBone = (int)BoneId.Hand_PinkyTip;
                    secondBone = (int)BoneId.Hand_WristRoot;
                    break;
                case 4:
                    firstBone = (int)BoneId.Hand_ThumbTip;
                    secondBone = (int)BoneId.Hand_Index2;
                    break;
            }

            string output = (debugIterator % 5).ToString() + " " +
                (_rightHandSkeleton.Bones[firstBone].Transform.position - _rightHandSkeleton.Bones[secondBone].Transform.position).magnitude.ToString();
            ILog(output,true);
        }

        /// <summary>
        /// Compare direction of index finger to direction of thumb
        /// </summary>
        private void LogFingerDirections()
        {
            Vector3 rightIndexDirection = (_rightHandSkeleton.Bones[20].Transform.position - _rightHandSkeleton.Bones[6].Transform.position).normalized;
            Vector3 leftIndexDirection = (_leftHandSkeleton.Bones[20].Transform.position - _leftHandSkeleton.Bones[6].Transform.position).normalized;
            Vector3 rightThumbDirection = (_rightHandSkeleton.Bones[19].Transform.position - _rightHandSkeleton.Bones[3].Transform.position).normalized;
            Vector3 leftThumbDirection = (_leftHandSkeleton.Bones[19].Transform.position - _leftHandSkeleton.Bones[3].Transform.position).normalized;

            ILog("Right Hand " + Vector3.Dot(rightIndexDirection, rightThumbDirection) + " ", true);
            ILog("Left Hand " + Vector3.Dot(leftIndexDirection, leftThumbDirection),false);
        }

        /// <summary>
        /// handler for all console output for hand input
        /// </summary>
        private void ILog(string text, bool right)
        {
            if (_debuggingEnabled)
            {
                if (right)
                    rightDebugOutput.text = text;
                else leftDebugOutput.text = text;
                ADM.QLog(text);
                Debug.Log(text);
            }
        }

        /// <summary>
        /// If we're in text mode and the hand is currently entering text,
        /// we need to prevent hand input from accidentally escaping drag selection (very common due to tracking noise)
        /// until the hand exits the text entry boundary
        /// </summary>
        private bool CheckForTextDragLock(bool right)
        {
            return
                (right && AtomicModeController.Instance.currentRightMode == TransformMode.Text &&
                    AtomicModeController.Instance.rightTextUIActive && AtomicModeController.Instance.rightTextUIInBounds)
                ||
                (!right && AtomicModeController.Instance.currentLeftMode == TransformMode.Text &&
                    AtomicModeController.Instance.leftTextUIActive && AtomicModeController.Instance.leftTextUIInBounds);
        }
        #endregion debugging
    }
}

