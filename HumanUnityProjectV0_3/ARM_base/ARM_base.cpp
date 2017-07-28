
#include "ARM_base.h"
#include <Windows.h>
#include "Lib_Examples\CommunicationLayerWindows.h"
#include "Lib_Examples\CommandLayer.h"
#include <conio.h>
#include "Lib_Examples\KinovaTypes.h"
#include <iostream>


using namespace std;

//A handle to the API.
HINSTANCE commandLayer_handle;

//Function pointers to the functions we need
int(*MyInitAPI)();
int(*MyCloseAPI)();
int(*MySendBasicTrajectory)(TrajectoryPoint command);
int(*MyGetDevices)(KinovaDevice devices[MAX_KINOVA_DEVICE], int &result);
int(*MySetActiveDevice)(KinovaDevice device);
int(*MyMoveHome)();
int(*MyInitFingers)();
int(*MyEraseAllTrajectories)();
int(*MyGetAngularCommand)(AngularPosition &);
int(*MyGetCartesianCommand)(CartesianPosition &);

KinovaDevice list[MAX_KINOVA_DEVICE];
char* leftArm = "PJ00650019161750001";
char* rightArm = "PJ00900006020921-0 ";
int leftArmIndex = -1;
int rightArmIndex = -1;

extern "C"
{
	// test function just to figure out if we can access dll & it works
	int TestFunction()
	{
		return 22;
	}

	// load library, intitalize robot and get device
	// returns:
	// 0 - success
	// -1 - not able to load KINOVA APIs
	// -2 - no device found
	// -3 - more devices found
	int InitRobot()
	{
		//We load the API.
		commandLayer_handle = LoadLibrary(L"CommandLayerWindows.dll");

		if (commandLayer_handle == NULL)
		{
			return -123;
		}

		int programResult = 0;

		//Initialise the function pointer from the API
		MyInitAPI = (int(*)()) GetProcAddress(commandLayer_handle, "InitAPI");
		MyCloseAPI = (int(*)()) GetProcAddress(commandLayer_handle, "CloseAPI");
		MyGetDevices = (int(*)(KinovaDevice[MAX_KINOVA_DEVICE], int&)) GetProcAddress(commandLayer_handle, "GetDevices");
		MySetActiveDevice = (int(*)(KinovaDevice)) GetProcAddress(commandLayer_handle, "SetActiveDevice");
		MySendBasicTrajectory = (int(*)(TrajectoryPoint)) GetProcAddress(commandLayer_handle, "SendBasicTrajectory");
		MyGetAngularCommand = (int(*)(AngularPosition &)) GetProcAddress(commandLayer_handle, "GetAngularCommand");
		MyMoveHome = (int(*)()) GetProcAddress(commandLayer_handle, "MoveHome");
		MyEraseAllTrajectories = (int(*)()) GetProcAddress(commandLayer_handle, "EraseAllTrajectories");
		MyInitFingers = (int(*)()) GetProcAddress(commandLayer_handle, "InitFingers");
		MyGetCartesianCommand = (int(*)(CartesianPosition &)) GetProcAddress(commandLayer_handle, "GetCartesianCommand");
		
		//Verify that all functions has been loaded correctly
		if (MyInitAPI == NULL)
		{
			return -10;
		}
		else if (MyCloseAPI == NULL)
		{
			return -11;
		}
		else if (MySendBasicTrajectory == NULL)
		{
			return -12;
		}
		else if (MyGetDevices == NULL)
		{
			return -13;
		}
		else if (MySetActiveDevice == NULL)
		{
			return -14;
		}
		else if (MyGetAngularCommand == NULL)
		{
			return -15;
		}
		else if (MyMoveHome == NULL)
		{
			return -16;
		}
		else if (MyInitFingers == NULL)
		{
			return -17;
		}

		int result = (*MyInitAPI)();

		int devicesCount = MyGetDevices(list, result);
		for (int i = 0; i < devicesCount; i++)
		{
			if (strcmp(leftArm, list[i].SerialNumber) == 0) {
				leftArmIndex = i;
			}
			else if (strcmp(rightArm, list[i].SerialNumber) == 0) {
				rightArmIndex = i;
			}
		}

		if (devicesCount >= 1)
		{
			return 0;
		}

		// not succesfull - no device found
		return -2;
	}

	void EnableDesiredArm(bool rightArm)
	{
		if (rightArm) {
			MySetActiveDevice(list[rightArmIndex]);
		}
		else {
			MySetActiveDevice(list[leftArmIndex]);
		}
	}

	// send robot to new point
	int MoveHand(bool rightArm, float x, float y, float z, float thetaX, float thetaY, float thetaZ)
	{
		EnableDesiredArm(rightArm);
		TrajectoryPoint pointToSend;
		pointToSend.InitStruct();
		pointToSend.Position.Type = CARTESIAN_POSITION;
		pointToSend.Position.CartesianPosition.X = x;
		pointToSend.Position.CartesianPosition.Y = y;
		pointToSend.Position.CartesianPosition.Z = z;
		pointToSend.Position.CartesianPosition.ThetaX = thetaX;
		pointToSend.Position.CartesianPosition.ThetaY = thetaY;
		pointToSend.Position.CartesianPosition.ThetaY = thetaZ;

		MySendBasicTrajectory(pointToSend);

		return 0;
	}

	int MoveArmHome(bool rightArm)
	{
		EnableDesiredArm(rightArm);
		MyMoveHome();
		return 0;
	}

	int MoveHandNoThetaY(bool rightArm, float x, float y, float z, float thetaX, float thetaZ)
	{
		EnableDesiredArm(rightArm);
		CartesianPosition currentCommand;
		//get the actual angular command of the robot.
		MyGetCartesianCommand(currentCommand);

		MoveHand(rightArm, x, y, z, thetaX, currentCommand.Coordinates.ThetaY, thetaZ);

		return 0;
	}

	/**
	* @param pinky is extended if TRUE and close otherwise
	* @param ring is extended if TRUE and close otherwise
	* @param middle is extended if TRUE and close otherwise
	* @param index is extended if TRUE and close otherwise
	* @param thumb is extended if TRUE and close otherwise
	*/
	int MoveFingers(bool rightArm, bool pinky, bool ring, bool middle, bool index, bool thumb) {
		EnableDesiredArm(rightArm);
		CartesianPosition currentCommand;
		//get the actual angular command of the robot.
		MyGetCartesianCommand(currentCommand);

		TrajectoryPoint pointToSend;
		pointToSend.InitStruct(); // initializes all values to 0.0
		pointToSend.Position.CartesianPosition.X = currentCommand.Coordinates.X;
		pointToSend.Position.CartesianPosition.Y = currentCommand.Coordinates.Y;
		pointToSend.Position.CartesianPosition.Z = currentCommand.Coordinates.Z;
		pointToSend.Position.CartesianPosition.ThetaX = currentCommand.Coordinates.ThetaX;
		pointToSend.Position.CartesianPosition.ThetaY = currentCommand.Coordinates.ThetaY;
		pointToSend.Position.CartesianPosition.ThetaZ = currentCommand.Coordinates.ThetaZ;

		float fingerValue = 0.0f;

		if (pinky && ring && middle && index && thumb) {
			//OPEN HAND CF_OpenHandOneFingers = 31, CF_OpenHandTwoFingers = 33,
			//0.0 to 10.0 are the possible finger opening steps See KinovaTypes.h line 560 (struct FingersPosition)
			fingerValue = 10.0f;
		}

		pointToSend.Position.Fingers.Finger1 = fingerValue;
		pointToSend.Position.Fingers.Finger2 = fingerValue;
		pointToSend.Position.Fingers.Finger3 = fingerValue;

		MySendBasicTrajectory(pointToSend);

		return fingerValue;

	}//END MOVEFINGER FUNCTION

	int StopArm(bool rightArm)
	{
		EnableDesiredArm(rightArm);
		MyEraseAllTrajectories();
		return 0;
	}

	// Close device & free the library
	int CloseDevice(bool rightArm)
	{
		EnableDesiredArm(rightArm);
		(*MyCloseAPI)();
		FreeLibrary(commandLayer_handle);

		return 0;
	}
}
