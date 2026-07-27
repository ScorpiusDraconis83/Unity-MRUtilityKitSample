// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.MRUtilityKitSamples.HandInput;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class IntroPanel : MonoBehaviour
{
    public UnityEvent ButtonPressed;
    [SerializeField]
    private GameObject controllerHintText;
    [SerializeField]
    private GameObject handHintText;
    private void Start()
    {
        OnInputModeChangeHandler(HandInputManager.Instance.CurrentInputMode);
        HandInputManager.Instance.OnInputModeChanged.AddListener(OnInputModeChangeHandler);
    }

    private void OnDestroy()
    {
        HandInputManager.Instance.OnInputModeChanged.RemoveListener(OnInputModeChangeHandler);
    }

    private void Update()
    {
        // Use Button enum (not RawButton) which works with active controller including hands
        // When hands are active, pinch is mapped to Button.One (A equivalent) and Button.Three (X equivalent)
        if (OVRInput.GetDown(OVRInput.Button.One) || OVRInput.GetDown(OVRInput.Button.Three)
            || HandInputManager.Instance.ThumbTapDown || Keyboard.current?.spaceKey.wasPressedThisFrame == true)
        {
            ButtonPressed.Invoke();
        }
    }

    public void EnableObject(GameObject goToEnable)
    {
        goToEnable.SetActive(true);
    }

    public void DisableObject(GameObject goToDisable)
    {
        goToDisable.SetActive(false);
    }
    private void OnInputModeChangeHandler(InputMode input)
    {
        if ((controllerHintText == null) || (handHintText == null))
        {
            return;
        }
        if (input == InputMode.Controllers)
        {
            controllerHintText.SetActive(true);
            handHintText.SetActive(false);
        }
        else
        {
            controllerHintText.SetActive(false);
            handHintText.SetActive(true);
        }

    }
}
