// Copyright (c) 2017 Enflux Inc.
// By downloading, accessing or using this SDK, you signify that you have read, understood and agree to the terms and conditions of the End User License Agreement located at: https://www.getenflux.com/pages/sdk-eula
using System;
using UnityEngine;

namespace Enflux.SDK.Core
{
    public abstract class EnfluxSuitStream : MonoBehaviour
    {
        private Vector3 _shirtBaseOrientation;
        private Vector3 _pantsBaseOrientation;

        /// <summary>
        /// The suit's limb angles in absolute real-world coordinates. For example, a rotation's y component of 0 corresponds to a yaw pointing north in real-world coordinates, 180 corresponds to south.
        /// </summary>
        public readonly HumanoidAngles<Vector3> AbsoluteAngles = new HumanoidAngles<Vector3>();

        public event Action<StateChange<DeviceState>> ShirtStateChanged;
        public event Action<StateChange<DeviceState>> PantsStateChanged;
        public event Action<DeviceNotification> ShirtReceivedNotification;
        public event Action<DeviceNotification> PantsReceivedNotification;
        public event Action<DeviceError> ShirtReceivedError;
        public event Action<DeviceError> PantsReceivedError;

        /// <summary>
        /// The base real-world orientation of the core module of the shirt.
        /// </summary>
        /// <param name="value">A y component of 0 corresponds to a yaw pointing north in real-world coordinates, 180 corresponds to south.</param>
        public Vector3 ShirtBaseOrientation
        {
            get { return _shirtBaseOrientation; }
            set
            {
                _shirtBaseOrientation = value;
                RaiseShirtNotificationEvent(DeviceNotification.ResetOrientation);
            }
        }

        /// <summary>
        /// The base real-world orientation of the waist module of the pants.
        /// </summary>
        /// <param name="value">A y component of 0 corresponds to a yaw pointing north in real-world coordinates, 180 corresponds to south.</param>
        public Vector3 PantsBaseOrientation
        {
            get { return _pantsBaseOrientation; }
            set
            {
                _pantsBaseOrientation = value;
                RaisePantsNotificationEvent(DeviceNotification.ResetOrientation);
            }
        }

        protected void ChangeShirtState(StateChange<DeviceState> statusChange)
        {
            var handler = ShirtStateChanged;
            if (handler != null)
            {
                handler(statusChange);
            }
        }

        protected void ChangePantsState(StateChange<DeviceState> statusChange)
        {
            var handler = PantsStateChanged;
            if (handler != null)
            {
                handler(statusChange);
            }
        }

        protected void RaiseShirtNotificationEvent(DeviceNotification shirtNotification)
        {
            var handler = ShirtReceivedNotification;
            if (handler != null)
            {
                handler(shirtNotification);
            }
        }

        protected void RaisePantsNotificationEvent(DeviceNotification pantsNotification)
        {
            var handler = PantsReceivedNotification;
            if (handler != null)
            {
                handler(pantsNotification);
            }
        }

        protected void RaiseShirtErrorEvent(DeviceError shirtError)
        {
            var handler = ShirtReceivedError;
            if (handler != null)
            {
                handler(shirtError);
            }
        }

        protected void RaisePantsErrorEvent(DeviceError pantsError)
        {
            var handler = PantsReceivedError;
            if (handler != null)
            {
                handler(pantsError);
            }
        }

        /// <summary>
        /// Sets ShirtBaseOrientation to the current orientation of the shirt's chest module.
        /// </summary>
        public void ResetShirtBaseOrientation()
        {
            ShirtBaseOrientation = AbsoluteAngles.Chest;
        }

        /// <summary>
        /// Sets PantsBaseOrientation to the current orientation of the pant's waist module.
        /// </summary>
        public void ResetPantsBaseOrientation()
        {
            PantsBaseOrientation = AbsoluteAngles.Waist;
        }

        /// <summary>
        /// Sets ShirtBaseOrientation to the current orientation of the shirt's chest module and PantsBaseOrientation to the current orientation of the pant's waist module.
        /// </summary>
        public void ResetFullBodyBaseOrientation()
        {
            ResetShirtBaseOrientation();
            ResetPantsBaseOrientation();
        }
    }
}