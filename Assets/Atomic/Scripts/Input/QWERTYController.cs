using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Atomic.Input
{
    public class QWERTYController : Singleton<QWERTYController>
    {
        private Collider[] _colliders;
        private CanvasGroup[] _groups;
        private Vector3 _currentPosition;
        private QWERTYKey[] _keys;
        private bool _isInitialized;
        private QWERTYCustomButtonBase[] _specialButtons;
        public bool capsLock;
        public UnityAction OnEnterActivated;
        public bool keyboardActive;
        
        #region init
        private void Awake()
        {
            if (!_isInitialized)
                Initialize();

            ToggleVisibility(false);
            ToggleColliders(false);
        }

        private void Initialize()
        {
            _colliders = GetComponentsInChildren<Collider>();
            _groups = GetComponentsInChildren<CanvasGroup>();
            
            _specialButtons = GetComponentsInChildren<QWERTYCustomButtonBase>();
            _keys = GetComponentsInChildren<QWERTYKey>();

            _specialButtons[0].OnActivate += OnBackActivate;
            _specialButtons[1].OnActivate += OnCapsActivate;
            _specialButtons[2].OnActivate += OnEnterActivate;

            foreach (QWERTYKey key in _keys)
                key.OnActivate += OnKeyActivate;
            _isInitialized = true;
        }
        #endregion init

        #region event handlers
        private void OnKeyActivate(string key)
        {
            Debug.Log("key " + key + " activated");
            TextOutputMarker.Instance.UpdateOutputs(TextOutputMarker.Instance.displays[0].text + key);
        }

        private void OnEnterActivate()
        {
            if (!_isInitialized)
                Initialize();

            TextOutputMarker.Instance.UnregisterAll();
            OnEnterActivated?.Invoke();
            ToggleColliders(false);
            ToggleVisibility(false);
            keyboardActive = false;
        }

        private void OnCapsActivate()
        {
            capsLock = !capsLock;
            foreach (QWERTYKey key in _keys)
                key.ToggleKeyCaps(capsLock);
        }

        private void OnBackActivate()
        {
            if (TextOutputMarker.Instance.displays[0].text.Length > 0)
                TextOutputMarker.Instance.displays[0].text = TextOutputMarker.Instance.displays[0].text.Substring(0, TextOutputMarker.Instance.displays[0].text.Length - 1);
        }
        #endregion event handlers

        #region helper methods
        public void Activate()
        {
            if (!_isInitialized)
                Initialize();

            UpdatePositionAndRotation();
            ToggleVisibility(true);
            ToggleColliders(true);
            keyboardActive = true;
        }

        private void ToggleVisibility(bool on)
        {
            if (!_isInitialized)
                Initialize();

            foreach (CanvasGroup group in _groups)
                group.alpha = (on) ? 1:0;
        }

        private void ToggleColliders(bool on)
        {
            if (!_isInitialized)
                Initialize();

            foreach (Collider collider in _colliders)
                collider.enabled = on;
        }

        private void UpdatePositionAndRotation()
        {
            if (!_isInitialized)
                Initialize();

            _currentPosition = AtomicHeadMarker.Instance.transform.position + AtomicHeadMarker.Instance.transform.forward * 0.4f;
            _currentPosition = new Vector3(_currentPosition.x, RightMarker.Instance.transform.position.y, _currentPosition.z);
            transform.position = _currentPosition;
            transform.rotation = Quaternion.LookRotation(_currentPosition - AtomicHeadMarker.Instance.transform.position);
        }
        #endregion helper methods
    }
}
