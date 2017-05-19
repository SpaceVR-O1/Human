// Copyright (c) 2017 Enflux Inc.
// By downloading, accessing or using this SDK, you signify that you have read, understood and agree to the terms and conditions of the End User License Agreement located at: https://www.getenflux.com/pages/sdk-eula
using System;

namespace Enflux.SDK.Core.DataTypes
{
    [Flags]
    public enum DeviceType
    {
        None = 0,
        Shirt = 1,
        Pants = 2,
        All = 3
    }
}