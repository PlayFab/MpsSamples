using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ext;

public class SwordBhv : MonoBehaviour {
	Vector3 mInitLocPos;
	Quaternion mInitLocRot;
	float mMeleeCd = float.MaxValue;
	float mDuration = 0f;
	bool mDir = false;

	public bool isReady {
		get {
			return mMeleeCd == float.MaxValue;
		}
	}

	private void Awake() {
		mInitLocPos = transform.localPosition;
		mInitLocRot = transform.localRotation;
	}

	public void Fire( float duration ) {
		mDuration = duration;
		mMeleeCd = Time.time + duration;
		transform.localPosition = new Vector3( 0.08398438f, 0.2120056f, 1.58f );
		transform.localRotation = Quaternion.Euler( 0, 0f, -90 );
		mDir = !mDir;
	}

	private void Update() {
		if( mMeleeCd < Time.time ) {
			mMeleeCd = float.MaxValue;
			transform.localPosition = mInitLocPos;
			transform.localRotation = mInitLocRot;

		} else if( mMeleeCd != float.MaxValue ) {
			transform.localRotation = Quaternion.Euler( 0, Ease.InOutQuad( mMeleeCd - Time.time, 0f, (mDir ? 540 : -540), mDuration ), -90 );
		}
	}
}
