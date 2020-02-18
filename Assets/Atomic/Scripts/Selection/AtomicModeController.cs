using Atomic.Input;
using Atomic.Transformation;
using cakeslice;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static OVRSkeleton;

namespace Atomic
{
    public class AtomicModeController : Singleton<AtomicModeController>
    {
        #region public vars
        public TransformMode currentRightMode = TransformMode.Translate;
        public TransformMode currentLeftMode = TransformMode.Translate;
        public UnityAction<TransformMode, TransformMode> RightModeChanged, LeftModeChanged;
        [HideInInspector] public Camera leftCam, rightCam;
        [HideInInspector] public bool rightTextUIActive, leftTextUIActive, rightTextUIInBounds, leftTextUIInBounds;
        [HideInInspector] public bool rightCreationCubeActive, leftCreationCubeActive, rightCreationCubeInBounds, leftCreationCubeInBounds;
        #endregion public vars

        #region private vars
        private AtomicInput _input;
        private RightMarker _rightHandMarker;
        private LeftMarker _leftHandMarker;
        private bool _rightModeMenuActive, _leftModeMenuActive;
        private TransformMode _potentialRightMode, _potentialLeftMode;
        private List<TransformationModeHighlight> _rightHighlights, _leftHighlights;
        private Collider[] _rightColliders, _leftColliders;
        private List<MeshRenderer> _rightMainRenderers, _leftMainRenderers;
        private Text[] _rightTextObjects, _leftTextObjects;
        private CanvasGroup _rightLabelGroup, _leftLabelGroup;
        private Transform _rightPositionMarker, _leftPositionMarker;
        [SerializeField] private Transform _rightModePointer, _leftModePointer;
        private OVRSkeleton _rightHandSkeleton, _leftHandSkeleton;
        private OVRHand _rightHand, _leftHand;

        private Vector3 _rightIndexProxKnuckleDirection, _leftIndexProxKnuckleDirection;
        private Vector3 _leftKnuckleLineDirection, _rightKnuckleLineDirection;
        private Vector3 _leftPalmDirection, _rightPalmDirection;
        private Vector3 _leftPalmNormal, _rightPalmNormal;
        private float _rightDialDirection, _leftDialDirection;


        [SerializeField] Text _rightDebugCanvas, _leftDebugCanvas;
        [SerializeField] private bool _debuggingEnabled;
        #endregion private vars

        #region init
        private void Awake()
        {
            _input = AtomicInput.Instance;
            _rightHandMarker = RightMarker.Instance;
            _leftHandMarker = LeftMarker.Instance;

            _rightHandSkeleton = _rightHandMarker.transform.GetComponentInChildren<OVRSkeleton>();
            _leftHandSkeleton = _leftHandMarker.transform.GetComponentInChildren<OVRSkeleton>();

            _rightHand = _rightHandMarker.transform.GetComponentInChildren<OVRHand>();
            _leftHand = _leftHandMarker.transform.GetComponentInChildren<OVRHand>();

            _rightHighlights = transform.GetChild(0).GetComponentsInChildren<TransformationModeHighlight>(true).ToList();
            _leftHighlights = transform.GetChild(1).GetComponentsInChildren<TransformationModeHighlight>(true).ToList();

            _rightColliders = transform.GetChild(0).GetComponentsInChildren<Collider>(true);
            _leftColliders = transform.GetChild(1).GetComponentsInChildren<Collider>(true);

            _rightMainRenderers = new List<MeshRenderer>();
            _leftMainRenderers = new List<MeshRenderer>();

            MeshRenderer[] allRightRenderers = transform.GetChild(0).GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer renderer in allRightRenderers)
                if (renderer.GetComponent<TransformationModeHighlight>() == null)
                    _rightMainRenderers.Add(renderer);

            MeshRenderer[] allLeftRenderers = transform.GetChild(1).GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer renderer in allLeftRenderers)
                if (renderer.GetComponent<TransformationModeHighlight>() == null)
                    _leftMainRenderers.Add(renderer);

            _rightTextObjects = transform.GetChild(0).GetComponentsInChildren<Text>();
            _leftTextObjects = transform.GetChild(1).GetComponentsInChildren<Text>();

            _rightLabelGroup = transform.GetChild(0).GetComponentInChildren<CanvasGroup>();
            _leftLabelGroup = transform.GetChild(1).GetComponentInChildren<CanvasGroup>();

            _rightPositionMarker = transform.GetChild(0);
            _leftPositionMarker = transform.GetChild(1);

            InitializeHighlightCapabilityOnBothCameras();
        }

        private void InitializeHighlightCapabilityOnBothCameras()
        {
            // if you're run an Oculus device, ensure that (OVRCameraRig.usePerEyeCameras == true)
            foreach (Camera cam in Camera.allCameras)
            {
                if (cam.stereoTargetEye == StereoTargetEyeMask.Left)
                    leftCam = cam;
                if (cam.stereoTargetEye == StereoTargetEyeMask.Right)
                    rightCam = cam;
            }
            if (leftCam == null || rightCam == null)
                Debug.LogError("Unity is not rendering both eyes at the moment.");
            else
            {
                OutlineEffect leftEyeHighlightEffect = leftCam.gameObject.AddComponent<OutlineEffect>();
                leftEyeHighlightEffect.sourceCamera = leftCam;
                OutlineEffect rightEyeHighlightEffect = rightCam.gameObject.AddComponent<OutlineEffect>();
                rightEyeHighlightEffect.sourceCamera = rightCam;
            }
        }

        private void Start()
        {
            // hide both menu gizmos
            ToggleVisibility(false, true);
            ToggleColliders(false, true);
            ToggleVisibility(false, false);
            ToggleColliders(false, false);
        }

        private void OnEnable()
        {
            _input.OnRightStateChanged += OnRightControllerStateChanged;
            _input.OnLeftStateChanged += OnLeftControllerStateChanged;
        }

        private void OnDisable()
        {
            _input.OnRightStateChanged -= OnRightControllerStateChanged;
            _input.OnLeftStateChanged -= OnLeftControllerStateChanged;
        }
        #endregion init

        #region loops and timers
        private void Update()
        {
            UpdateRight();
            UpdateLeft();
        }

        void UpdateRight()
        {
            if (!_rightModeMenuActive) return;

            _rightKnuckleLineDirection = (_rightHandSkeleton.Bones[(int)BoneId.Hand_Index1].Transform.position - _rightHandSkeleton.Bones[(int)BoneId.Hand_Pinky1].Transform.position).normalized;
            _rightPalmDirection = (_rightHandSkeleton.Bones[(int)BoneId.Hand_WristRoot].Transform.position - _rightHandSkeleton.Bones[(int)BoneId.Hand_Middle1].Transform.position).normalized;
            _rightPalmNormal = -1 * Vector3.Cross(_rightKnuckleLineDirection, _rightPalmDirection);
            _rightDialDirection = -1 * Vector3.Dot(Vector3.up, _rightPalmNormal);
            _rightModePointer.rotation = Quaternion.Euler(_rightModePointer.eulerAngles.x, _rightModePointer.eulerAngles.y, -90f * _rightDialDirection);

            if (_potentialRightMode != GetCurrentZone(_rightDialDirection))
            {
                _potentialRightMode = GetCurrentZone(_rightDialDirection);
                RefreshHighlights(true);
            }

            //if (_debuggingEnabled)
            //    _rightDebugCanvas.text = "_rightDialDirection: " + _rightDialDirection;
        }

        void UpdateLeft()
        {
            if (!_leftModeMenuActive) return;
            _leftKnuckleLineDirection = (_leftHandSkeleton.Bones[(int)BoneId.Hand_Index1].Transform.position - _leftHandSkeleton.Bones[(int)BoneId.Hand_Pinky1].Transform.position).normalized;
            _leftPalmDirection = (_leftHandSkeleton.Bones[(int)BoneId.Hand_WristRoot].Transform.position - _leftHandSkeleton.Bones[(int)BoneId.Hand_Middle1].Transform.position).normalized;
            _leftPalmNormal = Vector3.Cross(_leftKnuckleLineDirection, _leftPalmDirection);
            _leftDialDirection = -1 * Vector3.Dot(Vector3.up, _leftPalmNormal);
            _leftModePointer.rotation = Quaternion.Euler(_leftModePointer.eulerAngles.x, _leftModePointer.eulerAngles.y, 90f * _leftDialDirection + 180);

            if (_potentialLeftMode != GetCurrentZone(_leftDialDirection))
            {
                _potentialLeftMode = GetCurrentZone(_leftDialDirection);
                RefreshHighlights(false);
            }

            if (_debuggingEnabled)
                _leftDebugCanvas.text = "_rightDialDirection: " + _leftDialDirection;
        }
        #endregion loops and timers

        #region event handlers
        private void OnLeftControllerStateChanged(HandGestureState current, HandGestureState previous)
        {
            if (current == HandGestureState.SqueezeAll && !_leftModeMenuActive)
            {
                _leftModeMenuActive = true;
                _potentialLeftMode = currentLeftMode;
                _leftPositionMarker.position = _leftHandMarker.transform.position + _leftHand.PointerPose.forward*0.2f;
                _leftPositionMarker.LookAt(new Vector3(rightCam.transform.position.x, _leftPositionMarker.position.y, rightCam.transform.position.z));
                ToggleVisibility(true, false);
            }
            else if (current != HandGestureState.SqueezeAll && _leftModeMenuActive)
            {
                _leftModeMenuActive = false;
                TransformMode previousMode = currentLeftMode;
                currentLeftMode = _potentialLeftMode;
                LeftModeChanged?.Invoke(currentLeftMode, previousMode);
                ToggleVisibility(false, false);
            }
        }

        private void OnRightControllerStateChanged(HandGestureState current, HandGestureState previous)
        {
            if (current == HandGestureState.SqueezeAll && !_rightModeMenuActive)
            {
                _rightModeMenuActive = true;
                _potentialRightMode = currentRightMode;
                _rightPositionMarker.position = _rightHandMarker.transform.position + _rightHand.PointerPose.forward * 0.2f;
                _rightPositionMarker.LookAt(new Vector3(rightCam.transform.position.x,_rightPositionMarker.position.y,rightCam.transform.position.z));
                ToggleVisibility(true, true);
            }
            else if (current != HandGestureState.SqueezeAll && _rightModeMenuActive)
            {
                _rightModeMenuActive = false;
                TransformMode previousMode = currentRightMode;
                currentRightMode = _potentialRightMode;
                RightModeChanged?.Invoke(currentRightMode, previousMode);
                ToggleVisibility(false, true);
            }
        }
        #endregion eventhandlers

        #region helper methods
        public bool HasFlag(TransformMode a, TransformMode b)
        {
            return (a & b) == b;
        }

        private TransformMode GetCurrentZone(float dotRotation)
        {
            if (dotRotation > 0.7f)
                return TransformMode.Create;
            else if (dotRotation > 0.3f)
                return TransformMode.Rotate;
            else if (dotRotation > -0.3f)
                return TransformMode.Translate;
            else if (dotRotation > -0.7f)
                return TransformMode.Scale;
            else return TransformMode.Text;
        }

        private void RefreshHighlights(bool right)
        {
            if (right)
            {
                foreach (TransformationModeHighlight highlight in _rightHighlights)
                    if (highlight.hRenderer.enabled)
                        highlight.hRenderer.enabled = false;
                _rightHighlights[GetHighlightIndex(_potentialRightMode)].hRenderer.enabled = true;
            }
            else
            {
                foreach (TransformationModeHighlight highlight in _leftHighlights)
                    if (highlight.hRenderer.enabled)
                        highlight.hRenderer.enabled = false;
                _leftHighlights[GetHighlightIndex(_potentialLeftMode)].hRenderer.enabled = true;
            }
        }

        private int GetHighlightIndex(TransformMode mode)
        {
            switch (mode)
            {
                case TransformMode.Create: return 0;
                case TransformMode.Rotate: return 1;
                case TransformMode.Translate: return 2;
                case TransformMode.Scale: return 3;
                case TransformMode.Text: return 4;
                default: return 2;
            }
        }

        private void ToggleVisibility(bool on, bool right)
        {
            if (right)
            {
                foreach (MeshRenderer renderer in _rightMainRenderers)
                    renderer.enabled = on;
                //foreach (Text textObject in _rightTextObjects)
                //    textObject.enabled = on;
                _rightHighlights[GetHighlightIndex(currentRightMode)].hRenderer.enabled = on;
                _rightLabelGroup.alpha = (on) ? 1 : 0;
            }
            else
            {
                foreach (MeshRenderer renderer in _leftMainRenderers)
                    renderer.enabled = on;
                //foreach (Text textObject in _leftTextObjects)
                //    textObject.enabled = on;
                _leftHighlights[GetHighlightIndex(currentLeftMode)].hRenderer.enabled = on;
                _leftLabelGroup.alpha = (on) ? 1 : 0;
            }
        }

        private void ToggleColliders(bool on, bool right)
        {
            if (right)
            {
                if (_rightColliders.Length > 0)
                    foreach (Collider collider in _rightColliders)
                        collider.enabled = on;
            }
            else
            {
                if (_leftColliders.Length > 0)
                    foreach (Collider collider in _leftColliders)
                        collider.enabled = on;
            }
        }
        #endregion helper methods
    }
}


