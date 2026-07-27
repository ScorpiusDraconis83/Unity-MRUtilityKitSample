// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Meta.XR.MRUtilityKitSamples.FindMultiSpawn
{
    [MetaCodeSample("MRUKSample-FindMultiSpawn")]
    public class Target : MonoBehaviour
    {
        public enum TargetType
        {
            floor,
            ceiling,
            floating,
            wall,
            surface
        }

        private static readonly int ShaderColor = Shader.PropertyToID("_Color");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        public TargetType targetType = TargetType.floor;
        [SerializeField] public Transform Reticle;
        [SerializeField] private float _destroyIfCloserThan = 1;
        [SerializeField] private Color _color;
        [Header("Floor Targets")]
        [SerializeField] private Vector2 _minMaxHeight = new(0.0f, .5f);

        [Header("Floating targets")]
        [SerializeField] private float _floatingAmplitude = 0.1f;
        [SerializeField] private float _floatingSpeed = 2f;
        private Camera cam;
        private Vector3 _initialPosition;
        private MultiSpawnMenu _multiSpawnMenu;

        // Start is called before the first frame update
        private void Start()
        {
            cam = Camera.main;
            _multiSpawnMenu = FindAnyObjectByType<MultiSpawnMenu>();
            if (Camera.main != null && Vector3.Distance(transform.position, Camera.main.transform.position) <
                _destroyIfCloserThan)
            {
                Destroy(gameObject);
            }

            Invoke(nameof(DestroyHiddenTargets), .1f);

            switch (targetType)
            {
                case TargetType.floor:
                {
                    SnapTargetToSurface();
                    OrientTorwardsCamera();
                    var allChildren = GetComponentsInChildren<Transform>();
                    foreach (Transform child in allChildren)
                    {
                        if (child.name == "pedestal")
                        {
                            child.localPosition = -Vector3.right * Random.Range(_minMaxHeight.x, _minMaxHeight.y);
                        }
                    }

                }
                break;
                case TargetType.ceiling:
                {
                    OrientTorwardsCamera(true);
                    break;
                }
                case TargetType.floating:
                {
                    _initialPosition = transform.position;
                }
                break;
                case TargetType.wall:
                {
                    SnapTargetToSurface();
                    transform.rotation = Quaternion.LookRotation(Vector3.up, transform.up);
                    transform.Rotate(transform.up, 90f, Space.World);
                }
                break;
                case TargetType.surface:
                {
                    SetReticleReticleAnimationRandomStart();
                    SnapTargetToSurface();
                }
                break;
            }
            SetActiveColor();
        }

        private void Update()
        {
            if (Keyboard.current?.hKey.wasPressedThisFrame == true)
            {
                DestroyHiddenTargets();
            }

            if (targetType == TargetType.floating)
            {
                transform.rotation = Quaternion.Lerp(transform.rotation,
                    Quaternion.LookRotation(cam.transform.position - transform.position), Time.deltaTime * 1);
                transform.position = _initialPosition + new Vector3(
                    Mathf.PerlinNoise(0, Time.time * _floatingSpeed) * _floatingAmplitude,
                    Mathf.PerlinNoise(0, (-Time.time + 10) * _floatingSpeed) * _floatingAmplitude,
                    Mathf.PerlinNoise(0, (Time.time + 10) * _floatingSpeed) * _floatingAmplitude);
            }
        }

        [ContextMenu("Check if hidden")]
        private bool IsHiddenBehindSomething()
        {
            var directionToCamera = (cam.transform.position - transform.position).normalized;
            var distanceToCamera = Vector3.Distance(transform.position, cam.transform.position);

            RaycastHit hit;
            // Cast ray from target towards camera
            if (Physics.Raycast(Reticle.position, directionToCamera, out hit, distanceToCamera))
            {
                // If ray hits something before reaching the camera, target is obstructed
                Debug.DrawRay(Reticle.position, directionToCamera * hit.distance, Color.red, 10f);
                return true;
            }

            Debug.DrawLine(Reticle.position, cam.transform.position, Color.green, 10f);
            return false;
        }

        private void DestroyHiddenTargets()
        {
            if (IsHiddenBehindSomething())
            {
                Destroy(gameObject);
            }
        }

        public void GetDestroyedByBullet()
        {
            if (_multiSpawnMenu)
            {
                _multiSpawnMenu.RemainingTargets--;
            }
            Reticle.gameObject.SetActive(false);
            StartCoroutine(TurnOffBaseLight());
        }

        public void EnableShootingTarget()
        {
            Reticle.gameObject.SetActive(true);
            SetActiveColor();
        }

        private void SetReticleReticleAnimationRandomStart()
        {
            if (Reticle.GetComponent<SimpleCurveBasedAnimation>() != null)
            {
                Reticle.GetComponent<SimpleCurveBasedAnimation>()._timeOffset = Random.Range(0f, 1f);
            }
        }

        private void SetActiveColor()
        {
            var baseRenderer = GetComponentInChildren<Renderer>();
            // Increase emission intensity
            var factor = Mathf.Pow(2, 2.5f);
            baseRenderer.material.SetColor(EmissionColor, _color * factor);

            var recticleRenderer = Reticle.GetComponentsInChildren<Renderer>();
            foreach (var r in recticleRenderer)
            {
                r.material.SetColor(ShaderColor, _color);
            }
        }

        private void OrientTorwardsCamera(bool upsideDown = false)
        {
            if (cam != null)
            {
                var directionTowardsCamera = (cam.transform.position - transform.position).normalized;
                transform.rotation = Quaternion.LookRotation(Vector3.up, directionTowardsCamera);
                if (!upsideDown)
                {
                    transform.Rotate(Vector3.right, 90f);
                }
                else
                {
                    transform.Rotate(Vector3.right, -90f);

                }
            }
        }
        private void SnapTargetToSurface()
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, -transform.up, out hit, 10f))
            {
                transform.position = hit.point;
            }
        }
        IEnumerator TurnOffBaseLight()
        {
            var baseRenderer = GetComponentInChildren<Renderer>();
            float timer = 0;
            while (timer < 1f)
            {
                timer += Time.deltaTime;
                baseRenderer.material.SetColor(EmissionColor, Color.Lerp(_color, Color.black, timer));
                yield return null;
            }
        }
    }
}
