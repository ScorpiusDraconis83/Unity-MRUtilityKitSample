// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.MRUtilityKit;
using Meta.XR.Samples;
using UnityEngine;

namespace Meta.XR.MRUtilityKitSamples.VirtualHome
{
    [MetaCodeSample("MRUKSample-VirtualHome")]
    public class LightSpawner : AnchorPrefabSpawner
    {
        /// <summary>
        /// Helper method to tweak the point light's range so that it matches the room's bounds
        /// before instantiating it.
        /// </summary>
        /// <param name="anchorInfo">The anchor information used to spawn and configure the prefab.</param>
        protected override void SpawnPrefab(MRUKAnchor anchorInfo)
        {
            base.SpawnPrefab(anchorInfo);
            AnchorPrefabSpawnerObjects.TryGetValue(anchorInfo, out var gameObject);
            // Adjust the range of the point light
            var roomBoundsSize = anchorInfo.Room.GetRoomBounds().size;
            var maxExtent = Mathf.Max(roomBoundsSize.x, roomBoundsSize.y, roomBoundsSize.z);
            var lightComponent = gameObject?.GetComponentInChildren<Light>();
            if (gameObject == null)
            {
                return;
            }
            if (lightComponent == null)
            {
                Debug.LogWarning("No light source was found on the prefab to be spawned or its children");
                return;
            }

            lightComponent.range = maxExtent;
        }
    }
}
