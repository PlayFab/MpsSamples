using UnityEngine;
using System.Collections.Generic;

namespace Mirror {
	// Attach this to player prefab
	[RequireComponent( typeof( SphereCollider ) )]
	public class NetworkPlayerVisibility : NetworkBehaviour {

		[SerializeField]
		private int visRadius = 50; // Radius of sphere collider
		[SerializeField]
		private float visUpdateInterval = 2000; // Update time in ms
		private SphereCollider mcollider;
		private List<NetworkObjVisbility> changedObjects = new List<NetworkObjVisbility>(); // Objects that have changed visibility
		private float visUpdateTime;

		void Awake() {
			mcollider = GetComponent<SphereCollider>();
			mcollider.isTrigger = true;
			mcollider.radius = visRadius;
		}

		void Update() {
			if( !NetworkServer.active )
				return;

			if( Time.time - visUpdateTime > visUpdateInterval ) {
				//RebuildChangedObjects();
				{
					foreach( NetworkObjVisbility net in changedObjects ) {
						net.networkIdentity.RebuildObservers( false );
					}
					changedObjects.Clear();
				}
				visUpdateTime = Time.time;
			}
		}

		void OnTriggerEnter( Collider col ) {
			NetworkObjVisbility net = col.GetComponent<NetworkObjVisbility>();
			if( net != null && connectionToClient != null ) {
				net.playersObserving.Add( connectionToClient );
				changedObjects.Add( net );
			}
		}

		void OnTriggerExit( Collider col ) {
			NetworkObjVisbility net = col.GetComponent<NetworkObjVisbility>();
			if( net != null && connectionToClient != null ) {
				net.playersObserving.Remove( connectionToClient );
				changedObjects.Add( net );
			}
		}

		// Use these to update radius and interval in game
		public void SetVisualRadius( int radius ) {
			visRadius = radius;
			mcollider.radius = radius;
		}

		public void SetUpdateInterval( float interval ) {
			visUpdateInterval = interval;
		}

	}
}