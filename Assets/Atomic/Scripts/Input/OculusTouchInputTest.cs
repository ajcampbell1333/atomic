using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static OVRInput;
using Atomic;
using Atomic.Input;
using static Atomic.Input.InputTouchStates;

/// <summary>
/// Maps Oculus Touch open/closed finger inputs to relevant gesture states using capacitive touch whenever possible
/// </summary>
public class OculusTouchInputTest : Singleton<OculusTouchInputTest>
{
    #region vars
    [SerializeField] public float holdActionThreshold;
    [HideInInspector] public Action<HandGestureState,HandGestureState> LeftStateChanged, RightStateChanged;
    [HideInInspector] public Action<RotationState, RotationState> LeftRotationStateChanged, RightRotationStateChanged;
    [HideInInspector] public Action<bool, TouchStateIndex> HoldAction;
    [HideInInspector] public Action<bool, bool[]> RefreshHeldLongState;
    [HideInInspector] public TouchStates rightHandState, leftHandState;
    [HideInInspector] public HandGestureState currentRightHandGestureState, currentLeftHandGestureState;
    [HideInInspector] public RotationState currentRightRotationState, currentLeftRotationState;
    
    public bool[] rightHeldLong, leftHeldLong;

    private GameObject _selectionRightStartingMarker, _selectionLeftStartingMarker;
    #endregion vars

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
    #endregion init

    #region loops
    void Update()
    {
        //ADM.QLog("current right hand state: " + currentRightHandGestureState);
        //ADM.QLog("current left hand state: " + currentLeftHandGestureState);
        #region thumb
        if (OVRInput.GetDown(OVRInput.Touch.One, Controller.RTouch))
        {
            ADM.QLog("Got right thumb touch.");
            rightHandState |= TouchStates.thumb;
            UpdateHandState(true);
            StartCoroutine(HoldThresholdTrigger(true, TouchStates.thumb, TouchStateIndex.thumb, Time.time));
        }

        if (OVRInput.GetUp(OVRInput.Touch.One, Controller.RTouch))
        {
            ADM.QLog("Got right thumb touch release.");
            rightHandState &= ~TouchStates.thumb;
            UpdateHandState(true);
            rightHeldLong[(int)TouchStateIndex.thumb] = false;
            RefreshHeldLongState?.Invoke(true,rightHeldLong);
        }

        if (OVRInput.GetDown(OVRInput.Touch.One, Controller.LTouch))
        {
            ADM.QLog("Got left thumb touch.");
            leftHandState |= TouchStates.thumb;
            UpdateHandState(false);
            StartCoroutine(HoldThresholdTrigger(false, TouchStates.thumb, TouchStateIndex.thumb, Time.time));
        }

        if (OVRInput.GetUp(OVRInput.Touch.One, Controller.LTouch))
        {
            ADM.QLog("Got left thumb touch release.");
            leftHandState &= ~TouchStates.thumb;
            UpdateHandState(false);
            leftHeldLong[(int)TouchStateIndex.thumb] = false;
            RefreshHeldLongState?.Invoke(true, rightHeldLong);
        }
        #endregion thumb

        #region pointer
        if (OVRInput.GetDown(OVRInput.Touch.PrimaryIndexTrigger, Controller.LTouch))
        {
            ADM.QLog("Got left index finger touch.");
            leftHandState |= TouchStates.pointer;
            UpdateHandState(false);
            StartCoroutine(HoldThresholdTrigger(false, TouchStates.pointer, TouchStateIndex.pointer, Time.time));
        }

        if (OVRInput.GetUp(OVRInput.Touch.PrimaryIndexTrigger, Controller.LTouch))
        {
            ADM.QLog("Got left index finger touch release.");
            leftHandState &= ~TouchStates.pointer;
            UpdateHandState(false);
            leftHeldLong[(int)TouchStateIndex.pointer] = false;
            RefreshHeldLongState?.Invoke(true, rightHeldLong);
        }

        if (OVRInput.GetDown(OVRInput.Touch.PrimaryIndexTrigger, Controller.RTouch))
        {
            ADM.QLog("Got right index finger touch.");
            rightHandState |= TouchStates.pointer;
            UpdateHandState(true);
            StartCoroutine(HoldThresholdTrigger(true, TouchStates.pointer, TouchStateIndex.pointer, Time.time));
        }

        if (OVRInput.GetUp(OVRInput.Touch.PrimaryIndexTrigger, Controller.RTouch))
        {
            ADM.QLog("Got right index finger touch release.");
            rightHandState &= ~TouchStates.pointer;
            UpdateHandState(true);
            rightHeldLong[(int)TouchStateIndex.pointer] = false;
            RefreshHeldLongState?.Invoke(true, rightHeldLong);
        }
        #endregion pointer

        #region grip
        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, Controller.LTouch))
        {
            ADM.QLog("Got left grip touch.");
            leftHandState |= TouchStates.middle;
            UpdateHandState(false);
            StartCoroutine(HoldThresholdTrigger(false, TouchStates.middle, TouchStateIndex.middle, Time.time));
        }

        if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, Controller.LTouch))
        {
            ADM.QLog("Got left grip touch release.");
            leftHandState &= ~TouchStates.middle;
            UpdateHandState(false);
            leftHeldLong[(int)TouchStateIndex.middle] = false;
            RefreshHeldLongState?.Invoke(true, rightHeldLong);
        }

        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, Controller.RTouch))
        {
            ADM.QLog("Got right grip touch.");
            rightHandState |= TouchStates.middle;
            UpdateHandState(true);
            StartCoroutine(HoldThresholdTrigger(true, TouchStates.middle, TouchStateIndex.middle, Time.time));
        }

        if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, Controller.RTouch))
        {
            ADM.QLog("Got right grip touch release.");
            rightHandState &= ~TouchStates.middle;
            UpdateHandState(true);
            rightHeldLong[(int)TouchStateIndex.middle] = false;
            RefreshHeldLongState?.Invoke(true, rightHeldLong);
        }
        #endregion grip

        #region rotation
        if (currentRightHandGestureState == HandGestureState.DragSelection &&
            ((Vector3.Dot(Vector3.up, RightMarker.Instance.transform.up) < 0.75f && currentRightRotationState == RotationState.lowerCase) ||
            (Vector3.Dot(Vector3.up, RightMarker.Instance.transform.right) < 0.75f && currentRightRotationState == RotationState.upperCase) ||
            (Vector3.Dot(Vector3.up, -1 * RightMarker.Instance.transform.right) < 0.75f && currentRightRotationState == RotationState.specialCharacters) ||
            (Vector3.Dot(_selectionRightStartingMarker.transform.forward, RightMarker.Instance.transform.right) < 0.75f && currentRightRotationState == RotationState.numbers))
            )
        {
           // ADM.QLog("Right rotation state just changed.");
            UpdateRotationState(true);
        }

        if (currentLeftHandGestureState == HandGestureState.DragSelection &&
            ((Vector3.Dot(Vector3.up, LeftMarker.Instance.transform.up) < 0.75f && currentLeftRotationState == RotationState.lowerCase) ||
            (Vector3.Dot(Vector3.up, -1 * LeftMarker.Instance.transform.right) < 0.75f && currentLeftRotationState == RotationState.upperCase) ||
            (Vector3.Dot(Vector3.up, LeftMarker.Instance.transform.right) < 0.75f && currentLeftRotationState == RotationState.specialCharacters) ||
            (Vector3.Dot(_selectionLeftStartingMarker.transform.forward, -1 * LeftMarker.Instance.transform.right) < 0.75f && currentLeftRotationState == RotationState.numbers))
            )
        {
           // ADM.QLog("Left rotation state just changed.");
            UpdateRotationState(false);
        }
        #endregion rotation
    }

    void UpdateHandState(bool right)
    {
        //ADM.QLog("updating hand state for " + ((right) ? "right " : "left ") + "hand.");
        //ADM.QLog("Current state: " + currentRightHandGestureState +
        //       " middle " + (((rightHandState & TouchStates.middle) != 0) ? "on" : "off")
        //    + " pointer " + (((rightHandState & TouchStates.pointer) != 0) ? "on" : "off")
        //    + " thumb " + (((rightHandState & TouchStates.thumb) != 0) ? "on" : "off")
        //);
        HandGestureState previousState = (right) ? currentRightHandGestureState : currentLeftHandGestureState;
        if (right)
        {
            // Selection conditions: middle down, pointer up, thumb up
            if (currentRightHandGestureState != HandGestureState.Selection &&
                (rightHandState & TouchStates.middle) != 0 &&
                (rightHandState & TouchStates.pointer) == 0 &&
                (rightHandState & TouchStates.thumb) == 0)
            {
                currentRightHandGestureState = HandGestureState.Selection;
                _selectionRightStartingMarker.transform.position = RightMarker.Instance.transform.position;
                _selectionRightStartingMarker.transform.rotation = RightMarker.Instance.transform.rotation;
            }

            // Insertion conditions: all three down
            if (currentRightHandGestureState != HandGestureState.Neutral && 
                currentRightHandGestureState != HandGestureState.Insert &&
                (rightHandState & TouchStates.middle) != 0 &&
                (rightHandState & TouchStates.pointer) != 0 &&
                (rightHandState & TouchStates.thumb) != 0)
                currentRightHandGestureState = HandGestureState.Insert;

            // SqueezeAll conditions: all three down from neutral
            if (currentRightHandGestureState == HandGestureState.Neutral &&
                (rightHandState & TouchStates.middle) != 0 &&
                (rightHandState & TouchStates.pointer) != 0 &&
                (rightHandState & TouchStates.thumb) != 0)
                currentRightHandGestureState = HandGestureState.SqueezeAll;

            // DragSelection conditions: middle and thumb down, pointer up
            if (currentRightHandGestureState != HandGestureState.DragSelection &&
                (rightHandState & TouchStates.middle) != 0 &&
                (rightHandState & TouchStates.pointer) == 0 &&
                (rightHandState & TouchStates.thumb) != 0)
                currentRightHandGestureState = HandGestureState.DragSelection;

            // Neutral condtions: all up
            if (currentRightHandGestureState != HandGestureState.Neutral &&
                (rightHandState & TouchStates.middle) == 0 &&
                (rightHandState & TouchStates.pointer) == 0 &&
                (rightHandState & TouchStates.thumb) == 0)
                currentRightHandGestureState = HandGestureState.Neutral;

            // Click conditions: 
            if (currentRightHandGestureState != HandGestureState.Click &&
                (rightHandState & TouchStates.middle) != 0 &&
                (rightHandState & TouchStates.pointer) != 0 &&
                (rightHandState & TouchStates.thumb) == 0)
                currentRightHandGestureState = HandGestureState.Click;

            RightStateChanged?.Invoke(currentRightHandGestureState, previousState);
        }
        else
        {
            if (currentLeftHandGestureState != HandGestureState.Selection &&
                (leftHandState & TouchStates.middle) != 0 &&
                (leftHandState & TouchStates.pointer) == 0 &&
                (leftHandState & TouchStates.thumb) == 0)
            {
                currentLeftHandGestureState = HandGestureState.Selection;
                _selectionLeftStartingMarker.transform.position = LeftMarker.Instance.transform.position;
                _selectionLeftStartingMarker.transform.rotation = LeftMarker.Instance.transform.rotation;
            }

            if (currentRightHandGestureState != HandGestureState.Neutral &&
                currentRightHandGestureState != HandGestureState.Insert &&
                (leftHandState & TouchStates.middle) != 0 &&
                (leftHandState & TouchStates.pointer) != 0 &&
                (leftHandState & TouchStates.thumb) != 0)
                currentLeftHandGestureState = HandGestureState.Insert;

            if (currentLeftHandGestureState == HandGestureState.Neutral &&
                (leftHandState & TouchStates.middle) != 0 &&
                (leftHandState & TouchStates.pointer) != 0 &&
                (leftHandState & TouchStates.thumb) != 0)
                currentLeftHandGestureState = HandGestureState.SqueezeAll;

            if (currentLeftHandGestureState != HandGestureState.DragSelection &&
                (leftHandState & TouchStates.middle) != 0 &&
                (leftHandState & TouchStates.pointer) == 0 &&
                (leftHandState & TouchStates.thumb) != 0)
                currentLeftHandGestureState = HandGestureState.DragSelection;

            if (currentLeftHandGestureState != HandGestureState.Neutral &&
                    (leftHandState & TouchStates.middle) == 0 &&
                    (leftHandState & TouchStates.pointer) == 0 &&
                    (leftHandState & TouchStates.thumb) == 0)
                currentLeftHandGestureState = HandGestureState.Neutral;

            if (currentLeftHandGestureState != HandGestureState.Click &&
                (leftHandState & TouchStates.middle) != 0 &&
                (leftHandState & TouchStates.pointer) != 0 &&
                (leftHandState & TouchStates.thumb) == 0)
                currentLeftHandGestureState = HandGestureState.Click;

            LeftStateChanged?.Invoke(currentLeftHandGestureState, previousState);
        }
    }

    /// <summary>
    /// Send a message out to any subcribed component that wants to receive an event trigger 
    /// when a time threshold is exceeded for a finger being held down 
    /// </summary>
    /// <param name="right">If true, it's a finger on the right hand, otherwise left</param>
    /// <param name="finger">Pointer, thumb, or index finger?</param>
    /// <param name="fingerIndex">A non-flag enumeration of the finger</param>
    /// <param name="startTime">The time in seconds when the hold began</param>
    /// <returns></returns>
    IEnumerator HoldThresholdTrigger(bool right, TouchStates finger, TouchStateIndex fingerIndex, float startTime)
    {
        yield return new WaitUntil(()=>(Time.time-startTime > holdActionThreshold));
        if ((((right) ? rightHandState : leftHandState) & finger) != 0)
        {
            if (right)
                rightHeldLong[(int)fingerIndex] = true;
            else leftHeldLong[(int)fingerIndex] = true;
            HoldAction?.Invoke(right, fingerIndex);
        }
    }

    void UpdateRotationState(bool right)
    {
        RotationState previousRotationState = (right) ? currentRightRotationState : currentLeftRotationState;
        if (right)
        {
            if (Vector3.Dot(Vector3.up, RightMarker.Instance.transform.up) >= 0.75f)
                currentRightRotationState = RotationState.lowerCase;
                
            if (Vector3.Dot(Vector3.up, RightMarker.Instance.transform.right) >= 0.75f)
                currentRightRotationState = RotationState.upperCase;

            if (Vector3.Dot(Vector3.up, -1 * RightMarker.Instance.transform.right) >= 0.75f)
                currentRightRotationState = RotationState.specialCharacters;

            if ((rightHandState & TouchStates.middle) != 0 &&
                Vector3.Dot(
                            RightMarker.Instance.transform.right,
                            _selectionRightStartingMarker.transform.forward
                        ) > 0.75
                    )
                currentRightRotationState = RotationState.numbers;

            RightRotationStateChanged?.Invoke(currentRightRotationState, previousRotationState);
        }
        else {
            //ADM.QLog(string.Format("Input Left Rotation Results - " +
            //    "lowercase condition: {0} " +
            //    "uppercase condition: {1} " +
            //    "special char condition: {2} " +
            //    "numbers condition: {3}",
            //    Vector3.Dot(Vector3.up, LeftMarker.Instance.transform.up),
            //    Vector3.Dot(Vector3.up, -1 * LeftMarker.Instance.transform.right),
            //    Vector3.Dot(Vector3.up, LeftMarker.Instance.transform.right),
            //    Vector3.Dot(-1 * LeftMarker.Instance.transform.right, _selectionLeftStartingMarker.transform.forward)
            //    ));
            if (Vector3.Dot(Vector3.up, LeftMarker.Instance.transform.up) >= 0.75f)
                currentLeftRotationState = RotationState.lowerCase;

            if (Vector3.Dot(Vector3.up, -1 * LeftMarker.Instance.transform.right) >= 0.75f)
                currentLeftRotationState = RotationState.upperCase;
                 
            if (Vector3.Dot(Vector3.up, LeftMarker.Instance.transform.right) >= 0.75f)
                currentLeftRotationState = RotationState.specialCharacters;

            if ((leftHandState & TouchStates.middle) != 0 &&
                Vector3.Dot(-1 * LeftMarker.Instance.transform.right, _selectionLeftStartingMarker.transform.forward) > 0.75f)
                currentLeftRotationState = RotationState.numbers;

            ADM.QLog("Left new rotation state: " + currentLeftRotationState);
            LeftRotationStateChanged?.Invoke(currentLeftRotationState, previousRotationState);
        }
    }
    #endregion loops
}


