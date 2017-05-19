// Copyright (c) 2017 Enflux Inc.
// By downloading, accessing or using this SDK, you signify that you have read, understood and agree to the terms and conditions of the End User License Agreement located at: https://www.getenflux.com/pages/sdk-eula
using UnityEngine;

namespace Enflux.SDK.Extensions
{
    public static class GameObjectExtensions
    {
        public static T FindChildComponent<T>(this GameObject go, string childName) where T : Component
        {
            if (go == null)
            {
                return null;
            }
            var children = go.GetComponentsInChildren<T>(true);
            // ReSharper disable once LoopCanBeConvertedToQuery
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < children.Length; ++i)
            {
                if (children[i].name == childName)
                {
                    return children[i];
                }
            }
            return null;
        }
    }
}