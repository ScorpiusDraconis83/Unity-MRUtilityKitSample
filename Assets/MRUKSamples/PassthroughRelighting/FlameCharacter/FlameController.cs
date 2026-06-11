// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;
using UnityEngine.Serialization;

public class FlameController : MonoBehaviour
{
    private const int BlendShapesSpeed = 6;


    [SerializeField] private GameObject root;
    [SerializeField] private GameObject mid;
    [SerializeField] private GameObject tip;
    [SerializeField] private Transform attractionTarget;
    [SerializeField] private Light flameLight;
    [SerializeField] private GameObject lookAtTarget;
    [SerializeField] private int attractionForceMultiplier = 2;
    [SerializeField] private float lightIntensityVariance = 0.2f;

    private int _blendShapeCount;
    private GameObject _midGoal;
    private Rigidbody _midRigidbody;
    private GameObject _rootGoal;
    private Rigidbody _rootRigidbody;
    private SkinnedMeshRenderer _skinnedMeshRenderer;
    private float _startingLightIntensity;
    private GameObject _tipGoal;
    private Rigidbody _tipRigidbody;
    private float _randomTimeOffset;

    private void Start()
    {
        CreateTargetObject();
        _randomTimeOffset = UnityEngine.Random.Range(0, 100);
        _skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        _blendShapeCount = _skinnedMeshRenderer.sharedMesh.blendShapeCount;
        _rootRigidbody = root.GetComponent<Rigidbody>();
        _midRigidbody = mid.GetComponent<Rigidbody>();
        _tipRigidbody = tip.GetComponent<Rigidbody>();
        _startingLightIntensity = flameLight.intensity;
    }

    private void Update()
    {
        AnimateBlendShapes();
        RotateFlame();
        AnimateLightIntensity();
    }

    private void FixedUpdate()
    {
        AddAttractionForces();
    }

    private void AddAttractionForces()
    {
        _rootGoal.transform.position = attractionTarget.position;
        root.transform.position = attractionTarget.position;
        var midAttractDir = _midGoal.transform.position - mid.transform.position;
        _midRigidbody.AddForce(midAttractDir * attractionForceMultiplier);
        var tipAttractDir = _tipGoal.transform.position - tip.transform.position;
        _tipRigidbody.AddForce(tipAttractDir * attractionForceMultiplier);
    }

    private void AnimateLightIntensity()
    {
        flameLight.intensity = Remap(0, 100, _startingLightIntensity - lightIntensityVariance,
            _startingLightIntensity + lightIntensityVariance, _skinnedMeshRenderer.GetBlendShapeWeight(0));
    }

    private void RotateFlame()
    {
        var flameForward =
            Vector3.ProjectOnPlane(lookAtTarget.transform.position - root.transform.position, Vector3.up);
        tip.transform.rotation =
            Quaternion.LookRotation(tip.transform.position - mid.transform.position, flameForward);
        mid.transform.rotation =
            Quaternion.LookRotation(tip.transform.position - mid.transform.position, flameForward);
        root.transform.GetChild(0).rotation =
            Quaternion.LookRotation(mid.transform.position - root.transform.position, flameForward);
    }

    private void CreateTargetObject()
    {
        _rootGoal = new GameObject("rootGoal");
        _midGoal = new GameObject("midGoal");
        _tipGoal = new GameObject("topGoal");
        _midGoal.transform.SetParent(_rootGoal.transform);
        _tipGoal.transform.SetParent(_midGoal.transform);

        _rootGoal.transform.position = root.transform.position;
        _rootGoal.transform.rotation = root.transform.rotation;
        _midGoal.transform.position = mid.transform.position;
        _midGoal.transform.rotation = mid.transform.rotation;
        _tipGoal.transform.position = tip.transform.position;
        _tipGoal.transform.rotation = tip.transform.rotation;

        mid.transform.parent = null;
        tip.transform.parent = null;
    }

    private void AnimateBlendShapes()
    {
        for (var i = 0; i < _blendShapeCount; i++)
        {
            var noise = Mathf.PerlinNoise((Time.time * BlendShapesSpeed) + _randomTimeOffset, i + 1);
            _skinnedMeshRenderer.SetBlendShapeWeight(i, noise * 100);
        }

        var lowestBlendShape = Mathf.Infinity;
        for (var i = 0; i < _blendShapeCount; i++)
        {
            if (_skinnedMeshRenderer.GetBlendShapeWeight(i) < lowestBlendShape)
            {
                lowestBlendShape = _skinnedMeshRenderer.GetBlendShapeWeight(i);
            }
        }

        for (var i = 0; i < _blendShapeCount; i++)
        {
            var remappedWeight = Remap(lowestBlendShape, 100, 0, 100,
                _skinnedMeshRenderer.GetBlendShapeWeight(i));
            _skinnedMeshRenderer.SetBlendShapeWeight(i, remappedWeight);
        }
    }

    private float Remap(float a, float b, float c, float d, float x)
    {
        return c + (x - a) * (d - c) / (b - a);
    }
}
