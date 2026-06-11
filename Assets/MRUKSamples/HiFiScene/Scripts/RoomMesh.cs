// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.MRUtilityKit;
using Meta.XR.Samples;
using UnityEngine;

namespace Meta.XR.MRUtilityKitSamples.HiFiScene
{
    [MetaCodeSample("MRUKSample-HiFiScene")]
    public class RoomMesh : MonoBehaviour
    {
        /// <summary>
        /// Initializes the RoomMesh component by subscribing to the MRUK scene loaded event.
        /// This method is called before the first frame update and sets up the event listener
        /// that will trigger mesh creation when a scene is loaded from the device.
        /// </summary>
        void Start()
        {
            if (MRUK.Instance)
            {
                MRUK.Instance.SceneLoadedEvent.AddListener(SceneLoadedEvent);
            }
        }

        /// <summary>
        /// We need to unsubscribe to the scene loaded event once this objects gets destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (MRUK.Instance)
            {
                MRUK.Instance.SceneLoadedEvent.RemoveListener(SceneLoadedEvent);
            }
        }

        /// <summary>
        /// Creates a Unity Mesh from the provided room mesh data.
        /// Each face in the room mesh will be created as a separate submesh, allowing for
        /// different materials to be applied to different semantic surfaces (walls, floor, ceiling, etc.).
        /// The mesh vertices are set from the room mesh vertices, and triangles are organized
        /// into submeshes based on the face data.
        /// </summary>
        /// <param name="roomMesh">The room mesh data containing vertices and face information to convert into a Unity mesh.</param>
        /// <returns>A Unity Mesh created from the room mesh data with separate submeshes for each face.</returns>
        private static Mesh CreateMeshFromRoomMeshData(MRUKRoom.RoomMesh roomMesh)
        {
            // Find the average position of all vertices
            Vector3 center = Vector3.zero;
            foreach (var vertex in roomMesh.Vertices)
            {
                center += vertex;
            }
            center /= roomMesh.Vertices.Count;

            Vector3[] scaledVertices = new Vector3[roomMesh.Vertices.Count];
            for (int i = 0; i < roomMesh.Vertices.Count; i++)
            {
                // Scale each vertex relative to the center
                const float scaleFactor = 1.001f;
                scaledVertices[i] = center + (roomMesh.Vertices[i] - center) * scaleFactor;
            }

            Mesh mesh = new Mesh
            {
                vertices = scaledVertices,
                subMeshCount = roomMesh.Faces.Count
            };

            // Create submeshes for each face
            for (int i = 0; i < roomMesh.Faces.Count; i++)
            {
                // Set triangles for this submesh
                mesh.SetTriangles(roomMesh.Faces[i].Indices.ToArray(), i);
            }

            return mesh;
        }

        /// <summary>
        /// Event handler called when a scene is loaded from the device.
        /// This method retrieves the current room's mesh data and creates a visual representation
        /// by generating a Unity mesh with materials colored according to semantic labels.
        /// It automatically adds MeshFilter and MeshRenderer components if they don't exist,
        /// and assigns different colors to different surface types (floor, walls, ceiling, etc.).
        /// </summary>
        private void SceneLoadedEvent()
        {
            var room = MRUK.Instance.GetCurrentRoom();
            if (room != null && room.RoomMeshData != null)
            {
                transform.position = room.transform.position;
                transform.rotation = room.transform.rotation;
                var mesh = CreateMeshFromRoomMeshData(room.RoomMeshData.Value);
                if (room.RoomMeshData != null && mesh != null)
                {
                    var roomMesh = room.RoomMeshData.Value;
                    // Add MeshFilter if it doesn't exist
                    MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
                    if (meshFilter == null)
                    {
                        meshFilter = gameObject.AddComponent<MeshFilter>();
                    }

                    // Add MeshRenderer if it doesn't exist
                    MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
                    if (meshRenderer == null)
                    {
                        meshRenderer = gameObject.AddComponent<MeshRenderer>();
                    }

                    // Create materials for each submesh
                    int submeshCount = mesh.subMeshCount;
                    Material[] materials = new Material[submeshCount];


                    // Create a material for each submesh with different colors to distinguish them
                    for (int i = 0; i < submeshCount; i++)
                    {
                        // Create a local color variable
                        Color color = new Color(0.5f, 0.5f, 0.5f, 1.0f); // Default gray color

                        // Assign different colors to different semantic types if available
                        if (i < roomMesh.Faces.Count)
                        {
                            var semanticLabel = roomMesh.Faces[i].SemanticLabel;

                            // Assign colors based on semantic label
                            switch (semanticLabel)
                            {
                                case MRUKAnchor.SceneLabels.FLOOR:
                                    color = new Color(0.2f, 0.6f, 0.2f, 1.0f); // Green for floor
                                    break;
                                case MRUKAnchor.SceneLabels.CEILING:
                                    color = new Color(0.8f, 0.8f, 0.8f, 1.0f); // White for ceiling
                                    break;
                                case MRUKAnchor.SceneLabels.WALL_FACE:
                                    color = new Color(0.6f, 0.6f, 0.8f, 1.0f); // Blue for walls
                                    break;
                                case MRUKAnchor.SceneLabels.INVISIBLE_WALL_FACE:
                                    color = new Color(0.8f, 0.3f, 0.8f, 1.0f); // Purple for invisible walls
                                    break;
                                case MRUKAnchor.SceneLabels.INNER_WALL_FACE:
                                    color = new Color(0.4f, 0.4f, 0.6f, 1.0f); // Darker blue for inner walls
                                    break;
                                case MRUKAnchor.SceneLabels.WINDOW_FRAME:
                                    color = new Color(0.7f, 0.9f, 1.0f, 1.0f); // Light blue for windows
                                    break;
                                case MRUKAnchor.SceneLabels.DOOR_FRAME:
                                    color = new Color(0.6f, 0.4f, 0.2f, 1.0f); // Brown for doors
                                    break;
                            }
                        }

                        // Create the material
                        var shader = Shader.Find("Universal Render Pipeline/Lit");
                        materials[i] = new Material(shader)
                        {
                            color = color
                        };
                    }

                    // Assign the materials to the renderer
                    meshRenderer.materials = materials;

                    // Assign the mesh to the MeshFilter
                    meshFilter.mesh = mesh;
                }
            }
        }
    }
}
