using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static OVRSkeleton;

namespace Atomic.Input
{
    public class RightMarker : Singleton<RightMarker>
    {
        public GameObject triggerActivatorPrefab;
        public GameObject palmHoverNode;
        public GameObject palmNode;
        public Vector3 palmNormal;
        public const float handNoiseDampeningFactor = 0.5f;

        private OVRHand _rightHand;
        private OVRSkeleton _rightHandSkeleton;
        private GameObject _triggerActivator;
        private Vector3 _knuckleLineDirection, _wrist2FingertipDirection;

        private void Awake()
        {
            CreateTriggerActivator();
            _rightHand = transform.GetComponentInChildren<OVRHand>();
            _rightHandSkeleton = transform.GetComponentInChildren<OVRSkeleton>();
        }

        private void Update()
        {
            if (_triggerActivator != null)
                _triggerActivator.transform.position = Vector3.Lerp(_triggerActivator.transform.position,_rightHandSkeleton.Bones[(int)BoneId.Hand_ThumbTip].Transform.position,handNoiseDampeningFactor);

            _knuckleLineDirection = (_rightHandSkeleton.Bones[(int)BoneId.Hand_Index1].Transform.position - _rightHandSkeleton.Bones[(int)BoneId.Hand_Pinky1].Transform.position).normalized;
            _wrist2FingertipDirection = (_rightHandSkeleton.Bones[(int)BoneId.Hand_WristRoot].Transform.position - _rightHandSkeleton.Bones[(int)BoneId.Hand_Middle1].Transform.position).normalized;
            palmNormal = Vector3.Cross(_knuckleLineDirection, _wrist2FingertipDirection);
        }
        
        private void CreateTriggerActivator()
        {
            _triggerActivator = Instantiate(triggerActivatorPrefab);
        }
    }
}


