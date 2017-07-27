
#define DllExport __declspec(dllexport)
// https://docs.microsoft.com/en-us/cpp/build/exporting-from-a-dll-using-declspec-dllexport

extern "C"
{
  DllExport int TestFunction();
  DllExport int InitRobot();
  DllExport int MoveArmHome(bool rightArm);
  DllExport int MoveHand(bool rightArm, float x, float y, float z, float thetaX, float thetaY, float thetaZ);
  DllExport int MoveHandNoThetaY(bool rightArm, float x, float y, float z, float thetaX, float thetaZ);
  DllExport int StopArm(bool rightArm);
  DllExport int CloseDevice(bool rightArm);
}
