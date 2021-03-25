using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HiroExtensions;
using Mirror;
using NetworkRigidbody = Mirror.Experimental.NetworkRigidbody;

[RequireComponent( typeof( Rigidbody ) )]
[RequireComponent( typeof( CapsuleCollider ) )]

public class RigidbodyController : NetworkBehaviour /*MonoBehaviour*/ {
	public bool mAutoUserControl = false;
	public float speed = 10.0f;
	public float gravity = 25.0f;
	public float maxVelocityChange = 10.0f;
	public bool canJump = true;
	public float jumpHeight = 2.0f;
	public bool grounded = false;

	double mStunTime = 0f;

	double mReviveTime = 0f;
	Quaternion mReviveStartQuat = Quaternion.identity;
	Quaternion mReviveEndQuat = Quaternion.identity;
	const float mReviveTotalTime = .5f;

	Rigidbody mRigidbody;
	NetworkRigidbody mNetworkRigidbody;

	float mCapsuleH;
	float mCapsuleRadius;

	public Vector3? mAiWalkVec = null;

	float FInterpTo( float Current, float Target, float DeltaTime, float InterpSpeed ) {
		// If no interp speed, jump to target value
		if( InterpSpeed <= 0f ) {
			return Target;
		}

		// Distance to reach
		float Dist = Target - Current;

		// If distance is too small, just set the desired location
		if( Mathf.Sqrt( Mathf.Abs( Dist ) ) < 0.01f ) {
			return Target;
		}

		// Delta Move, Clamp so we do not over shoot.
		float DeltaMove = Dist * Mathf.Clamp( DeltaTime * InterpSpeed, 0f, 1f );

		return Current + DeltaMove;
	}




	[Command]	// client to server
	public void SetKinematic( bool val ) {
		kinematic = val;
	}

	// won't sync if it's assigned from client side
	[SyncVar( hook = nameof( Receiver_SetKinematic ) )]
	public bool kinematic = false;

	// server to all clients
	void Receiver_SetKinematic( bool oldVal, bool newVal ) {
		_kinematic = newVal;
	}

	public bool _kinematic {
		set {
			if( value ) {	// (faking) no physics
				//mRigidbody.freezeRotation = true;
				mRigidbody.useGravity = false;
				mRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
				mNetworkRigidbody.clearAngularVelocity = true;

			} else {
				mRigidbody.useGravity = true;
				mRigidbody.constraints = RigidbodyConstraints.None;
				mNetworkRigidbody.clearAngularVelocity = false;
			}

			mNetworkRigidbody.syncAngularVelocity = !mNetworkRigidbody.clearAngularVelocity;
		}
		get {
			return !mRigidbody.useGravity;
		}
	}




	void Awake() {
		mRigidbody = this.GetComponent<Rigidbody>();
		mNetworkRigidbody = this.GetComponent<NetworkRigidbody>();
		var cc = transform.GetComponent<CapsuleCollider>();
		mCapsuleH = cc.height;
		mCapsuleRadius = cc.radius;
		//kinematic = true;
	}
	public override void OnStartClient() {
		SetKinematic( true );
	}

	public float torque = 1f;

	public enum State {
		None,
		KnockedBack,
		KnockBackLanded,
		BlownAway,
		BlownAwayLanded,
		Reviving,
	}

	public State mState = State.None;

	public void UserControl() {
		Vector3 camForward = Camera.main.transform.forward.Y( 0f ).normalized;

		switch( mState ) {
			case State.None: {
					bool controllable = mStunTime < NetworkTime.time;

					if( controllable ) {
						mState = State.None;

						if( grounded ) {
							// Calculate how fast we should be moving

							Vector3 targetVelocity;

							if( false ) {
								targetVelocity = Camera.main.transform.TransformDirection( new Vector3( Input.GetAxis( "Horizontal" ), 0, Input.GetAxis( "Vertical" ) ) );

							} else {
								float horizontalAxis = Input.GetAxis( "Horizontal" );
								float verticalAxis = Input.GetAxis( "Vertical" );

								Vector3 camRgt = Camera.main.transform.right.Y( 0f ).normalized;
								targetVelocity = (camForward * verticalAxis + camRgt * horizontalAxis);
							}
							if( mAiWalkVec != null ) {
								targetVelocity = mAiWalkVec.Value;
								mAiWalkVec = null;
							}

							targetVelocity *= speed;

							SetForce( ref targetVelocity, 0f, maxVelocityChange );

							// Jump
							if( iSJumpKeyPressed ) {
								iSJumpKeyPressed = false;
								if( canJump ) {
									mRigidbody.velocity = mRigidbody.velocity.Y( CalculateJumpVerticalSpeed() );
								}
							}
						}

						{	// rotation
							float yawDiff = HiroMathExt.ShortestIncrement( HiroMathExt.GetYaw( mRigidbody.transform.forward ), HiroMathExt.GetYaw( Camera.main.transform.forward ) );
							float pitchDiff = HiroMathExt.ShortestIncrement( HiroMathExt.GetPitch( mRigidbody.transform.forward ), HiroMathExt.GetPitch( Camera.main.transform.forward ) );
							//Debug.Log( rigidbody.rotation.eulerAngles.y + " -> " + Camera.main.transform.rotation.eulerAngles.y + "; " + diff );
							Quaternion deltaRotation = Quaternion.Euler( new Vector3( 0, yawDiff, 0 ) );

							Quaternion toUprightQuat;
							if( mRigidbody.CreateUprightTorque( out toUprightQuat, 2f, 5f ) ) {
								deltaRotation = deltaRotation * (Quaternion.Inverse( mRigidbody.rotation ) * toUprightQuat);
								//mRigidbody.MoveRotation( toUprightQuat );
							}

							mRigidbody.MoveRotation( Quaternion.RotateTowards( mRigidbody.rotation, mRigidbody.rotation * deltaRotation, Time.deltaTime * 400f ) );
						}
					}
				}
				break;

			case State.KnockedBack: {
					if( grounded ) {	// landed
						mStunTime = NetworkTime.time + 1f; // a lilo more
						mState = State.KnockBackLanded;
					}
				}
				break;

			case State.KnockBackLanded: {
					bool controllable = mStunTime < NetworkTime.time;
					if( controllable ) {
						mState = State.None;
					}
				}
				break;

			case State.BlownAway: {
					// Jump
					if( mReviveTimer < NetworkTime.time ) {
						if( iSJumpKeyPressed ) {
							iSJumpKeyPressed = false;

							if( !kinematic ) {
								SetKinematic( true );
								_kinematic = true;
								mState = State.Reviving;
								mRigidbody.AddForce( new Vector3( 0, 10, 0 ), ForceMode.Impulse );
								mReviveTime = NetworkTime.time + mReviveTotalTime;

								mReviveStartQuat = transform.rotation;

								Vector3 tag = transform.rotation.eulerAngles;
								tag.z = 0f;
								tag.x = 0f;

								mReviveEndQuat = Quaternion.Euler( tag );
							}
						}
					}
				}
				break;

			case State.Reviving: {
					// for dbg
					if( false && iSJumpKeyPressed ) {
						iSJumpKeyPressed = false;

						mRigidbody.velocity = Vector3.zero;
						transform.position = new Vector3( 568f, 360f, 2f );
					}

					//mReviveTime = NetworkTime.time + mReviveTotalTime;	// 110 = 10 + 100
					// 110 - 10 = 100
					float t = (float)(mReviveTotalTime - mReviveTime + NetworkTime.time);
					if( mReviveTotalTime < t ) {
						t = mReviveTotalTime;
					}
					//Debug.Log( $"{t}->{mReviveTotalTime}" );

					mRigidbody.angularVelocity = new Vector3( 0, 0, 0 );
					transform.rotation = Quaternion.LerpUnclamped( mReviveStartQuat, mReviveEndQuat, CustomEase.EaseFunc.QuadInOut( t, 0f, 1f, mReviveTotalTime ) );

					if( grounded ) {	// landed
						if( mReviveTotalTime <= t ) {
							mState = State.None;
						}
					}
				}
				break;
		}

		//if( Input.GetButtonDown( "sendFwd" ) ) {
		//	KnockBack( camForward );
		//}
		//if( Input.GetButtonDown( "sendBwd" ) ) {
		//	KnockBack( -camForward );
		//}
		//if( Input.GetButtonDown( "sendToLeft" ) ) {
		//	KnockBack( -Camera.main.transform.right.Y( 0f ).normalized );
		//}
		//if( Input.GetButtonDown( "sendToRight" ) ) {
		//	KnockBack( Camera.main.transform.right.Y( 0f ).normalized );
		//}

		//if( Input.GetButtonDown( "dbgPosReset" ) || Input.GetButtonDown( "dbgRecover" ) ) {
		//	if( Input.GetButtonDown( "dbgPosReset" ) ) {
		//		mRigidbody.velocity = Vector3.zero;
		//		transform.position = new Vector3( 109.45f, 1.55f, 114f );
		//	}

		//	if( !kinematic ) {
		//		kinematic = true;
		//		mState = State.Reviving;
		//		mRigidbody.AddForce( new Vector3( 0, 10, 0 ), ForceMode.Impulse );
		//		mReviveTime = NetworkTime.time + mReviveTotalTime;

		//		mReviveStartQuat = transform.rotation;

		//		Vector3 tag = transform.rotation.eulerAngles;
		//		tag.z = 0f;
		//		tag.x = 0f;

		//		mReviveEndQuat = Quaternion.Euler( tag );
		//	}
		//}

		//for( int i = (int)KeyCode.Alpha0; i < (int)KeyCode.Alpha9; i++ ) {
		//	if( Input.GetKeyDown( (KeyCode)i ) ) {
		//		mRigidbody.drag = i - (int)KeyCode.Alpha0;
		//	}
		//}

		//{
		//	GameObject explosionGo = GameObject.Find( "Explosion" );
		//	if( explosionGo != null ) {
		//		//RbcExplosionController ec = explosionGo.GetComponent<RbcExplosionController>();
		//		//if( ec != null ) {
		//		//	float radius, reverseDist, force, upMod;
		//		//	ec.Dbg( transform, out radius, out reverseDist, out force, out upMod );
		//		//}
		//	}
		//}

		if( !mRigidbody.useGravity ) {
			// We apply gravity manually for more tuning control
			mRigidbody.AddForce( new Vector3( 0, -gravity * mRigidbody.mass, 0 ) );
		}

		if( .1f < mRigidbody.drag ) {
			mRigidbody.drag = FInterpTo( mRigidbody.drag, .1f, Time.deltaTime, 6f );
			//Debug.Log( mRigidbody.drag );
		}

		grounded = false;
	}

	//void FixedUpdate() {
	//	if( mAutoUserControl ) {
	//		UserControl();
	//	}
	//}

	// Apply a force that attempts to reach our target velocity
	void SetForce( ref Vector3 force, float Y, float velClamp = 0f ) {
		Vector3 velocityChange = force - mRigidbody.velocity;
		if( 0f < velClamp ) {
			velocityChange.x = Mathf.Clamp( velocityChange.x, -velClamp, velClamp );
			velocityChange.z = Mathf.Clamp( velocityChange.z, -velClamp, velClamp );
		}
		velocityChange.y = Y;

		mRigidbody.AddForce( velocityChange, ForceMode.VelocityChange );
	}
	


	public void KnockBack( Vector3 normalizedDir ) {
		if( mState == State.BlownAway ) {
			// got beat down or sent flying
			return;
		}
		mStunTime = NetworkTime.time + 3f;
		mState = State.KnockedBack;

		normalizedDir *= 5;
		normalizedDir.y = 2f;

		SetForce( ref normalizedDir, 5f );
	}

	public void BlowAway( Vector3 blastOrigin, float force, float radius, float upwardsModifier ) {
		SetKinematic( false );
		_kinematic = false;
		mState = State.BlownAway;
		mReviveTimer = NetworkTime.time + 3f;
		mRigidbody.velocity = Vector3.zero;
		mRigidbody.AddExplosionForce( force, blastOrigin, radius, upwardsModifier, ForceMode.Impulse );
		mRigidbody.drag = 5f;
	}

	float mISJumpKeyPressed = 0f;
	bool iSJumpKeyPressed {
		get {
			if( 0f < mISJumpKeyPressed && Time.time < mISJumpKeyPressed ) {
				return true;
			}
			return false;
		}
		set {
			mISJumpKeyPressed = value ? Time.time + .05f : 0f;
		}
	}
	double mReviveTimer = 0f;
	void Update() {
		if( Input.GetButtonDown( "Jump" ) ) {
			iSJumpKeyPressed = true;
		}
	}

	void OnCollisionEnter( Collision collisionInfo ) {
		//OnCollisionEnter( collisionInfo );
	}

	void OnCollisionStay( Collision collisionInfo ) {
		//if( 0f < mRigidbody.velocity.y ) { return; }
		//for( int i = 0, len = collisionInfo.contacts.Length; i < len; i++ ) {
		//	Vector3 bottom = transform.position - new Vector3( 0, mCapsuleH / 2f - mCapsuleRadius, 0 );
		//	Vector3 diff = collisionInfo.contacts[i].point - bottom;
		//	float dot = Vector3.Dot( diff, -transform.up );

		//	if( 0f < dot /*&& diff.HorizontalDistance() <= mCapsuleRadius * 2f */) {
		//		//Debug.Log( collisionInfo.collider.name + ", dot: " + dot );

		//	}
		//}
		grounded = true;
	}
	//void OnDrawGizmos() {
	//	Vector3 bottom = transform.position - new Vector3( 0, mCapsuleH / 2f - mCapsuleRadius, 0 );
	//	Gizmos.color = Color.red;
	//	Gizmos.DrawCube( bottom, new Vector3( mCapsuleRadius * 2f, .1f, mCapsuleRadius * 2f ) );
	//}

	float CalculateJumpVerticalSpeed() {
		// From the jump height and gravity we deduce the upwards speed 
		// for the character to reach at the apex.
		return Mathf.Sqrt( 2 * jumpHeight * gravity );
	}
}