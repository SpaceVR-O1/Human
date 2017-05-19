// Copyright (c) 2017 Enflux Inc.
// By downloading, accessing or using this SDK, you signify that you have read, understood and agree to the terms and conditions of the End User License Agreement located at: https://www.getenflux.com/pages/sdk-eula
using Enflux.SDK.Core;
using Enflux.SDK.Extensions;
using UnityEngine;

namespace Enflux.SDK.VR
{
    [RequireComponent(typeof(Humanoid))]
    public class HMDAdapter : MonoBehaviour
    {
        private Humanoid _humanoid;

        public Transform Hmd;
        public Transform EyeLocation;

        private void Reset()
        {
            Hmd = Camera.main != null ? Camera.main.transform : null;
            EyeLocation = gameObject.FindChildComponent<Transform>("VRCameraMount");
        }

        private void OnEnable()
        {
            _humanoid = _humanoid ?? GetComponent<Humanoid>();
            if (_humanoid == null)
            {
                Debug.LogError(name + ": Humanoid is null! Please attach one!");
                enabled = false;
            }
            AlignBodyWithHmd();
            if (_humanoid.AbsoluteAnglesStream != null)
            {
                _humanoid.AbsoluteAnglesStream.ShirtReceivedNotification += OnShirtRecievedNotification;
            }
        }

        private void OnDisable()
        {
            if (_humanoid != null && _humanoid.AbsoluteAnglesStream != null)
            {
                _humanoid.AbsoluteAnglesStream.ShirtReceivedNotification -= OnShirtRecievedNotification;
            }
        }

        private void OnShirtRecievedNotification(DeviceNotification notification)
        {
            if (notification == DeviceNotification.ResetOrientation)
            {
                AlignBodyWithHmd();
            }
        }

        private void LateUpdate()
        {
            if (Hmd != null && EyeLocation != null)
            {
                Vector3 difference = Hmd.position - EyeLocation.position;
                transform.Translate(difference, Space.World);
            }
        }

        public void AlignBodyWithHmd()
        {
            if (Hmd == null)
            {
                return;
            }
            transform.localRotation = Quaternion
                .AngleAxis(Hmd.rotation.eulerAngles.y, Vector3.up);
        }
    }
}