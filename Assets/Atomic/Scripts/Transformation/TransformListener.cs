using cakeslice;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Atomic.Transformation
{
    public class TransformListener : MonoBehaviour
    {
        #region public vars
        public UnityAction<bool,bool> SelectionChanged;
        #endregion public vars

        #region private vars
        private Outline _outline;
        #endregion private vars

        #region init
        private void OnEnable()
        {
            AtomicRaycaster.Instance.RightHoverChanged += OnRightHoverChanged;
            AtomicRaycaster.Instance.LeftHoverChanged += OnLeftHoverChanged;
            AtomicSelection.Instance.SelectionCleared += OnSelectionCleared;

            if (GetComponent<MeshRenderer>() != null)
                _outline = gameObject.AddComponent<Outline>();
            else _outline = transform.GetComponentInChildren<MeshRenderer>().gameObject.AddComponent<Outline>();
            
            _outline.color = 2;
            StartCoroutine(CompleteOutlineInit());
        }
        
        IEnumerator CompleteOutlineInit()
        {
            yield return new WaitForSeconds(0.1f);
            _outline.enabled = false;
        }

        private void OnDisable()
        {
            if (AtomicRaycaster.Instance != null)
            {
                AtomicRaycaster.Instance.RightHoverChanged -= OnRightHoverChanged;
                AtomicRaycaster.Instance.LeftHoverChanged -= OnLeftHoverChanged;
            }

            if (AtomicSelection.Instance != null)
                AtomicSelection.Instance.SelectionCleared -= OnSelectionCleared;
        }
        #endregion init
        
        #region event handlers
        private void OnRightHoverChanged(bool on, GameObject hit)
        {
            //Debug.Log("Listener named " + gameObject.name + " got hover change to " + on + " for obj named " + hit.name);
            
            if (gameObject == hit)
            {
                //_outline.enabled = (on) ? !_outline.enabled : _outline.enabled;

                if (AtomicRaycaster.Instance.rightSelecting)
                {
                    Debug.Log("Right is selecting");
                    AtomicSelection.Instance.AddToSelected(true, gameObject);
                    _outline.enabled = true;
                    SelectionChanged?.Invoke(true, true);
                }
                else {
                    Debug.Log("Right is deselecting");
                    AtomicSelection.Instance.RemoveFromSelected(true, gameObject);
                    _outline.enabled = false;
                    SelectionChanged?.Invoke(false, true);
                }
            }
        }

        private void OnLeftHoverChanged(bool on, GameObject hit)
        {
            if (gameObject == hit)
            {
                //_outline.enabled = (on) ? !_outline.enabled : _outline.enabled;
                if (AtomicRaycaster.Instance.leftSelecting)
                {
                    AtomicSelection.Instance.AddToSelected(false, gameObject);
                    _outline.enabled = true;
                    SelectionChanged?.Invoke(true, false);
                }
                else {
                    AtomicSelection.Instance.RemoveFromSelected(false, gameObject);
                    _outline.enabled = false;
                    SelectionChanged?.Invoke(false, false);
                }
            }
        }

        private void OnSelectionCleared()
        {
            if (_outline.enabled)
                _outline.enabled = false;
        }
        #endregion event handlers
    }
}

