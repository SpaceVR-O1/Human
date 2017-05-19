// Copyright (c) 2017 Enflux Inc.
// By downloading, accessing or using this SDK, you signify that you have read, understood and agree to the terms and conditions of the End User License Agreement located at: https://www.getenflux.com/pages/sdk-eula
using System;
using System.Runtime.InteropServices;

namespace Enflux.SDK.Core.DataTypes
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RawData
    {
        public short Timestamp { get; private set; }
        public Vector3 Accelerometer { get; private set; }
        public Vector3 Gyroscope { get; private set; }
        public Vector3 Magnetometer { get; private set; }

        public int Sensor
        {
            get { return Timestamp >> 12; }
        }

        public int Time
        {
            get { return Timestamp & 0x0FFF; }
        }

        public override string ToString()
        {
            return string.Format("(Sensor: {0}, Timestamp: {1}, Accel: {2}, Gyro: {2}, Mag: {3})",
                Sensor,
                Time,
                Accelerometer,
                Gyroscope);
        }
    }
}