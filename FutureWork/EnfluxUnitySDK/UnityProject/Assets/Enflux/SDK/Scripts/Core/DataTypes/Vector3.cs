// Copyright (c) 2017 Enflux Inc.
// By downloading, accessing or using this SDK, you signify that you have read, understood and agree to the terms and conditions of the End User License Agreement located at: https://www.getenflux.com/pages/sdk-eula
using System;
using System.Runtime.InteropServices;

namespace Enflux.SDK.Core.DataTypes
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vector3
    {
        public short X;
        public short Y;
        public short Z;

        public override string ToString()
        {
            return string.Format("({0}, {1}, {2})", X, Y, Z);
        }

        public Vector3(short x, short y, short z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}