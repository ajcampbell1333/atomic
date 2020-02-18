using Atomic.Input;
using Atomic.Transformation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static OVRSkeleton;

namespace Atomic.Molecules
{
    public class CreationCubeCursor : MonoBehaviour
    {
        #region public vars
        public float yOffset;
        #endregion public vars

        #region private vars
        private AtomicInput _input;
        private bool _offsetEngaged;
        private bool right;
        private MeshRenderer vRenderer;
        private OVRHand _hand;
        private OVRSkeleton _skeleton;
        #endregion private vars

        #region init
        private void Awake()
        {
            _input = AtomicInput.Instance;
            right = transform.name.Contains("Right");
            _hand = (right)
                ? RightMarker.Instance.GetComponentInChildren<OVRHand>()
                : LeftMarker.Instance.GetComponentInChildren<OVRHand>();
            _skeleton = (right)
                ? RightMarker.Instance.GetComponentInChildren<OVRSkeleton>()
                : LeftMarker.Instance.GetComponentInChildren<OVRSkeleton>();
            vRenderer = GetComponent<MeshRenderer>();
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
                AtomicModeController.Instance.RightModeChanged += OnRightModeChanged;
                AtomicModeController.Instance.LeftModeChanged += OnLeftModeChanged;
            }
        }
        #endregion init

        #region loops and timers
        private void Update()
        {
            if ((right && !(AtomicModeController.Instance.currentRightMode == TransformMode.Create)) ||
                (!right && !(AtomicModeController.Instance.currentLeftMode == TransformMode.Create)))
                return;

            transform.position = Vector3.Lerp
                                        (
                                            transform.position,
                                            _skeleton.Bones[(int)BoneId.Hand_ThumbTip].Transform.position + new Vector3(0, yOffset, 0),
                                            RightMarker.handNoiseDampeningFactor
                                        );
            transform.rotation = Quaternion.LookRotation(_hand.PointerPose.forward);
        }
        #endregion loops and timers

        #region event handlers
        private void OnLeftModeChanged(TransformMode current, TransformMode previous)
        {
            vRenderer.enabled = AtomicModeController.Instance.currentLeftMode == TransformMode.Create;
        }

        private void OnRightModeChanged(TransformMode current, TransformMode previous)
        {
            vRenderer.enabled = AtomicModeController.Instance.currentRightMode == TransformMode.Create;
        }
        #endregion event handlers
    }
}

