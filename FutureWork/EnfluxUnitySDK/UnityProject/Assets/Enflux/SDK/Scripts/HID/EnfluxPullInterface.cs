// Copyright (c) 2017 Enflux Inc.
// By downloading, accessing or using this SDK, you signify that you have read, understood and agree to the terms and conditions of the End User License Agreement located at: https://www.getenflux.com/pages/sdk-eula
using System;
using Enflux.SDK.Core.DataTypes;

namespace Enflux.SDK.HID
{
    public class EnfluxPullInterface
    {
        public enum StreamingStatus
        {
            Uninitialized,
            Connected,
            Disconnected
        }


        public StreamingStatus ShirtStatus = StreamingStatus.Uninitialized;
        public StreamingStatus PantsStatus = StreamingStatus.Uninitialized;

        public event Action<InputCommands> ReceivedShirtStatus;
        public event Action<InputCommands> ReceivedPantsStatus;

        public Quaternion[] PantsRotations = new Quaternion[5];
        public Quaternion[] ShirtRotations = new Quaternion[5];


        // Temporary variables, will be removed in future release!
        public static byte[] PantsRPY = new byte[20];
        public static byte[] ShirtRPY = new byte[20];


        public void StartStreaming(DeviceType deviceType)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            EnfluxNativePull.StartStreamingPull(deviceType);
#endif
        }

        public void EndStreaming()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            EnfluxNativePull.EndStreamingThread();
#endif
        }

        public void StartCalibration(DeviceType deviceType)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            EnfluxNativePull.StartCalibrationPull(deviceType);
#endif
        }

        public void PollDevices()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            // Check for a new shirt command
            if (HasNewCommand(DeviceType.Shirt))
            {
                var command = PopCommand(DeviceType.Shirt);
                var handler = ReceivedShirtStatus;
                if (handler != null)
                {
                    handler(command);
                }
                if (command == InputCommands.DeviceConnected)
                {
                    ShirtStatus = StreamingStatus.Connected;
                }
                else if (command == InputCommands.DeviceDisconnected)
                {
                    ShirtStatus = StreamingStatus.Disconnected;
                }
            }
            // Check for a new pants command
            if (HasNewCommand(DeviceType.Pants))
            {
                var command = PopCommand(DeviceType.Pants);
                var handler = ReceivedPantsStatus;
                if (handler != null)
                {
                    handler(command);
                }
                if (command == InputCommands.DeviceConnected)
                {
                    PantsStatus = StreamingStatus.Connected;
                }
                else if (command == InputCommands.DeviceDisconnected)
                {
                    PantsStatus = StreamingStatus.Disconnected;
                }
            }

            if (ShirtStatus == StreamingStatus.Connected)
            {
                //LoadRotations(DeviceType.Shirt, ShirtRotations);
                RPY.LoadRotations(DeviceType.Shirt, ShirtRPY);
            }
            if (PantsStatus == StreamingStatus.Connected)
            {
                //LoadRotations(DeviceType.Pants, PantsRotations);
                RPY.LoadRotations(DeviceType.Pants, PantsRPY);
            }
#endif
        }

        private bool HasNewCommand(DeviceType deviceType)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return Convert.ToBoolean(EnfluxNativePull.HasNewCommand(deviceType));
#else
            return false;
#endif
        }

        private InputCommands PopCommand(DeviceType deviceType)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return (InputCommands) EnfluxNativePull.PopCommand(deviceType);
#else
            return InputCommands.DeviceDisconnected;
#endif
        }
    }
}