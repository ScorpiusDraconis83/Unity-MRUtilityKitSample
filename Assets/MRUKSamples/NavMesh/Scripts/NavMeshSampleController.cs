// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.MRUtilityKit;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.AI;

namespace MRUtilityKitSample.NavMesh
{
    [MetaCodeSample("MRUK-NavMesh")]
    public class NavMeshSampleController : MonoBehaviour
    {
        public SceneNavigation SceneNavigation;
        public bool useGlobalMesh;
        private NavMeshAgentController _navMeshAgentController;
        private ImmersiveSceneDebugger _immersiveSceneDebugger;
        private Animator _animator;
        private NavMeshAgent _agent;

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _agent = GetComponent<NavMeshAgent>();
            _navMeshAgentController = FindAnyObjectByType<NavMeshAgentController>();
            _immersiveSceneDebugger = FindAnyObjectByType<ImmersiveSceneDebugger>();
        }

        private void Update()
        {
            if (OVRInput.GetDown(OVRInput.RawButton.LIndexTrigger) ||
                OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger))
            {
                if (!SceneNavigation)
                {
                    return;
                }

                useGlobalMesh = !useGlobalMesh;
                SceneNavigation.ToggleGlobalMeshNavigation(useGlobalMesh);
                if (_immersiveSceneDebugger)
                {
                    _immersiveSceneDebugger.DisplayNavMesh(false);
                    _immersiveSceneDebugger.DisplayNavMesh(true);
                }

                if (_navMeshAgentController)
                {
                    _navMeshAgentController.SetNewTargetObjectAndPostion();
                }
            }

            if (_animator && _agent)
            {
                AnimateAgent();
            }
        }

        private void AnimateAgent()
        {
        }
    }
}
