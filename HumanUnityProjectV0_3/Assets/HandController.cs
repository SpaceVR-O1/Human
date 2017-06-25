/** 
 * @file HandController.cs
 * @author Blaze Sanders SpaceVR(TM) 
 * @date 05/18/2017
 * @link https://github.com/SpaceVR-O1/Human
 * @version 0.3
 *
 * @brief Track a HTC VIVE controller 
 *
 * @section DESCRIPTION
 * 
 * TO-DO???
 * This script gets the device number (1 to 4) for the controller at program load.
 * Device number 0 = VIVE HMD
 */

/**
 * System.Collections contains interfaces and classes that define 
 * various collections of objects, such as lists, queues, bit arrays, 
 * hash tables and dictionaries.
 * 
 * UnityEngine contains Unity's C# library to control the game engine.
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class HandController : MonoBehaviour
{
  [DllImport("ARM_base", EntryPoint = "TestFunction")]
  public static extern int TestFunction();
  [DllImport("ARM_base", EntryPoint = "InitRobot")]
  public static extern int InitRobot();
  [DllImport("ARM_base", EntryPoint = "MoveHand")]
  public static extern int MoveHand(float x, float y, float z, float thetaX, float thetaY, float thetaZ);
  [DllImport("ARM_base", EntryPoint = "CloseDevice")]
  public static extern int CloseDevice();

  private bool handOpen = true;    //If true hand is in closed fist
  private bool armsActive = false; //If false users arm movements don't cause robot to move

  Vector3[] position = new Vector3[2];

  private double[,] velocity = new double[,]{
    {0.0, 0.0, 0.0},
    {0.0, 0.0, 0.0},
  };

  private double[,] acceleration = new double[,]{
    {0.0, 0.0, 0.0},
    {0.0, 0.0, 0.0},
  };

  private Valve.VR.EVRButtonId triggerButton = Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger; //Map VIVE trigger button to ID
  private bool triggerButtonDown = false;    //True when trigger button starts being pressed
  private bool triggerButtonUp = false;      //True when trigger button starts being released  
  private bool triggerButtonPressed = false; //Ture when trigger button is being held down

  private Valve.VR.EVRButtonId gripButton = Valve.VR.EVRButtonId.k_EButton_Grip;  //Map VIVE side grip button to ID
  private bool gripButtonDown = false;    //True when side grip buttons starts being pressed
  private bool gripButtonUp = false;      //True when side grip buttons button starts being released  
  private bool gripButtonPressed = false; //Ture when side grip buttons button is being held down


  private SteamVR_Controller.Device controller { get { return SteamVR_Controller.Input((int)trackedHandObj.index); } }
  private SteamVR_TrackedObject trackedHandObj;

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
    trackedHandObj = GetComponent<SteamVR_TrackedObject>();

    int ret = TestFunction();
    if (ret == 22) {
      Debug.Log("ARM_base dll working");
    }
    else {
      Debug.Log("ARM_base dll not working");
    }

    ret = InitRobot();
    switch (ret) {
      case 0:
        Debug.Log("Robot loaded and device found");
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
      default:
        Debug.LogWarning("Robot - unknown return from initialization");
        break;
    }
  }//END START() FUNCTION

  /**@brief Update is called once per game frame. 
   * 
   * section DESCRIPTION
   * 
   * Update(): Is the main workhorse function for frame updates.
   * While FixedUpdate() and and LateUpdate() add extra features.
   */
  void Update() {

    if (controller == null) {
      if(Main.DEBUG_STATEMENTS_ON) Debug.Log("Hand controller not found. Please turn on at least one HTC VIVE controller.");
      return; //Stops null reference expections
    }

    if (controller.GetPress(gripButton)){
      if (Main.DEBUG_STATEMENTS_ON) Debug.Log("Grip buttons " + (int)trackedHandObj.index + " pressed, arm is unlocked.");
      armsActive = true;
    }
    else {
      if (Main.DEBUG_STATEMENTS_ON) Debug.Log("Grip buttons " + (int)trackedHandObj.index + " NOT pressed, arm is locked.");
      armsActive = false;
    }//END GRIPBUTTONPRESS IF() STATEMENT

    if (controller.GetPressDown(triggerButton)){
      if (handOpen) {
        if (Main.DEBUG_STATEMENTS_ON) Debug.Log("Trigger " + (int)trackedHandObj.index + " pulled, closing hand.");
        handOpen = false;
      }
      else {
        if (Main.DEBUG_STATEMENTS_ON) Debug.Log("Trigger " + (int)trackedHandObj.index + " pulled, opening hand.");
        handOpen = true;
      }
    }//END TRIGGERBUTTONDOWN IF() STATEMENT
  }//END UPDATE() FUNCTION

  Vector3[] Position() {

    Vector3[] tempPosition = new Vector3[2];
    double LeftHandX, LeftHandY, LeftHandZ;
    double RigthHandX, RigthHandY, RigthHandZ;

    

    //tempPosition[0] = {LeftHandX, LeftHandY, LeftHandZ};
    //tempPosition[1] = {RigthHandX, RigthHandY, RigthHandZ};
    
    return tempPosition;
  }

  double[,] Velocity() {

    double[,] tempVelocity = new double[,]{
      {4.20, 4.20, 4.20},
      {4.20, 4.20, 4.20}
    };
    return tempVelocity;
  }


  double[,] Acceleration(){

    double[,] tempAcceleration = new double[,]{
      {9.81, 9.81, 9.81},
      {9.81, 9.81, 9.81}
    };
    return tempAcceleration;
  }
}

