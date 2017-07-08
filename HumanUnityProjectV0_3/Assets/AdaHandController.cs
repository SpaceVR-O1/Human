/** 
 * @file AdaHandController.cs
 * @author Blaze Sanders SpaceVR(TM) 
 * @date 07/07/2017
 * @link https://github.com/SpaceVR-O1/Human
 * @link https://www.openbionics.com/obtutorials/ada-v1-assembly
 * @version 0.4
 *
 * @brief Open and close individual fingers on the Open Bionics Ada Hand
 *
 * @section DESCRIPTION
 * 
 * TO-DO???
 * 
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdaHandController : MonoBehaviour {

  private bool LOCAL_DEBUG_STATEMENTS_ON = false;
  private int RIGHT_HAND = 1;
  private int LEFT_HAND  = 2;
  
  static public string ALL_FINGERS_EXTENDED = "31"; // = 0b0000000000011111 
  static public string FIST                 = "0";  // = 0b0000000000000000
  static public string PINKY_EXTENDED       = "1";  // = 0b0000000000000001
  static public string RING_EXTENDED        = "2";  // = 0b0000000000000010
  static public string MIDDLE_EXTENDED      = "4";  // = 0b0000000000000100
  static public string INDEX_EXTENDED       = "8";  // = 0b0000000000001000
  static public string THUMB_EXTENDED       = "16"; // = 0b0000000000010000
  static public string PEACE_SIGN           = "12"; // = 0b0000000000001100
  static public string I_LOVE_YOU           = "25"; // = 0b0000000000011001 

  static public bool rigthHandOpen = true;    //If false hand is in closed fist
  static public bool leftHandOpen = true;     //If false hand is in closed fist

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
    rigthHandOpen = true;
    leftHandOpen = true;	
  }//END START() FUNCTION
	
 /**@brief Update() is called once per game frame. 
  * 
  * @section DESCRIPTION
  * 
  * Update(): Is the main workhorse function for frame updates.
  * While FixedUpdate() and and LateUpdate() add extra features.
  */
  void Update() {
    if (Main.DEBUG_STATEMENTS_ON && LOCAL_DEBUG_STATEMENTS_ON) {	
			
    }
  }//END UPDATE() FUNCTION

 /**@brief MoveHand() High level control on entire Ada hand
  * 
  * @section DESCRIPTION
  * 
  * Finger state 
  * 
  * TO-DO???
  * 
  * Finger angle, finger spread, finger haptic feedback, ...
  */
  public void MoveHand(string fingerState, int trackedControllerIndex){

    int fingerIntState = Convert.ToInt16(fingerState); 

    if (trackedControllerIndex == RIGHT_HAND) {
	  MoveFingers(fingerIntState);
    }//END IF 
    else if (trackedControllerIndex == LEFT_HAND) {
	  MoveFingers(fingerIntState);
    }
    else {
	  if (Main.DEBUG_STATEMENTS_ON) Debug.Log("Please select LEFT_HAND or RIGHT_HAND");	//We may add more then two hands to Human long term
    }

  }//END MOVEHAND() FUNCTION

  private void MoveFingers(int fingerIntState){
	switch (fingerIntState) {
	case 0:
	  if (Main.DEBUG_STATEMENTS_ON) Debug.Log ("Closing all fingers to form fist");

	  break;
	case 1:
	  if (Main.DEBUG_STATEMENTS_ON) Debug.Log ("Extending PINKY");

	  break;
	case 2:
	  if (Main.DEBUG_STATEMENTS_ON) Debug.Log ("Extending RING fingers");

	  break;
	case 3:
	  if (Main.DEBUG_STATEMENTS_ON) Debug.Log ("Extending PINKY and RING fingers");

	  break;
	case 4:
	  if (Main.DEBUG_STATEMENTS_ON) Debug.Log ("Closing all fingers to form fist");

	  break;
	case 5:
	  if (Main.DEBUG_STATEMENTS_ON) Debug.Log ("Closing all fingers to form fist");

	  break;
	case 31:
	  if (Main.DEBUG_STATEMENTS_ON) Debug.Log ("Extending all fingers for high five :)");

	  break;
	}//END SWITCH

  }//END MOVEFINGERS() FUNCTION

}//END ADAHANDCONTROLLER CLASS
