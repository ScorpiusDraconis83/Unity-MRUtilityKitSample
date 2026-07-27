// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR.MRUtilityKitSamples.HandInput;
using Meta.XR.Samples;
using MRUtilityKitSample.NavMesh;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Meta.XR.MRUtilityKitSamples.FindMultiSpawn
{
    [MetaCodeSample("MRUKSample-FindMultiSpawn")]
    public class GunShot : MonoBehaviour
    {
        [SerializeField] private GameObject _slider;
        [SerializeField] private GameObject _muzzleFlashVFX;
        [SerializeField] private GameObject _shell;
        [SerializeField] private Transform _shellEmitter;
        [SerializeField] private AnimationCurve _sliderCurve;
        [SerializeField] private AnimationCurve _recoilCurve;
        [SerializeField] private Vector3 _recoilPosition;
        [SerializeField] private Quaternion _recoilRotation;

        [SerializeField] private Transform _gunBarrel;
        [SerializeField] private GameObject _bullet;
        [SerializeField] private float _bulletSpeed = 10;

        [Header("Hand Mode Offset")]
        [Tooltip("Local rotation offset applied when using hands to align the gun with hand pointing direction.")]
        [SerializeField] private Vector3 _handModeRotationOffset = new Vector3(0f, 90f, 90f);

        private Vector3 _sliderStartPos;
        private float _timer;
        private Transform _container;
        private Coroutine _sliderAnimationCoroutine;
        private Coroutine _recoilAnimationCoroutine;
        [Header("Audio")]
        private AudioSource _audioSource;
        [SerializeField] private AudioClip[] _shotSound;
        [SerializeField] private float _volume = 1;

        private Quaternion _controllerModeLocalRotation;
        private bool _isHandMode;


        private void Start()
        {
            _sliderStartPos = _slider.transform.localPosition;
            _container = transform.GetChild(0);
            _audioSource = GetComponent<AudioSource>();

            // Store the original local rotation (for controller mode)
            _controllerModeLocalRotation = transform.localRotation;

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
            Debug.Log("### On Input Mode Changed: " + mode.ToString() + ", isHandMode: " + _isHandMode + " ###");
        }

        private void LateUpdate()
        {
            // Apply rotation continuously in LateUpdate to override any other rotation changes
            if (_isHandMode)
            {
                // Hardcode the rotation to bypass any serialization issues
                // Barrel aligned with fingers, gun flat on left side, trigger/handle pointing right
                transform.localEulerAngles = new Vector3(0f, 0f, 90f);
            }
            else
            {
                transform.localRotation = _controllerModeLocalRotation;
            }
        }

        private void Update()
        {
            if (Keyboard.current?.spaceKey.wasPressedThisFrame == true || OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger) ||
                HandInputManager.Instance.ThumbTapDown)
            {
                Shoot();
            }

            _timer += Time.deltaTime;
        }

        private void Shoot()
        {
            if (_sliderAnimationCoroutine != null)
            {
                StopCoroutine(_sliderAnimationCoroutine);
            }

            if (_recoilAnimationCoroutine != null)
            {
                StopCoroutine(_recoilAnimationCoroutine);
            }

            if (_shell)
            {
                var shellInstance = Instantiate(_shell, _shellEmitter.position, _shellEmitter.rotation);
                shellInstance.AddComponent<DestroyAfterSeconds>().Seconds = 2;
                var shellRigidbody = shellInstance.GetComponent<Rigidbody>();
                shellRigidbody.AddForce(_shellEmitter.up * Random.Range(4, 8));
                shellRigidbody.angularVelocity = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f) * Random.Range(-100, 100));
            }

            if (_muzzleFlashVFX.activeSelf)
            {
                _muzzleFlashVFX.SetActive(false);
            }

            _muzzleFlashVFX.SetActive(true);
            _timer = 0;
            _sliderAnimationCoroutine = StartCoroutine(SliderAnimation());
            _recoilAnimationCoroutine = StartCoroutine(RecoilAnimation());
            var _instBullet = Instantiate(_bullet, _gunBarrel.position, _gunBarrel.rotation).GetComponent<Bullet>();
            _instBullet.Speed = _bulletSpeed;
            _instBullet.GunAudioSource = _audioSource;
            AudioSource.PlayClipAtPoint(_shotSound[Random.Range(0, _shotSound.Length)], transform.position, _volume);
        }

        private IEnumerator SliderAnimation()
        {
            while (_timer < _sliderCurve.keys[_sliderCurve.length - 1].time)
            {
                _slider.transform.localPosition = _sliderStartPos + _sliderCurve.Evaluate(_timer) * Vector3.forward;
                yield return null;
            }

            _slider.transform.localPosition = _sliderStartPos;
        }

        private IEnumerator RecoilAnimation()
        {
            while (_timer < _recoilCurve.keys[_recoilCurve.length - 1].time)
            {
                _container.localPosition = Vector3.Lerp(Vector3.zero, _recoilPosition, _recoilCurve.Evaluate(_timer));
                _container.localRotation =
                    Quaternion.Lerp(Quaternion.identity, _recoilRotation, _recoilCurve.Evaluate(_timer));

                yield return null;
            }

            _container.transform.localPosition = Vector3.zero;
            _container.transform.localRotation = Quaternion.identity;
        }

        [ContextMenu("Assign Current Pose to Recoil")]
        public void AssignCurrentPoseToRecoil()
        {
            var container = transform.GetChild(0);
            _recoilPosition = container.localPosition;
            _recoilRotation = container.localRotation;
        }
    }
}
