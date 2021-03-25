using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class BombColliderNetBhv : MonoBehaviour {
	public GameObject mOwner;
	double mTimer = double.MaxValue;
	float OrgScale = 4f;

	void Awake() {
		gameObject.SetActive( false );
		OrgScale = transform.localScale.y;
	}

	private void OnTriggerEnter( Collider other ) {
		if( other.gameObject != mOwner ) {
			PlayerNetBhv pnb = other.gameObject.GetComponent<PlayerNetBhv>();
			if( pnb != null ) {
				pnb.OnBlownAway( this, 10, transform.position );
			}
			BombNetBhv bombnb = other.gameObject.GetComponent<BombNetBhv>();
			if( bombnb != null ) {
				bombnb.Damaged();
			}
		}
	}

	private void OnTriggerStay( Collider other ) {
		//Debug.Log( "OnTriggerEnter : " + other.gameObject.name );
	}

	public void Fire( float duration ) {
		if( mTimer == double.MaxValue ) {
			gameObject.SetActive( true );
			mTimer = NetworkTime.time + duration;
			transform.localScale = Vector3.one * (OrgScale / transform.parent.localScale.y);
		}
	}
	private void Update() {
		if( mTimer < NetworkTime.time ) {
			mTimer = double.MaxValue;

			gameObject.SetActive( false );

			//if you need to just remove it from the clients do this
			//NetworkServer.UnSpawn( gameObject ); //removes an object on the server, but doesn't destroy it.

			NetworkServer.Destroy( transform.parent.gameObject );   //destroys object on server and clients.

		} else {
			transform.localScale = Vector3.one * (OrgScale / transform.parent.localScale.y);
		}
	}
}
