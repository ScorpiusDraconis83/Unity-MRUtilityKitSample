// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using TMPro;
using UnityEngine;

namespace Meta.XR.MRUtilityKitSamples.HandInput
{
    /// <summary>
    /// Defines how the UI should be positioned in hands mode.
    /// </summary>
    public enum HandsUIPositionMode
    {
        /// <summary>UI follows user's gaze, positioned in front of them.</summary>
        FollowGaze,
        /// <summary>UI is attached to the left wrist like a watch.</summary>
        LeftWrist,
        /// <summary>UI is attached to the right wrist like a watch.</summary>
        RightWrist
    }

    /// <summary>
    /// UI helper component that automatically updates instruction text and UI positioning
    /// when the input mode changes between controllers and hands.
    /// Supports wrist-mounted UI positioning for a "check the time" interaction style.
    /// </summary>
    [MetaCodeSample("MRUKSample-SharedAssets")]
    public class HandInputUIAdapter : MonoBehaviour
    {
        [Header("Text Replacement")]
        [Tooltip("The text component to update when input mode changes.")]
        [SerializeField] private TextMeshProUGUI instructionText;

        [Tooltip("Text to display when in controller mode.")]
        [SerializeField, TextArea(2, 5)] private string controllerModeText = "Press <b>B</b> to perform action";

        [Tooltip("Text to display when in hands mode.")]
        [SerializeField, TextArea(2, 5)] private string handsModeText = "Pinch with <b>middle finger</b> to perform action";

        [Header("UI Positioning (Optional)")]
        [Tooltip("The UI transform to reposition based on input mode.")]
        [SerializeField] private Transform uiTransform;

        [Tooltip("Reference to the OVRCameraRig for accessing hand anchors.")]
        [SerializeField] private OVRCameraRig cameraRig;

        [Tooltip("How the UI should be positioned when in hands mode.")]
        [SerializeField] private HandsUIPositionMode handsPositionMode = HandsUIPositionMode.LeftWrist;

        [Header("Gaze Follow Settings")]
        [Tooltip("How far in front of the user to position UI in gaze follow mode.")]
        [SerializeField] private float gazeForwardOffset = 0.5f;

        [Tooltip("Height offset for UI in gaze follow mode.")]
        [SerializeField] private float gazeHeightOffset = -0.1f;

        [Header("Wrist Mount Settings")]
        [Tooltip("Position offset from the wrist anchor (local space). X=right, Y=up, Z=forward relative to wrist.")]
        [SerializeField] private Vector3 wristPositionOffset = new Vector3(0f, 0.05f, 0f);

        [Tooltip("Rotation offset from the wrist (Euler angles). Adjusts the UI to face the user when looking at wrist.")]
        [SerializeField] private Vector3 wristRotationOffset = new Vector3(0f, 180f, 0f);

        [Tooltip("Scale of the UI when mounted on wrist (typically smaller than world-space UI).")]
        [SerializeField] private float wristUIScale = 0.5f;

        [Header("Smoothing")]
        [Tooltip("Smoothing speed for UI position updates. Set to 0 for instant positioning.")]
        [SerializeField] private float positionSmoothSpeed = 5f;

        private Vector3 _controllerModePosition;
        private Quaternion _controllerModeRotation;
        private Vector3 _controllerModeScale;
        private Transform _controllerModeParent;
        private bool _savedControllerModeTransform;
        private InputMode _currentMode = InputMode.Controllers;

        private void OnEnable()
        {
            if (HandInputManager.Instance != null)
            {
                HandInputManager.Instance.OnInputModeChanged.AddListener(HandleInputModeChanged);
                HandleInputModeChanged(HandInputManager.Instance.CurrentInputMode);
            }
        }

        private void Start()
        {
            // Auto-find OVRCameraRig if not assigned
            if (cameraRig == null)
            {
                cameraRig = FindAnyObjectByType<OVRCameraRig>();
            }

            // If HandInputManager wasn't available during OnEnable, try again
            if (HandInputManager.Instance != null)
            {
                HandInputManager.Instance.OnInputModeChanged.AddListener(HandleInputModeChanged);
                HandleInputModeChanged(HandInputManager.Instance.CurrentInputMode);
            }
        }

        private void OnDisable()
        {
            if (HandInputManager.Instance != null)
            {
                HandInputManager.Instance.OnInputModeChanged.RemoveListener(HandleInputModeChanged);
            }
        }

        private void Update()
        {
            if (_currentMode == InputMode.Hands && uiTransform != null)
            {
                UpdateHandsModePosition();
            }
        }

        private void HandleInputModeChanged(InputMode mode)
        {
            _currentMode = mode;
            UpdateText(mode);
            UpdatePositionMode(mode);
        }

        private void UpdateText(InputMode mode)
        {
            if (instructionText == null) return;

            instructionText.text = mode == InputMode.Controllers ? controllerModeText : handsModeText;
        }

        private void UpdatePositionMode(InputMode mode)
        {
            if (uiTransform == null) return;

            if (mode == InputMode.Controllers)
            {
                // Restore controller mode transform if we saved it
                if (_savedControllerModeTransform)
                {
                    // Reparent back to original parent
                    uiTransform.SetParent(_controllerModeParent);
                    uiTransform.localPosition = _controllerModePosition;
                    uiTransform.localRotation = _controllerModeRotation;
                    uiTransform.localScale = _controllerModeScale;
                }
            }
            else
            {
                // Save the controller mode transform before switching
                if (!_savedControllerModeTransform)
                {
                    _controllerModeParent = uiTransform.parent;
                    _controllerModePosition = uiTransform.localPosition;
                    _controllerModeRotation = uiTransform.localRotation;
                    _controllerModeScale = uiTransform.localScale;
                    _savedControllerModeTransform = true;
                }

                // Set up for hands mode based on position mode
                if (handsPositionMode == HandsUIPositionMode.LeftWrist ||
                    handsPositionMode == HandsUIPositionMode.RightWrist)
                {
                    SetupWristMount();
                }
            }
        }

        private void SetupWristMount()
        {
            if (cameraRig == null || uiTransform == null) return;

            // Get the appropriate hand anchor
            Transform wristAnchor = handsPositionMode == HandsUIPositionMode.LeftWrist
                ? cameraRig.leftHandAnchor
                : cameraRig.rightHandAnchor;

            if (wristAnchor == null) return;

            // Parent the UI to the wrist anchor
            uiTransform.SetParent(wristAnchor);

            // Apply wrist-specific transform
            uiTransform.localPosition = wristPositionOffset;
            uiTransform.localRotation = Quaternion.Euler(wristRotationOffset);
            uiTransform.localScale = Vector3.one * wristUIScale;
        }

        private void UpdateHandsModePosition()
        {
            if (uiTransform == null || cameraRig == null) return;

            switch (handsPositionMode)
            {
                case HandsUIPositionMode.FollowGaze:
                    UpdateGazeFollowPosition();
                    break;
                case HandsUIPositionMode.LeftWrist:
                case HandsUIPositionMode.RightWrist:
                    // Wrist mount is handled by parenting, but we can add smoothing here if needed
                    UpdateWristMountPosition();
                    break;
            }
        }

        private void UpdateGazeFollowPosition()
        {
            var centerEye = cameraRig.centerEyeAnchor;
            if (centerEye == null) return;

            var forward = Vector3.ProjectOnPlane(centerEye.forward, Vector3.up).normalized;
            var targetPosition = centerEye.position + forward * gazeForwardOffset + Vector3.up * gazeHeightOffset;
            var targetRotation = Quaternion.LookRotation(forward);

            if (positionSmoothSpeed > 0)
            {
                uiTransform.position = Vector3.Lerp(uiTransform.position, targetPosition, Time.deltaTime * positionSmoothSpeed);
                uiTransform.rotation = Quaternion.Slerp(uiTransform.rotation, targetRotation, Time.deltaTime * positionSmoothSpeed);
            }
            else
            {
                uiTransform.position = targetPosition;
                uiTransform.rotation = targetRotation;
            }
        }

        private void UpdateWristMountPosition()
        {
            // When parented to wrist, we just ensure the local offset is maintained
            // The position/rotation is automatically updated by the parent transform
            // We can optionally add smoothing or billboarding here

            if (positionSmoothSpeed > 0)
            {
                // Smooth the local position/rotation for more natural movement
                uiTransform.localPosition = Vector3.Lerp(uiTransform.localPosition, wristPositionOffset, Time.deltaTime * positionSmoothSpeed);
                uiTransform.localRotation = Quaternion.Slerp(uiTransform.localRotation, Quaternion.Euler(wristRotationOffset), Time.deltaTime * positionSmoothSpeed);
            }
        }

        /// <summary>
        /// Programmatically set the controller mode text.
        /// </summary>
        public void SetControllerModeText(string text)
        {
            controllerModeText = text;
            if (_currentMode == InputMode.Controllers && instructionText != null)
            {
                instructionText.text = text;
            }
        }

        /// <summary>
        /// Programmatically set the hands mode text.
        /// </summary>
        public void SetHandsModeText(string text)
        {
            handsModeText = text;
            if (_currentMode == InputMode.Hands && instructionText != null)
            {
                instructionText.text = text;
            }
        }

        /// <summary>
        /// Programmatically set the hands UI position mode.
        /// </summary>
        public void SetHandsPositionMode(HandsUIPositionMode mode)
        {
            handsPositionMode = mode;
            if (_currentMode == InputMode.Hands)
            {
                UpdatePositionMode(InputMode.Hands);
            }
        }
    }
}
