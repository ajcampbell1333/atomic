using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static OVRSkeleton;

namespace Atomic.Input
{
    public class LeftMarker : Singleton<LeftMarker>
    {
        //public OculusHaptics haptics;
        public GameObject triggerActivatorPrefab;
        public Vector3 palmNormal;
        public Vector3 indexThumbMidpoint;

        private Vector3 _knuckleLineDirection, _wrist2FingertipDirection;

        private OVRHand _leftHand;
        private OVRSkeleton _leftHandSkeleton;
        private GameObject _triggerActivator;

        private void Awake()
        {
            _triggerActivator = Instantiate(triggerActivatorPrefab);
            _leftHand = transform.GetComponentInChildren<OVRHand>();
            _leftHandSkeleton = transform.GetComponentInChildren<OVRSkeleton>();
        }

        private void Update()
        {
            if (_triggerActivator != null)
                _triggerActivator.transform.position = Vector3.Lerp(_triggerActivator.transform.position, _leftHandSkeleton.Bones[(int)BoneId.Hand_ThumbTip].Transform.position, RightMarker.handNoiseDampeningFactor);

            _knuckleLineDirection = (_leftHandSkeleton.Bones[(int)BoneId.Hand_Index1].Transform.position - _leftHandSkeleton.Bones[(int)BoneId.Hand_Pinky1].Transform.position).normalized;
            _wrist2FingertipDirection = (_leftHandSkeleton.Bones[(int)BoneId.Hand_WristRoot].Transform.position - _leftHandSkeleton.Bones[(int)BoneId.Hand_Middle1].Transform.position).normalized;
            palmNormal = -1 * Vector3.Cross(_knuckleLineDirection, _wrist2FingertipDirection);
        }
    }
}

