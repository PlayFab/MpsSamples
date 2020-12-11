//using System.Collections;
//using System.Collections.Generic;
using UnityEngine;

namespace Ext {

	public static class Math {
		public static Vector3 Y( this Vector3 val, float y ) {
			val.y = y;
			return val;
		}
	}

	public class Ease {
		public static float InOutQuad( float t, float b, float c, float d ) {
			if( (t /= d / 2) < 1 )
				return ((c / 2) * (t * t)) + b;
			return -c / 2 * (((t - 2) * (--t)) - 1) + b;
		}
	}

}
