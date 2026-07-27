// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using Meta.XR.MRUtilityKit;
using Meta.XR.MRUtilityKitSamples.HandInput;
using Meta.XR.Samples;
using System;

namespace Meta.XR.MRUtilityKitSamples.KeyboardTracker
{
    [MetaCodeSample("MRUKSample-KeyboardTracker")]
    public sealed class KeyboardManager : MonoBehaviour
    {
        [SerializeField]
        GameObject _prefab;

        [SerializeField]
        GameObject _skybox;

        public void OnTrackableAdded(MRUKTrackable trackable)
        {
            Debug.Log($"Detected new {trackable.TrackableType} with {trackable.name}");

            if (trackable.TrackableType != OVRAnchor.TrackableType.Keyboard)
            {
                // We only care about keyboards
                return;
            }

            // Instantiate the prefab
            var newGameObject = Instantiate(_prefab, trackable.transform);

            // Hook everything up
            var boundaryVisualizer = newGameObject.GetComponentInChildren<Bounded3DVisualizer>();
            if (boundaryVisualizer)
            {
                boundaryVisualizer.Initialize(trackable, _skybox);
            }
        }

        public void OnTrackableRemoved(MRUKTrackable trackable)
        {
            Debug.Log($"Removing GameObject '{trackable.name}'");
            Destroy(trackable.gameObject);
        }

        void Update()
        {
            // Toggle between full passthrough and surface-projected passthrough
            if (OVRInput.GetDown(OVRInput.Button.One) || HandInputManager.Instance.ThumbTapDown)
            {
                _skybox.SetActive(!_skybox.activeInHierarchy);
            }
        }
    }
}
