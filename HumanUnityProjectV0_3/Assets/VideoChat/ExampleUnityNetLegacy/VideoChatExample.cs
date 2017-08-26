using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class VideoChatExample : MonoBehaviour {
	
	public bool LAN;

	// Set these from the editor
	public GameObject   remoteView;
	public Material     cameraMaterial;
	public float        framerate = 5;
	public VideoQuality videoQuality;
	public AudioQuality audioQuality;
	public Compression  compression;
	public EchoCancellation echoCancellation;

	// Stream or broadcast from one person to many others?
	public bool oneToManyBroadcast;
	public int  numberReceivers = 2;

	// UI related controls
	private bool UI;
	private bool testMode;
	private bool echoCancel;

	//Optional, VideoChat will create these for you if you do not assign them	
	private Material   cameraView;
	private Shader     shader;
	
	// Show the UI only if the mouse moves, hide it if the mouse sits idle
	private Vector2      lastMousePosition;
	private int          mouseStillCount;
	private int          mouseStillThreshold = 60;

	private HostData[]   hostData;

	// Audio threshold variables controlled by UI slider, when networked you control your friend's mic
	private float        audioThreshold = 0.001f;
	private float        currentAudioThreshold = 0.001f;
	private float        setAudioThresholdTimer;

	private NetworkView  audioView;
	private NetworkView  videoView;
	
	// Use this for initialization
	IEnumerator Start() {
	
		if( Application.isWebPlayer ) {
			yield return Application.RequestUserAuthorization( UserAuthorization.WebCam | UserAuthorization.Microphone );
		}

		Screen.sleepTimeout = SleepTimeout.NeverSleep;		

		VideoChat.SetVideoQuality( videoQuality );
		VideoChat.SetAudioQuality( audioQuality );
		VideoChat.SetCompression( compression );
		VideoChat.SetEchoCancellation( echoCancellation );

		//Initialize to set base parameters such as the actual WebCamTexture height and width
		VideoChat.Init( 0, framerate );
		
		//Add was created in case we need to defer the assignment of a remoteView until after it has been Network instantiated
		//In this example we are not doing network instantiation but if we were, this would come in handy
		VideoChat.Add( remoteView, null, null );
		VideoChat.cameraView = cameraMaterial;
		
		if( LAN )
			LANParty.Init( "MidnightVideoChat", 1 );
		else {
			MasterServer.ClearHostList();
			MasterServer.RequestHostList( "MidnightVideoChat" );
			hostData = MasterServer.PollHostList();
		}

		//Make some adjustments to the default video chat quad object for this demo, this assumes a Main Camera at the origin
		if( !remoteView ) {
			VideoChat.vcObject.transform.localScale *= 1.5f;
			VideoChat.vcObject.transform.position = new Vector3( 0, -1.4f, 5 );
		}

		audioView = gameObject.AddComponent< NetworkView >();
		audioView.stateSynchronization = NetworkStateSynchronization.Off;
		audioView.group = VideoChat.audioGroup;

		videoView = gameObject.AddComponent< NetworkView >();
		videoView.stateSynchronization = NetworkStateSynchronization.Off;
		videoView.group = VideoChat.videoGroup;	
	}
	
	IEnumerator DelayedConnection() {
		yield return new WaitForSeconds( 2.0f );
		
		string connectionResult = "";
		if( LAN )
			connectionResult = "" + Network.Connect( LANParty.serverIPAddress, 2301 );
		else { 
			if( hostData.Length > 0 ) {
		 		connectionResult = "" + Network.Connect( hostData[ 0 ] );
				LANParty.log += "\nConnect to Server @ " + hostData[ 0 ].gameName + " " + hostData[ 0 ].gameType + " " + hostData[ 0 ].guid + " " + hostData[ 0 ].ip + " " + hostData[ 0 ].useNat + " " + connectionResult;
			}
			else
				connectionResult = "";
		}
		if( connectionResult == "IncorrectParameters" || connectionResult == "" )
			StartCoroutine( Restart() );
	}	

	void OnApplicationQuit() {
		LANParty.End();
	}
	
	void OnDisconnectedFromServer() {
		DelayedConnection();	
	}
	
	IEnumerator Restart() {
		
		if( LAN )
			LANParty.End();
		else {
			if( Network.peerType == NetworkPeerType.Server )
				MasterServer.UnregisterHost();	
		}

		yield return new WaitForSeconds( 1.0f );

		if( Network.peerType != NetworkPeerType.Disconnected ) {
			Debug.Log( "Disconnecting..." );
			Network.Disconnect();
			LANParty.log += "\nDisconnected";
		}	
		
		Resources.UnloadUnusedAssets();
		SceneManager.LoadScene( SceneManager.GetActiveScene().name );
	}

	void OnGUI () {
		if( !VideoChat.tempImage && !VideoChat.videoPrimed || !UI )
			return;

		VideoChat.framerate = framerate;
		
		if( !Network.isClient && !Network.isServer ) {
			
			bool oldTestMode = testMode;
			testMode = GUI.Toggle( new Rect( 0, 20, Screen.width / 3, 40 ), testMode, "Test Mode" );
			if( testMode == false && oldTestMode == true ) {
				VideoChat.ClearAudioOut();
			}
			if( testMode ) {
				GUI.Label( new Rect( 0, 40, Screen.width, 20 ), "Mic sensitivity " + audioThreshold );
				audioThreshold = GUI.HorizontalSlider( new Rect( 0, 60, Screen.width, 20 ), audioThreshold, 0.0f, 1.0f );
				echoCancel = GUI.Toggle( new Rect( 0, 80, Screen.width / 3, 40 ), echoCancel, "Echo Cancellation" );
				echoCancellation = ( echoCancel == true ) ? EchoCancellation.on : EchoCancellation.off;
				VideoChat.echoCancellation = echoCancellation;
			}
			
			VideoChat.testMode = testMode;
		
			if( !testMode ) {
				if( GUI.Button( new Rect( 0, 140, Screen.width, 40 ), "Start" ) ) {
					StartVideoChat ();
				}	
				
				if( GUI.Button( new Rect( 0, 180, Screen.width, 40 ), "Join" ) ) {
					JoinVideoChat ();
				}
			}
		} else {
			if( GUI.Button( new Rect( 0, 20, Screen.width, 40 ), "Disconnect" ) )
				StartCoroutine( Restart() );

			GUI.Label( new Rect( 0, 40, Screen.width, 20 ), "Friend's mic sensitivity " + audioThreshold );
		
			currentAudioThreshold = GUI.HorizontalSlider( new Rect( 0, 60, Screen.width, 20 ), currentAudioThreshold, 0.0f, 1.0f );
			if( !testMode ) {
				if( currentAudioThreshold != audioThreshold && setAudioThresholdTimer + 0.1f < Time.time ) {
					audioView.RPC( "SetAudioThreshold", RPCMode.Others, currentAudioThreshold );
					audioThreshold = currentAudioThreshold;
					setAudioThresholdTimer = Time.time;
				}
			}

			if( GUI.Button( new Rect( 0, 100, Screen.width, 40 ), "Change Camera " + VideoChat.deviceIndex ) )
				VideoChat.deviceIndex++;
			
			echoCancel = GUI.Toggle( new Rect( 0, 80, Screen.width / 3, 20 ), echoCancel, "Echo Cancellation" );
			echoCancellation = ( echoCancel == true ) ? EchoCancellation.on : EchoCancellation.off;
			VideoChat.echoCancellation = echoCancellation;
			
		}
	
		return;
	}
	
	
	
	void Update() {

		// You can utilize VideoChat.receivedAudioPackets and VideoChat.receivedVideoPackets to save/record AV data coming over the network
		// Otherwise, this clears those packets (not recording)
		// Comment this out or add conditional logic to control the recording process and then do something interesting with those lists of packets
		VideoChat.ClearReceivedPackets();

		if( Input.mousePosition.x == lastMousePosition.x && Input.mousePosition.y == lastMousePosition.y ) {
			mouseStillCount++;
			if( mouseStillCount > mouseStillThreshold ) {
				UI = false;
			}
		} else {
			mouseStillCount = 0;
			UI = true;
		}
		lastMousePosition = Input.mousePosition;

		if( Input.GetKey( KeyCode.Escape ) )
			Application.Quit();
		
		//This is new in version 1.004, initializes things early for thumbnail
		VideoChat.PreVideo();

		if( ( !testMode && Network.peerType == NetworkPeerType.Disconnected ) || ( Network.peerType != NetworkPeerType.Disconnected && Network.connections.Length < 1 ) ) {
			VideoChat.PostVideo();
			return;
		}

		if( oneToManyBroadcast ) {
			if( ( !testMode && Network.peerType != NetworkPeerType.Server ) || ( Network.peerType == NetworkPeerType.Server && Network.connections.Length < 1 ) ) {
				VideoChat.PostVideo();
				return;
			}
		}
		
		#region AUDIO
		VideoChat.audioThreshold = audioThreshold;

		//Collect source audio, this will create a new AudioPacket and add it to the audioPackets list in the VideoChat static class
		VideoChat.FromAudio();		

		//Send the latest VideoChat audio packet for a local test or your networking library of choice, in this case Unity Networking
		int numPackets = VideoChat.audioPackets.Count;				
		AudioPacket[] tempAudioPackets = new AudioPacket[ numPackets ];
		VideoChat.audioPackets.CopyTo( tempAudioPackets );
		
		for( int i = 0; i < numPackets; i++ ) {
			AudioPacket currentPacket = tempAudioPackets[ i ]; 
			
			if( testMode )
				ReceiveAudio( currentPacket.position, currentPacket.length, currentPacket.data, System.Convert.ToString( currentPacket.timestamp ) ); //Test mode just plays back on one machine
			else
				audioView.RPC( "ReceiveAudio", RPCMode.Others, currentPacket.position, currentPacket.length, currentPacket.data, System.Convert.ToString( currentPacket.timestamp ) ); //Unity Networking
			
			VideoChat.audioPackets.Remove( tempAudioPackets[ i ] );
		}
		#endregion
		

		#region VIDEO
		Network.sendRate = (int)( VideoChat.packetsPerFrame + ( ( 1 / Time.fixedDeltaTime ) / 10 ) );
		
		//Collect source video, this will create a new VideoPacket(s) and add it(them) to the videoPackets list in the VideoChat static class
		VideoChat.FromVideo();
	
		numPackets = VideoChat.videoPackets.Count > VideoChat.packetsPerFrame ? VideoChat.packetsPerFrame : VideoChat.videoPackets.Count;				
		VideoPacket[] tempVideoPackets = new VideoPacket[ VideoChat.videoPackets.Count ];
		VideoChat.videoPackets.CopyTo( tempVideoPackets );		
		
		//Send the latest VideoChat video packets for a local test or your networking library of choice, in this case Unity Networking
		for( int i = 0; i < numPackets; i++ ) {
			VideoPacket currentPacket = tempVideoPackets[ i ];

			if( testMode )
				ReceiveVideo( currentPacket.x, currentPacket.y, currentPacket.data, System.Convert.ToString( currentPacket.timestamp ) ); //Test mode just displays on one machine
			else
				videoView.RPC( "ReceiveVideo", RPCMode.Others, currentPacket.x, currentPacket.y, currentPacket.data, System.Convert.ToString( currentPacket.timestamp ) ); //Unity Networking
		
			VideoChat.videoPackets.Remove( tempVideoPackets[ i ] );
		}

		VideoChat.PostVideo();
		#endregion
	}

	public void StartVideoChat ()
	  {
	    Debug.Log ("Starting video chat server...");
		if (LAN) {
		  LANParty.peerType = "server";

		  if (oneToManyBroadcast)
			LANParty.possibleConnections = numberReceivers;
		  else
			LANParty.possibleConnections = 1;

		  LANParty.log += "\n" + Network.InitializeServer (LANParty.possibleConnections, 2301, false);
		} else {
		  if (oneToManyBroadcast)
			Network.InitializeServer (numberReceivers, 2301, true);
		  else
			Network.InitializeServer (1, 2301, true);
							
		  MasterServer.RegisterHost ("MidnightVideoChat", "Test");
		}

        Invoke("ConnectToRicohThetaS", 1.0f);
	  }

    void ConnectToRicohThetaS ()
    {
        List<WebCamDevice> cameras = VideoChat.webCamDevices;
        int cameraIndex = 0;
        for (int i = 0; i < cameras.Count; i++)
        {
            if (cameras[i].name.Equals("RICOH THETA S"))
            {
                cameraIndex = i;
                Debug.Log("Found Ricoh Theta S!");
                break;
            }
        }
        VideoChat.deviceIndex = cameraIndex;
    }


	 public void JoinVideoChat ()
	  {
		Debug.Log ("Joining video chat server...");
		if (LAN) {
		  LANParty.peerType = "client";
		  LANParty.Broadcast (LANParty.ipRequestString + LANParty.gameName);
		} else {
		  if (hostData.Length == 0)
			hostData = MasterServer.PollHostList ();
		}
		StartCoroutine ("DelayedConnection");
	 }

	[RPC]
	void ReceiveVideo( int x, int y, byte[] videoData, string timestamp ) {
		if( videoData.Length == 1 && Network.connections.Length > 0 ) {
			for( int i = 0; i < Network.connections.Length; i++ )
				Network.RemoveRPCs( Network.connections[ 0 ], VideoChat.videoGroup );
		}
		VideoChat.ToVideo( x, y, videoData, System.Convert.ToDouble( timestamp ) );
		if( !testMode && oneToManyBroadcast && Network.peerType != NetworkPeerType.Server )
			videoView.RPC( "ReceiveVideo", RPCMode.Server, VideoChat.requestedWidth, VideoChat.requestedHeight, new byte[ 3 ], System.Convert.ToString( VideoChat.CurrentTimestamp() ) );
	}
	
	[RPC]
	void ReceiveAudio( int micPosition, int length, byte[] audioData, string timestamp ) {
		if( micPosition == 0 && Network.connections.Length > 0 ) {
			for( int i = 0; i < Network.connections.Length; i++ )
				Network.RemoveRPCs( Network.connections[ i ], VideoChat.audioGroup );
		}
		VideoChat.ToAudio( micPosition, length, audioData, System.Convert.ToDouble( timestamp ) );
	}

	void OnPlayerDisconnected( NetworkPlayer player ) {
        Network.RemoveRPCsInGroup( VideoChat.videoGroup );
		Network.RemoveRPCsInGroup( VideoChat.audioGroup );
		Network.RemoveRPCs( player );
		VideoChat.deviceIndex = VideoChat.deviceIndex; //This resets the camera to prepare for a new connection
		VideoChat.ClearAudioOut();
    }
	
	void OnPlayerConnected( NetworkPlayer player ) {
		Network.RemoveRPCsInGroup( VideoChat.videoGroup );
		Network.RemoveRPCsInGroup( VideoChat.audioGroup );
		VideoChat.deviceIndex = VideoChat.deviceIndex; //This resets the camera to prepare for a new connection
		if( !testMode && oneToManyBroadcast && Network.peerType == NetworkPeerType.Server )
			videoView.RPC( "ReceiveVideo", player, VideoChat.requestedWidth, VideoChat.requestedHeight, new byte[ 2 ], System.Convert.ToString( VideoChat.CurrentTimestamp() ) );
	}
}
