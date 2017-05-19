// Copyright (c) 2017 Enflux Inc.
// By downloading, accessing or using this SDK, you signify that you have read, understood and agree to the terms and conditions of the End User License Agreement located at: https://www.getenflux.com/pages/sdk-eula
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Enflux.SDK.Core.DataTypes
{
    [Serializable]
    public class RPY
    {
        [DllImport("EnfluxHID", CallingConvention = CallingConvention.Cdecl)]
        public static extern int LoadRotations(DeviceType device,
            [Out] [MarshalAs(UnmanagedType.LPArray, SizeConst = 20)] byte[] outData);

        public float Roll { get; private set; }
        public float Pitch { get; private set; }
        public float Yaw { get; private set; }

        public RPY(float roll, float pitch, float yaw)
        {
            Roll = roll;
            Pitch = pitch;
            Yaw = yaw;
        }

        public bool IsIdentity
        {
            get
            {
                return Mathf.Approximately(Roll, 0.0f) &&
                       Mathf.Approximately(Pitch, 0.0f) &&
                       Mathf.Approximately(Yaw, 0.0f);
            }
        }

        public class ModuleAngles
        {
            public RPY Center;
            public RPY LeftUpper;
            public RPY LeftLower;
            public RPY RightUpper;
            public RPY RightLower;

            public ModuleAngles(RPY center, RPY leftUpper, RPY leftLower, RPY rightUpper, RPY rightLower)
            {
                Center = center;
                LeftUpper = leftUpper;
                LeftLower = leftLower;
                RightUpper = rightUpper;
                RightLower = rightLower;
            }

            public void ApplyUpperAnglesTo(HumanoidAngles<UnityEngine.Vector3> angles)
            {
                var chest = new UnityEngine.Vector3(Center.Roll, Center.Pitch, Center.Yaw)*Mathf.Rad2Deg;
                var leftUpperArm = new UnityEngine.Vector3(LeftUpper.Roll, LeftUpper.Pitch, LeftUpper.Yaw)*Mathf.Rad2Deg;
                var leftLowerArm = new UnityEngine.Vector3(LeftLower.Roll, LeftLower.Pitch, LeftLower.Yaw)*Mathf.Rad2Deg;
                var rightUpperArm = new UnityEngine.Vector3(RightUpper.Roll, RightUpper.Pitch, RightUpper.Yaw)*
                                    Mathf.Rad2Deg;
                var rightLowerArm = new UnityEngine.Vector3(RightLower.Roll, RightLower.Pitch, RightLower.Yaw)*
                                    Mathf.Rad2Deg;

                angles.SetUpperBodyAngles(chest, leftUpperArm, leftLowerArm, rightUpperArm, rightLowerArm);
            }

            public void ApplyLowerAnglesTo(HumanoidAngles<UnityEngine.Vector3> angles)
            {
                var waist = new UnityEngine.Vector3(Center.Roll, Center.Pitch, Center.Yaw)*Mathf.Rad2Deg;
                var leftUpperLeg = new UnityEngine.Vector3(LeftUpper.Roll, LeftUpper.Pitch, LeftUpper.Yaw)*Mathf.Rad2Deg;
                var leftLowerLeg = new UnityEngine.Vector3(LeftLower.Roll, LeftLower.Pitch, LeftLower.Yaw)*Mathf.Rad2Deg;
                var rightUpperLeg = new UnityEngine.Vector3(RightUpper.Roll, RightUpper.Pitch, RightUpper.Yaw)*
                                    Mathf.Rad2Deg;
                var rightLowerLeg = new UnityEngine.Vector3(RightLower.Roll, RightLower.Pitch, RightLower.Yaw)*
                                    Mathf.Rad2Deg;

                angles.SetLowerBodyAngles(waist, leftUpperLeg, leftLowerLeg, rightUpperLeg, rightLowerLeg);
            }

            public bool IsInitialized
            {
                get
                {
                    return !Center.IsIdentity ||
                           !LeftUpper.IsIdentity ||
                           !LeftLower.IsIdentity ||
                           !RightUpper.IsIdentity ||
                           !RightLower.IsIdentity;
                }
            }
        }

        public static ModuleAngles ParseDataForOrientationAngles(byte[] bleData)
        {
            const float bleScaling = 325.94932f;
            short temp;
            float tempRoll, tempPitch, tempYaw;
            RPY postCenter;
            RPY postLeftUpper;
            RPY postLeftLower;
            RPY postRightUpper;
            RPY postRightLower;
            //*********************************
            // Author: Elijah Schuldt
            // Ported: Matt Brown
            //Center
            //*********************************
            temp = (short)((bleData[3] & 0xE0) << 3);
            temp |= (short)bleData[0];
            if (temp > 1024)
            {
                temp -= 2048;
            }
            tempRoll = (float)temp / bleScaling;
            temp = (short)((bleData[3] & 0x18) << 5);
            temp |= (short)bleData[1];
            if (temp > 512)
            {
                temp -= 1024;
            }
            tempPitch = (float)temp / bleScaling;
            temp = (short)((bleData[3] & 0x07) << 8);
            temp |= (short)bleData[2];
            if (temp > 1024)
            {
                temp -= 2048;
            }
            tempYaw = (float)temp / bleScaling;
            postCenter = new RPY(tempRoll, tempPitch, tempYaw);
            //*********************************
            //leftUpper
            //*********************************
            temp = (short)((bleData[7] & 0xE0) << 3);
            temp |= (short)bleData[4];
            if (temp > 1024)
            {
                temp -= 2048;
            }
            tempRoll = (float)temp / bleScaling;
            temp = (short)((bleData[7] & 0x18) << 5);
            temp |= (short)bleData[5];
            if (temp > 512)
            {
                temp -= 1024;
            }
            tempPitch = (float)temp / bleScaling;
            temp = (short)((bleData[7] & 0x07) << 8);
            temp |= (short)bleData[6];
            if (temp > 1024)
            {
                temp -= 2048;
            }
            tempYaw = (float)temp / bleScaling;
            postLeftUpper = new RPY(tempRoll, tempPitch, tempYaw);
            //*********************************
            //leftLower
            //*********************************
            temp = (short)((bleData[11] & 0xE0) << 3);
            temp |= (short)bleData[8];
            if (temp > 1024)
            {
                temp -= 2048;
            }
            tempRoll = (float)temp / bleScaling;
            temp = (short)((bleData[11] & 0x18) << 5);
            temp |= (short)bleData[9];
            if (temp > 512)
            {
                temp -= 1024;
            }
            tempPitch = (float)temp / bleScaling;
            temp = (short)((bleData[11] & 0x07) << 8);
            temp |= (short)bleData[10];
            if (temp > 1024)
            {
                temp -= 2048;
            }
            tempYaw = (float)temp / bleScaling;
            postLeftLower = new RPY(tempRoll, tempPitch, tempYaw);
            //*********************************
            //rightUpper
            //*********************************
            temp = (short)((bleData[15] & 0xE0) << 3);
            temp |= (short)bleData[12];
            if (temp > 1024)
            {
                temp -= 2048;
            }
            tempRoll = (float)temp / bleScaling;
            temp = (short)((bleData[15] & 0x18) << 5);
            temp |= (short)bleData[13];
            if (temp > 512)
            {
                temp -= 1024;
            }
            tempPitch = (float)temp / bleScaling;
            temp = (short)((bleData[15] & 0x07) << 8);
            temp |= (short)bleData[14];
            if (temp > 1024)
            {
                temp -= 2048;
            }
            tempYaw = (float)temp / bleScaling;
            postRightUpper = new RPY(tempRoll, tempPitch, tempYaw);
            //*********************************
            //rightLower
            //*********************************
            temp = (short)((bleData[19] & 0xE0) << 3);
            temp |= (short)bleData[16];
            if (temp > 1024)
            {
                temp -= 2048;
            }
            tempRoll = (float)temp / bleScaling;
            temp = (short)((bleData[19] & 0x18) << 5);
            temp |= (short)bleData[17];
            if (temp > 512)
            {
                temp -= 1024;
            }
            tempPitch = (float)temp / bleScaling;
            temp = (short)((bleData[19] & 0x07) << 8);
            temp |= (short)bleData[18];
            if (temp > 1024)
            {
                temp -= 2048;
            }
            tempYaw = (float)temp / bleScaling;
            postRightLower = new RPY(tempRoll, tempPitch, tempYaw);
            var result = new ModuleAngles(
                postCenter,
                postLeftUpper,
                postLeftLower,
                postRightUpper,
                postRightLower);
            return result;
        }
    }
}