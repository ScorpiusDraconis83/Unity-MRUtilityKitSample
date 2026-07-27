// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.MRUtilityKit;
using Meta.XR.MRUtilityKitSamples.HandInput;
using Meta.XR.Samples;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Meta.XR.MRUtilityKitSamples.EnvironmentPanelPlacement
{
    [MetaCodeSample("MRUKSample-EnvironmentPanelPlacement")]
    public class EnvironmentPanelPlacement : MonoBehaviour
    {
        private const string WORLD_LOCK_STATUS_ON = "<color=#00FAFF><b>ON</b></color>";
        private const string WORLD_LOCK_STATUS_OFF = "<color=\"orange\"><b>OFF</b></color>";
        private const string WORLD_LOCK_ENABLE_ACTION = "(Use <b>X</b> to toggle this feature)";

        [SerializeField] private EnvironmentRaycastManager _raycastManager;
        [SerializeField] private Transform _centerEyeAnchor;
        [SerializeField] private Transform _raycastAnchor;
        [SerializeField] private OVRInput.RawButton _grabButton = OVRInput.RawButton.RIndexTrigger | OVRInput.RawButton.RHandTrigger;
        [SerializeField] private OVRInput.RawAxis2D _scaleAxis = OVRInput.RawAxis2D.RThumbstick;
        [SerializeField] private OVRInput.RawAxis2D _moveAxis = OVRInput.RawAxis2D.RThumbstick;
        [SerializeField] private Transform _panel;
        [SerializeField] private float _panelAspectRatio = 0.823f;
        [SerializeField] private GameObject _panelGlow;
        [SerializeField] private TextMeshProUGUI _worldLockStatus;
        [SerializeField] private LineRenderer _raycastVisualizationLine;
        [SerializeField] private Transform _raycastVisualizationNormal;

        [Header("Hand Input Settings")]
        [SerializeField] private float _microGestureScaleSpeed = 0.5f;
        [SerializeField] private float _microGestureMoveSpeed = 1.0f;

        private readonly RollingAverage _rollingAverageFilter = new RollingAverage();
        private Pose? _targetPose;
        private Vector3 _positionVelocity;
        private float _rotationVelocity;
        private bool _isGrabbingWithController;
        private bool _isGrabbingWithHand;
        private float _distanceFromController;
        private Pose? _environmentPose;
        private EnvironmentRaycastHitStatus _currentEnvHitStatus;
        private OVRCameraRig _cameraRig;

        private void Awake()
        {
            _cameraRig = FindAnyObjectByType<OVRCameraRig>();
        }

        private IEnumerator Start()
        {
            // Wait until headset starts tracking
            enabled = false;
            while (!OVRPlugin.userPresent || !OVRManager.isHmdPresent)
            {
                yield return null;
            }
            yield return null;
            enabled = true;

            // Place the panel in front of the user
            var position = _centerEyeAnchor.position + _centerEyeAnchor.forward;
            var forward = Vector3.ProjectOnPlane(_centerEyeAnchor.position - position, Vector3.up).normalized;
            _panel.position = position;
            _panel.rotation = Quaternion.LookRotation(forward);

        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                _isGrabbingWithController = false;
                _isGrabbingWithHand = false;
                _targetPose = null;
            }
        }

        private void Update()
        {
            if (!Application.isFocused && !Application.isEditor)
            {
                return;
            }

            VisualizeRaycast();
            if (_isGrabbingWithController || _isGrabbingWithHand)
            {
                UpdateTargetPose();
                UpdateGrabbingWithMicrogestures();

                if (_isGrabbingWithController && OVRInput.GetUp(_grabButton))
                {
                    _isGrabbingWithController = false;
                }

                if (_isGrabbingWithHand && !IsHandPinching())
                {
                    _isGrabbingWithHand = false;
                }

                if (!(_isGrabbingWithController || _isGrabbingWithHand))
                {
                    _panelGlow.SetActive(false);
                    _environmentPose = null;
                }
            }
            else
            {
                // Animate scale with right thumbstick or microgestures
                const float scaleSpeed = 1.5f;
                var panelScale = _panel.localScale.x;
                float scaleInput = OVRInput.Get(_scaleAxis).y;

                // Use microgestures for scaling when in hand tracking mode
                scaleInput += GetMicrogestureScaleInput();

                panelScale *= 1f + scaleInput * scaleSpeed * Time.deltaTime;
                panelScale = Mathf.Clamp(panelScale, 0.2f, 1.5f);
                _panel.localScale = new Vector3(panelScale, panelScale * _panelAspectRatio, 1f);

                // Detect grab gesture and update grab indicator
                bool didHitPanel = Physics.Raycast(GetRaycastRay(), out var hit) && hit.transform == _panel;
                _panelGlow.SetActive(didHitPanel);
                if (didHitPanel)
                {
                    if (OVRInput.GetDown(_grabButton))
                    {
                        _isGrabbingWithController = true;
                    }
                    if (DidHandPinchStart())
                    {
                        _isGrabbingWithHand = true;
                    }
                    if (_isGrabbingWithController || _isGrabbingWithHand)
                    {
                        _distanceFromController = Vector3.Distance(_raycastAnchor.position, _panel.position);
                    }
                }
            }
            AnimatePanelPose();

            if (OVRInput.GetUp(OVRInput.Button.Three))
            {
                MRUK.Instance.EnableWorldLock = !MRUK.Instance.EnableWorldLock;
            }
            if (_worldLockStatus)
            {
                string wlStatus = MRUK.Instance.IsWorldLockActive ? WORLD_LOCK_STATUS_ON : WORLD_LOCK_STATUS_OFF;
                _worldLockStatus.text = $"World Lock Active: {wlStatus}\n {WORLD_LOCK_ENABLE_ACTION}";
            }
        }

        private Ray GetRaycastRay()
        {
            return new Ray(_raycastAnchor.position + _raycastAnchor.forward * 0.1f, _raycastAnchor.forward);
        }

        /// <summary>
        /// Gets the scale input from microgestures (swipe up/down).
        /// Swipe up = scale up, Swipe down = scale down.
        /// </summary>
        private float GetMicrogestureScaleInput()
        {
            if (HandInputManager.Instance == null || HandInputManager.Instance.CurrentInputMode != InputMode.Hands)
            {
                return 0f;
            }

            // Swipe backward (up) = scale up, Swipe forward (down) = scale down
            if (HandInputManager.Instance.IsSwipeBackwardActive)
            {
                return _microGestureScaleSpeed;
            }
            if (HandInputManager.Instance.IsSwipeForwardActive)
            {
                return -_microGestureScaleSpeed;
            }
            return 0f;
        }

        /// <summary>
        /// Updates panel distance using microgestures while grabbing.
        /// Uses continuous pinch drag for analog stick-like behavior (move hand up/down while pinching).
        /// Swipe up = move away, Swipe down = move closer.
        /// </summary>
        private void UpdateGrabbingWithMicrogestures()
        {
            if (HandInputManager.Instance == null || HandInputManager.Instance.CurrentInputMode != InputMode.Hands)
            {
                return;
            }

            // Use continuous pinch drag if active (analog stick-like behavior)
            // This uses hand vertical movement during the index pinch used for grabbing
            if (HandInputManager.Instance.IsPinchDragActive)
            {
                // PinchDragValue is -1 to 1, similar to thumbstick input
                // Positive = up/away, Negative = down/closer
                _distanceFromController += HandInputManager.Instance.PinchDragValue * _microGestureMoveSpeed * Time.deltaTime;
            }
            // Fall back to discrete swipe gestures if pinch drag isn't active
            else if (HandInputManager.Instance.IsSwipeBackwardActive)
            {
                _distanceFromController += _microGestureMoveSpeed * Time.deltaTime;
            }
            else if (HandInputManager.Instance.IsSwipeForwardActive)
            {
                _distanceFromController -= _microGestureMoveSpeed * Time.deltaTime;
            }
            _distanceFromController = Mathf.Clamp(_distanceFromController, 0.3f, float.MaxValue);
        }

        /// <summary>
        /// Checks if the hand just started an index+thumb pinch (like GetDown for buttons).
        /// Uses the primary hand (right) for grabbing.
        /// </summary>
        private bool DidHandPinchStart()
        {
            if (HandInputManager.Instance == null || HandInputManager.Instance.CurrentInputMode != InputMode.Hands)
            {
                return false;
            }

            // Check if right hand index pinch just started (A button equivalent)
            // We check the event since IsIndexPinching is continuous
            return HandInputManager.Instance.IsIndexPinching && !_wasHandPinching;
        }

        /// <summary>
        /// Checks if the hand is currently performing an index+thumb pinch.
        /// </summary>
        private bool IsHandPinching()
        {
            if (HandInputManager.Instance == null || HandInputManager.Instance.CurrentInputMode != InputMode.Hands)
            {
                return false;
            }

            return HandInputManager.Instance.IsIndexPinching;
        }

        private bool _wasHandPinching;

        private void LateUpdate()
        {
            // Track previous pinch state for edge detection
            if (HandInputManager.Instance != null)
            {
                _wasHandPinching = HandInputManager.Instance.IsIndexPinching;
            }
        }

        private void UpdateTargetPose()
        {
            // Animate manual placement position with right thumbstick
            const float moveSpeed = 2.5f;
            _distanceFromController += OVRInput.Get(_moveAxis).y * moveSpeed * Time.deltaTime;
            _distanceFromController = Mathf.Clamp(_distanceFromController, 0.3f, float.MaxValue);

            // Try place the panel onto environment
            var newEnvPose = TryGetEnvironmentPose();
            if (newEnvPose.HasValue)
            {
                _environmentPose = newEnvPose.Value;
            }
            else if (_currentEnvHitStatus == EnvironmentRaycastHitStatus.HitPointOutsideOfCameraFrustum)
            {
                _environmentPose = null;
            }
            var manualPlacementPosition = _raycastAnchor.position + _raycastAnchor.forward * _distanceFromController;
            var panelForward = Vector3.ProjectOnPlane(_centerEyeAnchor.position - manualPlacementPosition, Vector3.up).normalized;
            var manualPlacementPose = new Pose(manualPlacementPosition, Quaternion.LookRotation(panelForward));
            // If environment pose is available and the panel is closer to it than to the user, place the panel onto environment to create a magnetism effect
            bool chooseEnvPose = _environmentPose.HasValue && Vector3.Distance(manualPlacementPose.position, _environmentPose.Value.position) / Vector3.Distance(manualPlacementPose.position, _centerEyeAnchor.position) < 0.5;
            _targetPose = chooseEnvPose ? _environmentPose.Value : manualPlacementPose;
        }

        private Pose? TryGetEnvironmentPose()
        {
            var ray = GetRaycastRay();
            if (!_raycastManager.Raycast(ray, out var hit) || hit.normalConfidence < 0.5f)
            {
                return null;
            }
            bool isCeiling = Vector3.Dot(hit.normal, Vector3.down) > 0.7f;
            if (isCeiling)
            {
                return null;
            }
            const float sizeTolerance = 0.2f;
            var panelSize = new Vector3(_panel.localScale.x, _panel.localScale.y, 0f) * (1f - sizeTolerance);
            bool isVerticalSurface = Mathf.Abs(Vector3.Dot(hit.normal, Vector3.up)) < 0.3f;
            if (isVerticalSurface)
            {
                // If the surface is vertical, stick the panel to the surface
                if (_raycastManager.PlaceBox(ray, panelSize, Vector3.up, out var result))
                {
                    // Apply the rolling average filter to smooth the normal
                    var smoothedNormal = _rollingAverageFilter.UpdateRollingAverage(result.normal);
                    return new Pose(result.point, Quaternion.LookRotation(smoothedNormal, Vector3.up));
                }
            }
            else
            {
                // Position the panel upright and check collisions with environment
                var position = hit.point + Vector3.up * _panel.localScale.y * 0.5f;
                var halfExtents = panelSize * 0.5f;
                var forward = Vector3.ProjectOnPlane(_centerEyeAnchor.position - position, Vector3.up).normalized;
                var orientation = Quaternion.LookRotation(forward, Vector3.up);
                const float collisionCheckOffset = 0.1f;
                if (!_raycastManager.CheckBox(position + Vector3.up * collisionCheckOffset, halfExtents, orientation))
                {
                    return new Pose(position, orientation);
                }
            }
            return null;
        }

        private void AnimatePanelPose()
        {
            if (!_targetPose.HasValue)
            {
                return;
            }

            const float smoothTime = 0.13f;
            _panel.position = Vector3.SmoothDamp(_panel.position, _targetPose.Value.position, ref _positionVelocity, smoothTime);

            float angle = Quaternion.Angle(_panel.rotation, _targetPose.Value.rotation);
            if (angle > 0f)
            {
                float dampedAngle = Mathf.SmoothDampAngle(angle, 0f, ref _rotationVelocity, smoothTime);
                float t = 1f - dampedAngle / angle;
                _panel.rotation = Quaternion.SlerpUnclamped(_panel.rotation, _targetPose.Value.rotation, t);
            }
        }

        private void VisualizeRaycast()
        {
            var ray = GetRaycastRay();
            bool hasHit = RaycastPanelOrEnvironment(ray, out var hit) || hit.status == EnvironmentRaycastHitStatus.HitPointOccluded;
            bool hasNormal = hit.normalConfidence > 0f;
            _raycastVisualizationLine.enabled = hasHit;
            _raycastVisualizationNormal.gameObject.SetActive(hasHit && hasNormal);
            if (hasHit)
            {
                _raycastVisualizationLine.SetPosition(0, ray.origin);
                _raycastVisualizationLine.SetPosition(1, hit.point);

                if (hasNormal)
                {
                    _raycastVisualizationNormal.SetPositionAndRotation(hit.point, Quaternion.LookRotation(hit.normal));
                }
            }

        }

        private bool RaycastPanelOrEnvironment(Ray ray, out EnvironmentRaycastHit envHit)
        {
            if (Physics.Raycast(ray, out var physicsHit) && physicsHit.transform == _panel)
            {
                envHit = new EnvironmentRaycastHit
                {
                    status = EnvironmentRaycastHitStatus.Hit,
                    point = physicsHit.point,
                    normal = physicsHit.normal,
                    normalConfidence = 1f
                };
                return true;
            }
            bool envHitResult = _raycastManager.Raycast(ray, out envHit);
            _currentEnvHitStatus = envHit.status;
            return envHitResult;
        }

        private class RollingAverage
        {
            private List<Vector3> _normals;
            private int _currentRollingAverageIndex;

            public Vector3 UpdateRollingAverage(Vector3 current)
            {
                if (_normals == null)
                {
                    const int filterSize = 10;
                    _normals = Enumerable.Repeat(current, filterSize).ToList();
                }
                _currentRollingAverageIndex++;
                _normals[_currentRollingAverageIndex % _normals.Count] = current;
                Vector3 result = default;
                foreach (var normal in _normals)
                {
                    result += normal;
                }
                return result.normalized;
            }
        }
    }
}
