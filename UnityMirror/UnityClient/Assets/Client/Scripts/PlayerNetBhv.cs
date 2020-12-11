using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CameraController = TMPro.Examples.CameraController;
using Ext;

//https://mirror-networking.com/docs/Articles/Guides/NetworkBehaviour.html isServer, isClient, isLocalPlayer, hasAuthority, etc

//https://youtu.be/COLp1PzPKvE
//https://youtu.be/zMItcD4CgLA address sync issue for late joiner
//https://youtu.be/2JsqOvinnC0 animator sync
//https://youtu.be/6OGAv4HW7J8 child network object (anything that's not part of the main model, e.g. a gun?)
//https://youtu.be/gb6UawUUbss address cmd broadcast issue for late joiner
//https://youtu.be/NXvDuSw4KzA remove Authority on distonnect; so that item you are holding won't be destroyed on exit
//https://youtu.be/D4Pt6zS63nU sync Non-Networked Projectiles
//https://youtu.be/6S0uOxYiyio sync Bulk Scene Objects e.g. trees
//https://youtu.be/VyAksl01krk server console, kick a player, kickall, exit, send message, etc
//https://youtu.be/QVjedbOtO4o smooth sync vs flex tran network vs flex transfrom
//https://youtu.be/GT-o7vwE2oE Reactive Physics, small stones, car and other physics objects
//https://youtu.be/JC8DEJ9_p00 multiplayer world in single server
//https://youtu.be/uu4JNvTGL8o Collider Rollback, to correct collision for high pin users

//https://youtu.be/RKvfRr_7O2E Network Proximity Checker

// https://mirror-networking.com/docs/Articles/Guides/Communications/RemoteActions.html another way of doing damage

//https://github.com/vis2k/Mirror/tree/master/Assets/Mirror/Examples examples


public class PlayerNetBhv : NetworkBehaviour {

	[SyncVar( hook = nameof( SetColor ) )]
	public Color32 _color = Color.red;

	[SyncVar( hook = nameof( SetHp ) )]
	public int _health = 100;   // todo: not really need to syn in release build

	#if USE_RIGID
	[SyncVar( hook = nameof( SetIsRigidbody ) )]
	public bool _isRigidbody = false;   // todo: not really need to syn in release build
	Rigidbody mRigidbody;
	#endif

	//[SyncVar( hook = nameof( SetStuntTime ) )]
	double mServerStuntTime = double.MaxValue;
	bool mClientControlLock;
	Vector3 mSentFlyngHorzVec = Vector3.zero;

	public float mDbgMeleeInpact = 40f;
	public float mDbgMeleeImpactUp = 0.04f;

	public float mMoveSpd = 15f;
	public float mJumpHeight = 0.05f;
	public float mMeleeInpactCoolDnSpd = 10f;
	public float mMass = .5f;

	float mMeleeDuration = 0.3f;
	public SwordBhv mSwordBhv;
	public SphereCollider mMeleeCollider;
	bool mMeleeCmdEstablished = false;

	CharacterController mCharaController;
	CameraController mCamController;
	Vector3 mFwdLerp;
	MeleeColliderNetBhv mMeleeColliderNetBhv;

	TextMesh mDbgTxt;

	// constructor
	void Awake() {
		mCharaController = GetComponent<CharacterController>();
		mFwdLerp = this.transform.forward;

		mMeleeColliderNetBhv = mMeleeCollider.GetComponent<MeleeColliderNetBhv>();
		mMeleeColliderNetBhv.mOwner = this.gameObject;
		mMeleeCollider.gameObject.SetActive( false );

		mDbgTxt = GetComponentInChildren<TextMesh>( true );

		#if USE_RIGID
		mRigidbody = GetComponent<Rigidbody>();
		if( mRigidbody != null ) {
			_isRigidbody = false;
		}
		#endif
	}

	// need rebuild to reflect the changes
	public override void OnStartServer() {
		NetworkIdentity ni = GetComponent<NetworkIdentity>();
		Debug.Log( "OnStartServer: " + (ni == null ? "null" : ni.netId.ToString()) );

		base.OnStartServer();
		StartCoroutine( _RandomizeColor() );

		_health = 30;

		//InvokeRepeating( nameof( UpdateData ), 1, 1 );
	}

	// This only runs on the server, called from OnStartServer via InvokeRepeating
	//[ServerCallback]
	//void UpdateData() {
	//	playerData = Random.Range( 100, 1000 );
	//}

	public override void OnStartClient() {
		NetworkIdentity ni = GetComponent<NetworkIdentity>();
		Debug.Log( "OnStartClient: " + (ni == null ? "null" : ni.netId.ToString()) );

		if( base.isLocalPlayer ) {	// controlled player
			Camera cam = Camera.main;
			if( cam == null ) {
				cam = Camera.current;
			}
			if( cam == null && 0 < Camera.allCameras.Length ) {
				cam = Camera.allCameras[0];
			}
			if( cam != null ) {
				mCamController = cam.GetComponent<CameraController>();
				if( mCamController != null ) {
					mCamController.CameraTarget = this.transform;
					mCamController.CameraMode = CameraController.CameraModes.Isometric;
				}
				//Cursor.visible = false;
				Cursor.lockState = CursorLockMode.Confined;
				//Screen.fullScreenMode = FullScreenMode.MaximizedWindow;
			}
		}
		base.OnStartClient();
	}

	private void SetColor( Color32 oldColor, Color32 newColor ) {
		MeshRenderer mr = GetComponent<MeshRenderer>();
		if( mr != null ) {
			mr.material.SetColor( "_Color", newColor );
		}
	}

	private void SetHp( int oldHp, int newHp ) {
		if( base.isLocalPlayer ) {
			name = "*hp: " + newHp;
		} else {
			name = "hp: " + newHp;
		}

		if( mDbgTxt != null ) {
			if( base.isLocalPlayer ) {
				mDbgTxt.text = "*hp: " + newHp;
			} else {
				mDbgTxt.text = "hp: " + newHp;
			}
		}
	}

	private void SetStuntTime( float oldVal, float newVal ) {
		if( newVal == float.MaxValue ) {
			_color = Random.ColorHSV( 0f, 1f, 1f, 1f, 0f, 1f, 1f, 1f );
		} else {
			_color = Color.gray;
		}
	}


	#if USE_RIGID
	private void SetIsRigidbody( bool oldVal, bool enable ) {
		if( mRigidbody != null ) {
			_color = enable ? Color.gray : Random.ColorHSV( 0f, 1f, 1f, 1f, 0f, 1f, 1f, 1f );
			mCharaController.enabled = !enable;
			mRigidbody.isKinematic = !enable;
			//mRigidbody.detectCollisions = false;
			//mRigidbody.isKinematic = !isLocalPlayer;

			if( enable ) {
				RecoverTimer = NetworkTime.time + .3f;
			}

		} else {
			_color = Random.ColorHSV( 0f, 1f, 1f, 1f, 0f, 1f, 1f, 1f );
			mCharaController.enabled = true;
			mRigidbody.isKinematic = true;
			//mRigidbody.detectCollisions = false;
			//mRigidbody.isKinematic = !isLocalPlayer;
		}
	}
	#endif



		private IEnumerator _RandomizeColor() {
		WaitForSeconds wait = new WaitForSeconds( 2f );
		//while( true ) {
		yield return wait;
		_color = Random.ColorHSV( 0f, 1f, 1f, 1f, 0f, 1f, 1f, 1f );
		//}
	}



	float mYmomentum = 0f;
	bool mIsOnGround_canJump = false;   // because mCharaController.isGrounded is not reliable, more reliable way is to raycast ourselves

	bool mAutoMode = false;
	int mAutoModeIdx = 0;
	Vector3[] mAutoModeDest = {
		new Vector3(576f, 359f, 4f),
		new Vector3(581f, 361f, -2.6f),
	};

	Vector3 DesiredMoveVel( out Vector3 forward ) {
		if( Input.GetKeyDown( KeyCode.T ) ) {
			mAutoMode = !mAutoMode;
		}

		if( 0.2f < Vector3.Distance( mSentFlyngHorzVec, Vector3.zero ) /*&& !mIsOnGround_canJump*/ ) {
			Vector3 Org = mSentFlyngHorzVec;
			mSentFlyngHorzVec = Vector3.Lerp( mSentFlyngHorzVec, Vector3.zero, Time.deltaTime * mMeleeInpactCoolDnSpd );
			forward = mFwdLerp;
			return Org;
		} else {
			mClientControlLock = false;
		}

		if( mClientControlLock ) {
			forward = mFwdLerp;
			return Vector3.zero;
		}

		if( mAutoMode ) {
			for( int i = 0; i < mAutoModeDest.Length; i++ ) {
				mAutoModeDest[i].y = transform.position.y;
			}

			if( Vector3.Distance( transform.position, mAutoModeDest[mAutoModeIdx] ) < .3f ) {	// close to dest
				mAutoModeIdx++;
				mAutoModeIdx %= mAutoModeDest.Length;
			}

			forward = (mAutoModeDest[mAutoModeIdx] - transform.position).normalized;
			return forward * mMoveSpd;

		} else {
			float horizontalAxis = Input.GetAxis( "Horizontal" );
			float verticalAxis = Input.GetAxis( "Vertical" );

			//camera forward and right vectors:
			forward = mCamController.transform.forward.Y( 0f );
			Vector3 right = mCamController.transform.right.Y( 0f );

			//project forward and right vectors on the horizontal plane (y = 0)
			forward.Normalize();
			right.Normalize();

			//576.2232, 359.8878, 4.515179
			//581.0367, 361.5247, -2.621153

			//this is the direction in the world space we want to move:
			return (forward * verticalAxis + right * horizontalAxis) * mMoveSpd;
		}
	}

	private void Update() {
		if( mDbgTxt != null && Camera.main != null ) {
			mDbgTxt.transform.rotation = Quaternion.LookRotation( mDbgTxt.transform.position - Camera.main.transform.position );
		}

		if( isServer ) {	// server me update
			if( mServerStuntTime < NetworkTime.time ) {
				mServerStuntTime = double.MaxValue;

				RpcLockControl( false, Vector3.zero );	// release client lock
			}
		}


		// todo: not a controllable player but as long as it's moving...
		//PlayMoveSound();


		if( !isLocalPlayer || !base.hasAuthority ) {
			return;
		}

		bool exeJump = Input.GetButtonDown( "Jump" );
		bool exeMelee = Input.GetKeyDown( KeyCode.Mouse0 ) || Input.GetKeyDown( KeyCode.E ) || mMeleeCmdEstablished;

		#if USE_RIGID
		bool isSentflying = _isRigidbody;
		#else
		bool isSentflying = mClientControlLock;
		#endif
		




		Vector3 forward;
		Vector3 desiredMoveVel = DesiredMoveVel( out forward );
		desiredMoveVel.y = mYmomentum;
		if( mIsOnGround_canJump ) {
			mYmomentum = Physics.gravity.y * Time.deltaTime;
		} else {
			mYmomentum += Physics.gravity.y * Time.deltaTime * mMass;
		}





		if( mClientControlLock ) {
			//exeMelee = false;	// atk? allow for now
			exeJump = false;
			// https://medium.com/ironequal/unity-character-controller-vs-rigidbody-a1e243591483

			//public float GroundDistance = 0.2f;
			//public float DashDistance = 5f;
			//public LayerMask Ground;

			//private bool _isGrounded = true;
			//private Transform _groundChecker;

			//mCharaController.enabled = false;

			//_isGrounded = true;//Physics.CheckSphere( _groundChecker.position, GroundDistance, Ground, QueryTriggerInteraction.Ignore );

			//if( exeJump && _isGrounded ) {
			//	mRigidbody.AddForce( Vector3.up * Mathf.Sqrt( JumpHeight * -2f * Physics.gravity.y ), ForceMode.VelocityChange );
			//}
			////if( Input.GetButtonDown( "Dash" ) ) {
			////	Vector3 dashVelocity = Vector3.Scale( transform.forward, DashDistance * new Vector3( (Mathf.Log( 1f / (Time.deltaTime * mRigidbody.drag + 1) ) / -Time.deltaTime), 0, (Mathf.Log( 1f / (Time.deltaTime * mRigidbody.drag + 1) ) / -Time.deltaTime) ) );
			////	mRigidbody.AddForce( dashVelocity, ForceMode.VelocityChange );
			////}

			//desiredMoveVel += Physics.gravity;
			//desiredMoveVel *= mRigidbody.drag;

			//mRigidbody.AddForce( desiredMoveVel, ForceMode.VelocityChange );
		}

		if( mCharaController != null ) {

			// melee button
			if( exeMelee ) {
				if( mSwordBhv.isReady ) {
					mMeleeCmdEstablished = false;
					CmdMeleeAtk();
					MeleeAtk();
				} else {
					mMeleeCmdEstablished = true;
				}
			}

			// jump button
			if( mIsOnGround_canJump ) {
				PlayMoveSound();

				if( exeJump ) {
					mIsOnGround_canJump = false;
					mYmomentum = Mathf.Sqrt( mJumpHeight * -2f * Physics.gravity.y );
					desiredMoveVel.y = mYmomentum;
				}

			} else {
				if( mCharaController.isGrounded ) {
					mIsOnGround_canJump = true;
				}
			}


			// move
			desiredMoveVel.x *= Time.deltaTime;
			desiredMoveVel.z *= Time.deltaTime;
			mCharaController.Move( desiredMoveVel );

			// rotation
			mFwdLerp = Vector3.Slerp( mFwdLerp, forward, Time.deltaTime * 30f );
			this.transform.LookAt( this.transform.position + mFwdLerp );
		}
	}







	//https://mirror-networking.com/docs/Articles/Guides/Communications/RemoteActions.html

	// me to server me
	[Command]
	void CmdMeleeAtk() {
		mMeleeColliderNetBhv.Fire( mMeleeDuration );
		RpcMeleeAtk();
	}

	void MeleeAtk() {
		mSwordBhv.Fire( mMeleeDuration );
		PlayMeleeAtkSound();
	}

	// server me to me in other clients
	[ClientRpc( excludeOwner = true )]
	void RpcMeleeAtk() {
		MeleeAtk();
	}

	// client only method
	[Client]
	void PlayMeleeAtkSound() {
		#if !UNITY_SERVER
		#endif
	}

	// called by server collider
	[Server]
	public void OnTakenDamage( int damage, Vector3? impactSource = null, float impactHorzStrength = 0f ) {
		_health -= damage;

		if( impactSource != null && 0f < impactHorzStrength ) {
			mServerStuntTime = NetworkTime.time + 3f;	// set server stunt time

			// create horizontal impact force
			Vector3 force = ( transform.position - impactSource.Value ).Y(0f).normalized * impactHorzStrength;

			RpcLockControl( true, force );
			#if USE_RIGID
			_isRigidbody = true;
			#endif
		}
	}

	// server me to client me
	[TargetRpc]
	void RpcLockControl( bool lockit, Vector3 impact ) {
		mClientControlLock = lockit;

		if( impact != Vector3.zero ) {

			if( false ) {
				impact = impact.Y( 0f ).normalized * mDbgMeleeInpact;
				if( 2f < mCharaController.velocity.y ) {	// flying upwards?
				} else {
					//impact.y = Mathf.Sqrt( mDbgMeleeInpactUp * -2f * Physics.gravity.y );
				}
			}

			mSentFlyngHorzVec = impact;
			mSentFlyngHorzVec.y = 0f;

			if( 0f < impact.y ) {
				mIsOnGround_canJump = false;
				//mCharaController.velocity.y = 0f;
				Debug.Log( "mYmomentum(" + mYmomentum + ") = impact.y(" + impact.y + "); cc.vel.y("+ mCharaController.velocity.y + ")" );
				mYmomentum = impact.y;
				if( mCharaController != null ) {
				}
			}
		}
	}



	// sent from objects on the server to objects on clients.
	[ClientRpc/*( excludeOwner = true )*/]
	void RpcFireWeapon() {
		//// generate bullets
		////ClientScene.RegisterPrefab()
		//GameObject bulletPrefab = NewNetworkManager.singleton.spawnPrefabs[0];
		////Mirror.spawn

		////bulletAudio.Play(); muzzleflash  etc
		//var bullet = (GameObject)Instantiate( bulletPrefab, this.transform.position + mCamController.transform.forward * 20f, Quaternion.identity );
		////bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * activeWeapon.weaponSpeed;
		//if( bullet ) { Destroy( bullet, activeWeapon.weaponLife ); }
	}





	[Command]
	void CmdDropCube() {
		GameObject cubePrefab = null;
		if( cubePrefab != null ) {
			Vector3 spawnPos = transform.position + transform.forward * 2;
			Quaternion spawnRot = transform.rotation;
			GameObject cube = Instantiate( cubePrefab, spawnPos, spawnRot );
			NetworkServer.Spawn( cube );
		}
	}




	// client only method
	[Client]
	void PlayMoveSound() {
		#if !UNITY_SERVER
		// foot step playback
		#endif
	}
}