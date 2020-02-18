using Atomic.Transformation;
using Atomic.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Atomic.Molecules
{
    /// <summary>
    /// Parent class for control interfaces for each atom and molecule
    /// </summary>
    public class AtomicSelectionModalUI : MonoBehaviour
    {
        [SerializeField] protected GameObject _uiPrefab;
        [SerializeField] protected float _scaleConstant;
        protected TransformListener _listener;

        public Atom owner;

        protected virtual void Awake()
        {
            _uiPrefab.transform.localScale = transform.localScale*_scaleConstant;
            _listener = GetComponent<TransformListener>();
            
        }

        protected virtual void OnEnable()
        {
            _listener.SelectionChanged += OnSelectionChanged;
            AtomicInput.Instance.OnRightStateChanged += OnRightStateChanged;
            AtomicInput.Instance.OnLeftStateChanged += OnLeftStateChanged;
        }

        protected virtual void OnDisable()
        {
            if (_listener != null)
                _listener.SelectionChanged -= OnSelectionChanged;

            if (AtomicInput.Instance != null)
            {
                AtomicInput.Instance.OnRightStateChanged -= OnRightStateChanged;
                AtomicInput.Instance.OnLeftStateChanged -= OnLeftStateChanged;
            }
        }

        protected virtual void OnSelectionChanged(bool on, bool right)
        {
            if (on)
                _uiPrefab.SetActive(on);
        }

        protected virtual void OnRightStateChanged(HandGestureState current, HandGestureState previous)
        {
            if (current == HandGestureState.DeselectAll)
                _uiPrefab.SetActive(false);
        }

        protected virtual void OnLeftStateChanged(HandGestureState current, HandGestureState previous)
        {
            if (current == HandGestureState.DeselectAll)
                _uiPrefab.SetActive(false);
        }
    }
}
