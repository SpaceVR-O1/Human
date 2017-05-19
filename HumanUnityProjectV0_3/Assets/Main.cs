/** 
 * @file Main.cs
 * @author Blaze Sanders SpaceVR(TM) 
 * @date 03/28/2017
 * @link https://github.com/SpaceVR-O1/Human
 * @version 0.1
 *
 * @brief The highest level Class / program driver for Human.
 *
 * @section DESCRIPTION
 * 
 * Program to configure and control the SpaceVR Human VR robot. 
 * All global variables should be stored here. All calls to the
 * OptiTrack, MYO, and/or VIVE Controllers should be performed here.
 * 
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

/** The highest level Class / program driver for Human.
 */
public class Main : MonoBehaviour
{
  static public bool DEBUG_STATEMENTS_ON = true;
  static public int RIGHT_HAND = 1;
  static public int LEFT_HAND = 2;

  // Draw some basic instructions.
  void OnGUI() {
    GUI.skin.label.fontSize = 20;
  }

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

  }//END START() FUNCTION

  /**@brief Update is called once per game frame. 
   * 
   * section DESCRIPTION
   * 
   * Update(): Is the main workhorse function for frame updates.
   * While FixedUpdate() and and LateUpdate() add extra features.
   */
  void Update() {


  }//END UPDATE() FUNCTION

  /*
  void UpdateRobotArmPosition() {
      print("Time since last call to Update() = " + Time.deltaTime + " = " + (1 / Time.deltaTime) + " Hz!");
      print("Acceleration = " + accelerometer + " G's (Rest = [0, 1, 0]) +Y = UP, +Z = FORWARD +X = TO USER RIGHT (Left handed system)");
      print("Gyroscope = " + gyroscope + " degress/sec");
      print("Local Rotation Transformation = " + localRotation);
      print("Local Position Transformation = " + transform.localPosition);
      print("World Position Transformation = " + transform.position);

      //TO-DO: Call IGUS API here

  }//END UpdateRobotArmPosition
 */



}//END MAIN CLASS

