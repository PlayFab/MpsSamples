using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class BombNetBhv : NetworkBehaviour {
	BombColliderNetBhv mColliderNetBhv;
	float mEnlargeTime = 0f;
	double mExploreTimer = 0f;
	float mFlashingTimer = 0f;
	float mFlashingTime = 1.3f;
	Material mMat;

	[SerializeField] GameObject mExplosivePrefab;

	void Awake() {
		mColliderNetBhv = GetComponentInChildren<BombColliderNetBhv>( true );
		mColliderNetBhv.mOwner = gameObject;

		mFlashingTimer = Time.time + mFlashingTime;
	}

	public override void OnStartServer() {
		mEnlargeTime = Time.time + .3f;

		transform.localScale = new Vector3( .3f, .3f, .3f );
		mExploreTimer = NetworkTime.time + 3f;
	}

	public override void OnStartClient() {
		mMat = GetComponent<MeshRenderer>().material;
		SetWiredThickness( 100 );
	}

	float scaler {
		get {
			return Mathf.Clamp01( CustomEase.EaseFunc.BackOut( (Time.time - mEnlargeTime) / .3f, .3f, 1f - .3f, 1f ) );
		}
	}


	class ExplosiveDestructor : MonoBehaviour {
		float timer = 0f;
		private void Awake() {
			timer = Time.time + 3f;
		}
	}



	void Update() {
		if( mEnlargeTime <= Time.time ) {
			float s = scaler;
			transform.localScale = new Vector3( s, s, s );
		}

		if( mExploreTimer <= NetworkTime.time ) {
			if( isServer ) {
				mExploreTimer = double.MaxValue;
				Damaged();
			}
			if( isClient ) {
				//GameObject explosiveGo = GameObject.Instantiate<GameObject>( mExplosivePrefab, transform.position, Quaternion.identity );
			}
		}
		if( isClient ) {
			if( Mathf.Abs( mFlashingTimer ) < Time.time ) {
				bool positive = 0f <= mFlashingTimer;
				SetWiredThickness( positive ? 100 : 800 );

				mFlashingTime *= .5f;
				if( mFlashingTime <= .05f ) {
					mFlashingTime = .05f;
				}
				mFlashingTimer = (positive ? -1f : 1f) * (Time.time + mFlashingTime);
			}
		}
	}

	public void Damaged() {
		mColliderNetBhv.Fire( .1f );
	}

	void SetWiredThickness( int thickness ) {
		if( mMat != null ) {
			mMat.SetInt( "_WireThickness", thickness ); // 100, 742
		}
	}
}
