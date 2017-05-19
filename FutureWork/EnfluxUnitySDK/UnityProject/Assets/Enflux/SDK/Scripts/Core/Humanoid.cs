// Copyright (c) 2017 Enflux Inc.
// By downloading, accessing or using this SDK, you signify that you have read, understood and agree to the terms and conditions of the End User License Agreement located at: https://www.getenflux.com/pages/sdk-eula
using System;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Enflux.SDK.Core
{
    public class Humanoid : MonoBehaviour
    {
        private readonly JointRotations _jointRotations = new JointRotations();

        // Caching these guys rather than re-allocating every frame
        private readonly float[] _baseChestAngles = new float[3];
        private readonly float[] _yawAdjustedChestAngles = new float[3];
        private readonly float[] _yawAdjustedLeftUpperArmAngles = new float[3];
        private readonly float[] _yawAdjustedLeftLowerArmAngles = new float[3];
        private readonly float[] _yawAdjustedRightUpperArmAngles = new float[3];
        private readonly float[] _yawAdjustedRightLowerArmAngles = new float[3];

        private readonly float[] _yawAdjustedWaistAngles = new float[3];
        private readonly float[] _yawAdjustedLeftUpperLegAngles = new float[3];
        private readonly float[] _yawAdjustedLeftLowerLegAngles = new float[3];
        private readonly float[] _yawAdjustedRightUpperLegAngles = new float[3];
        private readonly float[] _yawAdjustedRightLowerLegAngles = new float[3];

        /// <summary>
        /// The source of the absolute angles used to calculate local angles for each limb.
        /// </summary>
        public EnfluxSuitStream AbsoluteAnglesStream;

        /// <summary>
        /// The angles of each limb in the humanoid relative to its parent limb.
        /// </summary>
        public readonly HumanoidAngles<Quaternion> LocalAngles = new HumanoidAngles<Quaternion>();


        private Vector3 ChestBaseOrientation
        {
            get { return AbsoluteAnglesStream.ShirtBaseOrientation; }
        }

        private Vector3 WaistBaseOrientation
        {
            get { return AbsoluteAnglesStream.PantsBaseOrientation; }
        }


        protected virtual void Reset()
        {
            AbsoluteAnglesStream = AbsoluteAnglesStream ?? FindObjectOfType<EnfluxManager>();
        }

        protected virtual void OnEnable()
        {
            AbsoluteAnglesStream = AbsoluteAnglesStream ?? FindObjectOfType<EnfluxManager>();
            if (AbsoluteAnglesStream == null)
            {
                Debug.LogError(name + ": AbsoluteAnglesStream isn't assigned and no instance is in the scene!");
                enabled = false;
                return;
            }
            AbsoluteAnglesStream.AbsoluteAngles.UpperBodyAnglesChanged += OnUpperBodyAnglesChanged;
            AbsoluteAnglesStream.AbsoluteAngles.LowerBodyAnglesChanged += OnLowerBodyAnglesChanged;
        }

        protected virtual void OnDisable()
        {
            AbsoluteAnglesStream.AbsoluteAngles.UpperBodyAnglesChanged -= OnUpperBodyAnglesChanged;
            AbsoluteAnglesStream.AbsoluteAngles.LowerBodyAnglesChanged -= OnLowerBodyAnglesChanged;
        }

        private void OnUpperBodyAnglesChanged(HumanoidAngles<Vector3> absoluteAngles)
        {
            var headsetRotation = Quaternion.identity;

            // Pack absolute angles into arrays for calculations
            var baseChestYaw = ChestBaseOrientation.z;

            _baseChestAngles[0] = ChestBaseOrientation.x;
            _baseChestAngles[1] = ChestBaseOrientation.y;
            _baseChestAngles[2] = ChestBaseOrientation.z;

            _yawAdjustedChestAngles[0] = absoluteAngles.Chest.x;
            _yawAdjustedChestAngles[1] = absoluteAngles.Chest.y;
            _yawAdjustedChestAngles[2] = absoluteAngles.Chest.z - baseChestYaw;

            _yawAdjustedLeftUpperArmAngles[0] = absoluteAngles.LeftUpperArm.x;
            _yawAdjustedLeftUpperArmAngles[1] = absoluteAngles.LeftUpperArm.y;
            _yawAdjustedLeftUpperArmAngles[2] = absoluteAngles.LeftUpperArm.z - baseChestYaw;

            _yawAdjustedLeftLowerArmAngles[0] = absoluteAngles.LeftLowerArm.x;
            _yawAdjustedLeftLowerArmAngles[1] = absoluteAngles.LeftLowerArm.y;
            _yawAdjustedLeftLowerArmAngles[2] = absoluteAngles.LeftLowerArm.z - baseChestYaw;

            _yawAdjustedRightUpperArmAngles[0] = absoluteAngles.RightUpperArm.x;
            _yawAdjustedRightUpperArmAngles[1] = absoluteAngles.RightUpperArm.y;
            _yawAdjustedRightUpperArmAngles[2] = absoluteAngles.RightUpperArm.z - baseChestYaw;

            _yawAdjustedRightLowerArmAngles[0] = absoluteAngles.RightLowerArm.x;
            _yawAdjustedRightLowerArmAngles[1] = absoluteAngles.RightLowerArm.y;
            _yawAdjustedRightLowerArmAngles[2] = absoluteAngles.RightLowerArm.z - baseChestYaw;

            // Transform absolute upper body angles into local ones
            var localAngleChest = _jointRotations.rotateCore(
                _yawAdjustedChestAngles,
                _baseChestAngles,
                headsetRotation
            );
            var localAngleLeftUpperArm = _jointRotations.rotateLeftArm(
                _yawAdjustedLeftUpperArmAngles,
                localAngleChest,
                headsetRotation
            );
            var localAngleLeftLowerArm = _jointRotations.rotateLeftForearm(
                _yawAdjustedLeftLowerArmAngles,
                localAngleChest,
                localAngleLeftUpperArm,
                headsetRotation
            );
            var localAngleRightUpperArm = _jointRotations.rotateRightArm(
                _yawAdjustedRightUpperArmAngles,
                localAngleChest,
                headsetRotation
            );
            var localAngleRightLowerArm = _jointRotations.rotateRightForearm(
                _yawAdjustedRightLowerArmAngles,
                localAngleChest,
                localAngleRightUpperArm,
                headsetRotation
            );

            LocalAngles.SetUpperBodyAngles(localAngleChest, localAngleLeftUpperArm, localAngleLeftLowerArm,
                localAngleRightUpperArm, localAngleRightLowerArm);
        }

        private void OnLowerBodyAnglesChanged(HumanoidAngles<Vector3> absoluteAngles)
        {
            var headsetRotation = Quaternion.identity;
            var initialWaistYaw = WaistBaseOrientation.z;
            var initialWaistAngles = Quaternion.AngleAxis(initialWaistYaw, Vector3.up);

            _yawAdjustedWaistAngles[0] = absoluteAngles.Waist.x;
            _yawAdjustedWaistAngles[1] = absoluteAngles.Waist.y;
            _yawAdjustedWaistAngles[2] = absoluteAngles.Waist.z;

            _yawAdjustedLeftUpperLegAngles[0] = absoluteAngles.LeftUpperLeg.x;
            _yawAdjustedLeftUpperLegAngles[1] = absoluteAngles.LeftUpperLeg.y;
            _yawAdjustedLeftUpperLegAngles[2] = absoluteAngles.LeftUpperLeg.z;

            _yawAdjustedLeftLowerLegAngles[0] = absoluteAngles.LeftLowerLeg.x;
            _yawAdjustedLeftLowerLegAngles[1] = absoluteAngles.LeftLowerLeg.y;
            _yawAdjustedLeftLowerLegAngles[2] = absoluteAngles.LeftLowerLeg.z;

            _yawAdjustedRightUpperLegAngles[0] = absoluteAngles.RightUpperLeg.x;
            _yawAdjustedRightUpperLegAngles[1] = absoluteAngles.RightUpperLeg.y;
            _yawAdjustedRightUpperLegAngles[2] = absoluteAngles.RightUpperLeg.z;

            _yawAdjustedRightLowerLegAngles[0] = absoluteAngles.RightLowerLeg.x;
            _yawAdjustedRightLowerLegAngles[1] = absoluteAngles.RightLowerLeg.y;
            _yawAdjustedRightLowerLegAngles[2] = absoluteAngles.RightLowerLeg.z;

            // Transform absolute lower body angles into relative ones
            var localAngleWaist = _jointRotations.rotateWaist(
                _yawAdjustedWaistAngles,
                initialWaistAngles,
                headsetRotation
            );
            var localAngleLeftUpperLeg = _jointRotations.rotateLeftLeg(
                _yawAdjustedLeftUpperLegAngles,
                localAngleWaist,
                initialWaistAngles
            );
            var localAngleLeftLowerLeg = _jointRotations.rotateLeftShin(
                _yawAdjustedLeftLowerLegAngles,
                localAngleWaist,
                localAngleLeftUpperLeg,
                initialWaistAngles
            );
            var localAngleRightUpperLeg = _jointRotations.rotateRightLeg(
                _yawAdjustedRightUpperLegAngles,
                localAngleWaist,
                initialWaistAngles
            );
            var localAngleRightLowerLeg = _jointRotations.rotateRightShin(
                _yawAdjustedRightLowerLegAngles,
                localAngleWaist,
                localAngleRightUpperLeg,
                initialWaistAngles
            );

            LocalAngles.SetLowerBodyAngles(localAngleWaist, localAngleLeftUpperLeg, localAngleLeftLowerLeg,
                localAngleRightUpperLeg, localAngleRightLowerLeg);
        }
    }
}