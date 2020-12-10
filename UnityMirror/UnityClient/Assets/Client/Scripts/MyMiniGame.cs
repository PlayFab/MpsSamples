using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using System;
using PlayFab.ClientModels;
using Mirror;
using PlayFab.Helpers;

public class MyMiniGame : MonoBehaviour {


	static public void Begin() {
		GameObject go = new GameObject();
		go.AddComponent<MyMiniGame>();
	}

	public struct MousePosData : NetworkMessage {
		public float mX;
		public float mY;

		//public void Deserialize( NetworkReader reader ) {
		//	mX = reader.ReadSingle();
		//	mY = reader.ReadSingle();
		//}

		//public void Serialize( NetworkWriter writer ) {
		//	writer.WriteSingle( mX );
		//	writer.WriteSingle( mY );
		//}
	}

	void Start() {
		this.name = "MyMiniGame";

		NetworkClient.RegisterHandler<MousePosData>( OnMousePosData );
	}

	void OnMousePosData( NetworkConnection conn, MousePosData message ) {
		Debug.Log( "server cb; " + message.mX + " : " + message.mY );
	}

	// Update is called once per frame
	void Update() {
		//if( Input.GetKeyDown( KeyCode.Mouse0 ) ) {
		//	Debug.Log( "client; " + Input.mousePosition.x + " : " + Input.mousePosition.y );

		//	NetworkClient.connection.Send<MousePosData>( new MousePosData() {
		//		mX = Input.mousePosition.x,
		//		mY = Input.mousePosition.y,
		//	} );
		//}
	}








}
