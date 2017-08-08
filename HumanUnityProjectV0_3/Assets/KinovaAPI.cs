using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class KinovaAPI : MonoBehaviour
{

  // TODO: Give external functions prefix to easily identify them as such (e.g., extern_InitRobot)
  //https://stackoverflow.com/questions/7276389/confused-over-dll-entry-points-entry-point-not-found-exception
  [DllImport ("ARM_base_32", EntryPoint = "InitRobot")]
  private static extern int _InitRobot ();

  [DllImport ("ARM_base_32", EntryPoint = "MoveArmHome")]
  private static extern int _MoveArmHome (bool rightArm);

  [DllImport ("ARM_base_32", EntryPoint = "MoveHand")]
  private static extern int _MoveHand (bool rightArm, float x, float y, float z, float thetaX, float thetaY, float thetaZ);

  [DllImport ("ARM_base_32", EntryPoint = "MoveHandNoThetaY")]
  private static extern int _MoveHandNoThetaY (bool rightArm, float x, float y, float z, float thetaX, float thetaZ);

  [DllImport ("ARM_base_32", EntryPoint = "MoveFingers")]
  private static extern int _MoveFingers (bool rightArm, bool pinky, bool ring, bool middle, bool index, bool thumb);

  [DllImport ("ARM_base_32", EntryPoint = "CloseDevice")]
  private static extern int _CloseDevice (bool rightArm);

  [DllImport ("ARM_base_32", EntryPoint = "StopArm")]
  private static extern int _StopArm (bool rightArm);

  private static bool initSuccessful = false;

  public class Position
  {
	public float X { get; }

	public float Y { get; }

	public float Z { get; }

	public float ThetaX { get; }

	public float ThetaY { get; }

	public float ThetaZ { get; }

	public Position (float x, float y, float z, float thetaX, float thetaY, float thetaZ)
	{
	  // meters
	  X = x;
	  Y = y;
	  Z = z;

	  // radians
	  ThetaX = thetaX;
	  ThetaY = thetaY;
	  ThetaZ = thetaZ; // wrist rotation
	}
  }

  // Only path that is non-blocking at the moment:
  // RaiseTheRoof <--> Home Position <--> Scooping
  // Note that all these positions are for the left arm; for right
  // arm positions, negate x and compliment thetaX.

  // HOME (Cartesian Position for Joystick Home)
  // note: since Joystick home positions the arm by actuator, this
  // home position will not exactly match Joystick home

  public static Position HomePosition =
	new Position (0.29f, -0.26f, 0.29f, 1.5924f, -1.1792f, 0f);

  // Arm raised up
  public static Position RaiseTheRoof =
	new Position (0.0f, -0.60f, 0.33f, 1.5665f, -0.4711f, 0f);

  // Arm ready to scoop ice cream
  public static Position Scooping =
	new Position (-0.15f, 0.41f, 0.57f, -1.6554f, -0.6633f, 0f);

  // Arm stretched out from the shoulder
  public static Position StretchOut =
	new Position (-0.11f, -0.25f, 0.75f, 1.5956f, 0.0318f, 0f);

  // Arm hanging to the side
  public static Position RestingPosition =
	new Position (0.04f, 0.67f, 0.29f, -1.57f, -0.32f, 0f);

  // Arm flexing biceps
  public static Position FlexBiceps =
	new Position (-0.08f, -0.46f, 0.22f, 1.37f, -0.26f, 0f);

  public static void InitRobot ()
  {
	if (initSuccessful) {
	  Debug.Log ("Already initialized");
	  return;
	}
	int errorCode = _InitRobot ();
	switch (errorCode) {
	case 0:
	  Debug.Log ("Kinova robotic arm loaded and device found");
	  initSuccessful = true;
	  break;
	case -1:
	  Debug.LogError ("Robot APIs troubles");
	  break;
	case -2:
	  Debug.LogError ("Robot - no device found");
	  break;
	case -3:
	  Debug.LogError ("Robot - more devices found - not sure which to use");
	  break;
	case -10:
	  Debug.LogError ("Robot APIs troubles: InitAPI");
	  break;
	case -11:
	  Debug.LogError ("Robot APIs troubles: CloseAPI");
	  break;
	case -12:
	  Debug.LogError ("Robot APIs troubles: SendBasicTrajectory");
	  break;
	case -13:
	  Debug.LogError ("Robot APIs troubles: GetDevices");
	  break;
	case -14:
	  Debug.LogError ("Robot APIs troubles: SetActiveDevice");
	  break;
	case -15:
	  Debug.LogError ("Robot APIs troubles: GetAngularCommand");
	  break;
	case -16:
	  Debug.LogError ("Robot APIs troubles: MoveHome");
	  break;
	case -17:
	  Debug.LogError ("Robot APIs troubles: InitFingers");
	  break;
	case -18:
	  Debug.LogError ("Robot APIs troubles: StartForceControl");
	  break;
	case -123:
	  Debug.LogError ("Robot APIs troubles: Command Layer Handle");
	  break;
	default:
	  Debug.LogError ("Robot - unknown error from initialization");
	  break;
	}
  }

  public static void StopArm (bool rightArm)
  {
	if (initSuccessful) {
      _StopArm (rightArm);
	}
  }

  public static void MoveArmHome (bool rightArm)
  {
	if (initSuccessful) {
	  _MoveArmHome (rightArm);
	}
  }

  public static void MoveHand (bool rightArm, float x, float y, float z, float thetaX, float thetaY, float thetaZ)
  {
	if (initSuccessful) {
	  _MoveHand (rightArm, x, y, z, thetaX, thetaY, thetaZ);
	}
  }

  public static void MoveHandNoThetaY (bool rightArm, float x, float y, float z, float thetaX, float thetaZ)
  {
	if (initSuccessful) {
	  _MoveHandNoThetaY (rightArm, x, y, z, thetaX, thetaZ);
	}
  }

  public static void MoveFingers (bool rightArm, bool pinky, bool ring, bool middle, bool index, bool thumb)
  {
	if (initSuccessful) {
	  _MoveFingers (rightArm, pinky, ring, middle, index, thumb);
	}
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
  private void OnApplicationQuit ()
  {
	if (initSuccessful) {
	  Debug.Log("Closing Robot API...");
	  _CloseDevice (false);
	}
  }
}
