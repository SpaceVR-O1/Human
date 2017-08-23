using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class VideoChatExampleTestMode : MonoBehaviour {
	
	// Set these from the editor
	public GameObject   remoteView;
	public float        framerate = 5;
	public VideoQuality videoQuality;
	public AudioQuality audioQuality;
	public Compression  compression;
	public EchoCancellation echoCancellation;

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

	// Audio threshold variables controlled by UI slider, when networked you control your friend's mic
	private float        audioThreshold = 0.001f;

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
		
		//Make some adjustments to the default video chat quad object for this demo, this assumes a Main Camera at the origin
		if( !remoteView ) {
			VideoChat.vcObject.transform.localScale *= 1.5f;
			VideoChat.vcObject.transform.position = new Vector3( 0, -1.4f, 5 );
		}
	}

	void Restart() {	
		VideoChat.ClearAudioOut();
		
		Resources.UnloadUnusedAssets();
		SceneManager.LoadScene( SceneManager.GetActiveScene().name );
	}
	
	void OnGUI () {
		if( !VideoChat.tempImage && !VideoChat.videoPrimed || !UI )
			return;

			VideoChat.framerate = framerate;

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

			if( GUI.Button( new Rect( 0, 100, Screen.width, 40 ), "Change Camera " + VideoChat.deviceIndex ) )
				VideoChat.deviceIndex++;

			GUI.color = Color.black;
			GUI.Label( new Rect( 0, 120, Screen.width, Screen.height - 120 ), VideoChat.log );
			GUI.color = Color.white;

			VideoChat.testMode = testMode;
			
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
		
		VideoChat.PreVideo();

		if( !testMode ) {
			VideoChat.ClearAudioOut();
			VideoChat.PostVideo();
			return;
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
					
			VideoChat.audioPackets.Remove( tempAudioPackets[ i ] );
		}
		#endregion
		

		#region VIDEO						
		//Collect source video, this will create a new VideoPacket(s) and add it(them) to the videoPackets list in the VideoChat static class
		VideoChat.FromVideo();
	
		numPackets = VideoChat.videoPackets.Count > VideoChat.packetsPerFrame ? VideoChat.packetsPerFrame : VideoChat.videoPackets.Count;				
		VideoPacket[] tempVideoPackets = new VideoPacket[ VideoChat.videoPackets.Count ];
		VideoChat.videoPackets.CopyTo( tempVideoPackets );		
		
		//Send the latest VideoChat video packets for a local test or your networking library of choice, in this case Unity Networking
		for( int i = 0; i < numPackets; i++ ) {
			VideoPacket currentPacket = tempVideoPackets[ i ];

			if( testMode )
				ReceiveVideo( currentPacket.x, currentPacket.y, currentPacket.data, currentPacket.timestamp ); //Test mode just displays on one machine
					
			VideoChat.videoPackets.Remove( tempVideoPackets[ i ] );
		}

		VideoChat.PostVideo();
		#endregion
	}
	
	void ReceiveVideo( int x, int y, byte[] videoData, double timestamp ) {
		VideoChat.ToVideo( x, y, videoData, timestamp );
	}
	
	void ReceiveAudio( int micPosition, int length, byte[] audioData, double timestamp ) {
		VideoChat.ToAudio( micPosition, length, audioData, timestamp );
	}

	void SetAudioThreshold( float threshold ) {
		VideoChat.audioThreshold = threshold;
	}

	IEnumerator IncrementWindowsCamera() {
		yield return new WaitForSeconds( 1.0f );
		if( Application.platform == RuntimePlatform.WindowsPlayer && VideoChat.webCamDevices.Count == 1 )
			VideoChat.deviceIndex++;
	}
}
