// Copyright (c) 2017 Enflux Inc.
// By downloading, accessing or using this SDK, you signify that you have read, understood and agree to the terms and conditions of the End User License Agreement located at: https://www.getenflux.com/pages/sdk-eula
namespace Enflux.SDK.Core.DataTypes
{
    public enum InputCommands
    {
        ResetOrientation = 1,
        CalibrationFinished = 4,
        CalibrationStarted = 5,
        DeviceConnected = 128,
        DeviceDisconnected = 129,
        // Errors
        ErrorNoCalibration = -1,
        ErrorNoShirtPants = -2,
        ErrorCalibrationFailed = -3
    };
}