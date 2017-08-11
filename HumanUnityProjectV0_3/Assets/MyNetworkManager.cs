using UnityEngine;
using UnityEngine.Networking;

public class MyMsgTypes
{
	public static short MSG_MOVE_ARM = 1000;
	public static short MSG_MOVE_ARM_NO_THETAY = 1001;
}

public class MoveArmMessage : MessageBase
{
	public bool rightArm;
	public float x;
	public float y;
	public float z;
	public float thetaX;
	public float thetaY;
	public float thetaZ;
}

public class MoveArmNoThetaYMessage : MessageBase
{
	public bool rightArm;
	public float x;
	public float y;
	public float z;
	public float thetaX;
	public float thetaZ;
}

public class MyNetworkManager : MonoBehaviour
{

  public string address = "127.0.0.1";
  public int port = 4444;  
  public GameObject cameraRig;

  private bool isAtStartup = true;
    
  NetworkClient myClient;

  void Update ()
  {
	if (isAtStartup) {
	  if (Input.GetKeyDown (KeyCode.S)) {
		SetupServer ();
	  }
            
	  if (Input.GetKeyDown (KeyCode.C)) {
		SetupClient ();
	  }
            
	  if (Input.GetKeyDown (KeyCode.B)) {
		SetupServer ();
		SetupLocalClient ();
	  }
	}
  }

  void OnGUI ()
  {
	if (isAtStartup) {
	  GUI.Label (new Rect (2, 10, 200, 100), "Press S for server (robot)");     
	  GUI.Label (new Rect (2, 30, 200, 100), "Press C for client (controller)");
	  GUI.Label (new Rect (2, 50, 200, 100), "Press B for both");       
	}
  }

  // Create a server and listen on a port
  public void SetupServer ()
  {
	KinovaAPI.InitRobot ();
	NetworkServer.Listen (port);
	NetworkServer.RegisterHandler (MyMsgTypes.MSG_MOVE_ARM, ReceiveMoveArm);
	NetworkServer.RegisterHandler (MyMsgTypes.MSG_MOVE_ARM_NO_THETAY, ReceiveMoveArmNoThetaY);
	isAtStartup = false;
	Debug.Log ("Server running listening on port " + port);
  }
    
  // Create a client and connect to the server port
  public void SetupClient ()
  {
	myClient = new NetworkClient ();
	InitClient ();    
	myClient.Connect (address, port);
	Debug.Log ("Started client");
  }
    
  // Create a local client and connect to the local server
  public void SetupLocalClient ()
  {
	myClient = ClientScene.ConnectLocalServer ();
	InitClient ();
	Debug.Log ("Started local client");
  }

  private void InitClient ()
  {
	myClient.RegisterHandler (MsgType.Connect, OnConnected);
	cameraRig.SetActive (true);
	isAtStartup = false;
  }

  // Client function
  public void OnConnected (NetworkMessage netMsg)
  {
	Debug.Log ("Connected to server on " + address + ":" + port);
  }

  public void SendMoveArm (bool rightArm, float x, float y, float z, float thetaX, float thetaY, float thetaZ)
  {
    Debug.Log ("Sending move arm...");
    MoveArmMessage msg = new MoveArmMessage();
    msg.rightArm = rightArm;
    msg.x = x;
    msg.y = y;
    msg.z = z;
    msg.thetaX = thetaX;
    msg.thetaY = thetaY;
    msg.thetaZ = thetaZ;

    myClient.Send(MyMsgTypes.MSG_MOVE_ARM, msg);
  }

  private void ReceiveMoveArm (NetworkMessage message)
  {
	Debug.Log ("Move arm received!");
	MoveArmMessage m = message.ReadMessage<MoveArmMessage>();
    KinovaAPI.MoveHand(m.rightArm, m.x, m.y, m.z, m.thetaX, m.thetaY, m.thetaZ);
  }

  public void SendMoveArmNoThetaY (bool rightArm, float x, float y, float z, float thetaX, float thetaZ)
  {
    Debug.Log ("Sending move arm no theta y...");
	MoveArmNoThetaYMessage msg = new MoveArmNoThetaYMessage();
    msg.rightArm = rightArm;
    msg.x = x;
    msg.y = y;
    msg.z = z;
    msg.thetaX = thetaX;
    msg.thetaZ = thetaZ;

    myClient.Send(MyMsgTypes.MSG_MOVE_ARM_NO_THETAY, msg);
  }

  private void ReceiveMoveArmNoThetaY (NetworkMessage message)
  {
	Debug.Log ("Move arm received!");
	MoveArmNoThetaYMessage m = message.ReadMessage<MoveArmNoThetaYMessage>();
    KinovaAPI.MoveHandNoThetaY(m.rightArm, m.x, m.y, m.z, m.thetaX, m.thetaZ);
  }
}