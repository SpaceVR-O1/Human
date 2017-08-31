using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

//See Documentation for more details on setting up Photon

//Photon (available free on the Asset Store is required to use this example
//Comment this class out when you do and use the one below!

public class ExampleRenderTexture : MonoBehaviour {
	
	// Set these from the editor
	public GameObject   remoteView;
	public float        framerate = 5;
	public VideoQuality videoQuality;
	public AudioQuality audioQuality;
	public Compression  compression;
	public EchoCancellation echoCancellation;

	// Stream or broadcast from one person to many others?
	public bool oneToManyBroadcast;
	public int  numberReceivers = 2;

	public void Start() {
		Debug.LogError( "Install Photon from the Asset Store before using this script. Once it is installed, comment out this class and uncomment out the class below" );
	}
}



//USE THIS ONE ONCE PHOTON IS IMPORTED TO YOUR PROJECT\/
/*
public class ExampleRenderTexture : Photon.MonoBehaviour {
	
	// Set these from the editor
	public GameObject   remoteView;
	public float        framerate = 5;
	public VideoQuality videoQuality;
	public AudioQuality audioQuality;
	public Compression  compression;
	public EchoCancellation echoCancellation;

	// Stream or broadcast from one person to many others?
	public bool oneToManyBroadcast;
	public int  numberReceivers = 2;

	//public RenderTexture renderTexture;
	public Camera        renderTextureCamera;
	public bool          doRenderTexture;

	public Texture2D     texture2D;
	public bool          doTexture2D;

	// UI related controls
	private bool UI;
	private bool testMode;
	private bool echoCancel;

	//Optional, VideoChat will create these for you if you do not assign them	
	private Material   cameraView;
	private Shader     shader;
	
	//Two network views for access to audio and video network groups separately
	private PhotonView   audioView;
	private PhotonView   videoView;
	
	// Show the UI only if the mouse moves, hide it if the mouse sits idle
	private Vector2      lastMousePosition;
	private int          mouseStillCount;
	private int          mouseStillThreshold = 60;

	// Audio threshold variables controlled by UI slider, when networked you control your friend's mic
	private float        audioThreshold = 0.001f;
	private float        currentAudioThreshold = 0.001f;
	private float        setAudioThresholdTimer;

	// Use this for initialization
	IEnumerator Start() {

		audioView = gameObject.AddComponent< PhotonView >();
		audioView.synchronization = ViewSynchronization.Off;
		audioView.ObservedComponents = new List<Component>();
		audioView.ObservedComponents.Add( this );
		audioView.viewID = 1;

		videoView = gameObject.AddComponent< PhotonView >();
		videoView.synchronization = ViewSynchronization.Off;
		videoView.ObservedComponents = new List<Component>();
		videoView.ObservedComponents.Add( this );
		videoView.viewID = 2;

		if( Application.internetReachability != NetworkReachability.NotReachable )
			PhotonNetwork.ConnectUsingSettings( "1.0" );
	
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
		
		//Make some adjustments to the default video chat quad object for this demo, this assumes a Main Camera at the origin
		if( !remoteView ) {
			VideoChat.vcObject.transform.localScale *= 1.5f;
			VideoChat.vcObject.transform.position = new Vector3( 0, -1.4f, 5 );
		}
	}

	void Restart() {	
		VideoChat.ClearAudioOut();
	
		if( PhotonNetwork.room != null )
			PhotonNetwork.Disconnect();
		
		Resources.UnloadUnusedAssets();
		SceneManager.LoadScene( SceneManager.GetActiveScene().name );
	}
	
	void OnGUI () {
		if( !VideoChat.tempImage && !VideoChat.setup || !UI )
			return;
		
		VideoChat.framerate = framerate;
		
		if( PhotonNetwork.room == null ) {
			
			bool oldTestMode = testMode;
			testMode = GUI.Toggle( new Rect( 0, 20, Screen.width / 3, 40 ), testMode, "Test Mode" );
			if( testMode == false && oldTestMode == true ) {
				VideoChat.ClearAudioOut();
			}
			if( testMode ) {
				doRenderTexture = GUI.Toggle( new Rect( Screen.width / 2, 80, Screen.width / 3, 20 ), doRenderTexture, "RenderTexture" );
				doTexture2D = GUI.Toggle( new Rect( Screen.width / 2, 100, Screen.width / 3, 20 ), doTexture2D, "Texture2D" );
				GUI.Label( new Rect( 0, 40, Screen.width, 20 ), "Mic sensitivity " + audioThreshold );
				audioThreshold = GUI.HorizontalSlider( new Rect( 0, 60, Screen.width, 20 ), audioThreshold, 0.0f, 1.0f );
				echoCancel = GUI.Toggle( new Rect( 0, 80, Screen.width / 3, 40 ), echoCancel, "Echo Cancellation" );
				echoCancellation = ( echoCancel == true ) ? EchoCancellation.on : EchoCancellation.off;
				VideoChat.echoCancellation = echoCancellation;
			}
			
			VideoChat.testMode = testMode;
		
			if( !testMode ) {
				if( GUI.Button( new Rect( 0, 140, Screen.width, 40 ), "Start" ) ) {
					RoomOptions roomOptions = new RoomOptions();
					if( oneToManyBroadcast )
						roomOptions.maxPlayers = (byte)( numberReceivers + 1 );
					else
						roomOptions.maxPlayers = 2;
					roomOptions.cleanupCacheOnLeave = true;
					roomOptions.isVisible = true;
					roomOptions.isOpen = true;
					TypedLobby typedLobby = new TypedLobby();
					typedLobby.Name = "chat";
					typedLobby.Type = LobbyType.Default;
					PhotonNetwork.CreateRoom( "MidnightVideoChat", roomOptions, typedLobby );
					PhotonNetwork.NetworkStatisticsEnabled = true;
				}	
				
				if( GUI.Button( new Rect( 0, 180, Screen.width, 40 ), "Join" ) ) {
					PhotonNetwork.JoinRoom( "MidnightVideoChat" );
				}
			}
		} else {
			if( GUI.Button( new Rect( 0, 20, Screen.width, 40 ), "Disconnect" ) )
				Restart();

			GUI.Label( new Rect( 0, 40, Screen.width, 20 ), "Friend's mic sensitivity " + audioThreshold );
		
			currentAudioThreshold = GUI.HorizontalSlider( new Rect( 0, 60, Screen.width, 20 ), currentAudioThreshold, 0.0f, 1.0f );
			if( !testMode ) {
				if( currentAudioThreshold != audioThreshold && setAudioThresholdTimer + 0.1f < Time.time ) {
					audioView.RPC( "SetAudioThreshold", PhotonTargets.Others, currentAudioThreshold );
					audioThreshold = currentAudioThreshold;
					setAudioThresholdTimer = Time.time;
				}
			}
			
			doRenderTexture = GUI.Toggle( new Rect( Screen.width / 2, 80, Screen.width / 3, 20 ), doRenderTexture, "RenderTexture" );
			doTexture2D = GUI.Toggle( new Rect( Screen.width / 2, 100, Screen.width / 3, 20 ), doTexture2D, "Texture2D" );
			echoCancel = GUI.Toggle( new Rect( 0, 80, Screen.width / 3, 20 ), echoCancel, "Echo Cancellation" );
			echoCancellation = ( echoCancel == true ) ? EchoCancellation.on : EchoCancellation.off;
			VideoChat.echoCancellation = echoCancellation;
			
		}
	
		return;
	}
	
	void Update() {

		if( VideoChat.renderTextureCamera != renderTextureCamera )
			VideoChat.renderTextureCamera = renderTextureCamera;

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
		
		VideoChat.PreVideo();

		if( ( !testMode && PhotonNetwork.room == null ) || ( PhotonNetwork.room != null && PhotonNetwork.otherPlayers.Length < 1 ) ) {
			VideoChat.PostVideo();
			return;
		}

		if( oneToManyBroadcast ) {
			if( ( !testMode && !PhotonNetwork.isMasterClient ) || ( PhotonNetwork.isMasterClient && PhotonNetwork.otherPlayers.Length < 1 ) ) {
				VideoChat.PostVideo();
				return;
			}
		}

		#region AUDIO
		if( testMode )
			VideoChat.audioThreshold = audioThreshold;

		VideoChat.SetEchoCancellation( echoCancellation );

		//Collect source audio, this will create a new AudioPacket and add it to the audioPackets list in the VideoChat static class
		VideoChat.FromAudio();		

		//Send the latest VideoChat audio packet for a local test or your networking library of choice, in this case Unity Networking
		int numPackets = VideoChat.audioPackets.Count;				
		AudioPacket[] tempAudioPackets = new AudioPacket[ numPackets ];
		VideoChat.audioPackets.CopyTo( tempAudioPackets );
		
		for( int i = 0; i < numPackets; i++ ) {
			AudioPacket currentPacket = tempAudioPackets[ i ]; 
			
			if( testMode )
				ReceiveAudio( currentPacket.position, currentPacket.length, currentPacket.data, currentPacket.timestamp ); //Test mode just plays back on one machine
			else
				audioView.RPC( "ReceiveAudio", PhotonTargets.Others, currentPacket.position, currentPacket.length, currentPacket.data, currentPacket.timestamp ); //Photon Networking
			
			VideoChat.audioPackets.Remove( tempAudioPackets[ i ] );
		}
		#endregion
		

		#region VIDEO		
		PhotonNetwork.sendRate = (int)( VideoChat.packetsPerFrame + ( ( 1 / Time.fixedDeltaTime ) / 10 ) );
				
		//Collect source video, this will create a new VideoPacket(s) and add it(them) to the videoPackets list in the VideoChat static class
		if( doRenderTexture ) {
			StartCoroutine( VideoChat.FromVideoRenderTexture() );
		} else {
			if( doTexture2D ) {
				VideoChat.FromVideoTexture2D( texture2D );
			} else {
				VideoChat.FromVideo();
			}
		}	

		numPackets = VideoChat.videoPackets.Count > VideoChat.packetsPerFrame ? VideoChat.packetsPerFrame : VideoChat.videoPackets.Count;				
		VideoPacket[] tempVideoPackets = new VideoPacket[ VideoChat.videoPackets.Count ];
		VideoChat.videoPackets.CopyTo( tempVideoPackets );		
		
		//Send the latest VideoChat video packets for a local test or your networking library of choice, in this case Unity Networking
		for( int i = 0; i < numPackets; i++ ) {
			VideoPacket currentPacket = tempVideoPackets[ i ];

			if( testMode )
				ReceiveVideo( currentPacket.x, currentPacket.y, currentPacket.data, currentPacket.timestamp ); //Test mode just displays on one machine
			else
				videoView.RPC( "ReceiveVideo", PhotonTargets.Others, currentPacket.x, currentPacket.y, currentPacket.data, currentPacket.timestamp ); //Photon Networking
		
			VideoChat.videoPackets.Remove( tempVideoPackets[ i ] );
		}

		VideoChat.PostVideo();
		#endregion
	}
	
	[PunRPC]
	void ReceiveVideo( int x, int y, byte[] videoData, double timestamp ) {
		// Solution for Jimmy rotation
		//if( videoData.Length == 2 ) {
		//	transform.localEulerAngles = new Vector3( transform.localEulerAngles.x, transform.localEulerAngles.y, 90 * VideoChat.rotationPacket[ 0 ] );
		//}
		VideoChat.ToVideo( x, y, videoData, timestamp );
		if( oneToManyBroadcast && !PhotonNetwork.isMasterClient )
			videoView.RPC( "ReceiveVideo", PhotonTargets.MasterClient, VideoChat.requestedWidth, VideoChat.requestedHeight, new byte[ 3 ], VideoChat.CurrentTimestamp() );
	}
	
	[PunRPC]
	void ReceiveAudio( int micPosition, int length, byte[] audioData, double timestamp ) {
		VideoChat.ToAudio( micPosition, length, audioData, timestamp );
	}

	[PunRPC]
	void SetAudioThreshold( float threshold ) {
		VideoChat.audioThreshold = threshold;
	}
	
	void OnPhotonPlayerDisconnected( PhotonPlayer player ) {
		VideoChat.deviceIndex = VideoChat.deviceIndex; //This resets the camera to prepare for a new connection
		VideoChat.ClearAudioOut();
    }
	
	void OnPhotonPlayerConnected( PhotonPlayer player ) {
		VideoChat.deviceIndex = VideoChat.deviceIndex; //This resets the camera to prepare for a new connection
		if( oneToManyBroadcast && PhotonNetwork.isMasterClient )
			videoView.RPC( "ReceiveVideo", player, VideoChat.requestedWidth, VideoChat.requestedHeight, new byte[ 2 ], VideoChat.CurrentTimestamp() );
	}	

	void OnDisconnectedFromPhoton() {
		if( !testMode )
			Restart();	
	}

	IEnumerator IncrementWindowsCamera() {
		yield return new WaitForSeconds( 1.0f );
		if( Application.platform == RuntimePlatform.WindowsPlayer && VideoChat.webCamDevices.Count == 1 )
			VideoChat.deviceIndex++;
	}
}
*/