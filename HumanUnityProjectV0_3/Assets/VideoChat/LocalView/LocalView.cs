using UnityEngine;
using System.Collections;

public class LocalView : MonoBehaviour {

	public bool localView;

	void Update () {
		VideoChat.localView = localView;
		if( VideoChat.localView && GetComponent<Renderer>().material.GetTexture( "_MainTex" ) != VideoChat.localViewTexture )
			GetComponent<Renderer>().material.SetTexture( "_MainTex", VideoChat.localViewTexture );
		
		//This requires a shader that enables texture rotation, you can use the supplied CameraView material
		//or use a new material that also uses the UnlitRotatableTexture shader if you're already using the
		//CameraView material for another object
		if( VideoChat.webCamTexture != null ) {
			Quaternion rot = Quaternion.Euler( 0, 0, VideoChat.webCamTexture.videoRotationAngle );
   			Matrix4x4 m = Matrix4x4.TRS( Vector3.zero, rot, Vector3.one );
			GetComponent<Renderer>().material.SetMatrix( "_Rotation", m ); 
		}
	}

	/*
	void OnGUI() {
		//GUI.color = Color.black;
		//if( VideoChat.webCamTexture != null )
			//GUI.Label( new Rect( 0, Camera.main.pixelHeight / 2, Camera.main.pixelWidth, 40 ), "Video Rotation = " + VideoChat.webCamTexture.videoRotationAngle / 90 );
	}
	*/
}
