// Copyright (c) Meta Platforms, Inc. and affiliates.


using Meta.XR.MRUtilityKitSamples;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.SceneManagement;
using Meta.XR.MRUtilityKitSamples.HandInput;

namespace Meta.XR.MRUtilityKitSamples.StartScene
{
    [MetaCodeSample("MRUKSample-StartScene")]
    public class ReturnToStartScene : MonoBehaviour
    {
        [SerializeField] private GameObject Tooltip;
        private static ReturnToStartScene _instance;
        private const string _startSceneName = "StartScene";
        private bool _showStartButtonTooltip => SceneManager.GetActiveScene().name != _startSceneName;
        private const float _forwardTooltipOffset = -0.05f;
        private const float _upwardTooltipOffset = -0.003f;

        private const float _wristDownOffset = -0.05f;
        private static readonly Quaternion _wristRotationOffset = Quaternion.Euler(0, -90, 0);

        private Transform _leftControllerAnchor;
        private Transform _leftHandAnchor;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                SceneManager.sceneLoaded += (_, _) =>
                {
                    Tooltip.SetActive(_showStartButtonTooltip);
                    var cameraRig = FindAnyObjectByType<OVRCameraRig>();
                    if (cameraRig != null)
                    {
                        _leftControllerAnchor = cameraRig.leftControllerAnchor;
                        _leftHandAnchor = cameraRig.leftHandAnchor;
                    }
                };
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }

            var rig = FindAnyObjectByType<OVRCameraRig>();
            if (rig != null)
            {
                _leftControllerAnchor = rig.leftControllerAnchor;
                _leftHandAnchor = rig.leftHandAnchor;
            }
        }


        private void Update()
        {
            if (OVRInput.GetUp(OVRInput.Button.Start) && SceneManager.GetActiveScene().name != _startSceneName)
            {
                SceneManager.LoadScene(0);
            }

            Tooltip.SetActive(_showStartButtonTooltip);

            if (!_showStartButtonTooltip)
            {
                return;
            }

            bool isUsingHands = HandInputManager.Instance != null &&
                                HandInputManager.Instance.CurrentInputMode == InputMode.Hands;

            if (isUsingHands && _leftHandAnchor != null)
            {
                var finalRotation = _leftHandAnchor.rotation * _wristRotationOffset;
                var finalPosition = _leftHandAnchor.position + _leftHandAnchor.up * _wristDownOffset;
                Tooltip.transform.rotation = finalRotation;
                Tooltip.transform.position = finalPosition;
            }
            else if (_leftControllerAnchor != null)
            {
                var finalRotation = _leftControllerAnchor.rotation * Quaternion.Euler(45, 0, 0);
                var forwardOffsetPosition = finalRotation * Vector3.forward * _forwardTooltipOffset;
                var upwardOffsetPosition = finalRotation * Vector3.up * _upwardTooltipOffset;
                var finalPosition = _leftControllerAnchor.position +
                                   forwardOffsetPosition + upwardOffsetPosition;
                Tooltip.transform.rotation = finalRotation;
                Tooltip.transform.position = finalPosition;
            }
        }
    }
}
