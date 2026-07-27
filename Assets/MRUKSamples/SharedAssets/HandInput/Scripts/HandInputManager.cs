// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.XR.MRUtilityKitSamples.HandInput
{
    /// <summary>
    /// Defines the current input mode - whether the user is using hand tracking or controllers.
    /// </summary>
    public enum InputMode
    {
        Controllers,
        Hands
    }

    /// <summary>
    /// Singleton manager that handles hand input detection across all MRUK samples.
    /// Detects hand/controller switching, index finger pinch gestures (A/X button alternatives),
    /// middle finger pinch gestures (B/Y button alternatives), and microgestures (thumb tap, swipes).
    /// </summary>
    [MetaCodeSample("MRUKSample-SharedAssets")]
    public class HandInputManager : MonoBehaviour
    {
        private static HandInputManager _instance;

        /// <summary>
        /// Singleton instance of the HandInputManager.
        /// Auto-creates if it doesn't exist, ensuring hand input works regardless of which scene is loaded first.
        /// </summary>
        public static HandInputManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<HandInputManager>();

                    // Auto-create if not found in scene
                    if (_instance == null)
                    {
                        var go = new GameObject("[HandInputManager]");
                        _instance = go.AddComponent<HandInputManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Events - Input Mode")]
        [Tooltip("Fired when the input mode changes between Controllers and Hands.")]
        public UnityEvent<InputMode> OnInputModeChanged = new UnityEvent<InputMode>();

        [Header("Events - Right Hand Index Pinch (A Button Alternative)")]
        [Tooltip("Fired when right hand index finger pinch starts (A button equivalent).")]
        public UnityEvent OnIndexPinchStarted = new UnityEvent();

        [Tooltip("Fired when right hand index finger pinch ends.")]
        public UnityEvent OnIndexPinchEnded = new UnityEvent();

        [Header("Events - Left Hand Index Pinch (X Button Alternative)")]
        [Tooltip("Fired when left hand index finger pinch starts (X button equivalent).")]
        public UnityEvent OnSecondaryIndexPinchStarted = new UnityEvent();

        [Tooltip("Fired when left hand index finger pinch ends.")]
        public UnityEvent OnSecondaryIndexPinchEnded = new UnityEvent();

        [Header("Events - Right Hand Middle Pinch (B Button Alternative)")]
        [Tooltip("Fired when right hand middle finger pinch starts.")]
        public UnityEvent OnMiddlePinchStarted = new UnityEvent();

        [Tooltip("Fired when right hand middle finger pinch ends.")]
        public UnityEvent OnMiddlePinchEnded = new UnityEvent();

        [Header("Events - Left Hand Middle Pinch (Y Button Alternative)")]
        [Tooltip("Fired when left hand middle finger pinch starts.")]
        public UnityEvent OnSecondaryMiddlePinchStarted = new UnityEvent();

        [Tooltip("Fired when left hand middle finger pinch ends.")]
        public UnityEvent OnSecondaryMiddlePinchEnded = new UnityEvent();

        [Header("Events - Microgestures")]
        [Tooltip("Fired when a microgesture is recognized on the right hand. Provides the gesture type.")]
        public UnityEvent<OVRHand.MicrogestureType> OnRightMicrogesture = new UnityEvent<OVRHand.MicrogestureType>();

        [Tooltip("Fired when a microgesture is recognized on the left hand. Provides the gesture type.")]
        public UnityEvent<OVRHand.MicrogestureType> OnLeftMicrogesture = new UnityEvent<OVRHand.MicrogestureType>();

        [Tooltip("Fired when a thumb tap microgesture is detected on either hand.")]
        public UnityEvent OnThumbTap = new UnityEvent();

        [Tooltip("Fired when a swipe forward microgesture is detected on either hand.")]
        public UnityEvent OnSwipeForward = new UnityEvent();

        [Tooltip("Fired when a swipe backward microgesture is detected on either hand.")]
        public UnityEvent OnSwipeBackward = new UnityEvent();

        [Tooltip("Fired when a swipe left microgesture is detected on either hand.")]
        public UnityEvent OnSwipeLeft = new UnityEvent();

        [Tooltip("Fired when a swipe right microgesture is detected on either hand.")]
        public UnityEvent OnSwipeRight = new UnityEvent();

        [Tooltip("Fired when swipe forward gesture starts (for continuous actions).")]
        public UnityEvent OnSwipeForwardStarted = new UnityEvent();

        [Tooltip("Fired when swipe forward gesture ends.")]
        public UnityEvent OnSwipeForwardEnded = new UnityEvent();

        [Tooltip("Fired when swipe backward gesture starts (for continuous actions).")]
        public UnityEvent OnSwipeBackwardStarted = new UnityEvent();

        [Tooltip("Fired when swipe backward gesture ends.")]
        public UnityEvent OnSwipeBackwardEnded = new UnityEvent();

        [Header("Continuous Thumb Swipe Settings")]
        [Tooltip("The vertical distance (in meters) the thumb must travel to reach maximum swipe value.")]
        [SerializeField] private float _thumbSwipeRange = 0.05f;

        [Tooltip("Sensitivity multiplier for thumb swipe detection.")]
        [SerializeField] private float _thumbSwipeSensitivity = 1.0f;

        [Tooltip("Dead zone for thumb swipe (0-1). Values below this threshold are treated as zero.")]
        [SerializeField, Range(0f, 0.5f)] private float _thumbSwipeDeadZone = 0.1f;

        [Header("Continuous Microgesture Swipe Settings")]
        [Tooltip("Minimum duration (in seconds) that a swipe gesture stays active after detection.")]
        [SerializeField] private float _swipeHoldDuration = 0.5f;

        /// <summary>
        /// The current input mode (Controllers or Hands).
        /// </summary>
        public InputMode CurrentInputMode { get; private set; } = InputMode.Controllers;

        /// <summary>
        /// Returns true during the frame when a thumb tap microgesture was detected on either hand.
        /// Use this in Update() similar to OVRInput.GetDown().
        /// </summary>
        public bool ThumbTapDown { get; private set; }

        /// <summary>
        /// Returns true during the frame when a swipe forward microgesture was detected on either hand.
        /// Use this in Update() similar to OVRInput.GetDown().
        /// </summary>
        public bool SwipeForwardDown { get; private set; }

        /// <summary>
        /// Returns true during the frame when a swipe backward microgesture was detected on either hand.
        /// Use this in Update() similar to OVRInput.GetDown().
        /// </summary>
        public bool SwipeBackwardDown { get; private set; }

        /// <summary>
        /// Returns true during the frame when a swipe left microgesture was detected on either hand.
        /// Use this in Update() similar to OVRInput.GetDown().
        /// </summary>
        public bool SwipeLeftDown { get; private set; }

        /// <summary>
        /// Returns true during the frame when a swipe right microgesture was detected on either hand.
        /// Use this in Update() similar to OVRInput.GetDown().
        /// </summary>
        public bool SwipeRightDown { get; private set; }

        /// <summary>
        /// Returns true while a swipe forward gesture is active (from detection until NoGesture).
        /// Use this for continuous actions like reeling in a fishing line.
        /// </summary>
        public bool IsSwipeForwardActive { get; private set; }

        /// <summary>
        /// Returns true while a swipe backward gesture is active (from detection until NoGesture).
        /// Use this for continuous actions like reeling out a fishing line.
        /// </summary>
        public bool IsSwipeBackwardActive { get; private set; }

        /// <summary>
        /// Whether the right hand is currently performing an index finger pinch (A button alternative).
        /// </summary>
        public bool IsIndexPinching { get; private set; }

        /// <summary>
        /// Whether the left hand is currently performing an index finger pinch (X button alternative).
        /// </summary>
        public bool IsSecondaryIndexPinching { get; private set; }

        /// <summary>
        /// Whether the right hand is currently performing a middle finger pinch (B button alternative).
        /// </summary>
        public bool IsMiddlePinching { get; private set; }

        /// <summary>
        /// Whether the left hand is currently performing a middle finger pinch (Y button alternative).
        /// </summary>
        public bool IsSecondaryMiddlePinching { get; private set; }

        /// <summary>
        /// Returns a continuous value from -1 to 1 based on hand vertical movement during an index pinch.
        /// Positive values indicate upward movement (like thumbstick up), negative values indicate downward.
        /// Use this for analog stick-like continuous input during hand tracking.
        /// Automatically tracks when either hand is performing an index pinch.
        /// </summary>
        public float PinchDragValue { get; private set; }

        /// <summary>
        /// Returns true while pinch drag tracking is active (index pinch held and hand moving).
        /// </summary>
        public bool IsPinchDragActive { get; private set; }

        /// <summary>
        /// Whether right hand pinch drag tracking is currently active.
        /// </summary>
        public bool IsRightPinchDragActive { get; private set; }

        /// <summary>
        /// Whether left hand pinch drag tracking is currently active.
        /// </summary>
        public bool IsLeftPinchDragActive { get; private set; }

        /// <summary>
        /// The right hand's pinch drag value (-1 to 1).
        /// </summary>
        public float RightPinchDragValue { get; private set; }

        /// <summary>
        /// The left hand's pinch drag value (-1 to 1).
        /// </summary>
        public float LeftPinchDragValue { get; private set; }

        // Pinch drag tracking state
        private Vector3 _rightHandStartPosition;
        private Vector3 _leftHandStartPosition;
        private bool _rightPinchDragInitialized;
        private bool _leftPinchDragInitialized;
        private OVRPlugin.HandState _rightHandState;
        private OVRPlugin.HandState _leftHandState;

        // Continuous swipe hold timers
        private float _swipeForwardEndTime;
        private float _swipeBackwardEndTime;

        // Legacy properties for backward compatibility
        /// <summary>
        /// [DEPRECATED] Use PinchDragValue instead. Returns a continuous swipe value from -1 to 1.
        /// </summary>
        public float ThumbSwipeValue => PinchDragValue;

        /// <summary>
        /// [DEPRECATED] Use IsPinchDragActive instead.
        /// </summary>
        public bool IsThumbSwipeActive => IsPinchDragActive;

        /// <summary>
        /// The last recognized microgesture on the right hand (used for edge detection).
        /// </summary>
        private OVRHand.MicrogestureType _lastRightMicrogesture = OVRHand.MicrogestureType.NoGesture;

        /// <summary>
        /// The last recognized microgesture on the left hand (used for edge detection).
        /// </summary>
        private OVRHand.MicrogestureType _lastLeftMicrogesture = OVRHand.MicrogestureType.NoGesture;

        private OVRPlugin.HandState _cachedHandState;
        private OVRPlugin.HandTrackingState _cachedHandTrackingState;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            UpdateInputMode();
            UpdateIndexPinchState();
            UpdateMiddlePinchState();
            UpdateMicrogestureState();
            UpdateThumbSwipeTracking();
        }

        private void UpdateInputMode()
        {
            var activeController = OVRInput.GetActiveController();
            var newMode = IsHandController(activeController) ? InputMode.Hands : InputMode.Controllers;

            if (newMode != CurrentInputMode)
            {
                CurrentInputMode = newMode;
                Debug.Log($"## Input mode changed to {CurrentInputMode}");
                OnInputModeChanged?.Invoke(CurrentInputMode);
            }
        }

        private bool IsHandController(OVRInput.Controller controller)
        {
            return (controller & (OVRInput.Controller.LHand | OVRInput.Controller.RHand | OVRInput.Controller.Hands)) != 0;
        }

        private void UpdateIndexPinchState()
        {
            if (CurrentInputMode != InputMode.Hands)
            {
                if (IsIndexPinching)
                {
                    IsIndexPinching = false;
                    OnIndexPinchEnded?.Invoke();
                }

                if (IsSecondaryIndexPinching)
                {
                    IsSecondaryIndexPinching = false;
                    OnSecondaryIndexPinchEnded?.Invoke();
                }
                return;
            }

            // Check right hand index finger pinch (A button alternative)
            var rightIndexPinching = false;
            if (OVRPlugin.GetHandState(OVRPlugin.Step.Render, OVRPlugin.Hand.HandRight, ref _cachedHandState))
            {
                rightIndexPinching = (_cachedHandState.Pinches & OVRPlugin.HandFingerPinch.Index) != 0;
            }

            switch (rightIndexPinching)
            {
                case true when !IsIndexPinching:
                    IsIndexPinching = true;
                    OnIndexPinchStarted?.Invoke();
                    break;
                case false when IsIndexPinching:
                    IsIndexPinching = false;
                    OnIndexPinchEnded?.Invoke();
                    break;
            }

            // Check left hand index finger pinch (X button alternative)
            var leftIndexPinching = false;
            if (OVRPlugin.GetHandState(OVRPlugin.Step.Render, OVRPlugin.Hand.HandLeft, ref _cachedHandState))
            {
                leftIndexPinching = (_cachedHandState.Pinches & OVRPlugin.HandFingerPinch.Index) != 0;
            }

            if (leftIndexPinching && !IsSecondaryIndexPinching)
            {
                IsSecondaryIndexPinching = true;
                OnSecondaryIndexPinchStarted?.Invoke();
            }
            else if (!leftIndexPinching && IsSecondaryIndexPinching)
            {
                IsSecondaryIndexPinching = false;
                OnSecondaryIndexPinchEnded?.Invoke();
            }
        }

        private void UpdateMiddlePinchState()
        {
            if (CurrentInputMode != InputMode.Hands)
            {
                if (IsMiddlePinching)
                {
                    IsMiddlePinching = false;
                    OnMiddlePinchEnded?.Invoke();
                }

                if (!IsSecondaryMiddlePinching)
                {
                    return;
                }

                IsSecondaryMiddlePinching = false;
                OnSecondaryMiddlePinchEnded?.Invoke();
                return;
            }

            // Check right hand middle finger pinch (B button alternative)
            var rightMiddlePinching = false;
            if (OVRPlugin.GetHandState(OVRPlugin.Step.Render, OVRPlugin.Hand.HandRight, ref _cachedHandState))
            {
                rightMiddlePinching = (_cachedHandState.Pinches & OVRPlugin.HandFingerPinch.Middle) != 0;
            }

            switch (rightMiddlePinching)
            {
                case true when !IsMiddlePinching:
                    IsMiddlePinching = true;
                    OnMiddlePinchStarted?.Invoke();
                    break;
                case false when IsMiddlePinching:
                    IsMiddlePinching = false;
                    OnMiddlePinchEnded?.Invoke();
                    break;
            }

            // Check left-hand middle finger pinch (Y button alternative)
            var leftMiddlePinching = false;
            if (OVRPlugin.GetHandState(OVRPlugin.Step.Render, OVRPlugin.Hand.HandLeft, ref _cachedHandState))
            {
                leftMiddlePinching = (_cachedHandState.Pinches & OVRPlugin.HandFingerPinch.Middle) != 0;
            }

            if (leftMiddlePinching && !IsSecondaryMiddlePinching)
            {
                IsSecondaryMiddlePinching = true;
                OnSecondaryMiddlePinchStarted?.Invoke();
            }
            else if (!leftMiddlePinching && IsSecondaryMiddlePinching)
            {
                IsSecondaryMiddlePinching = false;
                OnSecondaryMiddlePinchEnded?.Invoke();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Updates the microgesture state for both hands.
        /// Uses edge detection to fire events only when a new gesture is recognized.
        /// Implements continuous swipe state with hold duration timer.
        /// </summary>
        private void UpdateMicrogestureState()
        {
            // Clear all microgesture flags at the start of each frame
            ThumbTapDown = false;
            SwipeForwardDown = false;
            SwipeBackwardDown = false;
            SwipeLeftDown = false;
            SwipeRightDown = false;

            if (CurrentInputMode != InputMode.Hands)
            {
                _lastRightMicrogesture = OVRHand.MicrogestureType.NoGesture;
                _lastLeftMicrogesture = OVRHand.MicrogestureType.NoGesture;
                _swipeForwardEndTime = 0f;
                _swipeBackwardEndTime = 0f;
                EndSwipeForwardIfActive();
                EndSwipeBackwardIfActive();
                return;
            }

            // Send hint to enable microgesture detection
            OVRPlugin.SendMicrogestureHint();

            // Track current gestures from both hands for continuous state
            var rightCurrentGesture = OVRHand.MicrogestureType.NoGesture;
            var leftCurrentGesture = OVRHand.MicrogestureType.NoGesture;

            // Check right hand microgesture
            if (OVRPlugin.GetHandTrackingState(OVRPlugin.Step.Render, OVRPlugin.Hand.HandRight, ref _cachedHandTrackingState))
            {
                rightCurrentGesture = (OVRHand.MicrogestureType)_cachedHandTrackingState.Microgesture;
                if (rightCurrentGesture != _lastRightMicrogesture && IsValidGesture(rightCurrentGesture))
                {
                    _lastRightMicrogesture = rightCurrentGesture;
                    OnRightMicrogesture?.Invoke(rightCurrentGesture);
                    SetGestureFlags(rightCurrentGesture);
                    FireGestureSpecificEvent(rightCurrentGesture);
                    UpdateContinuousSwipeState(rightCurrentGesture);
                }
                else if (rightCurrentGesture == OVRHand.MicrogestureType.NoGesture)
                {
                    _lastRightMicrogesture = rightCurrentGesture;
                }
            }

            // Check left hand microgesture
            if (OVRPlugin.GetHandTrackingState(OVRPlugin.Step.Render, OVRPlugin.Hand.HandLeft, ref _cachedHandTrackingState))
            {
                leftCurrentGesture = (OVRHand.MicrogestureType)_cachedHandTrackingState.Microgesture;
                if (leftCurrentGesture != _lastLeftMicrogesture && IsValidGesture(leftCurrentGesture))
                {
                    _lastLeftMicrogesture = leftCurrentGesture;
                    OnLeftMicrogesture?.Invoke(leftCurrentGesture);
                    SetGestureFlags(leftCurrentGesture);
                    FireGestureSpecificEvent(leftCurrentGesture);
                    UpdateContinuousSwipeState(leftCurrentGesture);
                }
                else if (leftCurrentGesture == OVRHand.MicrogestureType.NoGesture)
                {
                    _lastLeftMicrogesture = leftCurrentGesture;
                }
            }

            // End swipe states only after the hold timer expires
            if (Time.time >= _swipeForwardEndTime)
            {
                EndSwipeForwardIfActive();
            }
            if (Time.time >= _swipeBackwardEndTime)
            {
                EndSwipeBackwardIfActive();
            }
        }

        /// <summary>
        /// Updates the continuous swipe state when a new gesture is detected.
        /// Swipe gestures remain active until the hold timer expires.
        /// Each new detection extends the timer.
        /// </summary>
        private void UpdateContinuousSwipeState(OVRHand.MicrogestureType gesture)
        {
            switch (gesture)
            {
                case OVRHand.MicrogestureType.SwipeForward:
                    // Extend or start the swipe forward timer
                    _swipeForwardEndTime = Time.time + _swipeHoldDuration;
                    // End opposite direction
                    _swipeBackwardEndTime = 0f;
                    EndSwipeBackwardIfActive();
                    if (!IsSwipeForwardActive)
                    {
                        IsSwipeForwardActive = true;
                        OnSwipeForwardStarted?.Invoke();
                    }
                    break;
                case OVRHand.MicrogestureType.SwipeBackward:
                    // Extend or start the swipe backward timer
                    _swipeBackwardEndTime = Time.time + _swipeHoldDuration;
                    // End opposite direction
                    _swipeForwardEndTime = 0f;
                    EndSwipeForwardIfActive();
                    if (!IsSwipeBackwardActive)
                    {
                        IsSwipeBackwardActive = true;
                        OnSwipeBackwardStarted?.Invoke();
                    }
                    break;
                case OVRHand.MicrogestureType.ThumbTap:
                case OVRHand.MicrogestureType.SwipeLeft:
                case OVRHand.MicrogestureType.SwipeRight:
                    // End any active swipe when a different gesture is detected
                    _swipeForwardEndTime = 0f;
                    _swipeBackwardEndTime = 0f;
                    EndSwipeForwardIfActive();
                    EndSwipeBackwardIfActive();
                    break;
            }
        }

        /// <summary>
        /// Ends the swipe forward state if it's currently active.
        /// </summary>
        private void EndSwipeForwardIfActive()
        {
            if (IsSwipeForwardActive)
            {
                IsSwipeForwardActive = false;
                OnSwipeForwardEnded?.Invoke();
            }
        }

        /// <summary>
        /// Ends the swipe backward state if it's currently active.
        /// </summary>
        private void EndSwipeBackwardIfActive()
        {
            if (IsSwipeBackwardActive)
            {
                IsSwipeBackwardActive = false;
                OnSwipeBackwardEnded?.Invoke();
            }
        }

        /// <summary>
        /// Sets the appropriate boolean flag for the detected gesture type.
        /// </summary>
        private void SetGestureFlags(OVRHand.MicrogestureType gesture)
        {
            switch (gesture)
            {
                case OVRHand.MicrogestureType.ThumbTap:
                    ThumbTapDown = true;
                    break;
                case OVRHand.MicrogestureType.SwipeForward:
                    SwipeForwardDown = true;
                    break;
                case OVRHand.MicrogestureType.SwipeBackward:
                    SwipeBackwardDown = true;
                    break;
                case OVRHand.MicrogestureType.SwipeLeft:
                    SwipeLeftDown = true;
                    break;
                case OVRHand.MicrogestureType.SwipeRight:
                    SwipeRightDown = true;
                    break;
            }
        }

        /// <summary>
        /// Checks if the gesture is a valid, actionable gesture.
        /// </summary>
        private bool IsValidGesture(OVRHand.MicrogestureType gesture)
        {
            return gesture != OVRHand.MicrogestureType.NoGesture &&
                   gesture != OVRHand.MicrogestureType.Invalid;
        }

        /// <summary>
        /// Fires the appropriate gesture-specific event based on the gesture type.
        /// </summary>
        private void FireGestureSpecificEvent(OVRHand.MicrogestureType gesture)
        {
            switch (gesture)
            {
                case OVRHand.MicrogestureType.ThumbTap:
                    OnThumbTap?.Invoke();
                    break;
                case OVRHand.MicrogestureType.SwipeForward:
                    OnSwipeForward?.Invoke();
                    break;
                case OVRHand.MicrogestureType.SwipeBackward:
                    OnSwipeBackward?.Invoke();
                    break;
                case OVRHand.MicrogestureType.SwipeLeft:
                    OnSwipeLeft?.Invoke();
                    break;
                case OVRHand.MicrogestureType.SwipeRight:
                    OnSwipeRight?.Invoke();
                    break;
            }
        }

        public void DebugIt(string message)
        {
            Debug.Log("## " + message);
        }

        /// <summary>
        /// Updates the continuous pinch drag tracking based on hand position during an index pinch.
        /// This provides analog stick-like input for hand tracking, where vertical hand movement
        /// during a pinch maps to a -1 to 1 value.
        /// </summary>
        private void UpdateThumbSwipeTracking()
        {
            // Reset values at the start
            RightPinchDragValue = 0f;
            LeftPinchDragValue = 0f;
            IsRightPinchDragActive = false;
            IsLeftPinchDragActive = false;

            if (CurrentInputMode != InputMode.Hands)
            {
                _rightPinchDragInitialized = false;
                _leftPinchDragInitialized = false;
                PinchDragValue = 0f;
                IsPinchDragActive = false;
                return;
            }

            // Track right hand position during index pinch (already used for grabbing)
            if (IsIndexPinching)
            {
                if (OVRPlugin.GetHandState(OVRPlugin.Step.Render, OVRPlugin.Hand.HandRight, ref _rightHandState))
                {
                    var handPosition = GetHandPosition(_rightHandState);
                    if (handPosition.HasValue)
                    {
                        if (!_rightPinchDragInitialized)
                        {
                            _rightHandStartPosition = handPosition.Value;
                            _rightPinchDragInitialized = true;
                        }
                        else
                        {
                            var delta = handPosition.Value.y - _rightHandStartPosition.y;
                            RightPinchDragValue = CalculateSwipeValue(delta);
                            IsRightPinchDragActive = Mathf.Abs(RightPinchDragValue) > 0f;
                        }
                    }
                }
            }
            else
            {
                _rightPinchDragInitialized = false;
            }

            // Track left hand position during index pinch
            if (IsSecondaryIndexPinching)
            {
                if (OVRPlugin.GetHandState(OVRPlugin.Step.Render, OVRPlugin.Hand.HandLeft, ref _leftHandState))
                {
                    var handPosition = GetHandPosition(_leftHandState);
                    if (handPosition.HasValue)
                    {
                        if (!_leftPinchDragInitialized)
                        {
                            _leftHandStartPosition = handPosition.Value;
                            _leftPinchDragInitialized = true;
                        }
                        else
                        {
                            var delta = handPosition.Value.y - _leftHandStartPosition.y;
                            LeftPinchDragValue = CalculateSwipeValue(delta);
                            IsLeftPinchDragActive = Mathf.Abs(LeftPinchDragValue) > 0f;
                        }
                    }
                }
            }
            else
            {
                _leftPinchDragInitialized = false;
            }

            // Combine both hands - use whichever has the larger magnitude
            if (Mathf.Abs(RightPinchDragValue) >= Mathf.Abs(LeftPinchDragValue))
            {
                PinchDragValue = RightPinchDragValue;
                IsPinchDragActive = IsRightPinchDragActive;
            }
            else
            {
                PinchDragValue = LeftPinchDragValue;
                IsPinchDragActive = IsLeftPinchDragActive;
            }
        }

        /// <summary>
        /// Gets the hand position from the hand state using the root pose.
        /// Returns null if the data is not available.
        /// </summary>
        private Vector3? GetHandPosition(OVRPlugin.HandState handState)
        {
            if (handState.Status == 0)
            {
                return null;
            }

            // Use the root pose which represents the wrist/hand position
            var rootPose = handState.RootPose;
            return new Vector3(rootPose.Position.x, rootPose.Position.y, rootPose.Position.z);
        }

        /// <summary>
        /// Calculates the swipe value from a vertical delta, applying sensitivity and dead zone.
        /// </summary>
        /// <param name="delta">The vertical distance moved in meters</param>
        /// <returns>A value from -1 to 1</returns>
        private float CalculateSwipeValue(float delta)
        {
            // Normalize the delta to the configured range
            var normalizedValue = (delta / _thumbSwipeRange) * _thumbSwipeSensitivity;

            // Apply dead zone
            if (Mathf.Abs(normalizedValue) < _thumbSwipeDeadZone)
            {
                return 0f;
            }

            // Remap value to remove the dead zone gap
            var sign = Mathf.Sign(normalizedValue);
            var magnitude = Mathf.Abs(normalizedValue);
            var remappedMagnitude = (magnitude - _thumbSwipeDeadZone) / (1f - _thumbSwipeDeadZone);

            // Clamp to -1 to 1 range
            return Mathf.Clamp(sign * remappedMagnitude, -1f, 1f);
        }
    }
}
