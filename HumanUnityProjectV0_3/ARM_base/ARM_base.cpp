
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
int(*MyGetAngularCommand)(AngularPosition &);

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
		MyInitFingers = (int(*)()) GetProcAddress(commandLayer_handle, "InitFingers");

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
		KinovaDevice list[MAX_KINOVA_DEVICE];

		int devicesCount = MyGetDevices(list, result);
		if (devicesCount == 1)
		{
			// succesfull
			MySetActiveDevice(list[0]);
			return 0;
		}
		if (devicesCount > 1)
		{
			return -3;
		}

		// not succesfull - no device found
		return -2;
	}

	// send robot to new point
	int MoveHand(float x, float y, float z, float thetaX, float thetaY, float thetaZ)
	{
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

	int MoveHandNoThetaY(float x, float y, float z, float thetaX, float thetaZ)
	{
	}

	// Close device & free the library
	int CloseDevice()
	{
		(*MyCloseAPI)();
		FreeLibrary(commandLayer_handle);

		return 0;
	}
}
