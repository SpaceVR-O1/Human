/** 
 * @file CameraController.cs
 * @author codetricity (See link #1 below)
 * @author Sarah Stumbo (See link #2 below)
 * @author Blaze Sanders SpaceVR(TM) 
 * @date 07/05/2017
 * @link http://lists.theta360.guide/t/getting-unity-to-recognize-theta-uvc-fullhd-blender-camera/1035/4
 * @link https://github.com/sarahstumbo/360Video_Unity
 * @link https://github.com/SpaceVR-O1/Human
 * @version 0.1
 *
 * @brief Renders texture from a webcam onto a game object. 
 *
 * @section DESCRIPTION
 * 
 * TO-DO???
 * Know to work with the Ricoh Theta S camera and its 64 bit UVC FullHD Blender on the inside of an Unity3D sphere with TextureInsideUnlitColor material
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{

	private string cameraDeviceName = "RICOH THETA S";

 /**@brief Used for initialization of this class
  * 
  * @section DESCRIPTION
  * 
  * Start(): Is called before the first frame update only if 
  * the script instance is enabled. For objects added to the 
  * scene, the Start function will be called on all scripts before
  * Update, etc are called for any of them. 
  */
  void Start ()
  {
//	StartLocalStream ();
  }
	
 /**@brief Update() is called once per game frame. 
  * 
  * section DESCRIPTION
  * 
  * Update(): Is the main workhorse function for frame updates.
  * While FixedUpdate() and and LateUpdate() add extra features.
  */
  void Update ()
  {
  }

  public void StartLocalStream ()
  {
	WebCamDevice[] devices = WebCamTexture.devices;
	Debug.Log ("Number of web cams connected is: " + devices.Length);

	int cameraIndex = -1;
	for (int i = 0; i < devices.Length; i++) {
	  Debug.Log (i + " " + devices [i].name);
	  if (devices [i].name.Equals (cameraDeviceName)) {
		cameraIndex = i;
	  }
    }

    Renderer rend = this.GetComponentInChildren<Renderer> (); 

    WebCamTexture myCam = new WebCamTexture ();          
    string camName = devices [cameraIndex].name;                                //Use list of cameras generated above to select camera you would like to render in Unity3D
    Debug.Log ("The webcam texture rendering is from: " + camName);
    myCam.deviceName = camName;
    rend.material.mainTexture = myCam;				     //Render texture onto game object
    myCam.Play (); 						     //Start camera live stream
  }
}
