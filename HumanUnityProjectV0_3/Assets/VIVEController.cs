/** 
 * @file VIVEController.cs
 * @author Blaze Sanders SpaceVR(TM) 
 * @date 07/05/2017
 * @link https://github.com/SpaceVR-O1/Human
 * @version 0.1
 *
 * @brief Track a HTC VIVE controller, get its position and velocity, and interact with GameObjects.
 *
 * @section DESCRIPTION
 * 
 * TO-DO???
 * This script gets the device number (1 to 4) for the controller at program load.
 * Device number 0 = VIVE HMD
 */

/**
* System.Timers contains threaded timers to help with UI timing.
* 
* System.Collections contains interfaces and classes that define 
* various collections of objects, such as lists, queues, bit arrays, 
* hash tables and dictionaries.
*
* System.Runtime.InteropServices contains members to support COM interop 
* and use of external DLL's, SDK's, and API's.
* 
* UnityEngine contains Unity's C# library to control the game engine.
* 
*/
using System;
using System.Timers; 
using System.Collections;    
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class VIVEController : MonoBehaviour
{
  
  private bool LOCAL_DEBUG_STATEMENTS_ON = false; 
  private int CONTROLLER_OFFEST = 2; 
  
  private bool armsActive = false; //If false users arm movements don't cause the robot to move.

  private Valve.VR.EVRButtonId triggerButton = Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger;  //Map VIVE trigger button to device ID number
  private Valve.VR.EVRButtonId gripButton = Valve.VR.EVRButtonId.k_EButton_Grip;                //Map VIVE side grip button to device ID number

  private SteamVR_Controller.Device controller { get { return SteamVR_Controller.Input ((int)trackedControllerObj.index); } }
  private SteamVR_TrackedObject trackedControllerObj;                                                 //Used by Unity3D to tracker VIVE controllers
  
  private GameObject UI_Element;   //Used by Unity3D collider and rigid body components to allow user interaction
   
  private AdaHandController AdaHands;

 /**@brief Used for initialization of this class
  * 
  * @section DESCRIPTION
  * 
  * Start(): Is called before the first frame update only if 
  * the script instance is enabled. For objects added to the 
  * scene, the Start function will be called on all scripts before
  * Update, etc are called for any of them. 
  */
  void Start(){ 
    AdaHands = GetComponent<AdaHandController> ();                  //Left AND right hands	
    trackedControllerObj = GetComponent<SteamVR_TrackedObject> ();  //Left OR right controller	
  }
		 
 /**@brief Update() is called once per game frame. 
  * 
  * section DESCRIPTION
  * 
  * Update(): Is the main workhorse function for frame updates.
  * While FixedUpdate() and and LateUpdate() add extra features.
  */
  void Update(){

    Vector3 controllerGlobalPosition = GetGlobalPosition();
    Vector3 controllerLocalRotation = GetLocalRotation();
    
    if (Main.DEBUG_STATEMENTS_ON && LOCAL_DEBUG_STATEMENTS_ON) {
      Debug.Log("Controller #" + GetControllerNumber() + " POSITION is:");
      Debug.Log("Global X = " + controllerGlobalPosition.x + " Local X =  " + this.transform.localPosition.x);
      Debug.Log("Global Y = " + controllerGlobalPosition.y + " Local Y =  " + this.transform.localPosition.y);
      Debug.Log("Global Z = " + controllerGlobalPosition.z + " Local Z =  " + this.transform.localPosition.z);

      Debug.Log("Controller #" + GetControllerNumber() + " ROTATION is:");
      Debug.Log("Local thetaX (ROLL)  =  " + controllerLocalRotation.x);
      Debug.Log("Local thetaY (YAW)   =  " + controllerLocalRotation.y);
      Debug.Log("Local thetaZ (PITCH) =  " + controllerLocalRotation.z);
    }
  
    //CAPTURE CONTRLLER BUTTON INTERACTION
    if (controller == null) {
      if (Main.DEBUG_STATEMENTS_ON) Debug.Log ("Hand controller not found. Please turn on at least one HTC VIVE controller.");
        return; //Stops null reference expections
    }

    if (controller.GetPressDown (gripButton) && UI_Element != null) { //User held down grip buttons on top of CGI UI element
      if (armsActive == false) {			
	if (Main.DEBUG_STATEMENTS_ON) Debug.Log ("Grip buttons #" + GetControllerNumber() + " pressed, arm is unlocked.");
	UI_Element.transform.parent = this.transform;
	UI_Element.GetComponent<Rigidbody> ().isKinematic = true;  
        armsActive = true;
      }//END ARMSACTIVE IF STATEMENT
      else {
	if (Main.DEBUG_STATEMENTS_ON) Debug.Log ("Grip buttons #" + GetControllerNumber() + " pressed, arm is locked.");
        UI_Element.transform.parent = null;
        UI_Element.GetComponent<Rigidbody> ().isKinematic = false;
	armsActive = false;
      }
    }//END GETPRESSDOWN IF() STATEMENT

    if (controller.GetPress(triggerButton)) {    //User started to pull trigger
      if (Main.DEBUG_STATEMENTS_ON) Debug.Log ("Trigger #" + GetControllerNumber() + " pulled, starting to CLOSE hand.");
    }//END TRIGGERBUTTONPRESS IF() STATEMENT

    if (controller.GetPressDown(triggerButton)) {  //User held down trigger
      if (Main.DEBUG_STATEMENTS_ON) Debug.Log ("Trigger #" + GetControllerNumber() + " hand in fist.");
      //AdaHands.MoveHand(AdaHandController.FIST, GetControllerNumber());
    }//END TRIGGERBUTTONPRESSDOWN IF() STATEMENT 

    if (controller.GetPressUp(triggerButton)) { //User released trigger
      if (Main.DEBUG_STATEMENTS_ON) Debug.Log ("Trigger #" + GetControllerNumber() + " released, starting to OPEN hand.");  
//      AdaHands.MoveHand(Convert.ToInt16(AdaHandController.ALL_FINGERS_EXTENDED), GetControllerNumber());
    } 

  } //END UPDATE() FUNCTION
   
  private int GetControllerNumber(){
    return ((int)trackedControllerObj.index - CONTROLLER_OFFEST);
  }

 /**@brief OnApplicationQuit() is called when application closes.
  * 
  * section DESCRIPTION
  * 
  * OnApplicationQuit(): Is called on all game objects before the 
  * application is quit. In the editor it is called when the user 
  * stops playmode. This function is called on all game objects 
  * before the application is quit. In the editor it is called 
  * when the user stops playmode.
  */
  private void OnApplicationQuit (){
    //Clean up memory and and UI timers used  (e.g. armTimer.Close();)
	//CloseDevice();
  }

 /**@brief OnTriggerEnter() is called on collider trigger events.
  * 
  * section DESCRIPTION
  * 
  * OnTriggerEnter(): TO-DO???
  */
  private void OnTriggerEnter (Collider collider){
    if (Main.DEBUG_STATEMENTS_ON) Debug.Log ("Colllider trigger ENTER");
   UI_Element = collider.gameObject;
  }

 /**@brief OnTriggerExit() is called on collider trigger events.
  * 
  * section DESCRIPTION
  * 
  * OnTriggerEnter(): TO-DO???
  */
  private void OnTriggerExit (Collider collider){
    if (Main.DEBUG_STATEMENTS_ON) Debug.Log ("Colllider trigger EXIT");
    UI_Element = null;
  }
		
 /**@brief GetGlobalPosition() returns X, Y, Z coordinate of hand controller  
  * 
  * section DESCRIPTION
  * 
  * GetPosition(): returns X, Y, Z float coordinates to
  * ??? decimal points of hand controller in the global reference frame.
  */
  public Vector3 GetGlobalPosition() {

    Vector3 newPosition = new Vector3((float)this.transform.position.x, (float)this.transform.position.y, (float)this.transform.position.z);

    return newPosition;
  }

 /**@brief GetLocalPosition() returns X, Y, Z coordinate of hand controller  
  * 
  * section DESCRIPTION
  * 
  * GetPosition(): returns X, Y, Z float coordinates to 
  * ??? decimal points of hand controller in the Hand Mounted Display 
  * LOCAL reference frame.
  */
  public Vector3 GetLocalPosition() {

    Vector3 newPosition = new Vector3((float)this.transform.localPosition.x, (float)this.transform.localPosition.y, (float)this.transform.localPosition.z);

    return newPosition;
  }

 /**@brief GetLocalRotation() returns thetaX, thetaY, thetaZ angles of hand controller  
  * 
  * section DESCRIPTION
  * 
  * GetPosition(): returns thetaX, thetaY, thetaZ float angles in
  * degrees to ??? decimal points of hand controller in the 
  * Hand Mounted Display LOCAL reference frame.
  */
  Vector3 GetLocalRotation() {

    Vector3 newPosition = new Vector3((float)this.transform.localRotation.x, (float)this.transform.localRotation.y, (float)this.transform.localRotation.z);

    return newPosition;
  }

}//END VIVECONTROLLER CLASS
