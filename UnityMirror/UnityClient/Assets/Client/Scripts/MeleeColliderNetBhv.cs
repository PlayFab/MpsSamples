using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeColliderNetBhv : MonoBehaviour {
	public GameObject mOwner;
	double mTimer = double.MaxValue;

	private void OnTriggerEnter( Collider other ) {
		if( other.gameObject != mOwner ) {
			PlayerNetBhv pnb = other.gameObject.GetComponent<PlayerNetBhv>();
			if( pnb != null ) {
				pnb.OnTakenDamage( 3, transform.position, 40f );
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
		}
	}
	private void Update() {
		if( mTimer < NetworkTime.time ) {
			mTimer = double.MaxValue;

			gameObject.SetActive( false );
		}
	}
}
