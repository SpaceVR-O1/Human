using System;
using UnityEngine;

public class ModeTester : MonoBehaviour
{
	public GameObject OVRRig;
	public GameObject MouseCamera;

	private void Awake()
	{
		MouseCamera.SetActive(!OVRManager.isHmdPresent);
		OVRRig.SetActive(OVRManager.isHmdPresent);
	}
}

