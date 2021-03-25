using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CameraController = TMPro.Examples.CameraController;
using Ext;
using NetworkRigidbody = Mirror.Experimental.NetworkRigidbody;

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



	[Command]	// client to server
	void SetColor( Color clr ) {
		_color = clr;
	}

	[SyncVar( hook = nameof( Receiver_SetColor ) )]
	public Color32 _color = Color.gray;
	Color mCachedColor = Color.white;

	void Receiver_SetColor( Color32 oldColor, Color32 newColor ) {
		MeshRenderer mr = GetComponent<MeshRenderer>();
		if( mr != null ) {
			mr.material.SetColor( "_Color", newColor );
		}
		if( mCachedColor == Color.white ) {
			mCachedColor = newColor;
		}
	}





	[Command]   // client to server
	void Server_SetHealth( int value ) {
		_health = value;
	}

	[SyncVar( hook = nameof( Client_ReceiveHp ) )]
	public int _health = 100;	// todo: not really need to syn in release build

	private void Client_ReceiveHp( int oldHp, int newHp ) {
		if( base.isLocalPlayer ) {
			name = "loc.hp: " + newHp;
		} else {
			name = "hp: " + newHp;
		}
		UpdateDbgTxt();
	}






	[SyncVar( hook = nameof( Client_ReceiveAtkCnt ) )]
	int aktCnt = 0;

	private void Client_ReceiveAtkCnt( int oldCnt, int newCnt ) {
		UpdateDbgTxt();
	}





	// to check if server and client are align
	int stepCnt = 0;
	int _stepCnt = 0;



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
	RigidbodyController mRigController;
	CameraController mCamController;
	Vector3 mFwdLerp;
	MeleeColliderNetBhv mMeleeColliderNetBhv;

	TextMesh mDbgTxt;

	[SerializeField] GameObject mBombPrefab;
	double mBombCoolDn = 0f;

	[SerializeField] GameObject mDustStormPrefab;
	Transform mDustTran;


	enum AIMode {
		None,
		Patrol,
		MadAttack,
		God,
	}

	AIMode mAIMode = AIMode.None;

	// constructor
	void Awake() {
		mCharaController = GetComponent<CharacterController>();
		mRigController = GetComponent<RigidbodyController>();

		if( mRigController != null ) {
			mRigController.mAutoUserControl = false;
			NetworkRigidbody nrb = GetComponent<NetworkRigidbody>();
		}

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

		Physics.IgnoreCollision( mSwordBhv.GetComponent<Collider>(), GetComponent<Collider>() );
	}

	// need rebuild to reflect the changes
	public override void OnStartServer() {
		NetworkIdentity ni = GetComponent<NetworkIdentity>();
		Debug.Log( "OnStartServer: " + (ni == null ? "null" : ni.netId.ToString()) );

		base.OnStartServer();
		StartCoroutine( OnStartServer_RandomizeColor() );

		_health = 30;

		//InvokeRepeating( nameof( UpdateData ), 1, 1 );

		mAIMode = AIMode.God;
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
			GameObject dustGo = GameObject.Instantiate<GameObject>( mDustStormPrefab, transform.root, false );
			dustGo.transform.localPosition = Vector3.zero;
			var p = dustGo.GetComponent<ParticleSystem>();
			ParticleSystem.MainModule m = p.main;
			m.simulationSpeed = .3f;
			mDustTran = dustGo.transform;

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

	void UpdateDbgTxt() {
		if( mDbgTxt != null ) {
			if( isServer ) {
				mDbgTxt.text = "god: -";
			} else {
				mDbgTxt.text = "hp: " + _health + "; " + aktCnt + "/" + stepCnt;
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



	// called by server
	private IEnumerator OnStartServer_RandomizeColor() {
		WaitForSeconds wait = new WaitForSeconds( .1f );
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

	private void FixedUpdate() {
		if( !isLocalPlayer || !base.hasAuthority ) {
			return;
		}

		if( mRigController != null && !mRigController.mAutoUserControl ) {
			mRigController.UserControl();
		}
	}

	bool isK = false;
	private void Update() {
		if( stepCnt != _stepCnt ) {
			_stepCnt = stepCnt;
			UpdateDbgTxt();
		}

		if( mRigController != null ) {
			if( !isLocalPlayer || !base.hasAuthority ) {
			} else {
				if( isK != mRigController._kinematic ) {
					isK = mRigController._kinematic;
					SetColor( isK ? mCachedColor : Color.gray );
					stepCnt++;
				}
			}
		}

		var proxi = GetComponent<NetworkProximityChecker>();
		if( proxi != null ) {
			HiroExtensions.HiroVlogExt.DrawWiredSphere( transform.position, proxi.visRange, Color.red );
		}

		if( mDbgTxt != null && Camera.main != null ) {
			mDbgTxt.transform.rotation = Quaternion.LookRotation( mDbgTxt.transform.position - Camera.main.transform.position );
		}

		if( mRigController != null && !mRigController.mAutoUserControl ) {

			if( !isLocalPlayer || !base.hasAuthority ) {
				return;
			}

			if( Input.GetKeyDown( KeyCode.Alpha1 ) ) {
				mAIMode = AIMode.None;
			}
			if( Input.GetKeyDown( KeyCode.Alpha2 ) ) {
				mAIMode = AIMode.Patrol;
			}
			if( Input.GetKeyDown( KeyCode.Alpha3 ) ) {
				mAIMode = AIMode.MadAttack;
			}

			if( mRigController.mState == RigidbodyController.State.None ) {
				bool exeMelee = Input.GetKeyDown( KeyCode.Mouse0 ) || Input.GetKeyDown( KeyCode.E );

				bool exeBomb = Input.GetKeyDown( KeyCode.Mouse1 ) || Input.GetKeyDown( KeyCode.R );

				if( mAIMode == AIMode.MadAttack ) {
					exeMelee = true;

				} else if( mAIMode == AIMode.God ) {
					Vector3 diff = (new Vector3( 550f, 369f, -15f ) - transform.position).Y( 0f );
					if( .5f < diff.magnitude ) {
						mRigController.mAiWalkVec = diff.normalized;
					}
				}

				// melee button
				if( exeMelee ) {
					if( mSwordBhv.isReady ) {
						CmdMeleeAtk();
						MeleeAtk();
					}
				}
				if( exeBomb ) {
					if( mBombCoolDn < NetworkTime.time ) {
						mBombCoolDn = NetworkTime.time + .5f;
						CmdBomb();
					}
				}
			}

			if( mDustTran != null ) {
				mDustTran.rotation = Quaternion.identity;
			}
		}








		if( false && mCharaController != null ) {
			if( isServer ) {    // server me update
				if( mServerStuntTime < NetworkTime.time ) {
					mServerStuntTime = double.MaxValue;

					RpcLockControl( false, Vector3.zero );  // release client lock
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





	// me to server me
	[Command]
	void CmdBomb() {
		Vector3 SpawnPos = (mMeleeCollider.transform.position + transform.position) / 2f;
		GameObject bombClone = GameObject.Instantiate<GameObject>( mBombPrefab, SpawnPos, Quaternion.identity );
		NetworkServer.Spawn( bombClone );
		bombClone.GetComponent<Rigidbody>().velocity = transform.forward * 10f + new Vector3( 0, 4, 0 );
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
	public void OnTakenDamage( int damage, Vector3? impactSource = null, float impactHorzStrength = 0f ) {
		if( !isServer )
			return;

		_health -= damage;

		if( impactSource != null && 0f < impactHorzStrength ) {
			RcpTakeDamage( damage, impactSource.Value, impactHorzStrength );
		}
	}

	[ClientRpc]
	void RcpTakeDamage( int damage, Vector3 impactSource, float impactHorzStrength ) {
		if( mRigController != null ) {
			Vector3 force = (transform.position - impactSource).Y( 0f ).normalized;
			mRigController.KnockBack( force );
		}

		if( mCharaController != null ) {
			mServerStuntTime = NetworkTime.time + 3f;   // set server stunt time

			// create horizontal impact force
			Vector3 force = (transform.position - impactSource).Y( 0f ).normalized * impactHorzStrength;

			RpcLockControl( true, force );
			#if USE_RIGID
				_isRigidbody = true;
			#endif
		}

	}


	// called by server collider
	public void OnBlownAway( BombColliderNetBhv bombCollider, int damage, Vector3? impactSource = null ) {
		if( !isServer )
			return;

		_health -= damage;

		if( mAIMode != AIMode.God && impactSource != null ) {
			float force, radius, upMod, reverseDist;
			Dbg( this.transform, bombCollider.transform, out radius, out reverseDist, out force, out upMod );

			RpcOnBlownAway( force, radius, upMod, impactSource.Value );
		}
	}

	[ClientRpc]
	void RpcOnBlownAway( float force, float radius, float upMod, Vector3 impactSource ) {
		if( mAIMode != AIMode.God && mRigController != null ) {
			mRigController.BlowAway(
				blastOrigin: impactSource,
				force: force,
				radius: radius,
				upwardsModifier: upMod
			);
		}
	}

	public void Dbg( Transform ccTran, Transform bombTran, out float blastRadius, out float reverseDist, out float force, out float upMod ) {
		blastRadius = bombTran.localScale.z * .5f;  //bombTran.GetComponent<SphereCollider>().radius;
		MeshRenderer bombCollider = bombTran.GetComponent<MeshRenderer>();
		if( bombCollider != null ) {
			blastRadius = Mathf.Max( bombCollider.bounds.size.x, Mathf.Max( bombCollider.bounds.size.x, bombCollider.bounds.size.z ) );
		}

		Vector3 tagLoc = ccTran.position;
		tagLoc.y = bombTran.position.y;

		float ccRadius = 0f;
		CapsuleCollider cc = ccTran.GetComponent<CapsuleCollider>();
		if( cc != null ) {
			ccRadius = cc.radius;
		}

		float dist = Mathf.Clamp( (Vector3.Distance( bombTran.position, tagLoc ) - ccRadius - blastRadius * .25f) / blastRadius, 0f, 1f );
		reverseDist = 1f - dist;

		force = CustomEase.EaseFunc.QuintIn( reverseDist, 30f/*further*/, 20f/*closer*/ - 30f, 1f );
		upMod = CustomEase.EaseFunc.QuintIn( reverseDist, 2f/*further*/, 1f/*closer*/ - 2f, 1f );

		//force = 70f;
		//upMod = 1f;

		Debug.Log( $"ccRadius{ccRadius}; blastRadius({blastRadius.ToString( "F2" )}); dist({dist.ToString( "F2" )}), revDist({reverseDist.ToString( "F2" )}), upMod({upMod.ToString( "F2" )}), force({force})" );
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