// Copyright (c) 2017 Enflux Inc.
// By downloading, accessing or using this SDK, you signify that you have read, understood and agree to the terms and conditions of the End User License Agreement located at: https://www.getenflux.com/pages/sdk-eula
using UnityEngine;

namespace Enflux.SDK.Extensions
{
    public static class Vector3Extensions
    {
        public static Core.DataTypes.Vector3 ToEnfluxVector3(this Vector3 v)
        {
            return new Core.DataTypes.Vector3
            {
                X = (short) Mathf.RoundToInt(v.x),
                Y = (short) Mathf.RoundToInt(v.y),
                Z = (short) Mathf.RoundToInt(v.z)
            };
        }

        public static Vector3 ToUnityVector3(this Core.DataTypes.Vector3 v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }
    }
}