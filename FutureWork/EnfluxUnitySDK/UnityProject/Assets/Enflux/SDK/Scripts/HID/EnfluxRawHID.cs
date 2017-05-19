// Copyright (c) 2017 Enflux Inc.
// By downloading, accessing or using this SDK, you signify that you have read, understood and agree to the terms and conditions of the End User License Agreement located at: https://www.getenflux.com/pages/sdk-eula
using System.Runtime.InteropServices;

namespace Enflux.SDK.HID
{
    public class EnfluxRawHID
    {
        private const string DllName = "EnfluxHID";
        

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rawhid_open(int max, int vid, int pid, int usagePage, int usage);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rawhid_recv(int deviceId, out int report, byte[] buffer, int length, int timeout);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rawhid_send(int deviceId, int report, byte[] buffer, int length, int timeout);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void rawhid_close(int deviceId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rawhid_set_feature(int deviceId, int report, byte[] buffer, int length);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rawhid_get_feature(int deviceId, int report, byte[] buffer, int length);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int ConnectSuit(int status);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void Disconnect();


        public int VID = 0x1915;
        public int PID = 0xEEEE;
        public int PAGE = -1;
        public int USAGE = -1;
        
    }
}
