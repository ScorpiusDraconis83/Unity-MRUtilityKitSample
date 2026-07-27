// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;
using Meta.XR.MRUtilityKitSamples.HandInput;

namespace Meta.XR.MRUtilityKitSamples.BouncingBall
{
    [MetaCodeSample("MRUKSample-BouncingBall")]
    public class BouncingBallMgr : MonoBehaviour
    {
        [SerializeField] private Transform trackingSpace;
        [SerializeField] private Transform rightControllerPivot;
        [SerializeField] private GameObject ballPrefab;

        private BouncingBallLogic currentBall;
        private bool ballGrabbed;

        private void Update()
        {
            const OVRInput.RawButton grabButton = OVRInput.RawButton.RHandTrigger;
            if (!ballGrabbed && OVRInput.GetDown(grabButton))
            {
                currentBall = Instantiate(ballPrefab).GetComponent<BouncingBallLogic>();
                ballGrabbed = true;
            }

            if (ballGrabbed)
            {
                currentBall.Rigidbody.position = rightControllerPivot.position;
                if (OVRInput.GetUp(grabButton))
                {
                    var localVel = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.RTouch);
                    var vel = trackingSpace.TransformVector(localVel);
                    var angVel = OVRInput.GetLocalControllerAngularVelocityCw(OVRInput.Controller.RTouch);
                    currentBall.Release(rightControllerPivot.position, vel, angVel);
                    ballGrabbed = false;
                }
            }

            if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger) || HandInputManager.Instance.ThumbTapDown)
            {
                const float speed = 10f;
                var newBall = Instantiate(ballPrefab).GetComponent<BouncingBallLogic>();
                const float shiftToPreventCollisionWithGrabbedBall = 0.1f;
                var pos = rightControllerPivot.position +
                          rightControllerPivot.forward * shiftToPreventCollisionWithGrabbedBall;
                newBall.Release(pos, rightControllerPivot.forward * speed, Vector3.zero);
            }
        }
    }
}
