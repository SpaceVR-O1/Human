// Copyright (c) 2017 Enflux Inc.
// By downloading, accessing or using this SDK, you signify that you have read, understood and agree to the terms and conditions of the End User License Agreement located at: https://www.getenflux.com/pages/sdk-eula
using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using Enflux.Attributes;
using Enflux.SDK.Core;
using Enflux.SDK.Core.DataTypes;
using Enflux.SDK.Extensions;
using UnityEngine;
using DeviceType = Enflux.SDK.Core.DataTypes.DeviceType;

namespace Enflux.SDK.Recording
{
    public class EnfluxFilePlayer : EnfluxSuitStream
    {
        public string Filename = "";
        [SerializeField, Readonly] private bool _isPlaying;

        private Coroutine _routine;
        private const float SecondsToMilliseconds = 1000.0f;

        private AnimationHeader _header;


        private string DefaultFilename
        {
            get { return Application.streamingAssetsPath + "/PoseRecordings/recording.enfl"; }
        }

        public bool IsPlaying
        {
            set
            {
                if (_isPlaying == value)
                {
                    return;
                }

                if (!_isPlaying && value)
                {
                    if (_routine != null)
                    {
                        StopCoroutine(_routine);
                    }
                    _isPlaying = true;
                    _routine = StartCoroutine(Co_Playback());
                }
                else if (_isPlaying && !value)
                {
                    _isPlaying = false;
                    if (_routine != null)
                    {
                        StopCoroutine(_routine);
                        _routine = null;
                    }
                }
            }
            get { return _isPlaying; }
        }


        private void Reset()
        {
            Filename = DefaultFilename;
        }

        private void Start()
        {
            if (Filename == "")
            {
                Filename = DefaultFilename;
            }
        }

#if TEST_RECORD_PLAYBACK
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                IsPlaying = !IsPlaying;
            }
        }
#endif

        private bool ValidateHeader()
        {
            if (_header.HeaderVersion != AnimationHeader.HEADER_VERSION)
            {
                return false;
            }
            if (_header.FrameVersion != AnimationHeader.FRAME_VERSION)
            {
                return false;
            }
            return true;
        }

        private void ApplyAngles(DeviceType device, ref byte[] rawFrame)
        {
            var angles = RPY.ParseDataForOrientationAngles(rawFrame);
            if (device == DeviceType.Shirt)
            {
                AbsoluteAngles.SetUpperBodyAngles(
                    new UnityEngine.Vector3(angles.Center.Roll, angles.Center.Pitch, angles.Center.Yaw)*Mathf.Rad2Deg,
                    new UnityEngine.Vector3(angles.LeftUpper.Roll, angles.LeftUpper.Pitch, angles.LeftUpper.Yaw)*
                    Mathf.Rad2Deg,
                    new UnityEngine.Vector3(angles.LeftLower.Roll, angles.LeftLower.Pitch, angles.LeftLower.Yaw)*
                    Mathf.Rad2Deg,
                    new UnityEngine.Vector3(angles.RightUpper.Roll, angles.RightUpper.Pitch, angles.RightUpper.Yaw)*
                    Mathf.Rad2Deg,
                    new UnityEngine.Vector3(angles.RightLower.Roll, angles.RightLower.Pitch, angles.RightLower.Yaw)*
                    Mathf.Rad2Deg);
            }
            else if (device == DeviceType.Pants)
            {
                AbsoluteAngles.SetLowerBodyAngles(
                    new UnityEngine.Vector3(angles.Center.Roll, angles.Center.Pitch, angles.Center.Yaw)*Mathf.Rad2Deg,
                    new UnityEngine.Vector3(angles.LeftUpper.Roll, angles.LeftUpper.Pitch, angles.LeftUpper.Yaw)*
                    Mathf.Rad2Deg,
                    new UnityEngine.Vector3(angles.LeftLower.Roll, angles.LeftLower.Pitch, angles.LeftLower.Yaw)*
                    Mathf.Rad2Deg,
                    new UnityEngine.Vector3(angles.RightUpper.Roll, angles.RightUpper.Pitch, angles.RightUpper.Yaw)*
                    Mathf.Rad2Deg,
                    new UnityEngine.Vector3(angles.RightLower.Roll, angles.RightLower.Pitch, angles.RightLower.Yaw)*
                    Mathf.Rad2Deg);
            }
        }

        public IEnumerator Co_Playback()
        {
            if (!File.Exists(Filename))
            {
                Debug.LogError(name + ": Error, file path doesn't exist: '" + Filename + "'!");
                IsPlaying = false;
                yield break;
            }
            using (var fileStream = File.OpenRead(Filename))
            {
                var rawHeader = new byte[Marshal.SizeOf(typeof (AnimationHeader))];
                var rawTimestamp = new byte[4];
                var lastShirtFrame = new byte[20];
                var lastPantsFrame = new byte[20];
                var startTime = Time.time;
                _isPlaying = true;
                var initializedDevices = DeviceType.None;

                // Read file header
                if (fileStream.CanRead &&
                    fileStream.Read(rawHeader, 0, Marshal.SizeOf(typeof (AnimationHeader))) ==
                    Marshal.SizeOf(typeof (AnimationHeader)))
                {
                    _header = AnimationHeader.InitializeFromArray(rawHeader);     
                }
                else
                {
                    Debug.Log("Error reading the file header for: '" + Filename + "'");
                    IsPlaying = false;
                    yield break;
                }
                if (!ValidateHeader())
                {
                    Debug.LogError(name + ": Error, incorrect file header: '" + Filename + "'!");
                    IsPlaying = false;
                    yield break;
                }
                ShirtBaseOrientation = _header.ShirtBaseOrientation.ToUnityVector3();
                PantsBaseOrientation = _header.PantsBaseOrientation.ToUnityVector3();

                while (IsPlaying && fileStream.CanRead)
                {
                    if (fileStream.Read(rawTimestamp, 0, 4) != 4)
                    {
                        IsPlaying = false;
                    }
                    var latestTimestamp = BitConverter.ToUInt32(rawTimestamp, 0);
                    if (latestTimestamp > (Time.time - startTime)*SecondsToMilliseconds)
                    {
                        // We have read samples up to the point of the running time.
                        // Set the angle rotations
                        if ((initializedDevices & DeviceType.Shirt) == DeviceType.Shirt)
                        {
                            ApplyAngles(DeviceType.Shirt, ref lastShirtFrame);
                        }
                        if ((initializedDevices & DeviceType.Pants) == DeviceType.Pants)
                        {
                            ApplyAngles(DeviceType.Pants, ref lastPantsFrame);
                        }

                        // Let game catch up to the timestamp if the framerate is faster than the sample rate.
                        while (latestTimestamp > (Time.time - startTime)*SecondsToMilliseconds)
                        {
                            yield return null;
                        }

                    }
                    var dev_type = (DeviceType) fileStream.ReadByte();
                    if (dev_type == DeviceType.Shirt)
                    {
                        initializedDevices |= DeviceType.Shirt;
                        if (fileStream.Read(lastShirtFrame, 0, 20) != 20)
                        {
                            IsPlaying = false;
                            yield return true;
                        }
                    }
                    else if (dev_type == DeviceType.Pants)
                    {
                        initializedDevices |= DeviceType.Pants;
                        if (fileStream.Read(lastPantsFrame, 0, 20) != 20)
                        {
                            IsPlaying = false;
                            yield return true;
                        }
                    }
                }
                // Clean exit
                IsPlaying = false;
            }
        }
    }
}