// Copyright (c) 2017 Enflux Inc.
// By downloading, accessing or using this SDK, you signify that you have read, understood and agree to the terms and conditions of the End User License Agreement located at: https://www.getenflux.com/pages/sdk-eula

using Enflux.SDK.Core;
using UnityEditor;
using UnityEngine;
using DeviceType = Enflux.SDK.Core.DataTypes.DeviceType;

namespace Assets.Enflux.SDK.Scripts.Editor
{

    [CustomEditor(typeof(EnfluxManager))]
    [CanEditMultipleObjects]
    public class EnfluxManagerEditor : UnityEditor.Editor
    {
        private EnfluxManager _manager;


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            _manager = (EnfluxManager) target;

            var isPlaying = Application.isPlaying;
            GUI.enabled = isPlaying;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);

            // Connect/disconnect shirt or pants
            GUILayout.BeginHorizontal();
            {
                if (!_manager.IsShirtActive)
                {
                    if (GUILayout.Button("Connect Shirt"))
                    {
                        _manager.Connect(DeviceType.Shirt);
                    }
                }
                if (!_manager.ArePantsActive)
                {
                    if (GUILayout.Button("Connect Pants"))
                    {
                        _manager.Connect(DeviceType.Pants);
                    }
                }
                if (!_manager.IsShirtActive && !_manager.ArePantsActive)
                {
                    if (GUILayout.Button("Connect Shirt and Pants"))
                    {
                        _manager.Connect(DeviceType.All);
                    }
                }

                GUI.contentColor = Color.yellow;
                if (_manager.IsShirtActive && !_manager.ArePantsActive)
                {
                    if (GUILayout.Button("Disconnect Shirt"))
                    {
                        _manager.Disconnect();
                    }
                }
                else if (!_manager.IsShirtActive && _manager.ArePantsActive)
                {
                    if (GUILayout.Button("Disconnect Pants"))
                    {
                        _manager.Disconnect();
                    }
                }
                else if (_manager.IsShirtActive && _manager.ArePantsActive)
                {
                    if (GUILayout.Button("Disconnect Shirt and Pants"))
                    {
                        _manager.Disconnect();
                    }
                }
            }
            GUILayout.EndHorizontal();
       
            EditorGUILayout.Space();
            GUI.contentColor = Color.white;
            EditorGUILayout.LabelField("Calibration", EditorStyles.boldLabel);

            // Calibrate shirt or pants
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                {
                    GUI.enabled = isPlaying && !_manager.IsShirtActive;
                    if (GUILayout.Button("Calibrate Shirt"))
                    {
                        _manager.Calibrate(DeviceType.Shirt);
                    }

                    GUI.enabled = isPlaying && !_manager.ArePantsActive;
                    if (GUILayout.Button("Calibrate Pants"))
                    {
                        _manager.Calibrate(DeviceType.Pants);
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            EditorGUILayout.Space();
            GUI.contentColor = Color.white;
            GUI.enabled = isPlaying;
            EditorGUILayout.LabelField("Alignment", EditorStyles.boldLabel);
            // Align shirt + pants
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                {
                    GUI.enabled = isPlaying && _manager.IsAnyDeviceActive;
                    if (GUILayout.Button("Reset Orientation"))
                    {
                        _manager.ResetFullBodyBaseOrientation();
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }
    }
}