// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Meta.XR.Samples;
using Meta.XR.MRUtilityKitSamples.StartScene;

namespace Meta.XR.MRUtilityKitSamples.PassthroughRelighting
{
    [MetaCodeSample("MRUKSample-PassthroughRelighting")]
    public class CursorConstrainer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private UICursor Cursor;
        public void OnPointerEnter(PointerEventData eventData)
        {
            Cursor.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Cursor.gameObject.SetActive(false);
        }
    }
}
