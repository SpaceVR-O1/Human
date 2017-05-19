// Copyright (c) 2017 Enflux Inc.
// By downloading, accessing or using this SDK, you signify that you have read, understood and agree to the terms and conditions of the End User License Agreement located at: https://www.getenflux.com/pages/sdk-eula
namespace Enflux.SDK.Core
{
    public class StateChange<T>
    {
        public T Previous { get; private set; }
        public T Next { get; private set; }

        public StateChange(T previous, T next)
        {
            Previous = previous;
            Next = next;
        }

        public override string ToString()
        {
            return string.Format("(Previous: {0}, Next: {1})", Previous, Next);
        }
    }
}
