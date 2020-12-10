using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordBhv : MonoBehaviour {
	Vector3 mInitLocPos;
	Quaternion mInitLocRot;
	float mMeleeTimer = float.MaxValue;
	float mDuration = 0f;
	bool mDir = false;

	public bool isReady {
		get {
			return mMeleeTimer == float.MaxValue;
		}
	}

	private void Awake() {
		mInitLocPos = transform.localPosition;
		mInitLocRot = transform.localRotation;
	}

	public void Fire( float duration ) {
		mDuration = duration;
		mMeleeTimer = Time.time + duration;
		transform.localPosition = new Vector3( 0.08398438f, 0.2120056f, 1.58f );
		transform.localRotation = Quaternion.Euler( 0, 0f, -90 );
		mDir = !mDir;
	}

	public static float EaseInOutQuad( float t, float b, float c, float d ) {
		if( (t /= d / 2) < 1 )
			return ((c / 2) * (t * t)) + b;
		return -c / 2 * (((t - 2) * (--t)) - 1) + b;
	}

	private void Update() {
		if( mMeleeTimer < Time.time ) {
			mMeleeTimer = float.MaxValue;
			transform.localPosition = mInitLocPos;
			transform.localRotation = mInitLocRot;

		} else if( mMeleeTimer != float.MaxValue ) {
			transform.localRotation = Quaternion.Euler( 0, EaseInOutQuad( mMeleeTimer - Time.time, 0f, (mDir ? 540 : -540), mDuration ), -90 );
		}
	}
}
