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

	class MousePosData : IMessageBase {
		public float mX;
		public float mY;

		public void Deserialize( NetworkReader reader ) {
			mX = reader.ReadSingle();
			mY = reader.ReadSingle();
		}

		public void Serialize( NetworkWriter writer ) {
			writer.WriteSingle( mX );
			writer.WriteSingle( mY );
		}
	}

	void Start() {
		this.name = "MyMiniGame";

		NetworkClient.RegisterHandler<MousePosData>( OnMousePosData );
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








	private void OnMousePosData( NetworkConnection connection, MousePosData mousePosData ) {
		Debug.Log( "server cb; " + mousePosData.mX + " : " + mousePosData.mY );
	}

}
