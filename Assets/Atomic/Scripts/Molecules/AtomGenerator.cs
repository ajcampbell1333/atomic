using System;
using System.Collections;
using System.Collections.Generic;
using Atomic.Input;
using Atomic.Transformation;
using UnityEngine;

namespace Atomic.Molecules
{
    public class AtomGenerator : Singleton<AtomGenerator>
    {
        #region private vars
        private OVRSkeleton _rightHandSkeleton, _leftHandSkeleton;
        #endregion private vars

        #region public vars

        #endregion public vars

        #region init
        private void Awake()
        {
            _rightHandSkeleton = RightMarker.Instance.transform.GetComponentInChildren<OVRSkeleton>();
            _leftHandSkeleton = LeftMarker.Instance.transform.GetComponentInChildren<OVRSkeleton>();
        }

        private void OnEnable()
        {
            AtomicModeController.Instance.RightModeChanged += OnRightModeChanged;
            AtomicModeController.Instance.LeftModeChanged += OnLeftModeChanged;

            AtomicInput.Instance.OnRightStateChanged += OnRightStateChanged;
            AtomicInput.Instance.OnLeftStateChanged += OnLeftStateChanged;
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

        #region event handlers
        private void OnRightModeChanged(TransformMode current, TransformMode previous)
        {

        }

        private void OnLeftModeChanged(TransformMode current, TransformMode previous)
        {
            
        }

        private void OnRightStateChanged(HandGestureState current, HandGestureState previous)
        {
            if (current == HandGestureState.DragSelection)
            {

            }
        }

        private void OnLeftStateChanged(HandGestureState current, HandGestureState previous)
        {

        }
        #endregion event handlers
    }
}

