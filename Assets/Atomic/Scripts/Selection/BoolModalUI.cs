using Atomic.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Atomic.Molecules
{
    public class BoolModalUI : AtomicSelectionModalUI
    {
        #region private vars
        [SerializeField] private MeshRenderer onRenderer, offRenderer;
        private BoolDatum _boolRef;
        private BoolToggle onSwitch, offSwitch;
        #endregion private vars

        #region init
        protected override void Awake()
        {
            base.Awake();

            if (onRenderer == null)
                Debug.LogError("Bool atom's ON renderer couldn't be found. Is it assigned in Inspector?");

            if (offRenderer == null)
                Debug.LogError("Bool atom's OFF renderer couldn't be found. Is it assigned in Inspector?");

            onRenderer.enabled = false;
            offRenderer.enabled = true;

            _boolRef = GetComponentInChildren<BoolDatum>();
            BoolToggle[] switches = _uiPrefab.transform.GetComponentsInChildren<BoolToggle>();
            onSwitch = switches[0];
            offSwitch = switches[1];

            onSwitch.Activated += OnSwitchActivated;
            offSwitch.Activated += OffSwitchActivated;

            _uiPrefab.SetActive(false);
        }
        #endregion init

        #region event handlers
        private void OnSwitchActivated()
        {
            _boolRef.value = true;
            onRenderer.enabled = true;
            offRenderer.enabled = false;
        }

        private void OffSwitchActivated()
        {
            _boolRef.value = false;
            onRenderer.enabled = false;
            offRenderer.enabled = true;
        }
        #endregion event handlers
    }
}