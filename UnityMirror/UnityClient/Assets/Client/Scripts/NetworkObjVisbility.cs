using UnityEngine;
using System.Collections.Generic;

namespace Mirror {
	// Attach this to objects that need their visibility updated as the player moves around
	[RequireComponent( typeof( NetworkIdentity ) )]
	public class NetworkObjVisbility : NetworkVisibility {

		/// <summary>
		/// The maximim range that objects will be visible at.
		/// </summary>
		[Tooltip( "The maximum range that objects will be visible at." )]
		public int visRange = 10;

		public List<NetworkConnection> playersObserving = new List<NetworkConnection>();
		public NetworkIdentity networkIdentity;

		void Awake() {
			networkIdentity = GetComponent<NetworkIdentity>();
		}

		public override void OnRebuildObservers( HashSet<NetworkConnection> observers, bool initialize ) {
			foreach( NetworkConnection net in playersObserving ) {
				observers.Add( net );
			}
		}

		public override bool OnCheckObserver( NetworkConnection conn ) {
			//return false;   // system call, and false, so the other guy cannot see me
			return Vector3.Distance( conn.identity.transform.position, transform.position ) < visRange;
		}
	}
}
