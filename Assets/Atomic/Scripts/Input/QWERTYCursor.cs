using Atomic.Input;
using Atomic.Transformation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static OVRSkeleton;

namespace Atomic.Molecules
{
    public class QWERTYCursor : MonoBehaviour
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
        private Collider _collider;
        #endregion private vars

        #region init
        private void Awake()
        {
            _collider = GetComponent<Collider>();
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
        #endregion init

        #region loops and timers
        private void Update()
        {
            _collider.enabled = vRenderer.enabled = QWERTYController.Instance.keyboardActive;
            
            if (vRenderer.enabled)
            {
                transform.position = Vector3.Lerp
                                        (
                                            transform.position,
                                            _skeleton.Bones[(int)BoneId.Hand_ThumbTip].Transform.position +
                                            _hand.PointerPose.forward*0.1f + new Vector3(0, yOffset, 0),
                                            RightMarker.handNoiseDampeningFactor
                                        );
                transform.rotation = Quaternion.LookRotation(_hand.PointerPose.forward);
            }
        }
        #endregion loops and timers

        #region event handlers
        #endregion event handlers
    }
}

