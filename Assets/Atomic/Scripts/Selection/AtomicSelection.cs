using Atomic.Input;
using Atomic.Transformation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Atomic
{
    public class AtomicSelection : Singleton<AtomicSelection>
    {
        [HideInInspector] public List<GameObject> selectedObjects;

        public Transform masterPivot;

        public bool transformationInProgress;

        public UnityAction SelectionCleared;

        private bool _selectionNeedsRestoration;
        
        private AtomicInput _input;

        /// <summary>
        /// During transformation, the selection pivot will become the temp parent of all selected objects.
        /// If any selection had a parent, it will be stored here until transformation is complete.
        /// Key is child. Value is parent.
        /// </summary>
        private Dictionary<GameObject, GameObject> parentMemory;

        private void Awake()
        {
            selectedObjects = new List<GameObject>();
            parentMemory = new Dictionary<GameObject, GameObject>();
        }

        private void OnEnable()
        {
            _input = AtomicInput.Instance;
            _input.OnRightStateChanged += OnRightStateChanged;
            _input.OnLeftStateChanged += OnLeftStateChanged;
        }

        private void OnDisable()
        {
            if (_input != null)
            {
                _input.OnRightStateChanged -= OnRightStateChanged;
                _input.OnLeftStateChanged -= OnLeftStateChanged;
            }
        }

        #region event handlers
        private void OnLeftStateChanged(HandGestureState current, HandGestureState previous)
        {
            if (current == HandGestureState.DeselectAll)
                ClearAll();
        }

        private void OnRightStateChanged(HandGestureState current, HandGestureState previous)
        {
            if (current == HandGestureState.DeselectAll)
                ClearAll();
        }
        #endregion event handlers

        #region helper methods
        public void AddToSelected(bool right, GameObject selected)
        {
            selectedObjects.Add(selected);
            UpdatePivotOnSelection(true, right);
        }

        public void RemoveFromSelected(bool right, GameObject selected)
        {
            if (selectedObjects.Contains(selected))
                selectedObjects.Remove(selected);

            UpdatePivotOnSelection(false, right);
        }

        public void BeginTransformation(bool right)
        {
            if (transformationInProgress) return;

            if (!IsModeActive((right) ? Hand.Right : Hand.Left, TransformMode.Pivot))
                foreach (GameObject molecule in selectedObjects)
                {
                    if (!parentMemory.ContainsKey(molecule))
                    {
                        if (molecule.transform.parent != null)
                            parentMemory.Add(molecule, molecule.transform.parent.gameObject);
                        molecule.transform.SetParent(masterPivot);
                    }
                }
            transformationInProgress = true;
        }

        public void CompleteTransformation(bool right)
        {
            if (!transformationInProgress) return;

            if (!IsModeActive((right) ? Hand.Right : Hand.Left, TransformMode.Pivot))
                foreach (GameObject molecule in selectedObjects)
                {
                    if (parentMemory.ContainsKey(molecule))
                    {
                        molecule.transform.SetParent(parentMemory[molecule].transform);
                        parentMemory.Remove(molecule);
                    }
                    else if (molecule.transform.parent != null)
                        molecule.transform.parent = null;
                }
            transformationInProgress = false;
        }

        public void UpdatePivotOnSelection(bool on, bool right)
        {
            if (transformationInProgress) return;

            if (selectedObjects.Count > 0)
            {
                BreakPivotChildLinks();
                masterPivot.position = selectedObjects[selectedObjects.Count - 1].transform.position;
                if (_selectionNeedsRestoration)
                    RestorePivotChildLinks();
            }
        }

        public void ResetPivotScale(Vector3 newScale)
        {
            //StartCoroutine(PivotScaleResetSequence(newScale));
            BreakPivotChildLinks();
            masterPivot.transform.localScale = newScale;
            RestorePivotChildLinks();
        }

        private IEnumerator PivotScaleResetSequence(Vector3 newScale)
        {
            BreakPivotChildLinks();
            yield return new WaitForSeconds(2);
            masterPivot.transform.localScale = newScale;
            yield return new WaitForSeconds(2);
            RestorePivotChildLinks();
        }

        public void ClearAll()
        {
            selectedObjects.Clear();
            parentMemory.Clear();
            transformationInProgress = false;
            SelectionCleared?.Invoke();
        }

        private void BreakPivotChildLinks()
        {
            if (masterPivot.childCount > 0)
            {
                for (int i = masterPivot.childCount - 1; i >= 0; i--)
                    masterPivot.GetChild(i).SetParent(null);
            }
        }

        private void RestorePivotChildLinks()
        {
            foreach (GameObject obj in selectedObjects)
                obj.transform.SetParent(masterPivot);
        }

        private bool IsModeActive(Hand hand, TransformMode mode)
        {
            bool returnValue = AtomicModeController.Instance.HasFlag((hand == Hand.Right) ? AtomicModeController.Instance.currentRightMode : AtomicModeController.Instance.currentLeftMode, mode);
            Debug.Log("Is " + hand + " " + mode + " Active? " + returnValue);
            return returnValue;
        }
        #endregion helper methods
    }
}