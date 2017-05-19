// Copyright (c) 2017 Enflux Inc.
// By downloading, accessing or using this SDK, you signify that you have read, understood and agree to the terms and conditions of the End User License Agreement located at: https://www.getenflux.com/pages/sdk-eula
using Enflux.SDK.Core;
using Enflux.SDK.Extensions;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DeviceType = Enflux.SDK.Core.DataTypes.DeviceType;

namespace Enflux.SDK.UI
{
    public class EnfluxExampleConnectionPanel : MonoBehaviour
    {
        [SerializeField] private EnfluxManager _enfluxManager;

        [SerializeField] private Text _connectHeader;
        [SerializeField] private RectTransform _connectButtonsContainer;
        [SerializeField] private Button _connectShirtButton;
        [SerializeField] private Button _connectPantsButton;
        [SerializeField] private Text _disconnectHeader;
        [SerializeField] private Button _disconnectShirtButton;
        [SerializeField] private Button _disconnectPantsButton;
        [SerializeField] private Button _disconnectAllButton;
        [SerializeField] private Button _calibrateShirtButton;
        [SerializeField] private Button _calibratePantsButton;
        [SerializeField] private Button _resetOrientationButton;
        [SerializeField] private Text _shirtStateText;
        [SerializeField] private Text _pantsStateText;
        [SerializeField] private Text _resetOrientationText;

        [SerializeField] private float _resetOrientationTime = 4.0f;

        private IEnumerator _co_resetTimer;


        public float ResetOrientationTime
        {
            get { return _resetOrientationTime; }
            set
            {
                _resetOrientationTime = Mathf.Max(0f, value);
            }
        }

        private void Reset()
        {
            _enfluxManager = FindObjectOfType<EnfluxManager>();

            _connectHeader = gameObject.FindChildComponent<Text>("Header_Connect");
            _connectButtonsContainer = gameObject.FindChildComponent<RectTransform>("Container_ConnectButtons");
            _connectShirtButton = gameObject.FindChildComponent<Button>("Button_ConnectShirt");
            _connectPantsButton = gameObject.FindChildComponent<Button>("Button_ConnectPants");
            _disconnectHeader = gameObject.FindChildComponent<Text>("Header_Disconnect");
            _disconnectShirtButton = gameObject.FindChildComponent<Button>("Button_DisconnectShirt");
            _disconnectPantsButton = gameObject.FindChildComponent<Button>("Button_DisconnectPants");
            _disconnectAllButton = gameObject.FindChildComponent<Button>("Button_DisconnectAll");
            _calibrateShirtButton = gameObject.FindChildComponent<Button>("Button_CalibrateShirt");
            _calibratePantsButton = gameObject.FindChildComponent<Button>("Button_CalibratePants");
            _resetOrientationButton = gameObject.FindChildComponent<Button>("Button_ResetOrientation");
            _shirtStateText = gameObject.FindChildComponent<Text>("Text_ShirtState");
            _pantsStateText = gameObject.FindChildComponent<Text>("Text_PantsState");
            _resetOrientationText = gameObject.FindChildComponent<Text>("Text_ResetOrientation");
        }

        private void OnValidate()
        {
            // Force validation
            ResetOrientationTime = ResetOrientationTime;
        }

        private void OnEnable()
        {
            _enfluxManager = _enfluxManager ?? FindObjectOfType<EnfluxManager>();

            if (_enfluxManager == null)
            {
                Debug.LogError(name + ": EnfluxManager is not assigned and no instance is in the scene!");
                enabled = false;
            }
            SubscribeToEvents();
            UpdateUi();
        }

        private void OnDisable()
        {
            if (_enfluxManager != null)
            {
                UnsubscribeFromEvents();
            }
        }

        private void SubscribeToEvents()
        {
            _enfluxManager.ShirtStateChanged += EnfluxManagerOnShirtStateChanged;
            _enfluxManager.PantsStateChanged += EnfluxManagerOnPantsStateChanged;
            _enfluxManager.ShirtReceivedNotification += EnfluxManagerOnShirtReceivedNotification;
            _enfluxManager.PantsReceivedNotification += EnfluxManagerOnPantsReceivedNotification;
            _enfluxManager.ShirtReceivedError += EnfluxManagerOnShirtReceivedError;
            _enfluxManager.PantsReceivedError += EnfluxManagerOnPantsReceivedError;

            _connectShirtButton.onClick.AddListener(ConnectShirtButtonOnClick);
            _connectPantsButton.onClick.AddListener(ConnectPantsButtonOnClick);
            _disconnectShirtButton.onClick.AddListener(DisconnectButtonOnClick);
            _disconnectPantsButton.onClick.AddListener(DisconnectButtonOnClick);
            _disconnectAllButton.onClick.AddListener(DisconnectButtonOnClick);
            _calibrateShirtButton.onClick.AddListener(CalibrateShirtButtonOnClick);
            _calibratePantsButton.onClick.AddListener(CalibratePantsButtonOnClick);
            _resetOrientationButton.onClick.AddListener(ResetOrientationButtonOnClick);
        }

        private void UnsubscribeFromEvents()
        {
            _enfluxManager.ShirtStateChanged -= EnfluxManagerOnShirtStateChanged;
            _enfluxManager.PantsStateChanged -= EnfluxManagerOnPantsStateChanged;
            _enfluxManager.ShirtReceivedNotification -= EnfluxManagerOnShirtReceivedNotification;
            _enfluxManager.PantsReceivedNotification -= EnfluxManagerOnPantsReceivedNotification;
            _enfluxManager.ShirtReceivedError -= EnfluxManagerOnShirtReceivedError;
            _enfluxManager.PantsReceivedError -= EnfluxManagerOnPantsReceivedError;

            _connectShirtButton.onClick.RemoveListener(ConnectShirtButtonOnClick);
            _connectPantsButton.onClick.RemoveListener(ConnectPantsButtonOnClick);
            _disconnectShirtButton.onClick.RemoveListener(DisconnectButtonOnClick);
            _disconnectPantsButton.onClick.RemoveListener(DisconnectButtonOnClick);
            _disconnectAllButton.onClick.RemoveListener(DisconnectButtonOnClick);
            _calibrateShirtButton.onClick.RemoveListener(CalibrateShirtButtonOnClick);
            _calibratePantsButton.onClick.RemoveListener(CalibratePantsButtonOnClick);
            _resetOrientationButton.onClick.RemoveListener(ResetOrientationButtonOnClick);
        }

        private void EnfluxManagerOnShirtStateChanged(StateChange<DeviceState> stateChange)
        {
            UpdateUi();
            _shirtStateText.text = GetNicifiedString(stateChange);
            SetStatusTextColor(_shirtStateText, stateChange);
        }

        private void EnfluxManagerOnPantsStateChanged(StateChange<DeviceState> stateChange)
        {
            UpdateUi();
            _pantsStateText.text = GetNicifiedString(stateChange);
            SetStatusTextColor(_pantsStateText, stateChange);
        }

        private void EnfluxManagerOnPantsReceivedNotification(DeviceNotification deviceNotification)
        {
            if (deviceNotification == DeviceNotification.ResetOrientation)
            {
                DoResetOrientationAnimation(false);
            }
        }

        private void EnfluxManagerOnShirtReceivedNotification(DeviceNotification deviceNotification)
        {
            if (deviceNotification == DeviceNotification.ResetOrientation)
            {
                DoResetOrientationAnimation(false);
            }
        }

        private void EnfluxManagerOnShirtReceivedError(DeviceError deviceError)
        {
            UpdateUi();
            _shirtStateText.text = GetNicifiedString(deviceError);
            _shirtStateText.color = Color.red;
        }

        private void EnfluxManagerOnPantsReceivedError(DeviceError deviceError)
        {
            UpdateUi();
            _pantsStateText.text = GetNicifiedString(deviceError);
            _pantsStateText.color = Color.red;
        }

        private void ConnectShirtButtonOnClick()
        {
            _enfluxManager.Connect(DeviceType.Shirt);
        }

        private void ConnectPantsButtonOnClick()
        {
            _enfluxManager.Connect(DeviceType.Pants);
        }

        private void DisconnectButtonOnClick()
        {
            _enfluxManager.Disconnect();
        }

        private void CalibrateShirtButtonOnClick()
        {
            _enfluxManager.Calibrate(DeviceType.Shirt);
        }

        private void CalibratePantsButtonOnClick()
        {
            _enfluxManager.Calibrate(DeviceType.Pants);
        }

        private void ResetOrientationButtonOnClick()
        {
            DoResetOrientationAnimation();
        }

        private void UpdateUi()
        {
            _connectHeader.gameObject.SetActive(!_enfluxManager.IsShirtActive || !_enfluxManager.ArePantsActive);
            _connectButtonsContainer.gameObject.SetActive(_connectHeader.gameObject.activeInHierarchy);
            _connectShirtButton.gameObject.SetActive(!_enfluxManager.IsShirtActive);
            _connectPantsButton.gameObject.SetActive(!_enfluxManager.ArePantsActive);

            _disconnectHeader.gameObject.SetActive(_enfluxManager.IsAnyDeviceActive);
            _disconnectShirtButton.gameObject.SetActive(_enfluxManager.IsShirtActive && !_enfluxManager.ArePantsActive);
            _disconnectPantsButton.gameObject.SetActive(!_enfluxManager.IsShirtActive && _enfluxManager.ArePantsActive);
            _disconnectAllButton.gameObject.SetActive(_enfluxManager.IsShirtActive && _enfluxManager.ArePantsActive);

            _calibrateShirtButton.interactable = _enfluxManager.ShirtState == DeviceState.Disconnected;
            _calibratePantsButton.interactable = _enfluxManager.PantsState == DeviceState.Disconnected;
            _resetOrientationButton.interactable = _enfluxManager.ShirtState == DeviceState.Streaming ||
                _enfluxManager.PantsState == DeviceState.Streaming;
        }

        private void DoResetOrientationAnimation(bool doCountdown = true)
        {
            if (_co_resetTimer != null)
            {
                StopCoroutine(_co_resetTimer);
            }
            _co_resetTimer = Co_DoResetOrientationAnimation(doCountdown);
            StartCoroutine(_co_resetTimer);
        }

        private IEnumerator Co_DoResetOrientationAnimation(bool doCountdown)
        {
            var time = ResetOrientationTime;
            if (doCountdown)
            {
                while (time > 0.0f)
                {
                    _resetOrientationText.text = string.Format("Resetting in {0:0.0}...", time);
                    yield return null;
                    time -= Time.deltaTime;
                }
                _enfluxManager.ResetFullBodyBaseOrientation();
            }
            _resetOrientationText.text = "Reset!";

            yield return new WaitForSeconds(1.0f);
            _resetOrientationText.text = "Reset Orientation";
            _co_resetTimer = null;
        }

        private string GetNicifiedString(StateChange<DeviceState> stateChange)
        {
            switch (stateChange.Next)
            {
                case DeviceState.Disconnected:
                    if (stateChange.Previous == DeviceState.Calibrating)
                    {
                        return "Calibrated!";
                    }
                    return "Disconnected";

                case DeviceState.Initializing:
                    return "Initializing";

                case DeviceState.Connected:
                    return "Connected";

                case DeviceState.Streaming:
                    return "Streaming";

                case DeviceState.Calibrating:
                    return "Calibrating";
            }
            return "";
        }

        private string GetNicifiedString(DeviceError deviceError)
        {
            switch (deviceError)
            {
                case DeviceError.CalibrationFailed:
                    return "Please Recalibrate";

                case DeviceError.NoCalibration:
                    return "Please Calibrate";

                case DeviceError.UnknownDevice:
                    return "Unknown Device";
            }
            return "";
        }

        private void SetStatusTextColor(Text text, StateChange<DeviceState> stateChange)
        {
            if (stateChange.Previous == DeviceState.Calibrating)
            {
                text.color = Color.cyan;
                return;
            }

            switch (stateChange.Next)
            {
                case DeviceState.Streaming:
                    text.color = Color.green;
                    return;

                case DeviceState.Initializing:
                    text.color = Color.yellow;
                    return;

                case DeviceState.Calibrating:
                    text.color = Color.cyan;
                    return;

                default:
                    text.color = Color.white;
                    return;
            }
        }
    }
}