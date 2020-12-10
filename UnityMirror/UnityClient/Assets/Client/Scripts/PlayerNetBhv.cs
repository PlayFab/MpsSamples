using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CameraController = TMPro.Examples.CameraController;

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

public class PlayerNetBhv : NetworkBehaviour {

	[SyncVar( hook = nameof( SetColor ) )]
	public Color32 _color = Color.red;

	[SyncVar( hook = nameof( SetHp ) )]
	public int _health = 100;	// todo: not really need to syn in release build

	public float moveSpd = 15f;
	public float JumpHeight = 2f;
	float mMeleeDuration = 0.3f;
	public SwordBhv mSwordBhv;
	public SphereCollider mMeleeCollider;
	bool mMeleeCmdEstablished = false;

	CharacterController mCharaController;
	Rigidbody mRigidbody;
	CameraController mCamController;
	Vector3 mFwdLerp;
	MeleeColliderNetBhv mMeleeColliderNetBhv;

	TextMesh mDbgTxt;

	// constructor
	void Awake() {
		mCharaController = GetComponent<CharacterController>();
		mRigidbody = GetComponent<Rigidbody>();
		mFwdLerp = this.transform.forward;

		mMeleeColliderNetBhv = mMeleeCollider.GetComponent<MeleeColliderNetBhv>();
		mMeleeColliderNetBhv.mOwner = this.gameObject;
		mMeleeCollider.gameObject.SetActive( false );

		mDbgTxt = GetComponentInChildren<TextMesh>( true );

		if( mRigidbody != null ) {
			mRigidbody.detectCollisions = false;
			mRigidbody.isKinematic = !isLocalPlayer;
		}
	}

	// need rebuild to reflect the changes
	public override void OnStartServer() {
		NetworkIdentity ni = GetComponent<NetworkIdentity>();
		Debug.Log( "OnStartServer: " + (ni == null ? "null" : ni.netId.ToString()) );

		base.OnStartServer();
		StartCoroutine( _RandomizeColor() );

		_health = 30;
	}

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
		
		_health = newHp;
	}


	private IEnumerator _RandomizeColor() {
		WaitForSeconds wait = new WaitForSeconds( 2f );
		//while( true ) {
		yield return wait;
		_color = Random.ColorHSV( 0f, 1f, 1f, 1f, 0f, 1f, 1f, 1f );
		//}
	}



	float mYmomentum = 0f;
	bool mIsOnGround_canJump = false;	// because mCharaController.isGrounded is not reliable, more reliable way is to raycast ourselves

	Vector3 DesiredMoveVel( out Vector3 forward ) {
		float horizontalAxis = Input.GetAxis( "Horizontal" );
		float verticalAxis = Input.GetAxis( "Vertical" );

		//camera forward and right vectors:
		forward = mCamController.transform.forward;
		Vector3 right = mCamController.transform.right;

		//project forward and right vectors on the horizontal plane (y = 0)
		forward.y = 0f;
		right.y = 0f;
		forward.Normalize();
		right.Normalize();

		//this is the direction in the world space we want to move:
		return (forward * verticalAxis + right * horizontalAxis) * moveSpd * Time.deltaTime;
	}

	private void Update() {
		if( mDbgTxt != null && Camera.main != null ) {
			mDbgTxt.transform.rotation = Quaternion.LookRotation( mDbgTxt.transform.position - Camera.main.transform.position );
		}

		if( base.hasAuthority && base.isLocalPlayer ) {
			if( mCamController != null ) {

				bool isSentflying = false;

				if( isSentflying ) {
					// https://medium.com/ironequal/unity-character-controller-vs-rigidbody-a1e243591483

					//public float GroundDistance = 0.2f;
					//public float DashDistance = 5f;
					//public LayerMask Ground;

					//private bool _isGrounded = true;
					//private Transform _groundChecker;

					//mCharaController.enabled = false;

					//_isGrounded = true;//Physics.CheckSphere( _groundChecker.position, GroundDistance, Ground, QueryTriggerInteraction.Ignore );

					//if( Input.GetButtonDown( "Jump" ) && _isGrounded ) {
					//	mRigidbody.AddForce( Vector3.up * Mathf.Sqrt( JumpHeight * -2f * Physics.gravity.y ), ForceMode.VelocityChange );
					//}
					////if( Input.GetButtonDown( "Dash" ) ) {
					////	Vector3 dashVelocity = Vector3.Scale( transform.forward, DashDistance * new Vector3( (Mathf.Log( 1f / (Time.deltaTime * mRigidbody.drag + 1) ) / -Time.deltaTime), 0, (Mathf.Log( 1f / (Time.deltaTime * mRigidbody.drag + 1) ) / -Time.deltaTime) ) );
					////	mRigidbody.AddForce( dashVelocity, ForceMode.VelocityChange );
					////}

					//desiredMoveVel += Physics.gravity;
					//desiredMoveVel *= mRigidbody.drag;

					//mRigidbody.AddForce( desiredMoveVel, ForceMode.VelocityChange );

				} else {
					Vector3 forward;
					Vector3 desiredMoveVel = DesiredMoveVel( out forward );

					if( Input.GetKeyDown( KeyCode.Mouse0 ) || Input.GetKeyDown( KeyCode.E ) || mMeleeCmdEstablished ) {
						if( mSwordBhv.isReady ) {
							mMeleeCmdEstablished = false;
							CmdMeleeAtk();
							MeleeAtk();
							PlayAtkSound();
						} else {
							mMeleeCmdEstablished = true;
						}
					}

					if( mIsOnGround_canJump ) {
						if( Input.GetButtonDown( "Jump" ) ) {
							mIsOnGround_canJump = false;
							mYmomentum = Mathf.Sqrt( JumpHeight * -2f * Physics.gravity.y );
						} else {
							mYmomentum = Physics.gravity.y * Time.deltaTime;
						}
						desiredMoveVel.y = mYmomentum;
						mCharaController.Move( desiredMoveVel );

						PlayMoveSound();

					} else {
						mYmomentum += Physics.gravity.y * Time.deltaTime;

						if( mCharaController.isGrounded ) {
							mIsOnGround_canJump = true;
						}

						desiredMoveVel.y = mYmomentum;
						mCharaController.Move( desiredMoveVel );
					}

					mFwdLerp = Vector3.Slerp( mFwdLerp, forward, Time.deltaTime * 30f );
					this.transform.LookAt( this.transform.position + mFwdLerp );
				}

			}

		} else {
			// not a controllable player but as long as it's moving...
			//PlayMoveSound();
		}
	}







	//https://mirror-networking.com/docs/Articles/Guides/Communications/RemoteActions.html

	// client only method
	[Client]
	void PlayAtkSound() {
		#if !UNITY_SERVER
		#endif
	}

	// sent from player objects on the client to player objects on the server
	// server collision
	[Command] void CmdMeleeAtk() {
		mMeleeColliderNetBhv.Fire( mMeleeDuration );
	}

	// client animation
	void MeleeAtk() {
		mSwordBhv.Fire( mMeleeDuration );
	}

	// called by server collider
	public void TakeDamage( int damage, bool sendFlying = false ) {
		_health -= damage;

		if( sendFlying ) {
			//Rigidbody rb = gameObject.GetComponent<Rigidbody>();
			//if( rb == null ) {
			//	rb = gameObject.AddComponent<Rigidbody>();
			//}
			//rb.isKinematic = false;
			//rb.detectCollisions = false;
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