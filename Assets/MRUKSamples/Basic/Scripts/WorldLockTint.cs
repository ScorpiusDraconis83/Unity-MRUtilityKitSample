// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Meta.XR.MRUtilityKit;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.XR.MRUtilityKitSamples.Basic
{
    /// <summary>
    /// Shows a red tint on the effect mesh when world locking is inactive.
    /// </summary>
    [MetaCodeSample("MRUKSample-Basic")]
    public class WorldLockTint : MonoBehaviour
    {
        // Show a red tint on the effect mesh when world locking is inactive
        private static readonly Color InactiveColor = new Color(0.8666667f, 0.145098f, 0.1930136f);
        private bool? _wasWorldLockActive;
        private Color _defaultColor;
        private UnityAction _sceneLoadedAction;

        private void Start()
        {
            _sceneLoadedAction = OnSceneLoaded;
            if (MRUK.Instance)
            {
                MRUK.Instance.SceneLoadedEvent.AddListener(_sceneLoadedAction);
            }
        }

        private void ChangeEffectMeshObjectsColor(bool worldLockActive)
        {
            var effectMeshObjects = GetComponent<EffectMesh>().EffectMeshObjects;
            foreach (var effectMeshObject in effectMeshObjects)
            {
                effectMeshObject.Value.effectMeshGO.GetComponent<Renderer>().material.color = worldLockActive ? _defaultColor : InactiveColor;
            }
        }

        private void OnSceneLoaded()
        {
            _wasWorldLockActive = null;
        }

        private void Update()
        {
            if (_wasWorldLockActive == null)
            {
                _defaultColor = GetComponent<EffectMesh>().MeshMaterial.color;
            }

            bool worldLockActive = MRUK.Instance.IsWorldLockActive;
            if (worldLockActive != _wasWorldLockActive)
            {
                ChangeEffectMeshObjectsColor(worldLockActive);
                _wasWorldLockActive = worldLockActive;
            }
        }

        private void OnDestroy()
        {
            if (MRUK.Instance)
            {
                MRUK.Instance.SceneLoadedEvent.RemoveListener(_sceneLoadedAction);
            }
        }
    }
}
