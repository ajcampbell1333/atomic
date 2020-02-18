using cakeslice;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Atomic.Molecules
{
    /// <summary>
    /// A control interface that causes its corresponding boolean variable to switch on or off.
    /// There are two of these for each bool atom, one for on and one for off. User can hover
    /// over either to assign the corresponding value.
    /// </summary>
    public class BoolToggle : MonoBehaviour
    {
        public UnityAction Activated;
        private Outline _outline;

        [SerializeField] private bool toggleUnitTest;

        #region init
        private void Awake()
        {
            _outline = gameObject.AddComponent<Outline>();
            
            _outline.color = 2;
            StartCoroutine(CompleteOutlineInit());
        }

        IEnumerator CompleteOutlineInit()
        {
            yield return new WaitForSeconds(0.1f);
            _outline.enabled = false;
        }

        private void OnEnable()
        {
            AtomicRaycaster.Instance.RightHoverChanged += OnRightHoverChanged;
            AtomicRaycaster.Instance.LeftHoverChanged += OnLeftHoverChanged;
        }

        private void OnDisable()
        {
            if (AtomicRaycaster.Instance != null)
            {
                AtomicRaycaster.Instance.RightHoverChanged -= OnRightHoverChanged;
                AtomicRaycaster.Instance.LeftHoverChanged -= OnLeftHoverChanged;
            }
        }
        #endregion init

        private void Update()
        {
            if (toggleUnitTest)
            {
                toggleUnitTest = false;
                OnRightHoverChanged(true, gameObject);
            }
        }

        #region event handlers
        private void OnLeftHoverChanged(bool on, GameObject hit)
        {
            if (on && hit == gameObject)
            {
                _outline.enabled = true;
                Activated?.Invoke();
            }
            else _outline.enabled = false;
        }

        private void OnRightHoverChanged(bool on, GameObject hit)
        {
            if (on && hit == gameObject)
            {
                _outline.enabled = true;
                Activated?.Invoke();
            }
            else _outline.enabled = false;
        }
        #endregion eventhandlers
    }
}


