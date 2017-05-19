// Copyright (c) 2017 Enflux Inc.
// By downloading, accessing or using this SDK, you signify that you have read, understood and agree to the terms and conditions of the End User License Agreement located at: https://www.getenflux.com/pages/sdk-eula
using System.Runtime.InteropServices;
using Enflux.SDK.Core;
using Enflux.SDK.Core.DataTypes;

namespace Enflux.SDK.HID
{
    public static class EnfluxNativePull
    {
        private const string DllName = "EnfluxHID";

        #region Obsolete
        public static class RPY
        {
            // Temporary method, will be removed in future release!
            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int LoadRotations(DeviceType device,
                [Out] [MarshalAs(UnmanagedType.LPArray, SizeConst = 20)] byte[] outData);
        }
        #endregion


        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void StartStreamingPull(DeviceType device);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void StartCalibrationPull(DeviceType device);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int HasNewCommand(DeviceType device);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PopCommand(DeviceType device);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int LoadRotations(DeviceType device,
            [Out] [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Struct, SizeConst = 5)] Quaternion[]
                outData);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int EndStreamingThread();
    }
}
