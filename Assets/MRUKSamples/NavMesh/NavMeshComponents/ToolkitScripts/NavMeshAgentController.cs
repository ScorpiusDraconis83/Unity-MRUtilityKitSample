// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using Meta.XR.MRUtilityKitSamples.HandInput;
using Meta.XR.Samples;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace MRUtilityKitSample.NavMesh
{
    // Navmesh simplifies the job of navigating virtual characters through procedurally created environments.
    // By calling 'agent.SetDestination()' and providing a vector3, the agent will discover the best path to the target.
    // Object position and rotation will be animated to show movement along a chosen path.

    // Additionally, RandomNavPoint can be used for position finding (eg, discover a random floor area in the room for a minigolf hole)

    // The interval between changing positions is randomized as well as the characters speed

    [MetaCodeSample("MRUKSample-NavMesh")]
    public class NavMeshAgentController : MonoBehaviour
    {
        [SerializeField] private GameObject _positionIndicator;
        [HideInInspector] public GameObject PositionIndicatorInstance; // used to visualize the target position
        private NavMeshAgent _agent;
        private Animator _animator;

        private void Update()
        {
            if (Keyboard.current?.spaceKey.wasPressedThisFrame == true || OVRInput.GetDown(OVRInput.Button.One) ||
                OVRInput.GetDown(OVRInput.Button.Three) || HandInputManager.Instance.ThumbTapDown)
            {
                SetNewTargetObjectAndPostion();
            }
        }

        private void OnEnable()
        {
            _agent = GetComponent<NavMeshAgent>();
            Invoke("SetNewTargetObjectAndPostion", 1f);
        }

        public void SetNewTargetObjectAndPostion() //called by animation event
        {
            if (PositionIndicatorInstance)
            {
                Destroy(PositionIndicatorInstance);
            }
            PositionIndicatorInstance = Instantiate(_positionIndicator, SetNewDestination(), Quaternion.identity);
            var pos = PositionIndicatorInstance.transform.position;
            pos.y = MRUK.Instance.GetCurrentRoom().GetRoomBounds().min.y;
            PositionIndicatorInstance.transform.position = pos;
        }

        public Vector3 SetNewDestination()
        {
            // set the new set of randomized values for target position and speed
            var newPos = RandomNavPoint();

            var room = MRUK.Instance?.GetCurrentRoom();
            if (!room)
            {
                return _agent.transform.position;
            }

            var test = room.IsPositionInRoom(newPos,
                false); // occasionally NavMesh will generate areas outside the room, so we must test the value from RandomNavPoint

            if (!test)
            {
                Debug.Log("[NavMeshAgent] [Error]: destination is outside the room bounds, resetting to 0");
                newPos = Vector3.zero;
            }

            _agent.SetDestination(newPos);
            var newSpeed = Random.Range(1.2f, 1.6f);
            _agent.speed = newSpeed;
            return newPos;
        }

        // generate a new position on the NavMesh
        public static Vector3 RandomNavPoint()
        {
            // TODO: we can cache this and only update it once the navmesh changes
            var triangulation = UnityEngine.AI.NavMesh.CalculateTriangulation();

            if (triangulation.indices.Length == 0)
            {
                return Vector3.zero;
            }

            // Compute the area of each triangle and the total surface area of the navmesh
            var totalArea = 0.0f;
            var areas = new List<float>();
            for (var i = 0; i < triangulation.indices.Length;)
            {
                var i0 = triangulation.indices[i];
                var i1 = triangulation.indices[i + 1];
                var i2 = triangulation.indices[i + 2];
                var v0 = triangulation.vertices[i0];
                var v1 = triangulation.vertices[i1];
                var v2 = triangulation.vertices[i2];
                var cross = Vector3.Cross(v1 - v0, v2 - v0);
                var area = cross.magnitude * 0.5f;
                totalArea += area;
                areas.Add(area);
                i += 3;
            }

            // Pick a random triangle weighted by surface area (triangles with larger surface
            // area have more chance of being chosen)
            var rand = Random.Range(0, totalArea);
            var triangleIndex = 0;
            for (; triangleIndex < areas.Count - 1; ++triangleIndex)
            {
                rand -= areas[triangleIndex];
                if (rand <= 0.0f)
                {
                    break;
                }
            }

            {
                // Get the vertices of the chosen triangle
                var i0 = triangulation.indices[triangleIndex * 3];
                var i1 = triangulation.indices[triangleIndex * 3 + 1];
                var i2 = triangulation.indices[triangleIndex * 3 + 2];
                var v0 = triangulation.vertices[i0];
                var v1 = triangulation.vertices[i1];
                var v2 = triangulation.vertices[i2];

                // Calculate a random point on that triangle
                var u = Random.Range(0.0f, 1.0f);
                var v = Random.Range(0.0f, 1.0f);
                if (u + v > 1.0f)
                {
                    if (u > v)
                    {
                        u = 1.0f - u;
                    }
                    else
                    {
                        v = 1.0f - v;
                    }
                }

                return v0 + u * (v1 - v0) + v * (v2 - v0);
            }
        }
    }
}
