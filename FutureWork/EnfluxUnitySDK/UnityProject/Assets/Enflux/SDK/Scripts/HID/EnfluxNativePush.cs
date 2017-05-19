// Copyright (c) 2017 Enflux Inc.
// By downloading, accessing or using this SDK, you signify that you have read, understood and agree to the terms and conditions of the End User License Agreement located at: https://www.getenflux.com/pages/sdk-eula
using System.Runtime.InteropServices;
using Enflux.SDK.Core.DataTypes;

namespace Enflux.SDK.HID
{
    public static class EnfluxNativePush
    {
        private const string DllName = "EnfluxHID";


        // This callback updates the module rotations.
        public delegate void DataStreamCallback([In] DeviceType device, [MarshalAs(UnmanagedType.LPArray, SizeConst = 5)] [In] Quaternion[] data);

        // This callback updates the device status with input commands.
        public delegate void StatusCallback([In] DeviceType device, InputCommands status);

        // This callback updates the device status with raw data.
        public delegate void RawDataCallback([In] DeviceType device, int sensor, ref RawData data);

        public struct Callbacks
        {
            public StatusCallback StatusReceived;
            public DataStreamCallback DataStreamRecieved;
        };

        public struct RawCallbacks
        {
            public StatusCallback StatusReceived;
            public RawDataCallback RawDataReceived;
        }

        // Sets the connection interval on the module.
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetInterval(DeviceType devices, ushort intervalMs);

        // Starts streaming rotation data from the device. The callbacks will be called with updates.
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void StartStreamingThread(DeviceType devices, Callbacks callbacks);

        // Starts streaming raw sensor data from the device. The callbacks will be called with updates.
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void StartRawDataThread(DeviceType devices, RawCallbacks callbacks);

        // Starts calibrating the device. The callbacks will be called with updates.
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void StartCalibrationThread(DeviceType devices, Callbacks callbacks);

        // Stop streaming and receiving commands from a module.
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int EndStreamingThread();
    }
}
