/** 
 * @file HandController.cs
 * @author Blaze Sanders SpaceVR(TM) 
 * @date 05/18/2017
 * @link https://github.com/SpaceVR-O1/Human
 * @version 0.3
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



public class HandController : MonoBehaviour
{
  static public bool LOCAL_DEBUG_STATEMENTS_ON = false;     
  static public string OPEN_HAND       = "00000000";
  static public string FIST            = "00011111";
  static public string PINKY_EXTENDED  = "00000001";
  static public string RING_EXTENDED   = "00000010";
  static public string MIDDLE_EXTENDED = "00000100";
  static public string INDEX_EXTENDED  = "00001000";
  static public string THUMB_EXTENDED  = "00010000";

  public float NormalizationFactor = 1.0f;

  //https://stackoverflow.com/questions/7276389/confused-over-dll-entry-points-entry-point-not-found-exception
  [DllImport("ARM_base_32", EntryPoint = "TestFunction")]
  public static extern int TestFunction();
  [DllImport("ARM_base_32", EntryPoint = "InitRobot")]
  public static extern int InitRobot();
  [DllImport("ARM_base_32", EntryPoint = "MoveHand")]
  public static extern int MoveHand(float x, float y, float z, float thetaX, float thetaY, float thetaZ);
  //public static extern int MoveArm();
  //public static extern int MoveArm(float x, float y, float z, float thetaX, float thetaY, float thetaZ);
  //[DllImport("ARM_base.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
  //public static extern int MoveHand(Int16 gloveState);
  [DllImport("ARM_base_32", EntryPoint = "CloseDevice")]
  public static extern int CloseDevice();

  private bool initSuccessful = false;
  private bool handOpen = true;       //If false hand is in closed fist
  private bool armsActive = false;    //If false users arm movements don't cause the robot to move.
  static public int TEST_PASSED = 22; //Constant used inside the Kinova ARM_base.cpp file

  private Valve.VR.EVRButtonId triggerButton = Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger; //Map VIVE trigger button to ID
  private bool triggerButtonDown = false;    //True when trigger button starts being pressed
  private bool triggerButtonUp = false;      //True when trigger button starts being released  
  private bool triggerButtonPressed = false; //True when trigger button is being held down

  private Valve.VR.EVRButtonId gripButton = Valve.VR.EVRButtonId.k_EButton_Grip;  //Map VIVE side grip button to ID
  private bool gripButtonDown = false;    //True when side grip buttons starts being pressed
  private bool gripButtonUp = false;      //True when side grip buttons button starts being released  
  private bool gripButtonPressed = false; //True when side grip buttons button is being held down

  private SteamVR_Controller.Device controller { get { return SteamVR_Controller.Input((int)trackedHandObj.index); } }
  private SteamVR_TrackedObject trackedHandObj;

  private GameObject pickup;             //Used by Unity3D collider and rigid body components to allow user interaction 

  /**@brief Used for initialization of this class
   * 
   * @section DESCRIPTION
   * 
   * Start(): Is called before the first frame update only if 
   * the script instance is enabled. For objects added to the 
   * scene, the Start function will be called on all scripts before
   * Update, etc are called for any of them. 
   */
  void Start() {

	trackedHandObj = GetComponent<SteamVR_TrackedObject>();  //Left or right controller


	Debug.Log("START");
	if (TestFunction() == TEST_PASSED) {
	  Debug.Log("Kinova robotic arm DLL import is working");
	}
	else {
	  Debug.Log("Kinova robotic arm DLL import is not working");
	}

	int errorCode = InitRobot();
	switch (errorCode) {
	case 0:
	  Debug.Log ("Kinova robotic arm loaded and device found");
	  initSuccessful = true;
	  break;
	case -1:
	  Debug.LogWarning("Robot APIs troubles");
	  break;
	case -2:
	  Debug.LogWarning("Robot - no device found");
	  break;
	case -3:
	  Debug.LogWarning("Robot - more devices found - not sure which to use");
	  break;
	case -10:
	  Debug.LogWarning("Robot APIs troubles: InitAPI");
	  break;
	case -11:
	  Debug.LogWarning("Robot APIs troubles: CloseAPI");
	  break;
	case -12:
	  Debug.LogWarning("Robot APIs troubles: SendBasicTrajectory");
	  break;
	case -13:
	  Debug.LogWarning("Robot APIs troubles: GetDevices");
	  break;
	case -14:
	  Debug.LogWarning("Robot APIs troubles: SetActiveDevice");
	  break;
	case -15:
	  Debug.LogWarning("Robot APIs troubles: GetAngularCommand");
	  break;
	case -16:
	  Debug.LogWarning("Robot APIs troubles: MoveHome");
	  break;
	case -17:
	  Debug.LogWarning("Robot APIs troubles: InitFingers");
	  break;
	case -123:
	  Debug.LogWarning ("Robot APIs troubles: Command Layer Handle");
	  break;
	default:
	  Debug.LogWarning("Robot - unknown error from initialization");
	  break;
	}
  }//END START() FUNCTION

  /**@brief Update() is called once per game frame. 
   * 
   * section DESCRIPTION
   * 
   * Update(): Is the main workhorse function for frame updates.
   * While FixedUpdate() and and LateUpdate() add extra features.
   */
  void Update() {

	Vector3 controllerPosition = GetGlobalPosition();
	Vector3 controllerRotation = GetLocalRotation();
	float temp = 5.0f;

	//DoMoveArm(temp, temp, temp, temp, temp, temp);

	if (controller.GetPress(triggerButton)) {
	  float x = NormalizeValue (controllerPosition.x);
	  float y = NormalizeValue (controllerPosition.y);
	  float z = NormalizeValue (controllerPosition.z);
	  float thetaX = NormalizeValue (controllerRotation.x);
	  float thetaY = NormalizeValue (controllerRotation.y);
	  float thetaZ = NormalizeValue (controllerRotation.z);
	  DoMoveArm(x, y, z, thetaX, thetaY, thetaZ);
	}

	if (controller.GetPressDown (gripButton)) {
	  DoMoveArm(.2f, -.4f, .5f, 1.5f, .8f, .04f);
	}

	if (Main.DEBUG_STATEMENTS_ON && LOCAL_DEBUG_STATEMENTS_ON) {
	  Debug.Log("Controller #" + (int)trackedHandObj.index + " POSITION is:");
	  Debug.Log("Global X = " + controllerPosition.x + " Local X =  " + this.transform.localPosition.x);
	  Debug.Log("Global Y = " + controllerPosition.y + " Local Y =  " + this.transform.localPosition.y);
	  Debug.Log("Global Z = " + controllerPosition.z + " Local Z =  " + this.transform.localPosition.z);

	  Debug.Log("Controller #" + (int)trackedHandObj.index + " ROTATION is:");
	  Debug.Log("Local thetaX =  " + this.transform.localPosition.x);
	  Debug.Log("Local thetaY =  " + this.transform.localPosition.y);
	  Debug.Log("Local thetaZ =  " + this.transform.localPosition.z);
	}



	//CAPTURE CONTRLLER BUTTON INTERACTION
	if (controller == null) {
	  if(Main.DEBUG_STATEMENTS_ON) Debug.Log("Hand controller not found. Please turn on at least one HTC VIVE controller.");
	  return; //Stops null reference expections
	}

	if (controller.GetPressDown (gripButton) && pickup != null) {
	  if (armsActive == false) {			
		if (Main.DEBUG_STATEMENTS_ON)
		  Debug.Log ("Grip buttons " + ((int)trackedHandObj.index - 1) + " pressed, arm is unlocked.");
		pickup.transform.parent = this.transform;
		pickup.GetComponent<Rigidbody> ().isKinematic = true;  
		armsActive = true;
	  }//END ARMSACTIVE IF STATEMENT
	  else {
		if (Main.DEBUG_STATEMENTS_ON)
		  Debug.Log ("Grip buttons " + ((int)trackedHandObj.index - 1) + " pressed, arm is locked.");
		pickup.transform.parent = null;
		pickup.GetComponent<Rigidbody> ().isKinematic = false;
		armsActive = false;
	  }
	}//END GETPRESSDWN IF() STATEMENT
	  

	if (controller.GetPress(triggerButton)){ //User started to pull trigger
	  if (Main.DEBUG_STATEMENTS_ON) Debug.Log("Trigger " + (int)trackedHandObj.index + " pulled, starting to CLOSE hand.");
	     
		
	}//END TRIGGERBUTTONDOWN IF() STATEMENT

	if (controller.GetPressDown(triggerButton)){ //User held down trigger
	  if (Main.DEBUG_STATEMENTS_ON) Debug.Log("Trigger " + (int)trackedHandObj.index + " hand in fist.");
	  handOpen = false;
	  //MoveHand(Convert.ToInt16(FIST, 2));
	} 

	if (controller.GetPressUp(triggerButton)) { //User released trigger
	  if (Main.DEBUG_STATEMENTS_ON) Debug.Log("Trigger " + (int)trackedHandObj.index + " released, starting to OPEN hand.");
	  handOpen = true;
	  //MoveHand(Convert.ToInt16(OPEN_HAND, 2));
	} 


  }//END UPDATE() FUNCTION

  void DoMoveArm(float x, float y, float z, float thetaX, float thetaY, float thetaZ)
  {
	try {
	  if (initSuccessful) {
		Debug.Log("Moving robot arm to (" + x + ", " + y + ", " + z + ", " + thetaX + ", " + thetaY + ", " + thetaZ + ")");
		MoveHand(x, y, z, thetaX, thetaY, thetaZ);
	  }
	} catch(EntryPointNotFoundException e) {
	  Debug.Log(e.Data);
	  Debug.Log(e.GetType());
	  Debug.Log(e.GetBaseException());

	}
  }

  float NormalizeValue(float value)
  {
	return value * NormalizationFactor;
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
  private void OnApplicationQuit() {
	//Clean up memory and and UI timers used  (e.g. armTimer.Close();)
	CloseDevice();
  }

  /**@brief OnTriggerEnter() is called on collider trigger events.
   * 
   * section DESCRIPTION
   * 
   * OnTriggerEnter(): TO-DO???
   */
  private void OnTriggerEnter(Collider collider) {
	if (Main.DEBUG_STATEMENTS_ON) Debug.Log("Colllider trigger ENTER");
	pickup = collider.gameObject;
  }

  /**@brief OnTriggerExit() is called on collider trigger events.
   * 
   * section DESCRIPTION
   * 
   * OnTriggerEnter(): TO-DO???
   */
  private void OnTriggerExit(Collider collider) {
	if (Main.DEBUG_STATEMENTS_ON) Debug.Log("Colllider trigger EXIT");
	pickup = null;
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

  /**@brief GetGlobalVelocity() returns X, Y, Y velocity vector of hand controller  
  * 
  * section DESCRIPTION
  * 
  * GetGlobalVelocity(): returns X, Y, Y velocity vector of hand 
  * controller in Unity3D units per second to ??? decimal points 
  * by calculating change in position of hand controller between 
  * two game engine frames renders / calls to Update().
  */
  Vector3 GetGlobalVelocity(Vector3 previousPosition) {

	float frameRate = (1 / Time.deltaTime);

	float newXvelocity = (previousPosition.x - this.transform.position.x) / frameRate;  
	float newYvelocity = (previousPosition.y - this.transform.position.y) / frameRate;
	float newZvelocity = (previousPosition.z - this.transform.position.z) / frameRate;

	Vector3 newVelocity = new Vector3(newXvelocity, newYvelocity, newZvelocity);

	return newVelocity;
  }

  /**@brief GetGlobalAcceleration() returns X, Y, Y acceleration vector of hand controller  
  * 
  * section DESCRIPTION
  * 
  * GetGlobalAcceleration(): TO-DO???
  */
  Vector3 GetGlobalAcceleration(Vector3 previousVelocity) {

	Vector3 acceleration = new Vector3(0.00f, -9.81f, 0.00f); //WRONG!!!

	return acceleration;
  }


}//END HANDCONTROLLER CLASS

