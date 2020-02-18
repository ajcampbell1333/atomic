using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Atomic.Input
{
    /// <summary>
    /// Consolidates all input types into a single set of actions
    /// including input from Oculus Touch, Oculus Hands, ... (add others in future)
    /// </summary>
    class AtomicInput : Singleton<AtomicInput>
    {
        public bool modeActive;
        [HideInInspector] public UnityAction<bool> OnModeActiveStateChanged;
        [HideInInspector] public UnityAction<HandGestureState, HandGestureState> OnRightStateChanged, OnLeftStateChanged;
        [HideInInspector] public UnityAction<bool, InputTouchStates.TouchStateIndex> OnHoldAction;
        [HideInInspector] public UnityAction<RotationState, RotationState> OnLeftRotationStateChanged, OnRightRotationStateChanged;

        [HideInInspector] public HandGestureState currentRightHandGestureState, currentLeftHandGestureState;
        [HideInInspector] public RotationState currentRightRotationState, currentLeftRotationState;

        /// <summary>
        /// Touch Input Hold States - each array has 3 values, one for each of the 3 primary fingers
        /// </summary>
        public bool[] rightHeldLong, leftHeldLong;

        private void OnEnable()
        {
            OculusHandInput.Instance.RightStateChanged += RightStateChangedFromHandInput;
            OculusHandInput.Instance.LeftStateChanged += LeftStateChangedFromHandInput;
            OculusHandInput.Instance.ModeActiveStateChanged += ModeActiveStateChanged;
            OculusHandInput.Instance.RightRotationStateChanged += RightRotationStateChangedFromHandInput;
            OculusHandInput.Instance.LeftRotationStateChanged += LeftRotationStateChangedFromHandInput;
            OculusHandInput.Instance.HoldAction += HoldAction;

            OculusTouchInputTest.Instance.RightStateChanged += RightStateChangedFromTouchInput;
            OculusTouchInputTest.Instance.LeftStateChanged += LeftStateChangedFromTouchInput;
            OculusTouchInputTest.Instance.HoldAction += HoldAction;
            OculusTouchInputTest.Instance.RightRotationStateChanged += RightRotationStateChanged;
            OculusTouchInputTest.Instance.LeftRotationStateChanged += LeftRotationStateChanged;
            OculusTouchInputTest.Instance.RefreshHeldLongState += RefreshHeldLongState;
        }

        private void OnDisable()
        {
            if (OculusHandInput.Instance != null)
            {
                OculusHandInput.Instance.RightStateChanged -= RightStateChangedFromHandInput;
                OculusHandInput.Instance.LeftStateChanged -= LeftStateChangedFromHandInput;
                OculusHandInput.Instance.RightRotationStateChanged -= RightRotationStateChangedFromHandInput;
                OculusHandInput.Instance.ModeActiveStateChanged -= ModeActiveStateChanged;
            }
            if (OculusTouchInputTest.Instance != null)
            {
                OculusTouchInputTest.Instance.RightStateChanged -= RightStateChangedFromTouchInput;
                OculusTouchInputTest.Instance.LeftStateChanged -= LeftStateChangedFromTouchInput;
                OculusTouchInputTest.Instance.HoldAction -= HoldAction;
                OculusTouchInputTest.Instance.RightRotationStateChanged -= RightRotationStateChanged;
                OculusTouchInputTest.Instance.LeftRotationStateChanged -= LeftRotationStateChanged;
                OculusTouchInputTest.Instance.RefreshHeldLongState -= RefreshHeldLongState;
            }
        }

        #region event handlers

        private void LeftStateChangedFromTouchInput(HandGestureState current, HandGestureState previous)
        {
            currentLeftHandGestureState = current;
            OnLeftStateChanged?.Invoke(current, previous);
        }

        private void LeftStateChangedFromHandInput(HandGestureState current, HandGestureState previous)
        {
            currentLeftHandGestureState = current;
            OnLeftStateChanged?.Invoke(current, previous);
        }

        private void RightStateChangedFromTouchInput(HandGestureState current, HandGestureState previous)
        {
            currentRightHandGestureState = current;
            OnRightStateChanged?.Invoke(current, previous);
        }

        private void RightStateChangedFromHandInput(HandGestureState current, HandGestureState previous)
        {
            Debug.Log("AtomicInput RightStateChangedFromHandInput - current: " + current + " previous: " + previous);
            currentRightHandGestureState = current;
            OnRightStateChanged?.Invoke(current, previous);
        }

        private void LeftRotationStateChangedFromHandInput(RotationState current, RotationState previous)
        {
            currentLeftRotationState = current;
            OnLeftRotationStateChanged?.Invoke(current, previous);
        }

        private void RightRotationStateChangedFromHandInput(RotationState current, RotationState previous)
        {
            currentRightRotationState = current;
            OnRightRotationStateChanged?.Invoke(current, previous);
        }

        private void ModeActiveStateChanged(bool on)
        {
            modeActive = on;
            OnModeActiveStateChanged?.Invoke(modeActive);
        }

        private void HoldAction(bool right, InputTouchStates.TouchStateIndex finger)
        {
            if (right) rightHeldLong[(int)finger] = true;
            OnHoldAction?.Invoke(right, finger);
        }

        private void RightRotationStateChanged(RotationState current, RotationState previous)
        {
            currentRightRotationState = current;
            OnRightRotationStateChanged?.Invoke(current, previous);
        }

        private void LeftRotationStateChanged(RotationState current, RotationState previous)
        {
            currentLeftRotationState = current;
            OnLeftRotationStateChanged.Invoke(current, previous);
        }

        private void RefreshHeldLongState(bool right, bool[] newState)
        {
            if (right) rightHeldLong = newState;
            else leftHeldLong = newState;
        }
        #endregion event handlers
    }
}
