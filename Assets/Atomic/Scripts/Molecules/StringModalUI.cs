using Atomic.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Atomic.Molecules
{
    /// <summary>
    /// Handles text entry interaction when user hovers raycast over its string variable
    /// </summary>
    public class StringModalUI : AtomicSelectionModalUI
    {
        #region private vars
        private const float _hoverActivationTimeThreshold = 1.5f;
        private bool _isHoverTimerTicking;
        private int _hoverTally = 0;
        [SerializeField] private Text _inputDisplay;
        [SerializeField] private Text _valueDisplay;
        [SerializeField] private StringDatum _stringDatum;
        #endregion private vars
        
        #region init
        protected override void OnEnable()
        {
            base.OnEnable();
            AtomicRaycaster.Instance.RightHoverChanged += OnHoverChanged;
            AtomicRaycaster.Instance.LeftHoverChanged += OnHoverChanged;
        }

        
        protected override void OnDisable()
        {
            base.OnDisable();
            if (AtomicRaycaster.Instance != null)
            {
                AtomicRaycaster.Instance.RightHoverChanged -= OnHoverChanged;
                AtomicRaycaster.Instance.LeftHoverChanged -= OnHoverChanged;
            }
        }
        #endregion init

        #region event handlers
        private void OnHoverChanged(bool on, GameObject hit)
        {
            if (on)
            {
                _hoverTally++;
                if (!_isHoverTimerTicking)
                    StartCoroutine(HoverActivationTimer());
            }
            else if (!on && _hoverTally > 0)
                _hoverTally--;
        }

        private void OnEnterActivated()
        {
            _valueDisplay.text = _inputDisplay.text;
            _uiPrefab.SetActive(false);
            QWERTYController.Instance.OnEnterActivated -= OnEnterActivated;
        }

        protected override void OnRightStateChanged(HandGestureState current, HandGestureState previous)
        {
            if (current == HandGestureState.DeselectAll)
            {
                _inputDisplay.text = _valueDisplay.text;
                _uiPrefab.SetActive(false);
            }
        }

        protected override void OnLeftStateChanged(HandGestureState current, HandGestureState previous)
        {
            if (current == HandGestureState.DeselectAll)
            {
                _inputDisplay.text = _valueDisplay.text;
                _uiPrefab.SetActive(false);
            }
        }

        protected override void OnSelectionChanged(bool on, bool right)
        {
            // just here to kill the parent's behavior
        }
        #endregion event handlers

        #region helper methods
        private IEnumerator HoverActivationTimer()
        {
            float exit = Time.time + _hoverActivationTimeThreshold;
            while (Time.time < exit)
            {
                if (_hoverTally == 0)
                {
                    _isHoverTimerTicking = false;
                    yield break;
                }
                yield return new WaitForEndOfFrame();
            }
            if (_hoverTally > 0)
            {
                _uiPrefab.SetActive(true);
                TextOutputMarker.Instance.RegisterOutput(ref _inputDisplay);
                QWERTYController.Instance.Activate();
                QWERTYController.Instance.OnEnterActivated += OnEnterActivated;
            }
            _isHoverTimerTicking = false;
        }
        #endregion helper methods
    }
}