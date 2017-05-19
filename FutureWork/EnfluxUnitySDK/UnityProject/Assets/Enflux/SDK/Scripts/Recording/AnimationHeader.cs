// Copyright (c) 2017 Enflux Inc.
// By downloading, accessing or using this SDK, you signify that you have read, understood and agree to the terms and conditions of the End User License Agreement located at: https://www.getenflux.com/pages/sdk-eula
using System;
using System.Runtime.InteropServices;
using Enflux.SDK.Core.DataTypes;

namespace Enflux.SDK.Recording
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AnimationHeader
    {
        public const int HEADER_VERSION = 2;
        public const int FRAME_VERSION = 1;

        // Header format version
        public short HeaderVersion;

        // Version of the frame structure
        public short FrameVersion;

        // number of shirt frames recorded
        public ulong NumShirtFrames;

        // number of pants frames recorded
        public ulong NumPantsFrames;

        // Duration of the recording in milliseconds
        public uint Duration;

        public Vector3 ShirtBaseOrientation;
        public Vector3 PantsBaseOrientation;


        public static AnimationHeader InitializeFromArray(byte[] data)
        {
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            var header = (AnimationHeader) Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(AnimationHeader));
            handle.Free();
            return header;
        }

        public override string ToString()
        {
            var totalTime = TimeSpan.FromMilliseconds(Duration);
            return
                string.Format(
                    "[Enflux Animation, Total time: {0:00}:{1:00}.{2:000}, {3} shirt frames, {4} pants frames, ShirtBaseOrientation: {5}, PantsBaseOrientation: {6}]",
                    totalTime.Minutes,
                    totalTime.Seconds,
                    totalTime.Milliseconds,
                    NumShirtFrames,
                    NumPantsFrames,
                    ShirtBaseOrientation,
                    PantsBaseOrientation);
        }
    }
}