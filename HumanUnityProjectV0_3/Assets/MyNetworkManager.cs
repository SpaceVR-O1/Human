using UnityEngine;
using UnityEngine.Networking;

public class MyMsgTypes
{
	public static short MSG_MOVE_ARM = 1000;
	public static short MSG_MOVE_ARM_NO_THETAY = 1001;
	public static short MSG_MOVE_ARM_HOME = 1002;
	public static short MSG_STOP_ARM = 1003;
	public static short MSG_MOVE_FINGERS = 1004;
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

public class MoveArmHomeMessage : MessageBase
{
	public bool rightArm;
}

public class StopArmMessage : MessageBase
{
	public bool rightArm;
	public bool suppressLog;
}

public class MoveFingersMessage : MessageBase
{
	public bool rightArm;
	public bool pinky;
	public bool ring;
	public bool middle;
	public bool index;
	public bool thumb;
}

public class MyNetworkManager : MonoBehaviour
{

  public string address = "127.0.0.1";
  public int port = 11111;  
  public GameObject cameraRig;
  public VideoChatExample videoChat;

  private bool isAtStartup = true;
  private bool connectedToServer = false;
    
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
	NetworkServer.RegisterHandler (MyMsgTypes.MSG_MOVE_ARM_HOME, ReceiveMoveArmHome);
	NetworkServer.RegisterHandler (MyMsgTypes.MSG_STOP_ARM, ReceiveStopArm);
	videoChat.gameObject.SetActive (true);
	videoChat.StartVideoChat ();
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
	cameraRig.SetActive (true); // transitively enables VIVE controllers
	videoChat.gameObject.SetActive (true);
	isAtStartup = false;
  }

  // Client function
  public void OnConnected (NetworkMessage netMsg)
  {
	Debug.Log ("Connected to server on " + address + ":" + port);
	videoChat.JoinVideoChat ();
	connectedToServer = true;
  }

  public void SendMoveArm (bool rightArm, float x, float y, float z, float thetaX, float thetaY, float thetaZ)
  {
	if (!connectedToServer) {
	  Debug.LogWarning ("Not connected to server!");
	  return;
	}

	Debug.Log ("Sending move " + ArmSide(rightArm) + " arm...");
    MoveArmMessage m = new MoveArmMessage();
    m.rightArm = rightArm;
    m.x = x;
    m.y = y;
    m.z = z;
    m.thetaX = thetaX;
    m.thetaY = thetaY;
    m.thetaZ = thetaZ;

    myClient.Send (MyMsgTypes.MSG_MOVE_ARM, m);
  }

  private void ReceiveMoveArm (NetworkMessage message)
  {
	MoveArmMessage m = message.ReadMessage<MoveArmMessage>();
	Debug.Log ("Move " + ArmSide(m.rightArm) + " arm received!");
    KinovaAPI.MoveHand(m.rightArm, m.x, m.y, m.z, m.thetaX, m.thetaY, m.thetaZ);
  }

  public void SendMoveArmNoThetaY (bool rightArm, float x, float y, float z, float thetaX, float thetaZ)
  {
	if (!connectedToServer) {
	  Debug.LogWarning ("Not connected to server!");
	  return;
	}

	Debug.Log ("Sending move " + ArmSide(rightArm) + " arm no theta y...");
	MoveArmNoThetaYMessage m = new MoveArmNoThetaYMessage();
    m.rightArm = rightArm;
    m.x = x;
    m.y = y;
    m.z = z;
    m.thetaX = thetaX;
    m.thetaZ = thetaZ;

    myClient.Send (MyMsgTypes.MSG_MOVE_ARM_NO_THETAY, m);
  }

  private void ReceiveMoveArmNoThetaY (NetworkMessage message)
  {
	MoveArmNoThetaYMessage m = message.ReadMessage<MoveArmNoThetaYMessage>();
	Debug.Log ("Move " + ArmSide(m.rightArm) + " arm received!");
    KinovaAPI.MoveHandNoThetaY(m.rightArm, m.x, m.y, m.z, m.thetaX, m.thetaZ);
  }

  public void SendMoveArmHome (bool rightArm)
  {
	if (!connectedToServer) {
	  Debug.LogWarning ("Not connected to server!");
	  return;
	}

	Debug.Log ("Sending move " + ArmSide (rightArm) + " arm home...");
	MoveArmHomeMessage m = new MoveArmHomeMessage();
    m.rightArm = rightArm;

    myClient.Send (MyMsgTypes.MSG_MOVE_ARM_HOME, m);
  }

  private void ReceiveMoveArmHome (NetworkMessage message)
  {
	MoveArmHomeMessage m = message.ReadMessage<MoveArmHomeMessage> ();
	Debug.Log ("Stop " + ArmSide (m.rightArm) + " arm received!");
    KinovaAPI.MoveArmHome(m.rightArm);
  }

  public void SendStopArm (bool rightArm, bool suppressLog)
  {
	if (!connectedToServer) {
	  Debug.LogWarning ("Not connected to server!");
	  return;
	}

	if (!suppressLog) {
	  Debug.Log ("Sending stop " + ArmSide (rightArm) + " arm...");
	}
    StopArmMessage m = new StopArmMessage();
    m.rightArm = rightArm;
    m.suppressLog = suppressLog;

    myClient.Send (MyMsgTypes.MSG_STOP_ARM, m);
  }

  private void ReceiveStopArm (NetworkMessage message)
  {
	StopArmMessage m = message.ReadMessage<StopArmMessage> ();
	if (!m.suppressLog) {
	  Debug.Log ("Stop " + ArmSide (m.rightArm) + " arm received!");
	}
    KinovaAPI.StopArm(m.rightArm);
  }

  public void SendMoveFingers (bool rightArm, bool pinky, bool ring, bool middle, bool index, bool thumb)
  {
	if (!connectedToServer) {
	  Debug.LogWarning ("Not connected to server!");
	  return;
	}

    Debug.Log ("Sending move " + ArmSide (rightArm) + " arm fingers...");
    MoveFingersMessage m = new MoveFingersMessage();
    m.rightArm = rightArm;
    m.pinky = pinky;
    m.ring = ring;
    m.middle = middle;
    m.index = index;
    m.thumb = thumb;

    myClient.Send (MyMsgTypes.MSG_MOVE_FINGERS, m);
  }

  private void ReceiveMoveFingers (NetworkMessage message)
  {
	MoveFingersMessage m = message.ReadMessage<MoveFingersMessage> ();
	Debug.Log ("Move " + ArmSide (m.rightArm) + " arm fingers received!");
    KinovaAPI.MoveFingers(m.rightArm, m.pinky, m.ring, m.middle, m.index, m.thumb);
  }

  private string ArmSide (bool rightArm)
  {
	return rightArm ? "right" : "left";
  }
}