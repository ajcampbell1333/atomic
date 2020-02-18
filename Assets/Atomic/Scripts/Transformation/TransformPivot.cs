using Atomic.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Atomic.Transformation
{
    public class TransformPivot : MonoBehaviour
    {
        #region private vars
        [Range(0, 1)] private float _velocityDampener = 0.1f;
        private float _currentInertia;
        private IEnumerator _inertia;
        private const float _inertiaRateOfDecay = 0.02f;
        private IEnumerator _angularInertia;
        private bool _isRightDragging, _isLeftDragging;
        private Vector3 _previousRightStickDirection, _previousLeftStickDirection;
        private Quaternion _previousRightRotation, _previousLeftRotation;
        private Vector3 _previousRightYaw, _previousLeftYaw;
        private float _previousRightPitch, _previousLeftPitch;
        private float _previousRightRoll, _previousLeftRoll;
        private float _pitchVelocity, _yawVelocity, _rollVelocity;
        private Queue<float> _priorCumulativePitches, _priorCumulativeYaws, _priorCumulativeRolls;
        private int accumulationHistoryAmount = 5;
        private Vector3 _currentRightPitchAxis, _currentLeftPitchAxis, _currentRightRollAxis, _currentLeftRollAxis;

        private Vector3 _originalScale;

        private bool _inertiaEnabled = false;
        #endregion private vars

        #region init

        //private void Awake()
        //{
        //    _matchingHand = (name.Contains("Right")) ? Hand.Right : Hand.Left;
        //}

        private void OnEnable()
        {
            _priorCumulativePitches = new Queue<float>();
            _priorCumulativeRolls = new Queue<float>();
            _priorCumulativeYaws = new Queue<float>();

            _previousRightYaw = Vector3.zero;
            _previousLeftYaw = Vector3.zero;

            TransformTranslationController.Instance.BeginTranslationDrag += OnBeginTranslationDrag;
            TransformTranslationController.Instance.EndTranslationDrag += OnEndTranslationDrag;
            TransformRotationController.Instance.BeginRotationDrag += OnBeginRotationDrag;
            TransformRotationController.Instance.EndRotationDrag += OnEndRotationDrag;
            TransformScaleController.Instance.BeginScaleDrag += OnBeginScaleDrag;
            TransformScaleController.Instance.EndScaleDrag += OnEndScaleDrag;
            TransformScaleController.Instance.BeginScaleHold += OnBeginScaleHold;
            TransformScaleController.Instance.EndScaleHold += OnEndScaleHold;

            _originalScale = new Vector3(0.1f, 0.1f, 0.1f);
            transform.localScale = _originalScale;
        }

        private void OnDisable()
        {
            if (TransformTranslationController.Instance != null)
            {
                TransformTranslationController.Instance.BeginTranslationDrag -= OnBeginTranslationDrag;
                TransformTranslationController.Instance.EndTranslationDrag -= OnEndTranslationDrag;
            }

            if (TransformRotationController.Instance != null)
            {
                TransformRotationController.Instance.BeginRotationDrag -= OnBeginRotationDrag;
                TransformRotationController.Instance.EndRotationDrag -= OnEndRotationDrag;
            }

            if (TransformScaleController.Instance != null)
            {
                TransformScaleController.Instance.BeginScaleDrag -= OnBeginScaleDrag;
                TransformScaleController.Instance.EndScaleDrag -= OnEndScaleDrag;
                TransformScaleController.Instance.BeginScaleHold -= OnBeginScaleHold;
                TransformScaleController.Instance.EndScaleHold -= OnEndScaleHold;
            }
        }
        #endregion init

        #region loops
        void Update()
        {
            if (_isRightDragging)
            {
                // translation
                if (IsModeActive(Hand.Right, TransformMode.Translate) || IsModeActive(Hand.Right, TransformMode.Pivot))
                {
                    transform.position = Vector3.Lerp(
                                        transform.position,
                                        transform.position + TransformTranslationController.Instance.rightStickVelocity,
                                        _velocityDampener
                                    );
                }

                // rotation
                if (AtomicModeController.Instance.HasFlag(AtomicModeController.Instance.currentRightMode, TransformMode.Rotate) &&
                    TransformRotationController.Instance.rightRotationGearEngaged)
                {
                    _currentRightPitchAxis = Vector3.Cross(TransformRotationController.Instance.rightStickDirection, TransformRotationController.Instance.currentRightUpAxis);
                    Vector3 undampenedNewYaw = Quaternion.AngleAxis(90f, TransformRotationController.Instance.currentRightUpAxis) * _currentRightPitchAxis;
                    Vector3 yawTarget = Vector3.zero;

                    float undampenedNewPitch = Vector3.Angle(TransformRotationController.Instance.currentRightUpAxis, TransformRotationController.Instance.rightStickDirection);
                    float undampenedNewRoll = RightMarker.Instance.transform.parent.localEulerAngles.z;
                    
                    if (_previousRightYaw != Vector3.zero)
                    {
                        if (!TransformRotationController.Instance.singleAxisMode)
                        {
                            // prepare pitch data
                            float pitchDelta = 0.9f * ((undampenedNewPitch > _previousRightPitch) ? undampenedNewPitch - _previousRightPitch : _previousRightPitch - undampenedNewPitch);
                            float pitchSign = (undampenedNewPitch < _previousRightPitch) ? 1 : -1;
                            _pitchVelocity = pitchSign * pitchDelta;

                            // prepare roll data
                            _rollVelocity = Mathf.DeltaAngle(_previousRightRoll, undampenedNewRoll) * 0.2f; // ((undampenedNewRoll > _previousRightRoll) ? undampenedNewRoll - _previousRightRoll : _previousRightRoll - undampenedNewRoll) * 0.3 f;
                            float rollSign = (undampenedNewRoll > _previousRightRoll) ? 1 : -1;
                            _currentRightRollAxis = TransformRotationController.Instance.currentPivot.position - AtomicModeController.Instance.rightCam.transform.position;
                        }

                        // prepare yaw data
                        Vector3 previousOrtho = Vector3.Cross(_previousRightYaw, TransformRotationController.Instance.currentRightUpAxis);
                        yawTarget = Vector3.Slerp(_previousRightYaw, undampenedNewYaw, _velocityDampener * 0.5f);
                        float angleDeltaAroundAxis = -1 * Vector3.Angle(_previousRightYaw, yawTarget);
                        int yawSign = (Vector3.Dot(previousOrtho, undampenedNewYaw) > 0) ? 1 : -1;
                        _yawVelocity = yawSign * angleDeltaAroundAxis;

                        if (!TransformRotationController.Instance.singleAxisMode)
                        {
                            PrepareVelocityHistoryData(ref _pitchVelocity, ref _priorCumulativePitches);
                            PrepareVelocityHistoryData(ref _rollVelocity, ref _priorCumulativeRolls);
                        }
                        PrepareVelocityHistoryData(ref _yawVelocity, ref _priorCumulativeYaws);

                        // apply the changes to the transform
                        transform.Rotate(TransformRotationController.Instance.currentRightUpAxis, GetAverageVelocity(ref _priorCumulativeYaws), Space.World);
                        if (!TransformRotationController.Instance.singleAxisMode)
                        {
                            transform.Rotate(_currentRightPitchAxis, GetAverageVelocity(ref _priorCumulativePitches), Space.World);
                            transform.Rotate(_currentRightRollAxis, GetAverageVelocity(ref _priorCumulativeRolls), Space.World);
                        }
                        undampenedNewRoll = _previousRightRoll + _rollVelocity;
                    }
                    
                    // store values for comparison to the hand's new position next frame
                    if (!TransformRotationController.Instance.singleAxisMode)
                    {
                        _previousRightPitch = undampenedNewPitch;
                        _previousRightRoll = undampenedNewRoll;
                    }
                    _previousRightYaw = (yawTarget != Vector3.zero) ? yawTarget : undampenedNewYaw;
                }
                else if (_previousRightYaw != Vector3.zero)
                {
                    // reset pitch and yaw on release
                    _previousRightYaw = Vector3.zero;
                    _previousRightPitch = 0;
                    _previousRightRoll = 0;
                    ClearAverageVelocityCache();
                }

            }

            if (_isLeftDragging)
            {
                if (IsModeActive(Hand.Left, TransformMode.Translate) || IsModeActive(Hand.Left, TransformMode.Pivot))
                {
                    transform.position = Vector3.Lerp(
                                        transform.position,
                                        transform.position + TransformTranslationController.Instance.leftStickVelocity,
                                        _velocityDampener
                                    );
                }

                if (AtomicModeController.Instance.HasFlag(AtomicModeController.Instance.currentLeftMode, TransformMode.Rotate) &&
                    TransformRotationController.Instance.leftRotationGearEngaged)
                {
                    _currentLeftPitchAxis = Vector3.Cross(TransformRotationController.Instance.leftStickDirection, TransformRotationController.Instance.currentLeftUpAxis);
                    Vector3 undampenedNewYaw = Quaternion.AngleAxis(90f, TransformRotationController.Instance.currentLeftUpAxis) * _currentLeftPitchAxis;
                    float undampenedNewPitch = Vector3.Angle(TransformRotationController.Instance.currentLeftUpAxis, TransformRotationController.Instance.leftStickDirection);
                    float undampenedNewRoll = LeftMarker.Instance.transform.parent.localEulerAngles.z;

                    Vector3 yawTarget = Vector3.zero;

                    if (_previousLeftYaw != Vector3.zero)
                    {
                        if (!TransformRotationController.Instance.singleAxisMode)
                        {
                            // prepare pitch data
                            float pitchDelta = 0.9f * ((undampenedNewPitch > _previousLeftPitch) ? undampenedNewPitch - _previousLeftPitch : _previousLeftPitch - undampenedNewPitch);
                            float pitchSign = (undampenedNewPitch < _previousLeftPitch) ? 1 : -1;
                            _pitchVelocity = pitchSign * pitchDelta;

                            // prepare roll data
                            _rollVelocity = Mathf.DeltaAngle(_previousLeftRoll, undampenedNewRoll) * 0.2f; // ((undampenedNewRoll > _previousLeftRoll) ? undampenedNewRoll - _previousLeftRoll : _previousLeftRoll - undampenedNewRoll) * 0.3 f;
                            float rollSign = (undampenedNewRoll > _previousLeftRoll) ? 1 : -1;
                            _currentLeftRollAxis = TransformRotationController.Instance.currentPivot.position - AtomicModeController.Instance.rightCam.transform.position;
                        }

                        // prepare yaw data
                        Vector3 previousOrtho = Vector3.Cross(_previousLeftYaw, TransformRotationController.Instance.currentLeftUpAxis);
                        yawTarget = Vector3.Slerp(_previousLeftYaw, undampenedNewYaw, _velocityDampener * 0.5f);
                        float angleDeltaAroundAxis = -1 * Vector3.Angle(_previousLeftYaw, yawTarget);
                        int yawSign = (Vector3.Dot(previousOrtho, undampenedNewYaw) > 0) ? 1 : -1;
                        _yawVelocity = yawSign * angleDeltaAroundAxis;

                        if (!TransformRotationController.Instance.singleAxisMode)
                        {
                            PrepareVelocityHistoryData(ref _pitchVelocity, ref _priorCumulativePitches);
                            PrepareVelocityHistoryData(ref _rollVelocity, ref _priorCumulativeRolls);
                        }
                        PrepareVelocityHistoryData(ref _yawVelocity, ref _priorCumulativeYaws);

                        // apply the changes to the transform
                        transform.Rotate(TransformRotationController.Instance.currentLeftUpAxis, GetAverageVelocity(ref _priorCumulativeYaws), Space.World);
                        if (!TransformRotationController.Instance.singleAxisMode)
                        {
                            transform.Rotate(_currentLeftPitchAxis, GetAverageVelocity(ref _priorCumulativePitches), Space.World);
                            transform.Rotate(_currentLeftRollAxis, GetAverageVelocity(ref _priorCumulativeRolls), Space.World);
                            undampenedNewRoll = _previousLeftRoll + _rollVelocity;
                        }
                        
                    }

                    // store values for comparison to the hand's new position next frame


                    _previousLeftPitch = undampenedNewPitch;
                    _previousLeftRoll = undampenedNewRoll;
                    _previousLeftYaw = (yawTarget != Vector3.zero) ? yawTarget : undampenedNewYaw;
                }
                else if (_previousLeftYaw != Vector3.zero)
                {
                    // reset pitch and yaw on release
                    _previousLeftYaw = Vector3.zero;
                    _previousLeftPitch = 0;
                    _previousLeftRoll = 0;
                    ClearAverageVelocityCache();
                }
            }
        }

        private IEnumerator Inertia(Vector3 startingVelocity)
        {
            Vector3 velocity = startingVelocity;
            while (velocity.magnitude > 0.0001f)
            {
                transform.position = Vector3.Lerp(
                                        transform.position,
                                        transform.position + velocity * Time.deltaTime * 100,
                                        _velocityDampener
                                    );
                velocity *= (1 - _inertiaRateOfDecay);

                yield return new WaitForEndOfFrame();
            }
        }

        private bool StillMoving()
        {
            return _pitchVelocity > 0.1f || _rollVelocity > 0.1f || _yawVelocity > 0.001f;
        }

        private IEnumerator AngularInertia(bool right)
        {
            while (StillMoving())
            {
                if (right)
                {
                    transform.Rotate(TransformRotationController.Instance.currentRightUpAxis, _yawVelocity, Space.World);
                    transform.Rotate(_currentRightPitchAxis, _pitchVelocity, Space.World);
                    transform.Rotate(_currentRightRollAxis, _rollVelocity, Space.World);
                }
                else
                {
                    transform.Rotate(TransformRotationController.Instance.currentLeftUpAxis, _yawVelocity, Space.World);
                    transform.Rotate(_currentLeftPitchAxis, _pitchVelocity, Space.World);
                    transform.Rotate(_currentLeftRollAxis, _rollVelocity, Space.World);
                }
                _yawVelocity *= (1 - TransformRotationController.angularInertialRateOfDecay);
                _pitchVelocity *= (1 - TransformRotationController.angularInertialRateOfDecay);
                _rollVelocity *= (1 - TransformRotationController.angularInertialRateOfDecay);
                yield return new WaitForEndOfFrame();
            }
        }
        #endregion loops

        #region event handlers
        //public void OnBeginDrag(Hand hand)
        //{
        //    Debug.Log("listener received drag begin");
        //    InterruptInertia();
        //    if (hand == Hand.Right)
        //        _isRightDragging = true;
        //    else _isLeftDragging = true;
        //}

        //public void OnEndDrag(Hand hand)
        //{
        //    if (hand == Hand.Right)
        //    {
        //        _isRightDragging = false;
        //        if (AtomicModeController.Instance.HasFlag(AtomicModeController.Instance.currentRightMode, TransformMode.Translate))
        //            BeginInertia(TransformTranslationController.Instance.rightStickVelocity);

        //        if (AtomicModeController.Instance.HasFlag(AtomicModeController.Instance.currentRightMode, TransformMode.Rotate))
        //            BeginAngularInertia(true);
        //    }
        //    else
        //    {
        //        _isLeftDragging = false;
        //        if (AtomicModeController.Instance.HasFlag(AtomicModeController.Instance.currentRightMode, TransformMode.Translate))
        //            BeginInertia(TransformTranslationController.Instance.leftStickVelocity);

        //        if (AtomicModeController.Instance.HasFlag(AtomicModeController.Instance.currentRightMode, TransformMode.Rotate))
        //            BeginAngularInertia(false);
        //    }
        //}

        private void OnBeginTranslationDrag(Hand hand)
        {
            //if (hand != _matchingHand) return;
            Debug.Log("listener received translation drag begin");
            InterruptInertia();
            if (hand == Hand.Right)
                _isRightDragging = true;
            else _isLeftDragging = true;
        }

        private void OnEndTranslationDrag(Hand hand)
        {
            // if (hand != _matchingHand) return;
            if (hand == Hand.Right)
            {
                _isRightDragging = false;
                if (_inertiaEnabled && AtomicModeController.Instance.HasFlag(AtomicModeController.Instance.currentRightMode, TransformMode.Translate))
                    BeginInertia(TransformTranslationController.Instance.rightStickVelocity);
            }
            else
            {
                _isLeftDragging = false;
                if (_inertiaEnabled && AtomicModeController.Instance.HasFlag(AtomicModeController.Instance.currentLeftMode, TransformMode.Translate))
                    BeginInertia(TransformTranslationController.Instance.leftStickVelocity);
            }
        }

        private void OnBeginRotationDrag(Hand hand)
        {
            // if (hand != _matchingHand) return;
            Debug.Log("listener received rotation drag begin");
            InterruptInertia();
            if (hand == Hand.Right)
                _isRightDragging = true;
            else _isLeftDragging = true;
        }

        private void OnEndRotationDrag(Hand hand)
        {
            // if (hand != _matchingHand) return;
            if (hand == Hand.Right)
            {
                _isRightDragging = false;
                if (AtomicModeController.Instance.HasFlag(AtomicModeController.Instance.currentRightMode, TransformMode.Rotate)
                    && VecolityIsAboveStartingThreshold())
                    BeginAngularInertia(true);
            }
            else
            {
                _isLeftDragging = false;
                if (AtomicModeController.Instance.HasFlag(AtomicModeController.Instance.currentLeftMode, TransformMode.Rotate)
                    && VecolityIsAboveStartingThreshold())
                    BeginAngularInertia(false);
            }
        }

        private void OnBeginScaleDrag(Hand hand)
        {
            //  if (hand != _matchingHand) return;
            Debug.Log("listener received translation drag begin");
            InterruptInertia();
            if (hand == Hand.Right)
                _isRightDragging = true;
            else _isLeftDragging = true;
        }

        private void OnEndScaleDrag(Hand hand)
        {
            // if (hand != _matchingHand) return;
            if (hand == Hand.Right)
                _isRightDragging = false;
            else _isLeftDragging = false;

        }

        private void OnBeginScaleHold(Hand hand)
        {


        }
        private void OnEndScaleHold(Hand hand)
        {
            AtomicSelection.Instance.ResetPivotScale(_originalScale);
        }

        #endregion event handlers

        #region helper methods
        private bool VecolityIsAboveStartingThreshold()
        {
            return _yawVelocity > 1 || _pitchVelocity > 1 || _rollVelocity > 1;
        }

        private void BeginInertia(Vector3 startingVelocity)
        {
            InterruptInertia();
            _inertia = Inertia(startingVelocity);
            StartCoroutine(_inertia);
        }

        private void InterruptInertia()
        {
            if (_inertia != null)
                StopCoroutine(_inertia);
        }

        private void BeginAngularInertia(bool right)
        {
            InterruptAngularInertia();
            _angularInertia = AngularInertia(right);
            StartCoroutine(_angularInertia);
        }

        private void InterruptAngularInertia()
        {
            if (_angularInertia != null)
                StopCoroutine(_angularInertia);
        }

        private void PrepareVelocityHistoryData(ref float newVelocity, ref Queue<float> vHistory)
        {
            while (vHistory.Count >= accumulationHistoryAmount)
                vHistory.Dequeue();

            vHistory.Enqueue(newVelocity);
        }

        private float GetAverageVelocity(ref Queue<float> vHistory)
        {
            float total = 0;

            while (vHistory.Count > accumulationHistoryAmount)
                vHistory.Dequeue();

            foreach (float velocity in vHistory)
                total += velocity;

            return total / accumulationHistoryAmount;
        }

        private void ClearAverageVelocityCache()
        {
            _priorCumulativePitches.Clear();
            _priorCumulativeRolls.Clear();
            _priorCumulativeYaws.Clear();
        }

        private bool IsModeActive(Hand hand, TransformMode mode)
        {
            return AtomicModeController.Instance.HasFlag((hand == Hand.Right) ? AtomicModeController.Instance.currentRightMode : AtomicModeController.Instance.currentLeftMode, mode);
        }
        #endregion helper methods
    }
}

