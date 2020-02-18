using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Atomic.Molecules
{
    public class CreationCubeTrigger : MonoBehaviour
    {
        #region public vars
        [HideInInspector] public CreationController controller;
        [HideInInspector] public bool right;
        #endregion public vars

        #region private vars
        [SerializeField] private CreationMode mode;
        private MeshRenderer _highlightRenderer;
        private CreationCubeChildTrigger[] _childTriggers;
        #endregion private vars

        #region init
        private void Awake()
        {
            if (transform.name.Contains("Views"))
                _highlightRenderer = GetComponent<MeshRenderer>();
            else _highlightRenderer = transform.parent.parent.GetChild(0).GetComponent<MeshRenderer>();
            _childTriggers = GetComponentsInChildren<CreationCubeChildTrigger>();
            foreach (CreationCubeChildTrigger childTrigger in _childTriggers)
                childTrigger.parentTrigger = this;
        }
        #endregion init

        public void OnTriggerEnter(Collider other)
        {
            if (other.tag == "Activator_Right")
            {
                if (!right) return;
                controller.OnTriggerEnter(true, mode, _highlightRenderer);
            }
            else if (other.tag == "Activator_Left")
            {
                if (right) return;
                controller.OnTriggerEnter(false, mode, _highlightRenderer);
            }
        }

        public void OnTriggerExit(Collider other)
        {
            if (other.tag == "Activator_Right")
            {
                if (!right) return;
                controller.OnTriggerExit(true, mode, _highlightRenderer);
            }
            else if (other.tag == "Activator_Left")
            {
                if (right) return;
                controller.OnTriggerExit(false, mode, _highlightRenderer);
            }
        }
    }
}
