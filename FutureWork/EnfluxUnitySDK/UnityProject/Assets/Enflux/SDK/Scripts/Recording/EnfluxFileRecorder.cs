// Copyright (c) 2017 Enflux Inc.
// By downloading, accessing or using this SDK, you signify that you have read, understood and agree to the terms and conditions of the End User License Agreement located at: https://www.getenflux.com/pages/sdk-eula

using System.IO;
using Enflux.Attributes;
using Enflux.SDK.Core;
using Enflux.SDK.Extensions;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Enflux.SDK.Recording
{
    public class EnfluxFileRecorder : MonoBehaviour
    {
        public string Filename = "";
        [SerializeField, Readonly] private bool _isRecording;
        [SerializeField] private EnfluxSuitStream _sourceSuitStream;

        private string DefaultFilename
        {
            get { return Application.streamingAssetsPath + "/PoseRecordings/recording.enfl"; }
        }

        private void Reset()
        {
            Filename = DefaultFilename;
        }

        private void OnEnable()
        {
            _sourceSuitStream = _sourceSuitStream ?? FindObjectOfType<EnfluxManager>();

            if (_sourceSuitStream == null)
            {
                Debug.LogError(name + ": SourceSuitStream is not assigned and no EnfluxSuitStream instance is in the scene!");
            }
        }

        private void Start()
        {
            if (Filename == "")
            {
                Filename = DefaultFilename;
            }
        }

        private void OnApplicationQuit()
        {
            if (_isRecording)
            {
                EndRecording();
            }
        }

#if TEST_RECORD_PLAYBACK
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                IsRecording = !IsRecording;
            }
        }
#endif

        public bool IsRecording
        {
            get { return _isRecording; }
            set
            {
                if (IsRecording == value)
                {
                    return;
                }
                if (!_isRecording)
                {
                    var error = StartRecording(Filename);
                    if (error != 0)
                    {
                        Debug.LogError(name + " - Unable to start recording. Error code: " + error);
                        IsRecording = false;
                        return;
                    }                 
                }
                else
                {
                    EndRecording();
                }
                _isRecording = value;
            }
        }   

        private int StartRecording(string filename)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return EnfluxNativeFileRecorder.StartRecording(filename);
#else
            return 0;
#endif
        }

        private int EndRecording()
        {
            if (_sourceSuitStream != null)
            {
                // TODO: Check error codes
                SetShirtBaseOrientation(_sourceSuitStream.ShirtBaseOrientation);
                SetPantsBaseOrientation(_sourceSuitStream.PantsBaseOrientation);
            }
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return EnfluxNativeFileRecorder.EndRecording();
#else
            return 0;
#endif
        }

        private int SetShirtBaseOrientation(Vector3 orientation)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return EnfluxNativeFileRecorder.SetShirtBaseOrientation(orientation.ToEnfluxVector3());
#else
            return 0;
#endif
        }

        private int SetPantsBaseOrientation(Vector3 orientation)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return EnfluxNativeFileRecorder.SetPantsBaseOrientation(orientation.ToEnfluxVector3());
#else
            return 0;
#endif
        }
    }
}