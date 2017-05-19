// Copyright (c) 2017 Enflux Inc.
// By downloading, accessing or using this SDK, you signify that you have read, understood and agree to the terms and conditions of the End User License Agreement located at: https://www.getenflux.com/pages/sdk-eula
using Enflux.Attributes;
using Enflux.SDK.Core.DataTypes;
using Enflux.SDK.HID;
using UnityEngine;
using DeviceType = Enflux.SDK.Core.DataTypes.DeviceType;
using Quaternion = Enflux.SDK.Core.DataTypes.Quaternion;

namespace Enflux.SDK.Core
{
    public class EnfluxManager : EnfluxSuitStream
    {
        [SerializeField, Readonly] private DeviceState _shirtState = DeviceState.Disconnected;
        [SerializeField, Readonly] private DeviceState _pantsState = DeviceState.Disconnected;

        private readonly EnfluxPullInterface _pullInterface = new EnfluxPullInterface();

        public bool IsShirtActive
        {
            get { return _shirtState != DeviceState.Disconnected; }
        }

        public bool ArePantsActive
        {
            get { return _pantsState != DeviceState.Disconnected; }
        }

        public bool IsAnyDeviceActive
        {
            get { return IsShirtActive || ArePantsActive; }
        }

        public DeviceState ShirtState
        {
            get { return _shirtState; }
            private set
            {
                if (_shirtState == value)
                {
                    return;
                }
                var previous = _shirtState;
                _shirtState = value;
                ChangeShirtState(new StateChange<DeviceState>(previous, _shirtState));
            }
        }

        public DeviceState PantsState
        {
            get { return _pantsState; }
            private set
            {
                if (_pantsState == value)
                {
                    return;
                }
                var previous = _pantsState;
                _pantsState = value;
                ChangePantsState(new StateChange<DeviceState>(previous, _pantsState));
            }
        }


        public interface IQueuedEvent
        {
            void OnDequeue();
        }

        public class DataEvent : IQueuedEvent
        {
            public Quaternion[] Data;
            public bool IsPants;

            public DataEvent(Quaternion[] data, bool isPants)
            {
                Data = data;
                IsPants = isPants;
            }

            void IQueuedEvent.OnDequeue()
            {
            }
        }

        public class DeviceStatusEvent : IQueuedEvent
        {
            public DeviceStatusEvent(DeviceType device, int status)
            {
            }

            void IQueuedEvent.OnDequeue()
            {
            }
        }

        private void OnEnable()
        {
            _pullInterface.ReceivedShirtStatus += OnReceivedShirtStatus;
            _pullInterface.ReceivedPantsStatus += OnReceivedPantsStatus;
        }

        private void OnDisable()
        {
            _pullInterface.ReceivedShirtStatus -= OnReceivedShirtStatus;
            _pullInterface.ReceivedPantsStatus -= OnReceivedPantsStatus;
        }

        private void OnApplicationQuit()
        {
            Shutdown();
        }

        private void OnReceivedShirtStatus(InputCommands status)
        {
            switch (status)
            {
                case InputCommands.DeviceConnected:
                    ShirtState = DeviceState.Initializing;
                    break;

                case InputCommands.DeviceDisconnected:
                    ShirtState = DeviceState.Disconnected;
                    break;

                case InputCommands.CalibrationStarted:
                    ShirtState = DeviceState.Calibrating;
                    break;

                case InputCommands.CalibrationFinished:
                    Disconnect();
                    break;

                case InputCommands.ResetOrientation:
                    ResetFullBodyBaseOrientation();
                    break;

                case InputCommands.ErrorCalibrationFailed:
                    ShirtState = DeviceState.Connected;
                    RaiseShirtErrorEvent(DeviceError.CalibrationFailed);
                    Debug.LogError(name + ": Device 'Shirt' failed to calibrate!");
                    break;

                case InputCommands.ErrorNoCalibration:
                    ShirtState = DeviceState.Connected;
                    RaiseShirtErrorEvent(DeviceError.NoCalibration);
                    Debug.LogError(name + ": Device 'Shirt' isn't calibrated. Please calibrate.");
                    break;

                case InputCommands.ErrorNoShirtPants:
                    ShirtState = DeviceState.Connected;
                    RaiseShirtErrorEvent(DeviceError.UnknownDevice);
                    Debug.LogError(name +
                                   ": Attempted to connect unknown device. Defaulting to 'Shirt'. Please fix this with the firmware update tools.");
                    break;
            }
        }

        private void OnReceivedPantsStatus(InputCommands status)
        {
            switch (status)
            {
                case InputCommands.DeviceConnected:
                    PantsState = DeviceState.Initializing;
                    break;

                case InputCommands.DeviceDisconnected:
                    PantsState = DeviceState.Disconnected;
                    break;

                case InputCommands.CalibrationStarted:
                    PantsState = DeviceState.Calibrating;
                    break;

                case InputCommands.CalibrationFinished:
                    Disconnect();
                    break;

                case InputCommands.ResetOrientation:
                    ResetFullBodyBaseOrientation();
                    break;

                case InputCommands.ErrorCalibrationFailed:
                    PantsState = DeviceState.Connected;
                    RaisePantsErrorEvent(DeviceError.CalibrationFailed);
                    Debug.LogError(name + ": Device 'Pants' failed to calibrate!");
                    break;

                case InputCommands.ErrorNoCalibration:
                    PantsState = DeviceState.Connected;
                    RaisePantsErrorEvent(DeviceError.NoCalibration);
                    Debug.LogError(name + ": Device 'Pants' isn't calibrated. Please calibrate.");
                    break;

                case InputCommands.ErrorNoShirtPants:
                    PantsState = DeviceState.Connected;
                    RaisePantsErrorEvent(DeviceError.UnknownDevice);
                    Debug.LogError(name +
                                   ": Attempted to connect unknown device. Defaulting to 'Shirt'. Please fix this with the firmware update tools.");
                    break;
            }
        }

        private void Update()
        {
            _pullInterface.PollDevices();

            if (IsShirtActive)
            {
                var upperModuleAngles = RPY.ParseDataForOrientationAngles(EnfluxPullInterface.ShirtRPY);
                if (ShirtState == DeviceState.Initializing && upperModuleAngles.IsInitialized)
                {
                    ShirtState = DeviceState.Streaming;
                    upperModuleAngles.ApplyUpperAnglesTo(AbsoluteAngles);
                    ResetFullBodyBaseOrientation();
                }
                else if (ShirtState == DeviceState.Streaming)
                {
                    upperModuleAngles.ApplyUpperAnglesTo(AbsoluteAngles);
                }
            }
            if (ArePantsActive)
            {
                var lowerModuleAngles = RPY.ParseDataForOrientationAngles(EnfluxPullInterface.PantsRPY);
                if (PantsState == DeviceState.Initializing && lowerModuleAngles.IsInitialized)
                {
                    PantsState = DeviceState.Streaming;
                    lowerModuleAngles.ApplyLowerAnglesTo(AbsoluteAngles);
                    ResetFullBodyBaseOrientation();
                }
                else if (PantsState == DeviceState.Streaming)
                {
                    lowerModuleAngles.ApplyLowerAnglesTo(AbsoluteAngles);
                }
            }
        }

        public void Connect(DeviceType device)
        {
            if (device == DeviceType.None)
            {
                Debug.LogError(name + ": Device is 'None'!");
                return;
            }
            if (IsActive(device))
            {
                Debug.LogError(name + ": Device '" + device + "' is already connected!");
                return;
            }
            // If should connect to both but we're already connected to one device, still connect to the other.
            if (device == DeviceType.All && IsShirtActive)
            {
                Debug.LogError(name + ": Device 'Shirt' is already connected!");
                device = DeviceType.Pants;
            }
            else if (device == DeviceType.All && ArePantsActive)
            {
                Debug.LogError(name + ": Device 'Pants' is already connected!");
                device = DeviceType.Shirt;
            }
            else if (device == DeviceType.Shirt && ArePantsActive)
            {
                Disconnect();
                device = DeviceType.All;
            }
            else if (device == DeviceType.Pants && IsShirtActive)
            {
                Disconnect();
                device = DeviceType.All;
            }

            Debug.Log(name + ": Connecting '" + device + "'...");
            _pullInterface.StartStreaming(device);
        }

        public void Disconnect()
        {
            if (!IsShirtActive && !ArePantsActive)
            {
                Debug.LogError(name + ": No devices are connected!");
                return;
            }
            Debug.Log(name + ": Disconnecting all devices...");
            _pullInterface.EndStreaming();

            ShirtState = DeviceState.Disconnected;
            PantsState = DeviceState.Disconnected;
        }

        private void Shutdown()
        {
            _pullInterface.EndStreaming();

            ShirtState = DeviceState.Disconnected;
            PantsState = DeviceState.Disconnected;
        }

        public bool IsActive(DeviceType device)
        {
            switch (device)
            {
                case DeviceType.Shirt:
                    return IsShirtActive;

                case DeviceType.Pants:
                    return ArePantsActive;

                case DeviceType.All:
                    return IsShirtActive && ArePantsActive;
            }
            return false;
        }

        public void Calibrate(DeviceType device)
        {
            if (device == DeviceType.None)
            {
                Debug.LogError(name + ": Device is 'None'!");
                return;
            }
            if (IsActive(device))
            {
                Debug.LogError(name + ": Device '" + device + "' must be disconnected to calibrate!");
                return;
            }
            Debug.Log(name + ": Calibrating '" + device + "'...");
            _pullInterface.StartCalibration(device);
        }
    }
}