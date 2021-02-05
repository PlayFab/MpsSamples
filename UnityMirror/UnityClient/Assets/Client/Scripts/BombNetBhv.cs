using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class BombNetBhv : NetworkBehaviour {
	BombColliderNetBhv mColliderNetBhv;

	void Awake() {
		mColliderNetBhv = GetComponentInChildren<BombColliderNetBhv>( true );
		mColliderNetBhv.mOwner = gameObject;
	}

	public void Damaged() {
		mColliderNetBhv.Fire( .1f );
	}
}
