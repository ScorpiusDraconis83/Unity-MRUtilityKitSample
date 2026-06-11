// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR.MRUtilityKit;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Meta.XR.MRUtilityKitSamples.PassthroughRelighting
{
    /// <summary>
    ///     Listens to the user's input, moves and animates Oppy accordingly.
    /// </summary>
    [MetaCodeSample("MRUKSample-PassthroughRelighting")]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class OppyCharacterController : MonoBehaviour
    {
        [SerializeField] private Transform respawnTransform;
        [SerializeField] private float _speed = 1;
        [SerializeField] private float _turningLerp = 10;
        [SerializeField] OVRInput.RawButton _jumpButton = OVRInput.RawButton.A;
        [SerializeField] private float _jumpPower = .3f;
        [SerializeField] private float _jumpMultiplier = 6;
        float _finaljumpMultiplier = 1;
        private const float _gravity = 9.8f;
        [SerializeField] private float _gravityMultiplier = 1;
        private float _fallVelocity = 0;
        private CharacterController _characterController;
        private Camera _cam;
        private Animator _animator;
        private int grounded = Animator.StringToHash("Grounded");
        private int locomotionSpeed = Animator.StringToHash("LocomotionSpeed");
        private int jumpTrigger = Animator.StringToHash("Jump");
        private int wonder = Animator.StringToHash("Wonder");
        [SerializeField, Tooltip("How much of the character's center must be over ground to be considered grounded (0-1)")]
        private float _groundCheckRadius = 0.5f;
        [SerializeField, Tooltip("How fast the character slides off edges when hanging")]
        private float _edgeSlideSpeed = 2f;
        [SerializeField, Tooltip("Time in seconds after leaving ground where jump is still allowed")]
        private float _coyoteTime = 0.15f;
        private float _timeSinceGrounded;
        private Vector3 _viewAlignedMovingDirection;
        private float _inactivityTimer;
        private float _inactivityThreshold;
        [SerializeField] Transform _leftFoot;
        [SerializeField] Transform _rightFoot;

        private InputAction _moveAction;
        private InputAction _jumpAction;

        void Start()
        {
            _characterController = GetComponent<CharacterController>();
            _animator = GetComponent<Animator>();
            _cam = Camera.main;

            // Disable until MRUK scene is loaded to prevent falling through unloaded floor
            _characterController.enabled = false;

            if (MRUK.Instance != null)
            {
                MRUK.Instance.RegisterSceneLoadedCallback(OnSceneLoaded);
            }
        }

        private void OnSceneLoaded()
        {
            Respawn();
        }

        void Awake()
        {
            _moveAction = new InputAction("Move", InputActionType.Value);
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            _jumpAction = new InputAction("Jump", InputActionType.Button, binding: "<Keyboard>/space");
        }

        void OnEnable()
        {
            _moveAction.Enable();
            _jumpAction.Enable();
        }

        void OnDisable()
        {
            _moveAction?.Disable();
            _jumpAction?.Disable();
        }

        void OnDestroy()
        {
            _moveAction?.Dispose();
            _jumpAction?.Dispose();

            if (MRUK.Instance != null)
            {
                MRUK.Instance.SceneLoadedEvent.RemoveListener(OnSceneLoaded);
            }
        }
        void Update()
        {
            if (!_characterController.enabled)
                return;
            ReceiveInputs();
            Rotate();
            Gravity();
            Move();
            HandleEdgeSlide();
            GroundContactAndReadyToJump();
            Inactivity();
        }
        void LateUpdate()
        {
            AlignFeetToSlope();
        }
        void ReceiveInputs()
        {
            Vector2 moveInput = _moveAction.ReadValue<Vector2>();
            Vector3 horizontalAndVertical;
            Vector2 ovrStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
            if (ovrStick == Vector2.zero)
            {
                horizontalAndVertical = new Vector3(moveInput.x, 0, moveInput.y);
            }
            else
            {
                horizontalAndVertical = new Vector3(ovrStick.x, 0, ovrStick.y);
            }
            Vector3 viewerProjectedForward = Vector3.ProjectOnPlane(_cam.transform.forward, Vector3.up);
            Vector3 viewerProjectedRight = Vector3.ProjectOnPlane(_cam.transform.right, Vector3.up);
            _viewAlignedMovingDirection = (viewerProjectedForward * horizontalAndVertical.z * _speed) + (viewerProjectedRight * horizontalAndVertical.x * _speed);
        }
        void Gravity()
        {
            _fallVelocity -= _gravity * _gravityMultiplier * Time.deltaTime;
        }
        void Move()
        {
            _characterController.Move(new Vector3(_viewAlignedMovingDirection.x * Time.deltaTime, _fallVelocity, _viewAlignedMovingDirection.z * Time.deltaTime));
        }
        void Rotate()
        {
            if (_viewAlignedMovingDirection != Vector3.zero)
            {
                ResetInactivitySettings();
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(_viewAlignedMovingDirection), _turningLerp * Time.deltaTime);
                if (_animator)
                {
                    // Only update locomotion speed when grounded - don't run in mid-air
                    if (IsProperlyGrounded())
                    {
                        float prevLocomotionSpeed = _animator.GetFloat(locomotionSpeed);
                        _animator.SetFloat(locomotionSpeed, Mathf.Lerp(prevLocomotionSpeed, _viewAlignedMovingDirection.magnitude, 5 * Time.deltaTime));
                    }
                    else
                    {
                        _animator.SetFloat(locomotionSpeed, 0);
                    }
                }
            }
            else
            {
                _animator.SetFloat(locomotionSpeed, 0);
            }
        }
        bool IsProperlyGrounded()
        {
            return IsProperlyGrounded(out _);
        }

        bool IsProperlyGrounded(out RaycastHit groundHit)
        {
            groundHit = default;

            // First check Unity's built-in ground detection
            if (!_characterController.isGrounded)
            {
                return false;
            }

            // Additional check: ensure ground is beneath character's center, not just the edge
            float checkRadius = _characterController.radius * _groundCheckRadius;
            Vector3 origin = transform.position + Vector3.up * (checkRadius + 0.05f);

            return Physics.SphereCast(origin, checkRadius, Vector3.down, out groundHit, _characterController.skinWidth + 0.1f);
        }

        void HandleEdgeSlide()
        {
            // If CharacterController thinks we're grounded but our center check disagrees,
            // the character is hanging off an edge - push them off
            if (!_characterController.isGrounded)
            {
                return;
            }

            if (IsProperlyGrounded())
            {
                return;
            }

            // Find the direction to slide off by checking where there's no ground
            Vector3 slideDirection = Vector3.zero;
            float checkDistance = _characterController.radius * 1.5f;
            Vector3 origin = transform.position + Vector3.up * 0.1f;

            // Check in 8 directions to find where there's no ground
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 checkPos = origin + dir * checkDistance;

                if (!Physics.Raycast(checkPos, Vector3.down, 0.3f))
                {
                    slideDirection += dir;
                }
            }

            if (slideDirection == Vector3.zero)
            {
                return;
            }

            slideDirection.Normalize();

            // Don't slide off if the player is actively trying to move onto the platform
            // (i.e., moving in the opposite direction of the slide)
            Vector3 flatMovement = new Vector3(_viewAlignedMovingDirection.x, 0, _viewAlignedMovingDirection.z);
            if (flatMovement.sqrMagnitude > 0.01f)
            {
                float dotProduct = Vector3.Dot(flatMovement.normalized, slideDirection);
                // If player is moving against the slide direction (trying to climb), don't push them off
                if (dotProduct < -0.3f)
                {
                    return;
                }
            }

            _characterController.Move(slideDirection * _edgeSlideSpeed * Time.deltaTime);
        }

        void GroundContactAndReadyToJump()
        {
            bool isProperlyGrounded = IsProperlyGrounded();
            // For animations, use CharacterController's grounded (includes steep slopes)
            // For jumping/edge mechanics, use our stricter center-based check
            bool isTouchingGround = _characterController.isGrounded;

            // Track time since last properly grounded for coyote time (jumping purposes)
            if (isProperlyGrounded)
            {
                _timeSinceGrounded = 0f;
            }
            else
            {
                _timeSinceGrounded += Time.deltaTime;
            }

            bool canJump = isProperlyGrounded || _timeSinceGrounded <= _coyoteTime;

            if (isTouchingGround)
            {
                _fallVelocity = -.01f;
            }

            if (canJump)
            {
                if ((_jumpAction.WasPressedThisFrame() || OVRInput.GetDown(_jumpButton)) && !IsInvoking("Jump"))
                {
                    _finaljumpMultiplier = 1;
                    _timeSinceGrounded = _coyoteTime + 1f; // Prevent double jumps
                    ResetInactivitySettings();
                    _animator.SetTrigger(jumpTrigger);
                    Invoke("Jump", .2f);
                }
                // Only accumulate jump power while jump is charging (after pressing, before executing)
                if (IsInvoking("Jump") && (_jumpAction.IsPressed() || OVRInput.Get(_jumpButton)))
                {
                    _finaljumpMultiplier += _jumpMultiplier * Time.deltaTime;
                }
            }
            else
            {
                CancelInvoke("Jump");
            }
            // Use isTouchingGround for animation so character doesn't "fall" on steep slopes
            _animator.SetBool(grounded, isTouchingGround);
        }
        void Inactivity()
        {
            _inactivityTimer += Time.deltaTime;
            if (_inactivityTimer > _inactivityThreshold + 3)//the +3 is to give time to the animator to enter the next animation
            {
                float anim = _animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                if (anim > .9f)
                {
                    ResetInactivitySettings();
                }
            }
            if (_inactivityTimer > _inactivityThreshold)
            {
                _animator.SetBool(wonder, true);
            }
        }
        void ResetInactivitySettings()
        {
            _inactivityThreshold = Random.Range(5, 10);
            _inactivityTimer = 0;
            _animator.SetBool(wonder, false);
        }
        void Jump()
        {
            _fallVelocity = _jumpPower * _finaljumpMultiplier;
            _finaljumpMultiplier = 1;
            _animator.ResetTrigger(jumpTrigger);
        }
        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            //To quickly interrupt the jump if he tries to jump and hits his head
            if (Vector3.Dot(-Vector3.up, hit.normal) > .5f && _fallVelocity > 0)
            {
                CancelInvoke("Jump");
                _fallVelocity = -_fallVelocity;
            }
        }

        void AlignFeetToSlope()
        {
            if (_viewAlignedMovingDirection.sqrMagnitude < 0.001f)
            {
                RaycastHit hit;
                if (Physics.Raycast(_leftFoot.position, -Vector3.up, out hit, 100f))
                {
                    _leftFoot.rotation = Quaternion.FromToRotation(_leftFoot.up, hit.normal) * _leftFoot.rotation;
                }
                if (Physics.Raycast(_rightFoot.position, -Vector3.up, out hit, 100f))
                {
                    _rightFoot.rotation = Quaternion.FromToRotation(-_rightFoot.up, hit.normal) * _rightFoot.rotation;
                }
            }
        }
        [ContextMenu("Respawn")]
        public void Respawn()
        {
            if (_characterController == null)
            {
                _characterController = GetComponent<CharacterController>();
            }

            _characterController.enabled = false;
            var mruk = MRUK.Instance;
            if (mruk != null)
            {
                MRUKRoom currentRoom = mruk.GetCurrentRoom();
                Vector3 respawnPos = respawnTransform.position + respawnTransform.forward;
                if (currentRoom.IsPositionInRoom(respawnPos))
                {
                    transform.position = respawnPos;
                }
                else if (currentRoom.GenerateRandomPositionOnSurface(MRUK.SurfaceType.FACING_UP, minDistanceToEdge: _characterController.radius, new LabelFilter(MRUKAnchor.SceneLabels.FLOOR), out Vector3 randomPosOnFloor, out _))
                {
                    transform.position = randomPosOnFloor;
                }
                else if (currentRoom.GenerateRandomPositionInRoom(minDistanceToSurface: _characterController.radius, avoidVolumes: true) is Vector3 randomPos)
                {
                    transform.position = randomPos;
                }
                else
                {
                    transform.position = currentRoom.GetRoomBounds().center;
                }
            }
            _characterController.enabled = true;
        }
    }

}
