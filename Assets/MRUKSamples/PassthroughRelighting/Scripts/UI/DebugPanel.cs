// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Meta.XR.MRUtilityKit;
using Meta.XR.Samples;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Meta.XR.MRUtilityKitSamples.PassthroughRelighting
{
    /// <summary>
    /// A UI panel for debugging and controlling passthrough relighting settings.
    /// </summary>
    [MetaCodeSample("MRUKSample-PassthroughRelighting")]
    public class DebugPanel : MonoBehaviour
    {
        private const string HighlightAttenuationShaderPropertyName = "_HighLightAttenuation";
        private const string HighlightOpaquenessShaderPropertyName = "_HighlightOpacity";

        [SerializeField] private TMP_Dropdown _shadowDropDown;
        [SerializeField] private Toggle _highlightsToggle;
        [SerializeField] private TMP_Dropdown _geometryDropDown;
        [SerializeField] private Button _respawnButton;
        [SerializeField] private Slider _lightIntensitySlider;
        [SerializeField] private Slider _passthroughBrightnessSlider;
        [SerializeField] private Slider _lightBlendFactor;
        [SerializeField] private Renderer _oppyRenderer;
        [SerializeField] private Material _sceneMaterial;
        [SerializeField] private OppyCharacterController _oppyController;
        [SerializeField] private OppyLightGlow _oppyLightGlow;
        [SerializeField] private OVRPassthroughLayer _passthroughLayer;
        [SerializeField] private bool _highlights;

        private EffectMesh[] _effectMeshes;

        private void Awake()
        {
            _shadowDropDown.onValueChanged.AddListener(ShadowsSettingsChanged);
            _highlightsToggle.onValueChanged.AddListener(HighlightSettingsToggled);
            _geometryDropDown.onValueChanged.AddListener(GeometrySettingsChanged);
            _respawnButton.onClick.AddListener(_oppyController.Respawn);
            _highlights = true;
            _lightIntensitySlider.onValueChanged.AddListener(
                (val) =>
                {
                    if (_highlights)
                    {
                        _sceneMaterial.SetFloat(HighlightAttenuationShaderPropertyName, val);
                    }
                }
            );
            _lightBlendFactor.onValueChanged.AddListener(
                (val) => { _sceneMaterial.SetFloat(HighlightOpaquenessShaderPropertyName, val); }
            );
            _passthroughBrightnessSlider.onValueChanged.AddListener(
                (brightness) => { _passthroughLayer.SetBrightnessContrastSaturation(brightness); }
            );
#if UNITY_6000_4_OR_NEWER
            _effectMeshes = FindObjectsByType<EffectMesh>(FindObjectsInactive.Include);
#else
            _effectMeshes = FindObjectsByType<EffectMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#endif
        }

        private void Start()
        {
            ToggleGeometryDropDown();
            if (_highlightsToggle)
            {
                _highlightsToggle.isOn = true;
            }

            if (_lightIntensitySlider)
            {
                _lightIntensitySlider.value = 0.5f;
            }

            if (_geometryDropDown)
            {
                _geometryDropDown.value = 0;
            }
        }

        /// <summary>
        /// Enables or disables the geometry dropdown based on whether a global mesh exists.
        /// </summary>
        public void ToggleGeometryDropDown()
        {
            bool globalMeshExists = MRUK.Instance && MRUK.Instance.GetCurrentRoom() &&
                                    MRUK.Instance.GetCurrentRoom().GlobalMeshAnchor;
            _geometryDropDown.interactable = globalMeshExists;
        }

        private void GeometrySettingsChanged(int optionSelected)
        {
            if (optionSelected == 1)
            {
                foreach (var effectMesh in _effectMeshes)
                {
                    if ((effectMesh.Labels & MRUKAnchor.SceneLabels.GLOBAL_MESH) == 0)
                    {
                        effectMesh.DestroyMesh();
                    }
                    else
                    {
                        effectMesh.CreateMesh();
                    }
                }
            }
            else
            {
                foreach (var effectMesh in _effectMeshes)
                {
                    if ((effectMesh.Labels & MRUKAnchor.SceneLabels.GLOBAL_MESH) != 0)
                    {
                        effectMesh.DestroyMesh(new LabelFilter(effectMesh.Labels));
                    }
                    else
                    {
                        effectMesh.CreateMesh();
                    }
                }
            }

            _oppyController.Respawn();
        }

        private void HighlightSettingsToggled(bool highlightsOn)
        {
            _highlights = highlightsOn;
            _sceneMaterial.SetFloat(HighlightAttenuationShaderPropertyName, highlightsOn ? 1 : 0);
            _oppyLightGlow.SetGlowActive(highlightsOn);
            if (highlightsOn)
            {
                _sceneMaterial.SetFloat(HighlightAttenuationShaderPropertyName, _lightIntensitySlider.value);
            }
        }

        private void ShadowsSettingsChanged(int dynamicShadow)
        {
            if (dynamicShadow == 0)
            {
                _oppyRenderer.shadowCastingMode = ShadowCastingMode.On;
            }
            else
            {
                _oppyRenderer.shadowCastingMode = ShadowCastingMode.Off;
            }
        }
    }
}
