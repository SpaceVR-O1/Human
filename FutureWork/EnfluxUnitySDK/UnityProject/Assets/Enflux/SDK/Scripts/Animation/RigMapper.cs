// Copyright (c) 2017 Enflux Inc.
// By downloading, accessing or using this SDK, you signify that you have read, understood and agree to the terms and conditions of the End User License Agreement located at: https://www.getenflux.com/pages/sdk-eula
using Enflux.SDK.Core;
using UnityEngine;

namespace Enflux.SDK.Animation
{
    public class RigMapper : MonoBehaviour
    {
        [SerializeField] private Humanoid _humanoid;

        [SerializeField] private Transform _chest;
        [SerializeField] private Transform _leftUpperArm;
        [SerializeField] private Transform _leftLowerArm;
        [SerializeField] private Transform _rightUpperArm;
        [SerializeField] private Transform _rightLowerArm;
        [SerializeField] private Transform _waist;
        [SerializeField] private Transform _leftUpperLeg;
        [SerializeField] private Transform _leftLowerLeg;
        [SerializeField] private Transform _rightUpperLeg;
        [SerializeField] private Transform _rightLowerLeg;


        public Transform Chest
        {
            get { return _chest; }
            set { _chest = value; }
        }

        public Transform LeftUpperArm
        {
            get { return _leftUpperArm; }
            set { _leftUpperArm = value; }
        }

        public Transform LeftLowerArm
        {
            get { return _leftLowerArm; }
            set { _leftLowerArm = value; }
        }

        public Transform RightUpperArm
        {
            get { return _rightUpperArm; }
            set { _rightUpperArm = value; }
        }

        public Transform RightLowerArm
        {
            get { return _rightLowerArm; }
            set { _rightLowerArm = value; }
        }

        public Transform Waist
        {
            get { return _waist; }
            set { _waist = value; }
        }

        public Transform LeftUpperLeg
        {
            get { return _leftUpperLeg; }
            set { _leftUpperLeg = value; }
        }

        public Transform LeftLowerLeg
        {
            get { return _leftLowerLeg; }
            set { _leftLowerLeg = value; }
        }

        public Transform RightUpperLeg
        {
            get { return _rightUpperLeg; }
            set { _rightUpperLeg = value; }
        }

        public Transform RightLowerLeg
        {
            get { return _rightLowerLeg; }
            set { _rightLowerLeg = value; }
        }

        public Humanoid Humanoid
        {
            get { return _humanoid; }
            set { _humanoid = value; }
        }


        protected virtual void Reset()
        {
            Humanoid = FindObjectOfType<Humanoid>();
        }

        protected virtual void OnEnable()
        {
            Humanoid = Humanoid ?? FindObjectOfType<Humanoid>();

            if (Humanoid == null)
            {
                Debug.LogError("Humanoid is not assigned and there is no instance in the scene!");
                enabled = false;
            }
        }

        protected virtual void LateUpdate()
        {
            UpdateRig();
        }

        public virtual void UpdateRig()
        {
            if (Humanoid == null)
            {
                return;
            }

            // Apply upper body rotations to rig
            if (Chest != null)
            {
                Chest.localRotation = Humanoid.LocalAngles.Chest;
            }
            if (LeftUpperArm != null)
            {
                LeftUpperArm.localRotation = Humanoid.LocalAngles.LeftUpperArm;
            }
            if (LeftLowerArm != null)
            {
                LeftLowerArm.localRotation = Humanoid.LocalAngles.LeftLowerArm;
            }
            if (RightUpperArm != null)
            {
                RightUpperArm.localRotation = Humanoid.LocalAngles.RightUpperArm;
            }
            if (RightLowerArm != null)
            {
                RightLowerArm.localRotation = Humanoid.LocalAngles.RightLowerArm;
            }

            // Apply lower body rotations to rig
            if (Waist != null)
            {
                Waist.localRotation = Humanoid.LocalAngles.Waist;
            }
            if (LeftUpperLeg != null)
            {
                LeftUpperLeg.localRotation = Humanoid.LocalAngles.LeftUpperLeg;
            }
            if (LeftLowerLeg != null)
            {
                LeftLowerLeg.localRotation = Humanoid.LocalAngles.LeftLowerLeg;
            }
            if (RightUpperLeg != null)
            {
                RightUpperLeg.localRotation = Humanoid.LocalAngles.RightUpperLeg;
            }
            if (RightLowerLeg != null)
            {
                RightLowerLeg.localRotation = Humanoid.LocalAngles.RightLowerLeg;
            }
        }
    }
}