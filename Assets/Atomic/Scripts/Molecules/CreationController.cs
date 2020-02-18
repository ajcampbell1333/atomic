using Atomic.Input;
using Atomic.Transformation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static OVRSkeleton;

namespace Atomic.Molecules
{
    public class CreationController : Singleton<CreationController>
    {
        #region private vars
        private Collider[] _rightCreationMenuColliders, _leftCreationMenuColliders;
        private MeshRenderer[] _rightCreationMenuRenderers, _leftCreationMenuRenderers;
        private List<MeshRenderer> _rightNonHighlightRenderers, _leftNonHighlightRenderers;
        private CanvasGroup[] _rightCanvasGroups, _leftCanvasGroups;
        [SerializeField] private Transform _rightCubeTransform, _leftCubeTransform;
        private CreationMode _currentRightHoveredMode = CreationMode.neutral, _currentLeftHoveredMode = CreationMode.neutral;
        private int _rightCollisionTally = 0, _leftCollisionTally = 0;
        private CreationCubeTrigger[] _rightTriggers, _leftTriggers;
        [SerializeField] private List<SerializablePrimitivePrefab> _creationPrimitivePrefabs;
        private OVRSkeleton _rightHandSkeleton, _leftHandSkeleton;
        [SerializeField] private Material hoverMaterial, selectedMaterial;
        private MeshRenderer _currentRightHighlightRenderer, _currentLeftHighlightRenderer;
        private MeshRenderer _currentRightSelectionRenderer, _currentLeftSelectionRenderer;
        private Vector3 _creationCubePosition;

        [SerializeField] private bool visibilityUnitTest;
        private bool _isVisible = true;
        [SerializeField] private bool positionUdateUnitTest;
        private int _insertionNoiseTally = 0;
        private int _insertionNoiseThreshhold = 5;
        private bool _isDenoising;
        #endregion private vars

        #region public vars
        public CreationMode currentRightCreationMode = CreationMode.neutral, currentLeftCreationMode = CreationMode.neutral;
        #endregion public vars

        #region init
        private void Awake()
        {
            _rightCreationMenuColliders = _rightCubeTransform.GetComponentsInChildren<Collider>();
            if (_rightCreationMenuColliders == null || _rightCreationMenuColliders.Length == 0)
                Debug.LogError("The right-hand creation menu's colliders were not found. Are they missing from the prefab?");

            _rightCreationMenuRenderers = _rightCubeTransform.GetComponentsInChildren<MeshRenderer>();
            if (_rightCreationMenuRenderers == null || _rightCreationMenuRenderers.Length == 0)
                Debug.LogError("The right-hand creation menu's renderers were not found. Are they missing from the prefab?");

            _leftCreationMenuColliders = _leftCubeTransform.GetComponentsInChildren<Collider>();
            if (_leftCreationMenuColliders == null || _leftCreationMenuColliders.Length == 0)
                Debug.LogError("The left-hand creation menu's colliders were not found. Are they missing from the prefab?");

            _leftCreationMenuRenderers = _leftCubeTransform.GetComponentsInChildren<MeshRenderer>();
            if (_leftCreationMenuRenderers == null || _leftCreationMenuRenderers.Length == 0)
                Debug.LogError("The left-hand creation menu's renderers were not found. Are they missing from the prefab?");

            _rightNonHighlightRenderers = new List<MeshRenderer>();
            foreach (MeshRenderer renderer in _rightCreationMenuRenderers)
                if (!renderer.name.Contains("Highlight"))
                    _rightNonHighlightRenderers.Add(renderer);

            _leftNonHighlightRenderers = new List<MeshRenderer>();
            foreach (MeshRenderer renderer in _leftCreationMenuRenderers)
                if (!renderer.name.Contains("Highlight"))
                    _leftNonHighlightRenderers.Add(renderer);

            _rightCanvasGroups = _rightCubeTransform.GetComponentsInChildren<CanvasGroup>();
            if (_rightCanvasGroups == null || _rightCanvasGroups.Length == 0)
                Debug.LogError("The right-hand creation menu's canvas groups were not found. Are they missing from the prefab?");

            _leftCanvasGroups = _leftCubeTransform.GetComponentsInChildren<CanvasGroup>();
            if (_leftCanvasGroups == null || _leftCanvasGroups.Length == 0)
                Debug.LogError("The left-hand creation menu's canvas groups were not found. Are they missing from the prefab?");

            _rightTriggers = _rightCubeTransform.GetComponentsInChildren<CreationCubeTrigger>();
            foreach (CreationCubeTrigger trigger in _rightTriggers)
            {
                trigger.controller = this;
                trigger.right = true;
            }

            _leftTriggers = _leftCubeTransform.GetComponentsInChildren<CreationCubeTrigger>();
            foreach (CreationCubeTrigger trigger in _leftTriggers)
                trigger.controller = this;

            _rightHandSkeleton = RightMarker.Instance.transform.GetComponentInChildren<OVRSkeleton>();
            _leftHandSkeleton = LeftMarker.Instance.transform.GetComponentInChildren<OVRSkeleton>();

            ToggleColliders(false, true);
            ToggleColliders(false, false);
            ToggleVisibility(false, true);
            ToggleVisibility(false, false);            
        }

        private void OnEnable()
        {
            AtomicInput.Instance.OnRightStateChanged += OnRightStateChanged;
            AtomicInput.Instance.OnRightRotationStateChanged += OnRightRotationStateChanged;

            AtomicInput.Instance.OnLeftStateChanged += OnLeftStateChanged;
            AtomicInput.Instance.OnLeftRotationStateChanged += OnLeftRotationStateChanged;

            AtomicModeController.Instance.RightModeChanged += OnRightModeChanged;
            AtomicModeController.Instance.LeftModeChanged += OnLeftModeChanged;
        }

        

        private void OnDisable()
        {
            if (AtomicInput.Instance != null)
            {
                AtomicInput.Instance.OnRightStateChanged -= OnRightStateChanged;
                AtomicInput.Instance.OnLeftStateChanged -= OnLeftStateChanged;
                AtomicInput.Instance.OnRightRotationStateChanged -= OnRightRotationStateChanged;
                AtomicInput.Instance.OnRightRotationStateChanged -= OnLeftRotationStateChanged;
            }

            if (AtomicModeController.Instance != null)
            {
                AtomicModeController.Instance.RightModeChanged -= OnRightModeChanged;
                AtomicModeController.Instance.LeftModeChanged -= OnLeftModeChanged;
            }
        }
        #endregion Init

        #region loops
        private void Update()
        {
            if (visibilityUnitTest)
            {
                visibilityUnitTest = false;
                ToggleVisibility(!_isVisible, true);
            }

            if (positionUdateUnitTest)
            {
                positionUdateUnitTest = false;
                UpdateCubePosition(true);
            }
        }
        #endregion loops

        #region event handlers
        private void OnRightRotationStateChanged(RotationState current, RotationState previous)
        {
            
        }

        private void OnLeftRotationStateChanged(RotationState current, RotationState previous)
        {
           
        }

        void OnRightStateChanged(HandGestureState newState, HandGestureState previousState)
        {
            HandlePerHandStateChange(true, newState, previousState);
        }

        void OnLeftStateChanged(HandGestureState newState, HandGestureState previousState)
        {
            HandlePerHandStateChange(false, newState, previousState);
        }

        private void OnRightModeChanged(TransformMode current, TransformMode previous)
        {
            
        }

        private void OnLeftModeChanged(TransformMode current, TransformMode previous)
        {
            
        }
        
        /// <summary>
        /// Called by CreationCubeTrigger when hand hovers into a node of the creation cube
        /// </summary>
        public void OnTriggerEnter(bool right, CreationMode hoveredMode, MeshRenderer highlightRenderer)
        {
            //Debug.Log(((right) ? " right ":" left ") + "hoveredMode: " + hoveredMode + " highlightRenderer: " + highlightRenderer.name);

            if (right) _rightCollisionTally++;
            else _leftCollisionTally++;
            if (right && _currentRightHoveredMode != hoveredMode)
            {
                _currentRightHoveredMode = hoveredMode;
                if (currentRightCreationMode != hoveredMode)
                    _currentRightHighlightRenderer = highlightRenderer;
            }
            else if (!right && _currentLeftHoveredMode != hoveredMode)
            {
                _currentLeftHoveredMode = hoveredMode;
                if (currentLeftCreationMode != hoveredMode)
                    _currentLeftHighlightRenderer = highlightRenderer;
            }
            highlightRenderer.enabled = true;
        }

        /// <summary>
        /// Called by CreationCubeTrigger when hand hovers out of a node of the creation cube
        /// </summary>
        public void OnTriggerExit(bool right, CreationMode hoveredMode, MeshRenderer highlightRenderer)
        {
            if (right && _rightCollisionTally > 0)
                _rightCollisionTally--;
            else if (!right && _leftCollisionTally > 0)
                _leftCollisionTally--;

            if (right && _rightCollisionTally == 0 && _currentRightHoveredMode != CreationMode.neutral && currentRightCreationMode != _currentRightHoveredMode)
            {
                _currentRightHoveredMode = CreationMode.neutral;
                _currentRightHighlightRenderer = null;
                highlightRenderer.enabled = false;
            }
            else if (!right && _leftCollisionTally == 0 && _currentLeftHoveredMode != CreationMode.neutral && currentLeftCreationMode != _currentLeftHoveredMode)
            {
                _currentLeftHoveredMode = CreationMode.neutral;
                _currentLeftHighlightRenderer = null;
                highlightRenderer.enabled = false;
            }
        }
        #endregion event handlers

        #region helpers
        private void HandlePerHandStateChange(bool right, HandGestureState newState, HandGestureState previous)
        {
            TransformMode mode = (right) ? AtomicModeController.Instance.currentRightMode : AtomicModeController.Instance.currentLeftMode;
            if (mode != TransformMode.Create) return;

            if (ShouldCreatePrimitive(right, newState, previous))
            //    _isDenoising = true;
            //else if (_isDenoising)
            //{
            //    if (DenoisingCriteriaMet(right, newState, previous))
            //    {
            //        if (_insertionNoiseTally < _insertionNoiseThreshhold)
            //        {
            //            _insertionNoiseTally++;
            //            return;
            //        }
            //        else
                    {
                        _insertionNoiseTally = 0;
                        _isDenoising = false;
                        OVRSkeleton currentHand = (right) ? _rightHandSkeleton : _leftHandSkeleton;
                        Vector3 thumbPos = currentHand.Bones[(int)BoneId.Hand_ThumbTip].Transform.position;
                        Quaternion newAtomLookRotation = Quaternion.LookRotation(AtomicHeadMarker.Instance.transform.position - thumbPos);
                        CreationMode creationMode = (right) ? currentRightCreationMode : currentLeftCreationMode;
                        AtomicSpatialObjectModel.Instance.CreateAtom(Instantiate(_creationPrimitivePrefabs[(int)creationMode].prefab, thumbPos, newAtomLookRotation));
                //    }
                //}
                //else {
                //    _insertionNoiseTally = 0;
                //    _isDenoising = false;
                //}
            }
            else if (ShouldToggleCreationCubeOn(right, newState, previous))
            {
                UpdateCubePosition(right);
                ToggleVisibility(true, right);
                ToggleColliders(true, right);
                if (right) AtomicModeController.Instance.rightCreationCubeActive = true;
                else AtomicModeController.Instance.leftCreationCubeActive = true;
            }
            else if (newState == HandGestureState.Insert)
                ActivateCurrentHover(right);
            else if (newState == HandGestureState.Stop)
            {
                ToggleVisibility(false, right);
                ToggleColliders(false, right);
                if (right) AtomicModeController.Instance.rightCreationCubeActive = false;
                else AtomicModeController.Instance.leftCreationCubeActive = false;
            }
        }

        private bool ShouldCreatePrimitive(bool right, HandGestureState newState, HandGestureState previous)
        {
            return
                (right && !AtomicModeController.Instance.rightCreationCubeInBounds && newState == HandGestureState.Insert && previous != HandGestureState.Insert) ||
                (!right && !AtomicModeController.Instance.leftCreationCubeInBounds && newState == HandGestureState.Insert && previous != HandGestureState.Insert);
        }

        private bool DenoisingCriteriaMet(bool right, HandGestureState newState, HandGestureState previous)
        {
            return (right && !AtomicModeController.Instance.rightCreationCubeInBounds && newState == HandGestureState.Insert) ||
                (!right && !AtomicModeController.Instance.leftCreationCubeInBounds && newState == HandGestureState.Insert);
        }

        private void ActivateCurrentHover(bool right)
        {
            if (right)
            {
                if (_currentRightSelectionRenderer != null)
                {
                    _currentRightSelectionRenderer.material = hoverMaterial;
                    _currentRightSelectionRenderer.enabled = false;
                }

                if (_currentRightHighlightRenderer != null)
                {
                    _currentRightHighlightRenderer.material = selectedMaterial;
                    _currentRightSelectionRenderer = _currentRightHighlightRenderer;
                }
                currentRightCreationMode = _currentRightHoveredMode;
            }
            else
            {
                if (_currentLeftSelectionRenderer != null)
                {
                    _currentLeftSelectionRenderer.material = hoverMaterial;
                    _currentLeftSelectionRenderer.enabled = false;
                }

                if (_currentLeftHighlightRenderer != null)
                {
                    _currentLeftHighlightRenderer.material = selectedMaterial;
                    _currentLeftSelectionRenderer = _currentLeftHighlightRenderer;
                }
                currentLeftCreationMode = _currentLeftHoveredMode;
            }
            
            CreationMode currentMode = (right) ? currentRightCreationMode : currentLeftCreationMode;
            
            // need to track selected state <----------------
            switch (currentMode)
            {
                // logic 
                case CreationMode.andLogicalOperators:
                    break;
                case CreationMode.orLogicalOperators:
                    break;
                case CreationMode.sameAsLogicalOperators:
                    break;
                case CreationMode.greaterThanOperators:
                    break;
                case CreationMode.greaterThanOrSameAsLogicalOperators:
                    break;
                case CreationMode.lessThanOperators:
                    break;
                case CreationMode.lessThanOrSameAsLogicalOperators:
                    break;

                // assignment
                case CreationMode.equalToOperators:
                    break;
                case CreationMode.functions:
                    break;
                
                // data
                case CreationMode.bytes:
                    break;
                case CreationMode.bools:
                    break;
                case CreationMode.numbers:
                    break;
                case CreationMode.strings:
                    break;
                case CreationMode.molecules:
                    break;

                // arithmetic
                case CreationMode.minusOperators:
                    break;
                case CreationMode.multiplicationOperators:
                    break;
                case CreationMode.plusOperators:
                    break;
                case CreationMode.divisionOperators:
                    break;
                    
                // execution
                case CreationMode.starts:
                    break;
                case CreationMode.frameLoops:
                    break;
                case CreationMode.branches:
                    break;
                case CreationMode.multiBranches:
                    break;
                case CreationMode.counters:
                    break;
                case CreationMode.repeats:
                    break;
                case CreationMode.timers:
                    break;
                    
                // other
                case CreationMode.views:
                    break;
                case CreationMode.neutral:
                    break;
            }
        }

        private void ToggleColliders(bool on, bool right)
        {
            Collider[] colliderList = (right) ? _rightCreationMenuColliders : _leftCreationMenuColliders;

            foreach (Collider collider in colliderList)
                collider.enabled = on;
        }

        private void ToggleVisibility(bool on, bool right)
        {
            List<MeshRenderer> rendererList = (right) ? _rightNonHighlightRenderers : _leftNonHighlightRenderers;
            CanvasGroup[] canvasGroups = (right) ? _rightCanvasGroups : _leftCanvasGroups;

            foreach (MeshRenderer renderer in rendererList)
                renderer.enabled = on;
            foreach (CanvasGroup canvasGroup in canvasGroups)
                canvasGroup.alpha = (on) ? 1 : 0;

            if (_currentRightHighlightRenderer != null)
                _currentRightHighlightRenderer.enabled = on;

            if (_currentLeftHighlightRenderer != null)
                _currentLeftHighlightRenderer.enabled = on;

            _isVisible = on;
        }

        private void UpdateCubePosition(bool right)
        {
            _creationCubePosition = AtomicHeadMarker.Instance.transform.position + AtomicHeadMarker.Instance.transform.forward * 0.4f;
            _creationCubePosition = new Vector3(_creationCubePosition.x, AtomicHeadMarker.Instance.transform.position.y, _creationCubePosition.z);
            Vector3 rightwardHorizontal = -1 * Vector3.Cross(AtomicHeadMarker.Instance.transform.forward, Vector3.up).normalized;
            _creationCubePosition += new Vector3(0, -0.4f, 0) + 0.1f * ((right) ? rightwardHorizontal : -1f * rightwardHorizontal) - rightwardHorizontal * 0.04f;
            if (right)
            {
                _rightCubeTransform.position = _creationCubePosition;
                _rightCubeTransform.rotation = Quaternion.Euler(new Vector3(0, Quaternion.LookRotation(AtomicHeadMarker.Instance.transform.forward).eulerAngles.y, 0));
            }
            else {
                _leftCubeTransform.position = _creationCubePosition;
                _leftCubeTransform.rotation = Quaternion.Euler(new Vector3(0, Quaternion.LookRotation(AtomicHeadMarker.Instance.transform.forward).eulerAngles.y, 0));
            }
            
        }
        
        private bool ShouldToggleCreationCubeOn(bool right, HandGestureState newState, HandGestureState previous)
        {
            if (newState == HandGestureState.DragSelection && previous == HandGestureState.Neutral &&
                ((right && !AtomicModeController.Instance.rightCreationCubeInBounds) ||
                    (!right && !AtomicModeController.Instance.leftCreationCubeInBounds)))
                return previous == HandGestureState.Neutral;
            else return false;
        }
            #endregion helpers
        }

    [Serializable]
    public enum CreationMode
    {
        neutral,
        numbers,
        strings,
        bytes,
        bools,
        molecules,  // primitive molecules with a variety of common elements
        plusOperators,
        minusOperators,
        multiplicationOperators,
        divisionOperators,
        equalToOperators,
        andLogicalOperators,
        orLogicalOperators,
        sameAsLogicalOperators,
        greaterThanOrSameAsLogicalOperators,
        lessThanOrSameAsLogicalOperators,
        greaterThanOperators,
        lessThanOperators,
        functions,
        branches,  // if
        multiBranches,// switch
        counters, // for 
        repeats,// while
        timers,
        views,
        starts,
        frameLoops
    }

    [Serializable]
    public struct SerializablePrimitivePrefab
    {
        public SerializablePrimitivePrefab(CreationMode _mode, GameObject _prefab)
        {
            mode = _mode;
            prefab = _prefab;
        }
        public CreationMode mode;
        public GameObject prefab;
    }
}

