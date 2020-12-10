using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeColliderNetBhv : NetworkBehaviour {
	public GameObject mOwner;
	float mTimer = float.MaxValue;

	private void OnTriggerEnter( Collider other ) {
		if( other.gameObject != mOwner ) {
			PlayerNetBhv pnb = other.gameObject.GetComponent<PlayerNetBhv>();
			if( pnb != null ) {
				pnb.TakeDamage( 3, true );
			}
		}
	}

	private void OnTriggerStay( Collider other ) {
		//Debug.Log( "OnTriggerEnter : " + other.gameObject.name );
	}

	public void Fire( float duration ) {
		if( mTimer == float.MaxValue ) {
			gameObject.SetActive( true );
			mTimer = Time.time + duration;
		}
	}
	private void Update() {
		if( mTimer < Time.time ) {
			mTimer = float.MaxValue;

			gameObject.SetActive( false );
		}
	}
}
