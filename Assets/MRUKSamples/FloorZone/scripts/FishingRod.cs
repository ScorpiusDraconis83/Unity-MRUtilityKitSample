// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR.MRUtilityKitSamples.HandInput;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MRUtilityKitSample.FindFloorZone
{
    /// <summary>
    /// Controls the fishing rod mechanics including line tension, fish hooking, and controller input handling.
    /// Manages the interaction between the fishing rod, floater, and hooked fish.
    /// </summary>
    [MetaCodeSample("MRUK-FindFloorZone")]
    public class FishingRod : MonoBehaviour
    {
        private const float DEADZONE = 0.1f; // Deadzone to ignore small movements
        [SerializeField] private AimConstraint _rodBase, _rodMid, _rodEnd;
        [SerializeField] private int _rodLayerIndex;
        public Transform _rodTip, _rodTipUndeformed;
        public Transform _floater, _floaterInWater;
        public Transform _activeFloater;
        public bool _isFloaterInWater;
        public float _pullingOutTimer = .1f;
        public float _floaterInWaterAttractionForce = 10;

        [SerializeField] private Transform _stringRoll;
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private float _maxStringLength = 10f;
        [SerializeField] private float _buttonReelSpeed = 0.4f;
        [SerializeField] private float _microGestureReelSpeed = 1.0f;

        [Header("Hand Tracking")]
        [Tooltip("Reference to the right hand OVRSkeleton for getting finger bone positions.")]
        [SerializeField] private OVRSkeleton _rightHandSkeleton;
        [Tooltip("Rotation offset applied to the Handle's ParentConstraint when using hands.")]
        [SerializeField] private Vector3 _handModeRotationOffset = new Vector3(0f, -35f, -90f);

        [Tooltip("Reference to the Handle's ParentConstraint (auto-found if not assigned).")]
        [SerializeField] private ParentConstraint _handleParentConstraint;

        [SerializeField] private float _tension = 0.1f;
        public Fish _fishHooked;
        public float _stringGiven = .1f;
        public Slider StaminaSlider;

        public OVRInput.Controller controller = OVRInput.Controller.LTouch;
        [SerializeField] private AnimationCurve _vibrationCurve;
        [SerializeField] private AudioClip _stretchSound;
        [SerializeField] private AudioClip _breakingSound;
        [SerializeField] private AudioClip[] _plickInTheWaterSound;
        [SerializeField] private AudioClip[] _perturbedWaterSound;
        private readonly float _pullingOutTimerStart = .1f;
        private ConfigurableJoint _configurableJoint;
        private float _distance;
        private float _stamina = 1;

        private Rigidbody _floaterInWaterRigidbody;
        private Rigidbody _floaterRigidbody;
        private Transform _floaterInWaterTransform;
        private Transform _floaterTransform;
        private Transform _rodTipTransform;
        private Transform _rodTipUndeformedTransform;

        private Ray _tempRay;
        private ConstraintSource _tempConstraintSource;
        private float _previousAngle;
        private bool _wasInDeadzone = true;

        private float _previousPinchAngle;
        private bool _wasPinchInDeadzone = true;

        private Quaternion _controllerModeLocalRotation;
        private bool _isHandMode;
        private Vector3 _controllerModeConstraintRotationOffset;

        private Camera _mainCamera;
        private OVRCameraRig _cameraRig;
        private OVRPlugin.HandState _handState;
        private Coroutine _moveHandleCoroutine;

        [Header("Audio")] private AudioSource _audioSource;

        private float Stamina
        {
            get => _stamina;
            set
            {
                _stamina = value;
                if (StaminaSlider != null)
                {
                    StaminaSlider.value = value;
                }
            }
        }



        private void Start()
        {
            _audioSource = GetComponent<AudioSource>();
            _configurableJoint = _floater.GetComponent<ConfigurableJoint>();
            _floaterInWaterRigidbody = _floaterInWater.GetComponent<Rigidbody>();
            _floaterRigidbody = _floater.GetComponent<Rigidbody>();

            _floaterInWaterTransform = _floaterInWater.transform;
            _floaterTransform = _floater.transform;
            _rodTipTransform = _rodTip;
            _rodTipUndeformedTransform = _rodTipUndeformed;

            _tempConstraintSource = new ConstraintSource();

            _configurableJoint.anchor = Vector3.up * _stringGiven;
            _activeFloater = _floater;
            _isFloaterInWater = false;
            _pullingOutTimer = _pullingOutTimerStart;

            _mainCamera = Camera.main;
            _controllerModeLocalRotation = transform.localRotation;

            // Find the Handle's ParentConstraint if not assigned
            if (_handleParentConstraint == null)
            {
                _handleParentConstraint = GetComponentInChildren<ParentConstraint>();
            }

            // Store the original constraint rotation offset for controller mode
            if (_handleParentConstraint != null && _handleParentConstraint.sourceCount > 0)
            {
                _controllerModeConstraintRotationOffset = _handleParentConstraint.GetRotationOffset(0);
            }

            HandInputManager.Instance.OnInputModeChanged.AddListener(OnInputModeChanged);
            OnInputModeChanged(HandInputManager.Instance.CurrentInputMode);
        }

        private void OnDestroy()
        {
            if (HandInputManager.Instance != null)
            {
                HandInputManager.Instance.OnInputModeChanged.RemoveListener(OnInputModeChanged);
            }
        }

        private void OnInputModeChanged(InputMode mode)
        {
            _isHandMode = mode == InputMode.Hands;
            if (_isHandMode)
            {
                _handleParentConstraint.enabled = false;
                RepositionRodHandleInHandsMode();
            }
            else
            {
                _handleParentConstraint.enabled = true;
            }
        }
        private void RepositionRodHandleInHandsMode()
        {
            if (!_isHandMode)
            {
                return;
            }
            else
            {
                var camTransform = _mainCamera.transform;
                Vector3 targetRestPos = camTransform.position +
                                        Vector3.ProjectOnPlane(camTransform.forward, Vector3.up).normalized * .2f +
                                        camTransform.up * -.1f +
                                        camTransform.right * -.2f;
                if (_moveHandleCoroutine != null)
                {
                    StopCoroutine(_moveHandleCoroutine);
                }
                var flatForward = Vector3.ProjectOnPlane(camTransform.forward, Vector3.up).normalized;
                _moveHandleCoroutine = StartCoroutine(MoveHandleToPosition(targetRestPos, Quaternion.LookRotation(flatForward, Vector3.up), 0.5f));
            }
        }
        private void CheckForBringHandleInReach()
        {
            if (_isHandMode && Vector3.SqrMagnitude(_handleParentConstraint.transform.position - _mainCamera.transform.position) > 0.8f)
            {
                RepositionRodHandleInHandsMode();
            }
        }
        private void LateUpdate()
        {
            _lineRenderer.SetPosition(0, _rodTipTransform.position);
            _lineRenderer.SetPosition(1, _activeFloater.transform.position + _activeFloater.transform.up * 0.05f);
        }
        private void Update()
        {
            CheckForBringHandleInReach();
            ControllerRodAdjustment();
            PullingOutTimerHandler();
            _configurableJoint.anchor = Vector3.up * _stringGiven;

            if (_fishHooked)
            {
                FishHookedBehaviour();
            }

            // Switch to float in water
            if (!_isFloaterInWater && _floaterTransform.position.y < 0 && _pullingOutTimer <= 0)
            {
                var rayStart = _floaterTransform.position + Vector3.up * 10f;
                _tempRay = new Ray(rayStart, Vector3.down);

                if (Physics.Raycast(_tempRay, out _, 1000, 1 << 4))
                {
                    AudioSource.PlayClipAtPoint(_plickInTheWaterSound[Random.Range(0, _plickInTheWaterSound.Length)],
                        _activeFloater.position);
                    _isFloaterInWater = true;
                    _activeFloater = _floaterInWater;
                    _floater.gameObject.SetActive(false);
                    _floaterInWater.gameObject.SetActive(true);
                    _floaterInWaterTransform.position = _floaterTransform.position;
                    _floaterInWaterTransform.rotation = _floaterTransform.rotation;

                    _tempConstraintSource.sourceTransform = _floaterInWater;
                    _tempConstraintSource.weight = 1;
                    _rodBase.SetSource(0, _tempConstraintSource);
                    _rodMid.SetSource(0, _tempConstraintSource);
                    _rodEnd.SetSource(0, _tempConstraintSource);
                }
            }

            if (_isFloaterInWater && !_fishHooked)
            {
                _rodBase.weight = _tension * 0.2f;
                _rodMid.weight = _tension * 0.4f;
                _rodEnd.weight = _tension * 0.9f;
                OVRInput.SetControllerVibration(0, 0, controller);
                _distance = GetDistanceBetweenTargetAndUndeformedTip();
                _tension = _distance - _stringGiven;

                var projectedTip = new Vector3(_rodTipUndeformedTransform.position.x, 0,
                    _rodTipUndeformedTransform.position.z);
                if (Vector3.Distance(_rodTipUndeformedTransform.position, projectedTip) > _stringGiven + 0.05f)
                {
                    ChangeFloaterFromWaterToAir();
                }

                FloaterInWaterBehaviour();
            }

            if (!_isFloaterInWater)
            {
                FloaterInAirBehaviour();
            }

            // Debug controls
            if (Keyboard.current?.upArrowKey.isPressed == true || OVRInput.Get(OVRInput.Button.Four))
            {
                StringAdjustment(_buttonReelSpeed);
            }

            if (Keyboard.current?.downArrowKey.isPressed == true || OVRInput.Get(OVRInput.Button.Three))
            {
                StringAdjustment(-_buttonReelSpeed);
            }

            // Microgesture controls - continuous wheeling while gesture is active
            // Swipe up (backward gesture) = wheel in (reel in line / lower the line)
            if (HandInputManager.Instance.IsSwipeBackwardActive)
            {
                StringAdjustment(-_microGestureReelSpeed);
            }

            // Swipe down (forward gesture) = wheel out (let out line / raise the line)
            if (HandInputManager.Instance.IsSwipeForwardActive)
            {
                StringAdjustment(_microGestureReelSpeed);
            }
        }

        private void FishHookedBehaviour()
        {
            if (!_fishHooked)
            {
                return;
            }

            _rodBase.weight = 0.1f + _tension * 0.2f;
            _rodMid.weight = 0.1f + _tension * 0.4f;
            _rodEnd.weight = 0.1f + _tension * 0.9f;
            Vibrations();
            _distance = GetDistanceBetweenTargetAndUndeformedTip();
            _tension = _distance - _stringGiven;
            if (_tension >= 0.3f)
            {
                if (_fishHooked.FishStamina > 0.1f)
                {
                    _fishHooked.FishStamina -= _tension * 0.1f * Time.deltaTime;
                }

                if (_tension > 0.75f)
                {
                    if (Stamina > 0)
                    {
                        if (_audioSource.clip != _stretchSound && _audioSource.isPlaying == false)
                        {
                            _audioSource.clip = _stretchSound;
                            _audioSource.Play();
                        }

                        Stamina -= 0.4f * Time.deltaTime;
                    }
                }
                else
                {
                    if (_audioSource.clip == _stretchSound)
                    {
                        _audioSource.clip = null;
                        _audioSource.Stop();
                    }
                }
            }
            else
            {
                if (Stamina < 1)
                {
                    Stamina += Time.deltaTime;
                }

                if (_fishHooked.FishStamina < 1)
                {
                    _fishHooked.FishStamina += 0.1f * Time.deltaTime;
                }
            }

            if (Stamina < 0)
            {
                StringBroken();
            }

            if (_fishHooked && _fishHooked.FishStamina < 0.2f && _tension > 0.8f)
            {
                FishGetsFished();
            }
        }

        private void FishGetsFished()
        {
            _audioSource.clip = null;
            _audioSource.Stop();
            PlayPerturbedWaterSound();
            _tension = 0;
            _fishHooked.Caught();
            _fishHooked.FishStamina = 1;
            _fishHooked = null;
            ResetRod();
        }

        private void StringBroken()
        {
            _audioSource.clip = null;
            _audioSource.Stop();
            AudioSource.PlayClipAtPoint(_breakingSound, _rodTipTransform.position);
            _fishHooked.FishStamina = 1;
            _fishHooked.SwimRange = _fishHooked.SwimRangeStart;
            _fishHooked.Speed = _fishHooked.StartSpeed * 2;
            _fishHooked = null;
            ResetRod();
        }

        private void ResetRod()
        {
            _floaterInWaterRigidbody.isKinematic = false;
            _floaterInWater.gameObject.layer = _rodLayerIndex;
            _stringGiven = 0.01f;
            Stamina = 1;
            _activeFloater = _floater;
            _floater.gameObject.SetActive(true);
            _floaterInWater.gameObject.SetActive(false);
            _floaterInWaterTransform.parent = _floaterTransform.parent;
            _floaterInWaterTransform.position = _floaterTransform.position;
            _floaterInWaterTransform.rotation = _floaterTransform.rotation;
            _isFloaterInWater = false;
        }

        private void FloaterInAirBehaviour()
        {
            OVRInput.SetControllerVibration(0, 0, controller);
            _rodBase.weight = 0;
            _rodMid.weight = 0;
            _rodEnd.weight = 0;
        }

        private void ChangeFloaterFromWaterToAir()
        {
            _configurableJoint.anchor = Vector3.up * _stringGiven;
            _isFloaterInWater = false;
            _activeFloater = _floater;
            _floater.gameObject.SetActive(true);
            _floaterInWater.gameObject.SetActive(false);
            _floaterTransform.position = _floaterInWaterTransform.position;
            _floaterTransform.rotation = _floaterInWaterTransform.rotation;

            _tempConstraintSource.sourceTransform = _floater;
            _tempConstraintSource.weight = 1;
            _rodBase.SetSource(0, _tempConstraintSource);
            _rodMid.SetSource(0, _tempConstraintSource);
            _rodEnd.SetSource(0, _tempConstraintSource);

#if UNITY_6000_0_OR_NEWER
            _floaterRigidbody.linearVelocity = Vector3.zero;
#else
            _floaterRigidbody.velocity = Vector3.zero;
#endif
            _pullingOutTimer = _pullingOutTimerStart;
        }

        private void PullingOutTimerHandler()
        {
            if (_pullingOutTimer <= 0)
            {
                return;
            }

            _pullingOutTimer -= Time.deltaTime;
        }

        private void FloaterInWaterBehaviour()
        {
            if (!_isFloaterInWater)
            {
                return;
            }

            var tipProjectedOnGround = new Vector3(_rodTipTransform.position.x, 0, _rodTipTransform.position.z);
            var forceDirection = tipProjectedOnGround - _floaterInWaterTransform.position;
            var clampedTension = Mathf.Clamp(_tension, 0, Mathf.Infinity);
            _floaterInWaterRigidbody.AddForce(forceDirection *
                                              (_floaterInWaterAttractionForce * clampedTension * Time.deltaTime));

            var rayStart = _floaterInWaterTransform.position + Vector3.up * 10f;
            _tempRay = new Ray(rayStart, Vector3.down);

            if (Physics.Raycast(_tempRay, out var hit, 1000, 1 << 4))
            {
                var newPosition = new Vector3(
                    _floaterInWaterTransform.position.x,
                    hit.point.y + (Mathf.PerlinNoise(Time.time * 1.5f, 0) - 0.5f) * 0.12f,
                    _floaterInWaterTransform.position.z
                );
                _floaterInWaterRigidbody.position = newPosition;

                _floaterInWaterTransform.rotation = Quaternion.Lerp(_floaterInWaterTransform.rotation,
                    Quaternion.identity, Time.deltaTime * 5);
            }
            else
            {
                ChangeFloaterFromWaterToAir();
            }
        }

        public void PlayPerturbedWaterSound()
        {
            AudioSource.PlayClipAtPoint(_perturbedWaterSound[Random.Range(0, _perturbedWaterSound.Length)],
                _activeFloater.position);
        }

        private void Vibrations()
        {
            OVRInput.SetControllerVibration(_vibrationCurve.Evaluate(_tension), _vibrationCurve.Evaluate(_tension),
                controller);
        }

        public float GetDistanceBetweenTargetAndUndeformedTip()
        {
            return Vector3.Distance(_activeFloater.position, _rodTipUndeformedTransform.position);
        }

        private void StringAdjustment(float stringAdjust)
        {
            _stringGiven += stringAdjust * Time.deltaTime;
            _stringGiven = Mathf.Clamp(_stringGiven, 0.1f, _maxStringLength);
            _stringRoll.transform.Rotate(Vector3.right, stringAdjust * 5000 * Time.deltaTime);
        }

        private void ControllerRodAdjustment()
        {
            if (_stringGiven < 0.1f)
            {
                _stringGiven = 0.1f;
            }

            if (!_isHandMode)
            {
                var thumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, controller);

                if (thumbstick.magnitude > DEADZONE)
                {
                    var currentAngle = Mathf.Atan2(thumbstick.y, thumbstick.x) * Mathf.Rad2Deg;

                    // If not the first frame after exiting deadzone, calculate delta
                    if (!_wasInDeadzone)
                    {
                        var deltaAngle = currentAngle - _previousAngle;

                        // Normalize the delta to handle wraparound (e.g., 350° to 10°)
                        if (deltaAngle > 180f)
                        {
                            deltaAngle -= 360f;
                        }

                        if (deltaAngle < -180f)
                        {
                            deltaAngle += 360f;
                        }

                        _stringGiven += deltaAngle * Time.deltaTime * -.1f;
                        _stringGiven = Mathf.Clamp(_stringGiven, 0.1f, _maxStringLength);
                        _stringRoll.transform.localRotation = Quaternion.Euler(Vector3.right * -currentAngle);
                    }

                    _previousAngle = currentAngle;
                    _wasInDeadzone = false;
                }
                else
                {
                    _wasInDeadzone = true;
                }
            }
            else
            {
                if (TryGetRightHandPinchPosition(out var pinchPosition))
                {
                    if (Vector3.Distance(pinchPosition, _stringRoll.position) > 0.1f)
                    {
                        return;
                    }
                    //MakeASphereAndMoveit(pinchPosition);
                    // Get direction from string roller to pinch position in world space
                    var directionToPinch = pinchPosition - _stringRoll.position;

                    // Transform direction to the string roller's parent local space
                    var localDir = _stringRoll.parent.InverseTransformDirection(directionToPinch);

                    // The string roller rotates around its local X axis
                    // We want Z to point towards the pinch, so we calculate the angle in the Y-Z plane
                    var currentAngle = Mathf.Atan2(localDir.y, localDir.z) * Mathf.Rad2Deg;

                    if (!_wasPinchInDeadzone)
                    {
                        var deltaAngle = currentAngle - _previousPinchAngle;

                        // Normalize the delta to handle wraparound (e.g., 350° to 10°)
                        if (deltaAngle > 180f)
                        {
                            deltaAngle -= 360f;
                        }

                        if (deltaAngle < -180f)
                        {
                            deltaAngle += 360f;
                        }

                        // Convert angular change to string adjustment
                        _stringGiven += deltaAngle * -0.001f;
                        _stringGiven = Mathf.Clamp(_stringGiven, 0.1f, _maxStringLength);
                    }

                    // Rotate the string roller so its Z axis points towards the pinch (constrained to X rotation)
                    _stringRoll.localRotation = Quaternion.Euler(-currentAngle, 0, 0);

                    _previousPinchAngle = currentAngle;
                    _wasPinchInDeadzone = false;
                }
                else
                {
                    _wasPinchInDeadzone = true;
                }
            }
        }


        private IEnumerator MoveHandleToPosition(Vector3 targetPosition, Quaternion targetRotation, float duration)
        {
            var handleTransform = _handleParentConstraint.transform;
            var startPosition = handleTransform.position;
            var startRotation = handleTransform.rotation;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                handleTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
                handleTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                yield return null;
            }

            handleTransform.position = targetPosition;
            handleTransform.rotation = targetRotation;
            _moveHandleCoroutine = null;
        }

        public bool TryGetRightHandPinchPosition(out Vector3 pinchPosition)
        {
            pinchPosition = Vector3.zero;

            if (!HandInputManager.Instance.IsIndexPinching)
            {
                return false;
            }

            if (_cameraRig == null)
            {
                _cameraRig = FindAnyObjectByType<OVRCameraRig>();
                if (_cameraRig == null)
                {
                    return false;
                }
            }

            if (!OVRPlugin.GetHandState(OVRPlugin.Step.Render, OVRPlugin.Hand.HandRight, ref _handState))
            {
                return false;
            }

            const int indexTipIndex = (int)OVRPlugin.BoneId.XRHand_IndexTip;

            if (_handState.BonePositions == null || _handState.BonePositions.Length <= indexTipIndex)
            {
                return false;
            }

            var tipPos = _handState.BonePositions[indexTipIndex];
            // BonePositions are in tracking space with OVR conventions (right-handed, flip Z for Unity)
            var trackingSpacePos = new Vector3(tipPos.x, tipPos.y, -tipPos.z);
            // Transform from tracking space to world space via the camera rig
            pinchPosition = _cameraRig.trackingSpace.TransformPoint(trackingSpacePos);
            return true;
        }
    }
}
