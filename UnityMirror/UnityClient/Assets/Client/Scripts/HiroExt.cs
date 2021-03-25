#define MAKE_SURE_DATA_IDENTICAL
#define PATH_FIND_DBG_DRAW

//using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HiroExtensions {
	public class HiroClr {
		static Color mPurple = new Color( 1f, 0f, 1f );
		public static Color purple { get { return mPurple; } }
		static Color mDarkGray = new Color32( 91, 91, 91, 255 );
		public static Color darkGray { get { return mDarkGray; } }
		public static string darkGrayStr { get { return "5b5b5b"; } }
	}

	/// <summary>
	/// start of Vlog Extensions
	/// </summary>
	public class HiroVlogExt : MonoBehaviour {
		public static GameObject mGo = null;
		static void Init() {
			if( mGo == null ) {
				mGo = (GameObject)GameObject.CreatePrimitive( PrimitiveType.Cube );
				mGo.name = "VlogExt";
				mGo.AddComponent<HiroVlogExt>();
				mGo.transform.position = new Vector3( -9999f, -9999f, -9999f );
			}
		}

		void OnDrawGizmos() {
			for( int i = 0, len = mVlogs.Count; i < len; i++ ) {
				if( mVlogs[i].Draw( i ) ) {	// can remove
					mVlogs.RemoveAt( i );
					i--;
					len--;
				}
			}
		}



		class BaseVLog {
			public Vector3 mPos;
			public Color mClr;
			public float mDuration;
			virtual public bool Draw( int i ) { return true; }	// true: can remove

			protected float GetGizmoSize( Vector3 pos ) {
				Camera current = Camera.current;
				if( current ) {
					pos = Gizmos.matrix.MultiplyPoint( pos );

					Transform transform = current.transform;
					Vector3 position2 = transform.position;
					float z = Vector3.Dot( pos - position2, transform.TransformDirection( new Vector3( 0f, 0f, 1f ) ) );
					Vector3 a = current.WorldToScreenPoint( position2 + transform.TransformDirection( new Vector3( 0f, 0f, z ) ) );
					Vector3 b = current.WorldToScreenPoint( position2 + transform.TransformDirection( new Vector3( 1f, 0f, z ) ) );
					float magnitude = (a - b).magnitude;
					return 80f / Mathf.Max( magnitude, 0.0001f );
				}
				return 20f;
			}
			protected float GetHandleSize( Vector3 pos ) {
				#if UNITY_EDITOR
				return UnityEditor.HandleUtility.GetHandleSize( pos );
				#else
				return GetGizmoSize( pos );
				#endif
			}



			protected void DrawArrow( Vector3 pos, Vector3 vec, Color clr, float capSize, bool constantCap ) {
				if( vec.magnitude == 0f ) { return; }
				#if UNITY_EDITOR
				bool sameClr = UnityEditor.Handles.color == clr;
				if( !sameClr ) {
					UnityEditor.Handles.color = clr;
				}

				Debug.DrawRay( pos, vec, clr );

				//Debug.DrawRay( pos + dir, Vector3.up, Color.blue );
				//Debug.DrawRay( pos + dir, Vector3.down, Color.blue );
				//Debug.DrawRay( pos + dir, Vector3.left, Color.blue );
				//Debug.DrawRay( pos + dir, Vector3.right, Color.blue );
				//Debug.DrawRay( pos + dir, Vector3.forward, Color.blue );
				//Debug.DrawRay( pos + dir, Vector3.back, Color.blue );

				if( constantCap ) {
					float scaler = GetHandleSize( pos ) * capSize;
					UnityEditor.Handles.ConeCap( 0, HiroMathExt.AbsPosFromPt2( pos, pos + vec, scaler * .7f ), vec == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation( vec ), scaler );
				} else {
					UnityEditor.Handles.ConeCap( 0, HiroMathExt.AbsPosFromPt2( pos, pos + vec, capSize * .7f ), vec == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation( vec ), capSize );
				}

				if( !sameClr ) {
					UnityEditor.Handles.color = Color.white;
				}
				#endif
			}
		}
		static List<BaseVLog> mVlogs = new List<BaseVLog>();




		class Text : BaseVLog {
			public string mText;
			public TextAnchor mAlignment;
			public bool mBorder = false;

			static GUIStyle mStyle = null;
			static GUIStyle mBorderedStyle = null;
			Texture2D MakeTex( int width, int height, Color col ) {
				Color[] pix = new Color[width * height];
				for( int i = 0; i < pix.Length; ++i ) {
					pix[i] = col;
				}
				Texture2D result = new Texture2D( width, height );
				result.SetPixels( pix );
				result.Apply();
				return result;
			}
			override public bool Draw( int i ) {
				#if UNITY_EDITOR
				if( mBorderedStyle == null ) {
					mBorderedStyle = new GUIStyle( GUI.skin.button ) { richText = true, alignment = mAlignment };
					mBorderedStyle.normal.background = MakeTex( 1, 1, new Color( 0f, 0f, 0f, 0.2f ) );

					mStyle = new GUIStyle() { richText = true, alignment = mAlignment };
				}
				UnityEditor.Handles.Label( mPos, "<color=#" + mClr.ToHex() + ">" + mText + "</color>", mBorder ?mBorderedStyle: mStyle );
				#endif
				//UnityEditor.Handles.Label( mPos, "<color=#000000>" + mText + "</color>", new GUIStyle() { richText = true, alignment = mAlignment, fontSize = 13 } );
				return mDuration <= Time.time;
			}
		}
		public static void DrawText( Vector3 pos, string text, Color color, float duration = 0f, TextAnchor alignment = TextAnchor.MiddleCenter, bool border = false ){
			Init();
			mVlogs.Add( new Text() { mPos = pos, mText = text, mClr = color, mDuration = Time.time + duration, mAlignment = alignment, mBorder = border } );
		}




		class WiredSphere : BaseVLog {
			public float mRadius;

			override public bool Draw(int i ) {
				//UnityEditor.Handles.DrawSphere( 0, mPos, Quaternion.identity, mSize );
				Gizmos.color = mClr;
				Gizmos.DrawWireSphere( mPos, mRadius );
				Gizmos.color = Color.gray;
				return mDuration <= Time.time;
			}
		}
		public static void DrawWiredSphere( Vector3 pos, float radius, Color color, float duration = 0f ) {
			Init();
			mVlogs.Add( new WiredSphere() { mPos = pos, mRadius = radius, mClr = color, mDuration = Time.time + duration } );
		}



		class Arrow : BaseVLog {
			public Vector3 mVec;
			public float mCapSize = .3f;
			public bool mConstantCap = false;

			override public bool Draw( int i ) {
				DrawArrow( mPos, mVec, mClr, mCapSize, mConstantCap );
				return mDuration <= Time.time;
			}
		}
		public static void DrawArrow( Vector3 pos, Vector3 vec, Color color, float capSize = .2f, bool constantCap = false, float duration = 0f ) {
			Init();
			mVlogs.Add( new Arrow() { mPos = pos, mVec = vec, mClr = color, mDuration = Time.time + duration, mCapSize = capSize, mConstantCap = constantCap } );
		}



		class WiredArc : BaseVLog {
			public Vector3 mNormal;
			public Vector3 mFrom;
			public float mAngle;
			public float mRadius;

			override public bool Draw( int i ) {
				#if UNITY_EDITOR
				UnityEditor.Handles.color = mClr;
				UnityEditor.Handles.DrawWireArc( mPos, mNormal, mFrom, mAngle, mRadius );
				UnityEditor.Handles.color = Color.white;
				#endif
				return mDuration <= Time.time;
			}
		}
		public static void DrawWiredArc( Vector3 pos, Vector3 normal, Vector3 from, float angle, float radius, Color color, float duration = 0f ) {
			Init();
			mVlogs.Add( new WiredArc() { mPos = pos, mNormal = normal, mFrom = from, mAngle = angle, mRadius = radius, mClr = color, mDuration = Time.time + duration } );
		}


		class SphereCast : BaseVLog {
			public Vector3 mDir;
			public float mRadius;

			override public bool Draw( int i ) {
				Gizmos.color = mClr;

				Gizmos.DrawSphere( mPos, mRadius );
				Gizmos.DrawWireSphere( mPos + mDir, mRadius );

				Vector3 up = Vector3.up * mRadius;
				DrawArrow( mPos - up, mDir, mClr, .2f, false );
				DrawArrow( mPos + up, mDir, mClr, .2f, false );

				Gizmos.color = Color.gray;
				return mDuration <= Time.time;
			}
		}
		public static void DrawSphereCast( Vector3 pos, float radius, Vector3 dir, float dist, Color color, float duration = 0f ) {
			Init();
			mVlogs.Add( new SphereCast() { mPos = pos, mRadius = radius, mDir = dir.normalized * dist, mClr = color, mDuration = Time.time + duration } );
		}




		class Sphere: BaseVLog {
			public Vector3 mDir;
			public float mRadius;

			override public bool Draw( int i ) {
				Gizmos.color = mClr;

				Gizmos.DrawSphere( mPos, mRadius );

				Gizmos.color = Color.gray;
				return mDuration <= Time.time;
			}
		}
		public static void DrawSphere( Vector3 pos, float radius, Color color, float duration = 0f ) {
			Init();
			mVlogs.Add( new SphereCast() { mPos = pos, mRadius = radius, mClr = color, mDuration = Time.time + duration } );
		}




		class GzmBounds : BaseVLog {
			public Bounds mBounds;
			public bool mDrawMinMax = false;

			override public bool Draw( int i ) {
				Gizmos.color = mClr;

				Gizmos.DrawWireCube( mBounds.center, mBounds.size );
				HiroVlogExt.DrawText( mBounds.min, "min(" + mBounds.min.ToPrintable() + ")", mClr, mDuration );
				HiroVlogExt.DrawText( mBounds.max, "max(" + mBounds.max.ToPrintable() + ")", mClr, mDuration );

				Gizmos.color = Color.gray;
				return mDuration <= Time.time;
			}
		}
		public static void DrawBound( Bounds bounds, Color color, float duration = 0f, bool drawMinMax = false ) {
			Init();
			mVlogs.Add( new GzmBounds() { mBounds = bounds, mClr = color, mDuration = Time.time + duration, mDrawMinMax = drawMinMax } );
		}


	}






























	/// <summary>
	/// start of Math Extensions
	/// </summary>
	public static class HiroMathExt {
		public static Vector3 AbsPosFromPt2( Vector3 pt1, Vector3 pt2, float dist ) {
			Vector3 vec = pt2 - pt1;
			return pt2 - vec.normalized * dist;
		}
		public static Vector3 AbsPosFromPt1( Vector3 pt1, Vector3 pt2, float dist ) {
			Vector3 vec = pt2 - pt1;
			return pt1 + vec.normalized * dist;
		}

		public static float DirToAng( Vector3 from, Vector3 to ) {
			return DirToAng( to - from );
		}
		public static float DirToAng( Vector3 dir ) {
			return Mathf.Atan2( dir.x, dir.z ) * Mathf.Rad2Deg;
		}


		public static float getSign( this float val ) {
			return val < 0f ? -1f : 1f;
		}
		public static int getSign( this int val ) {
			return val < 0 ? -1 : 1;
		}

		//x :		-2| -1|  0| -5| -4| -3| -2| -1|  0| -5| -4| -3| -2| -1|  0| -5| -4| -3| -2| -1|  0|  1|  2|  3|  4|  5|  0|  1|  2|  3|  4|  5|  0|  1|  2|  3|  4|  5|  0|  1|
		public static int I2X( int i, int len ) {
			return i % len;
		}
		//y :		-3| -3| -3| -2| -2| -2| -2| -2| -2| -1| -1| -1| -1| -1| -1|  0|  0|  0|  0|  0|  0|  0|  0|  0|  0|  0|  1|  1|  1|  1|  1|  1|  2|  2|  2|  2|  2|  2|  3|  3|
		public static int I2Y( int i, int len ) {
			return i / len;
		}
		//repeat :	 4|  5|  0|  1|  2|  3|  4|  5|  0|  1|  2|  3|  4|  5|  0|  1|  2|  3|  4|  5|  0|  1|  2|  3|  4|  5|  0|  1|  2|  3|  4|  5|  0|  1|  2|  3|  4|  5|  0|  1|
		public static int Repeat( int i, int len ) {
			if( i < 0 ) { return (len - (i * -1) % len) % len; }
			return i % len;
		}
		//evenOdd:	-9|  9| -8|  8| -7|  7| -6|  6| -5|  5| -4|  4| -3|  3| -2|  2| -1|  1|  0|  0|  0| -1|  1| -2|  2| -3|  3| -4|  4| -5|  5| -6|  6| -7|  7| -8|  8| -9|  9|-10|
		public static int EvenOdd( int i, int len ) {
			return (i + 1) / 2 * (i % 2 == 0 ? 1 : -1);
		}
		//I2Ratio:	 0.0|0.1|0.2|0.3|0.4|0.6|0.7|0.8|0.9|1.0|
		public static float I2Ratio( float i, float len ) {
			return i / (len - 1f);
		}
		//halfRat:	 1.0|0.8|0.5|0.3|0.0|0.6|0.7|0.8|0.9|1.0|
		public static float I2HalfRatio( float i, float len ) {
			return CustomEase.EaseFunc.LinearRecursive( i, 0f, 1f, len - 1 );
		}
		public static float I2CenterAlignment( int idx, int len ) {
			//int half = (int)Math.Round( (Decimal)(len/2f), System.MidpointRounding.AwayFromZero );
			int half = len / 2;
			if( len % 2 == 0 ) {
				return (idx - half) + .5f;
			} else {
				return idx - half;
			}
		}



		//arrayToCurve is original Vector3 array, smoothness is the number of interpolations. 
		public static List<Vector3> MakeSmoothCurve( List<Vector3> arrayToCurve, float smoothness ) {
			if( smoothness < 1.0f ) { smoothness = 1.0f; }

			int ptsLen = arrayToCurve.Count;
			int curvedLen = (ptsLen * Mathf.RoundToInt( smoothness )) - 1;
			if( curvedLen <= 0 ) { return arrayToCurve; }
			List<Vector3> curvedPts = new List<Vector3>( curvedLen );
			List<Vector3> pts;

			float t = 0.0f;
			for( int pointInTimeOnCurve = 0; pointInTimeOnCurve < curvedLen + 1; pointInTimeOnCurve++ ) {
				t = Mathf.InverseLerp( 0, curvedLen, pointInTimeOnCurve );

				pts = new List<Vector3>( arrayToCurve );

				for( int j = ptsLen - 1; j > 0; j-- ) {
					for( int i = 0; i < j; i++ ) {
						pts[i] = (1 - t) * pts[i] + t * pts[i + 1];
					}
				}

				curvedPts.Add( pts[0] );
			}

			return curvedPts;
		}


		public static float GetYawDiff( Vector3 forward, Vector3 vec, bool clamp360 = true ) {
			float ang = (Mathf.Atan2( vec.x, vec.z ) - Mathf.Atan2( forward.x, forward.z )) * Mathf.Rad2Deg;
			if( clamp360 && ang < 0f ) { ang = 360f + ang; }
			return ang;
		}
		public static float GetPitch( Vector3 vec ) {
			Quaternion q = Quaternion.LookRotation( vec, Vector3.up );
			return q.eulerAngles.x;
		}
		public static float GetYaw( Vector3 vec ) {
			Quaternion q = Quaternion.LookRotation( vec, Vector3.up );
			return q.eulerAngles.y;
		}

		public static float GetPitchDiff2( Vector3 forward, Vector3 vec ) {
			Quaternion q = Quaternion.LookRotation( forward, Vector3.up );
			Quaternion q2 = Quaternion.LookRotation( vec, Vector3.up );
			return ShortestIncrement( q.eulerAngles.x, q2.eulerAngles.x );
		}
		public static float GetPitchDiff( Vector3 forward, Vector3 vec, bool shift90 = false ) {
			float tPi = Mathf.PI * 2f;

			float pitch = Mathf.Atan2( Mathf.Sqrt( vec.x * vec.x + vec.z * vec.z ), vec.y );
			float orgPitch = Mathf.Atan2( Mathf.Sqrt( forward.x * forward.x + forward.z * forward.z ), forward.y );

			//float bkDir = ( (int)(Vector3.Dot( vec, fwd ) * 100f) ) / 100f;
			if( false ) {
				float bkDir = Vector3.Dot( vec, forward );
				if( bkDir < 0f ) { pitch = tPi - pitch; }

				bkDir = Vector3.Dot( forward, forward );
				if( bkDir < 0f ) { orgPitch = tPi - orgPitch; }
			}

			pitch -= orgPitch;

			if( !shift90 ) {
				pitch -= Mathf.PI / 2f;
				if( pitch < 0 ) { pitch += tPi; }
			}
			return pitch * Mathf.Rad2Deg;
		}
		public static float Get2dDegree( Vector2 vec, bool clamp360 = true ) {
			float ang = Mathf.Atan2( vec.y, vec.x ) * Mathf.Rad2Deg;
			if( clamp360 && ang < 0f ) { ang = 360f + ang; }
			return ang;
		}
		public static float Get2dDegreeDiff( Vector2 fwd, Vector2 vec ) {
			float nowDeg = HiroMathExt.Get2dDegree( fwd, true );
			float tagDeg = HiroMathExt.Get2dDegree( vec, true );
			return HiroMathExt.ShortestIncrement( nowDeg, tagDeg );
		}



		public static Vector3 InitVecFromAngle( Vector3 origin, Vector3 target, float initAngle ) {
			//Debug.Log( "initAngle(" + initAngle + ")" );

			float gravity = Physics.gravity.magnitude;
			// Selected angle in radians
			float angle = initAngle * Mathf.Deg2Rad;

			// Positions of this object and the target on the same plane
			Vector3 planarTarget = new Vector3( target.x, 0, target.z );
			Vector3 planarPostion = new Vector3( origin.x, 0, origin.z );

			// Planar distance between objects
			float distance = Vector3.Distance( planarTarget, planarPostion );
			// Distance along the y axis between objects
			float yOffset = origin.y - target.y;

			float sqrtVal = (0.5f * gravity * Mathf.Pow( distance, 2 )) / (distance * Mathf.Tan( angle ) + yOffset);
			if( sqrtVal < -1 ) {	// unreachable
				#if UNITY_EDITOR
				Debug.Log( "<color=red>wont hit</color>; sqrtVal("+sqrtVal+")" );
				#endif
				return InitVecFromTime( origin, target );
			}
			if( float.IsNaN( sqrtVal ) ) {	// NaN?
				#if UNITY_EDITOR
				Debug.Log( "<color=red>IsNaN</color>; sqrtVal(" + sqrtVal + ")" );
				#endif
				return InitVecFromTime( origin, target );
			}

			float initialVelocity = (1 / Mathf.Cos( angle )) * Mathf.Sqrt( sqrtVal );
			if( float.IsNaN( initialVelocity ) ) {	// NaN?
				#if UNITY_EDITOR
				Debug.Log( "<color=red>initialVelocity is NaN</color>; initialVelocity(" + initialVelocity + ")" );
				#endif
				return InitVecFromTime( origin, target );
			}

			Vector3 velocity = new Vector3( 0, initialVelocity * Mathf.Sin( angle ), initialVelocity * Mathf.Cos( angle ) );

			// Rotate our velocity to match the direction between the two objects
			#if false
			float angleBetweenObjects = Vector3.Angle( Vector3.forward, planarTarget - planarPostion );
			Vector3 finalVelocity = Quaternion.AngleAxis( angleBetweenObjects, Vector3.up ) * velocity;
			#else
			Vector3 finalVelocity = Quaternion.LookRotation( planarTarget - planarPostion ) * velocity;
			#endif

			return finalVelocity;
		}

		public static Vector3 InitVecFromTime( Vector3 origin, Vector3 target, float timeToTarget = 0f ) {
			if( timeToTarget <= 0f ) {
				timeToTarget = (target - origin).magnitude / 30f;
			}
			// calculate vectors
			Vector3 toTarget = target - origin;
			Vector3 toTargetXZ = toTarget;
			toTargetXZ.y = 0;

			// calculate xz and y
			float y = toTarget.y;
			float xz = toTargetXZ.magnitude;

			// calculate starting speeds for xz and y. Physics forumulase deltaX = v0 * t + 1/2 * a * t * t
			// where a is "-gravity" but only on the y plane, and a is 0 in xz plane.
			// so xz = v0xz * t => v0xz = xz / t
			// and y = v0y * t - 1/2 * gravity * t * t => v0y * t = y + 1/2 * gravity * t * t => v0y = y / t + 1/2 * gravity * t
			float t = timeToTarget;
			float v0y = y / t + 0.5f * Physics.gravity.magnitude * t;
			float v0xz = xz / t;

			// create result vector for calculated starting speeds
			Vector3 result = toTargetXZ.normalized;		// get direction of xz but with magnitude 1
			result *= v0xz;								// set magnitude of xz to v0xz (starting speed in xz plane)
			result.y = v0y;								// set y to v0y (starting speed of y plane)

			return result;
		}

		public static float ShortestIncrement( float from, float to, float deathZone = 0f ) {
			to *= Mathf.Deg2Rad;
			from *= Mathf.Deg2Rad;
			float rslt = Mathf.Atan2( Mathf.Sin( to - from ), Mathf.Cos( to - from ) );
			if( Mathf.Abs( rslt ) < deathZone ) {
				return 0f;
			}
			return rslt * Mathf.Rad2Deg;
		}

		public static float CalcShortestRot( float from, float to ) {
			return from + ShortestIncrement( from, to );

			//float s = 3 * Mathf.Deg2Rad;
			//float d = 3 * Mathf.Rad2Deg;
			// If from or to is a negative, we have to recalculate them.
			// For an example, if from = -45 then from(-45) + 360 = 315.
			if( from < 0 ) { from += 360; }
			if( to < 0 ) { to += 360; }
			// Do not rotate if from == to.
			if( from == to || from == 0 && to == 360 || from == 360 && to == 0 ) { return 0; }

			// Pre-calculate left and right.
			float left = (360 - from) + to;
			float right = from - to;
			// If from < to, re-calculate left and right.
			if( from < to ) {
				if( to > 0 ) {
					left = to - from;
					right = (360 - to) + from;
				} else {
					left = (360 - to) + from;
					right = to - from;
				}
			}

			// Determine the shortest direction.
			return ((left <= right) ? left : (right * -1));
		}

		public static float ClampAngle( float ang ) {
			if( 360f < ang ) { ang -= 360f; }
			if( ang < 0f ) { ang += 360f; }
			return ang;
		}
	}











	/// <summary>
	/// start of General Extensions
	/// </summary>
	public static class HiroExt {

		public static void ClearLog() {
			#if UNITY_EDITOR_WIN
			System.Reflection.Assembly assembly = System.Reflection.Assembly.GetAssembly( typeof( UnityEditor.SceneView ) );
			System.Type type = assembly.GetType( "UnityEditorInternal.LogEntries" );
			System.Reflection.MethodInfo method = type.GetMethod( "Clear" );
			method.Invoke( new object(), null );
			#endif
		}
		static float mLogIntvl = 0f;
		public static void ItvlLog( string msg, float itvl = 0f ) {
			if( itvl == 0f ) { mLogIntvl = 0f; }
			if( mLogIntvl < Time.time ) {
				Debug.Log( msg );
				mLogIntvl = Time.time + itvl;
			}
		}

		public static Camera GetMainCam() {
			Camera cam = Camera.main;
			if( cam == null ) {
				cam = GameObject.FindObjectOfType<Camera>();
			}
			if( cam == null ) {
				GameObject camGo = GameObject.FindGameObjectWithTag( "MainCamera" );
				if( camGo != null ) {
					cam = camGo.GetComponent<Camera>();
				}
			}
			if( cam == null ) {
				GameObject camGo = GameObject.Find( "Main Camera" );
				if( camGo != null ) {
					cam = camGo.GetComponent<Camera>();
				}
			}
			return cam;
		}

		public static void FrameController() {
			#if UNITY_EDITOR
			if( Input.GetKeyDown( KeyCode.Alpha1 ) ) {
				Time.timeScale = Time.timeScale == 1f ? 1f / 9f : 1f;
			}
			if( Input.GetKeyDown( KeyCode.Alpha2 ) ) {
				Time.timeScale = 1f / 2f;
			}
			if( Input.GetKeyDown( KeyCode.Alpha3 ) ) {
				Time.timeScale = 1f / 3f;
			}
			if( Input.GetKeyDown( KeyCode.Alpha4 ) ) {
				Time.timeScale = 1f / 4f;
			}
			if( Input.GetKeyDown( KeyCode.Alpha5 ) ) {
				Time.timeScale = 1f / 5f;
			}
			if( Input.GetKeyDown( KeyCode.Alpha6 ) ) {
				Time.timeScale = 1f / 6f;
			}
			if( Input.GetKeyDown( KeyCode.Alpha7 ) ) {
				Time.timeScale = 1f / 7f;
			}
			if( Input.GetKeyDown( KeyCode.Alpha8 ) ) {
				Time.timeScale = 1f / 8f;
			}
			if( Input.GetKeyDown( KeyCode.Alpha9 ) ) {
				Time.timeScale = 1f / 9f;
			}
			#endif
		}



		#if UNITY_EDITOR
		//static void AlignCamToSelect() {
		//	//Selection.activeGameObject = Camera.main.gameObject;
		//}
		static bool mDoUpdateSceneCam = true;
		public static void UpdateSceneCam( Vector3 pos, Quaternion rot ) {
			if( mDoUpdateSceneCam ) {
				UnityEditor.SceneView sv = UnityEditor.SceneView.lastActiveSceneView;
				if( sv != null ) {
					sv.pivot = pos;
					sv.rotation = rot;
					sv.Repaint();
				}
			}
			if( Input.GetKeyDown( KeyCode.S ) && Input.GetKey( KeyCode.LeftControl ) && Input.GetKey( KeyCode.LeftAlt ) ) {
				mDoUpdateSceneCam = !mDoUpdateSceneCam;
			}
			//HiroVlogExt.DrawText( pos, "ctrl+alt+s to toggle scene cam", Color.red );
		}
		#endif



		public static float FrontVision( Transform source, Transform target, float deadZone = 3f, float effectorMultiplier = 1f ) {
			float yaw = HiroMathExt.DirToAng( source.forward );

			Vector3 dir = target.position - source.position;
			dir.y = 0f;

			float dist = dir.magnitude;
			float ang = HiroMathExt.ShortestIncrement( yaw, HiroMathExt.DirToAng( dir ) );

			float visionEffector = 1f - Mathf.Abs( ang ) / 180f;	// 1 - 179 / 180; 0deg = 1f, 180deg = 0f
			visionEffector = Mathf.Pow( visionEffector * effectorMultiplier, 2 );

			float vision = (deadZone - dist) / deadZone * visionEffector;	// no vision after 3f
			if( vision < 0f ) { vision = 0f; }

			#if DBG
			Debug.Log( "dir(" + dir + "), dist(" + dist + "), ang(" + ang + "), effector(" + visionEffector + "), vision(" + dist + ")" );
			HiroVlogExt.DrawText( target.position + Vector3.forward, "ang(" + ang + "), dist(" + dist + ")", new Color( 1f, 0f, 1f, vision ) );

			//Debug.DrawRay( source.position, source.forward * 4f, Color.red );
			//Debug.DrawRay( source.position, Quaternion.AngleAxis( yaw, Vector3.up ) * (Vector3.forward), Color.yellow );

			//HiroVlogExt.DrawText( source.position + Quaternion.AngleAxis( yaw, Vector3.up ) * Vector3.forward * 3f, "yaw(" + yaw + ")", Color.red );

			Vector3 pos, next;
			for( int i = 0, intvl = 15; i < 360; i += intvl ) {
				pos = source.position + Quaternion.AngleAxis( yaw + i, Vector3.up ) * (Vector3.forward * FrontVisionLimit( i, deadZone, effectorMultiplier ));
				int j = i + intvl;
				next = source.position + Quaternion.AngleAxis( yaw + j, Vector3.up ) * (Vector3.forward * FrontVisionLimit( j, deadZone, effectorMultiplier ));
				Debug.DrawLine( pos, next, new Color( 1f - vision, 0f, vision ) );
				if( i % 90 == 0 ) {
					HiroVlogExt.DrawText( pos, i.ToString(), i == 0f ? Color.red : Color.blue );
				}
			}
			#endif

			return vision;
		}
		public static float FrontVisionLimit( Transform source, Transform target, float deadZone = 3f, float effectorMultiplier = 1f ) {
			float yaw = HiroMathExt.DirToAng( source.forward );
			Vector3 dir = target.position - source.position;
			float ang = HiroMathExt.ShortestIncrement( yaw, HiroMathExt.DirToAng( dir ) );
			return FrontVisionLimit( ang, deadZone, effectorMultiplier );
		}
		public static float FrontVisionLimit( Transform source, Vector3 tagPos, float deadZone = 3f, float effectorMultiplier = 1f ) {
			float yaw = HiroMathExt.DirToAng( source.forward );
			Vector3 dir = tagPos - source.position;
			float ang = HiroMathExt.ShortestIncrement( yaw, HiroMathExt.DirToAng( dir ) );
			return FrontVisionLimit( ang, deadZone, effectorMultiplier );
		}
		public static float FrontVisionLimit( float ang, float deadZone = 3f, float effectorMultiplier = 1f ) {
			if( 180f <= ang ) { ang -= 360f; }
			float visionEffector = 1f - Mathf.Abs( ang ) / 180f;	// 1 - 179 / 180; 0deg = 1f, 180deg = 0f
			visionEffector = Mathf.Pow( visionEffector * effectorMultiplier, 2 );
			return deadZone * visionEffector;
		}

		static byte ToByte( float f ) {
			f = Mathf.Clamp01( f );
			return (byte)(f * 255);
		}
		public static string ToHex( this Color c ) {
			return string.Format( "{0:X2}{1:X2}{2:X2}{3:X2}", ToByte( c.r ), ToByte( c.g ), ToByte( c.b ), ToByte( c.a ) );
		}
		public static string ToHex( this Color32 color ) {
			string hex = color.r.ToString( "X2" ) + color.g.ToString( "X2" ) + color.b.ToString( "X2" ) + color.a.ToString( "X2" );
			return hex;
		}

		public static string ToPrintable( this Vector3 vec ) {
			return vec.x.ToString( "F2" ) + ", " + vec.y.ToString( "F2" ) + ", " + vec.z.ToString( "F2" );
		}
		public static string ToPrintable( this Vector2 vec ) {
			return vec.x.ToString( "F2" ) + ", " + vec.y.ToString( "F2" );
		}
		public static string ToPrintable( this Rect r ) {
			return r.min.x.ToString( "F1" ) + ", " + r.min.y.ToString( "F1" ) + " > " + r.max.x.ToString( "F1" ) + ", " + r.max.y.ToString( "F1" );
		}
		public static string ToPrintable( this Bounds b ) {
			return b.min.x.ToString( "F1" ) + ", " + b.min.y.ToString( "F1" ) + ",  " + b.min.z.ToString( "F1" ) + " > " + b.max.x.ToString( "F1" ) + ", " + b.max.y.ToString( "F1" ) + ", " + b.max.z.ToString( "F1" );
		}
		public static string ToPrintableSize( this Bounds b ) {
			return b.size.x.ToString( "F1" ) + ", " + b.size.y.ToString( "F1" ) + b.size.z.ToString( "F1" );
		}
		public static string ToPrintableCenter( this Bounds b ) {
			return b.center.x.ToString( "F1" ) + ", " + b.center.y.ToString( "F1" ) + b.center.z.ToString( "F1" );
		}
		public static string ToPrintableBinary( this int val, int padLft = 0 ) {
			if( 0 < padLft ) {
				return System.Convert.ToString( val, 2 ).PadLeft( padLft, '0' );
			} else {
				return System.Convert.ToString( val, 2 );
			}
			//return (len > 1 ? ToBin( val >> 1, len - 1 ) : null) + "01"[val & 1];
		}
		public static string ToPrintableAllComps( this Transform tran ) {
			Component[] comps = tran.GetComponents<Component>();
			return string.Join( ", ", System.Array.ConvertAll( comps, e => e.GetType().Name ) );
		}
		public static string ToPrintable( this System.Reflection.MethodBase m ) {
			string typeName = m.DeclaringType.ToString();
			if( typeName.IndexOf( "Ryan.Scenes" ) != -1 ) {
				return typeName.Replace( "Ryan.Scenes.", "" ) + "." + m.Name;
			} else {
				return m.DeclaringType.Name + "." + m.Name;
			}
			// DeclaringType
			//+		ReflectedType	"Ryan.Scenes.HomeTop.Scene"	System.Type
		}

		public static string ToUpperFirst( this string s ) {
			// Check for empty string.
			if( string.IsNullOrEmpty( s ) ) {
				return string.Empty;
			}
			// Return char and concat substring.
			return char.ToUpper( s[0] ) + s.Substring( 1 ).ToLower();
		}

		public static void InsertionSort<T>( this IList<T> list, System.Comparison<T> comparison ) {
			for( int j = 1, jlen = list.Count; j < jlen; j++ ) {
				T key = list[j];

				int i = j - 1;
				for( ; i >= 0 && comparison( list[i], key ) > 0; i-- ) {
					list[i + 1] = list[i];
				}
				list[i + 1] = key;
			}
		}



		public static void SetAnchor( this RectTransform rt, TextAnchor anchor ) {
			if( anchor == TextAnchor.UpperLeft ) {
				rt.anchorMin = new Vector2( 0f, 1f );
				rt.anchorMax = new Vector2( 0f, 1f );

			} else if( anchor == TextAnchor.UpperCenter ) {
				rt.anchorMin = new Vector2( .5f, 1f );
				rt.anchorMax = new Vector2( .5f, 1f );

			} else if( anchor == TextAnchor.UpperRight ) {
				rt.anchorMin = Vector2.one;
				rt.anchorMax = Vector2.one;

			} else if( anchor == TextAnchor.MiddleLeft ) {
				rt.anchorMin = new Vector2( 0f, .5f );
				rt.anchorMax = new Vector2( 0f, .5f );

			} else if( anchor == TextAnchor.MiddleCenter ) {
				rt.anchorMin = new Vector2( .5f, .5f );
				rt.anchorMax = new Vector2( .5f, .5f );

			} else if( anchor == TextAnchor.MiddleRight ) {
				rt.anchorMin = new Vector2( 1f, .5f );
				rt.anchorMax = new Vector2( 1f, .5f );

			} else if( anchor == TextAnchor.LowerLeft ) {
				rt.anchorMin = Vector2.zero;
				rt.anchorMax = Vector2.zero;

			} else if( anchor == TextAnchor.LowerCenter ) {
				rt.anchorMin = new Vector2( .5f, 0f );
				rt.anchorMax = new Vector2( .5f, 0f );

			} else if( anchor == TextAnchor.LowerRight ) {
				rt.anchorMin = new Vector2( 1f, 0f );
				rt.anchorMax = new Vector2( 1f, 0f );

			}
		}
		public static TextAnchor GetAnchor( this RectTransform rt ) {
			return GetAnchor( rt.anchorMin, rt.anchorMax );
		}
		public static TextAnchor GetAnchor( Vector2 anchorMin, Vector2 anchorMax ) {
			if( anchorMin.x == 0f && anchorMax.x == 0f ) {    // left

				if( anchorMin.y == 0f && anchorMax.y == 0f ) {    // bottom
					return TextAnchor.LowerLeft;
				} else if( anchorMin.y == .5f && anchorMax.y == .5f ) {    // center
					return TextAnchor.MiddleLeft;
				} else if( anchorMin.y == 1f && anchorMax.y == 1f ) {    // top
					return TextAnchor.UpperLeft;
				}

			} else if( anchorMin.x == .5f && anchorMax.x == .5f ) {    // center

				if( anchorMin.y == 0f && anchorMax.y == 0f ) {    // bottom
					return TextAnchor.LowerLeft;
				} else if( anchorMin.y == .5f && anchorMax.y == .5f ) {    // center
					return TextAnchor.MiddleLeft;
				} else if( anchorMin.y == 1f && anchorMax.y == 1f ) {    // top
					return TextAnchor.UpperLeft;
				}

			} else if( anchorMin.x == 1f && anchorMax.x == 1f ) {    // right

				if( anchorMin.y == 0f && anchorMax.y == 0f ) {    // bottom
					return TextAnchor.LowerLeft;
				} else if( anchorMin.y == .5f && anchorMax.y == .5f ) {    // center
					return TextAnchor.MiddleLeft;
				} else if( anchorMin.y == 1f && anchorMax.y == 1f ) {    // top
					return TextAnchor.UpperLeft;
				}

			}

			return TextAnchor.MiddleCenter;
		}

		public static bool IsAnchoredTop(this RectTransform rt ) {
			TextAnchor a = GetAnchor( rt.anchorMin, rt.anchorMax );
			if( a == TextAnchor.UpperCenter || a == TextAnchor.UpperLeft || a == TextAnchor.UpperRight ) {
				return true;
			}
			return false;
		}



		public static float HorizontalDistance( this Vector3 vec ) {
			return Mathf.Sqrt( vec.x * vec.x + vec.z * vec.z );
		}

		public static Vector3 X( this Vector3 vec, float x = 0f ) {
			vec.x = x;
			return vec;
		}
		public static Vector3 Y( this Vector3 vec, float y = 0f ) {
			vec.y = y;
			return vec;
		}
		public static Vector3 Z( this Vector3 vec, float z = 0f ) {
			vec.z = z;
			return vec;
		}
		public static Vector3 XY( this Vector3 vec, float x = 0f, float y = 0f ) {
			vec.x = x;
			vec.y = y;
			return vec;
		}
		public static Vector3 XZ( this Vector3 vec, float x = 0f, float z = 0f ) {
			vec.x = x;
			vec.z = z;
			return vec;
		}
		public static Vector3 AddX( this Vector3 vec, float x = 0f ) {
			vec.x += x;
			return vec;
		}
		public static Vector3 AddY( this Vector3 vec, float y = 0f ) {
			vec.y += y;
			return vec;
		}
		public static Vector3 AddZ( this Vector3 vec, float z = 0f ) {
			vec.z += z;
			return vec;
		}
		public static Vector3 AddXY( this Vector3 vec, float x = 0f, float y = 0f ) {
			vec.x += x;
			vec.y += y;
			return vec;
		}
		public static Vector3 AddXZ( this Vector3 vec, float x = 0f, float z = 0f ) {
			vec.x += x;
			vec.z += z;
			return vec;
		}
		public static Vector3 AddYZ( this Vector3 vec, float y = 0f, float z = 0f ) {
			vec.y += y;
			vec.z += z;
			return vec;
		}
		public static Vector3 MulX( this Vector3 vec, float x = 0f ) {
			vec.x *= x;
			return vec;
		}
		public static Vector3 MulY( this Vector3 vec, float y = 0f ) {
			vec.y *= y;
			return vec;
		}
		public static Vector3 MulZ( this Vector3 vec, float z = 0f ) {
			vec.z *= z;
			return vec;
		}
		public static Vector3 MulXY( this Vector3 vec, float x = 0f, float y = 0f ) {
			vec.x *= x;
			vec.y *= y;
			return vec;
		}
		public static Vector3 MulXZ( this Vector3 vec, float x = 0f, float z = 0f ) {
			vec.x *= x;
			vec.z *= z;
			return vec;
		}

		public static Vector2 X( this Vector2 vec, float x = 0f ) {
			vec.x = x;
			return vec;
		}
		public static Vector2 Y( this Vector2 vec, float y = 0f ) {
			vec.y = y;
			return vec;
		}
		public static Vector2 XY( this Vector2 vec, float x = 0f, float y = 0f ) {
			vec.x = x;
			vec.y = y;
			return vec;
		}



		public static Color R( this Color clr, float r = 0f ) {
			return new Color( r, clr.g, clr.b, clr.a );
		}
		public static Color G( this Color clr, float g = 0f ) {
			return new Color( clr.r, g, clr.b, clr.a );
		}
		public static Color B( this Color clr, float b = 0f ) {
			return new Color( clr.r, clr.g, b, clr.a );
		}
		public static Color A( this Color clr, float a = 0f ) {
			return new Color( clr.r, clr.g, clr.b, a );
		}

		public static string SecToStr( int sec ) {
			System.TimeSpan time = System.TimeSpan.FromSeconds( sec );
			return string.Format( "{0:D2}:{1:D2}:{2:D2}", time.Hours, time.Minutes, time.Seconds );
		}

		public static string SecToStr(long sec)
		{
			System.TimeSpan time = System.TimeSpan.FromSeconds(sec);
			return string.Format("{0:D2}:{1:D2}:{2:D2}", time.Hours, time.Minutes, time.Seconds);
		}

		public static string MinToStr(int min)
		{
			System.TimeSpan time = System.TimeSpan.FromMinutes(min);
			return string.Format("{0:D2}:{1:D2}:{2:D2}", time.Hours, time.Minutes, time.Seconds);
		}


		/// <summary>
		/// Get the full path of a GameObject
		/// </summary>
		/// <returns>The full path</returns>
		/// <param name="obj">The root GameObject</param>
		public static string GetFullPath( this GameObject obj ) {
			string path = "/" + obj.name;
			while( obj.transform.parent != null ) {
				obj = obj.transform.parent.gameObject;
				path = "/" + obj.name + path;
			}
			return path;
		}

		/// <summary>
		/// Get the full path of a Transform
		/// </summary>
		/// <returns>The full path</returns>
		/// <param name="tran">the root transform</param>
		public static string GetFullPath( this Transform tran ) {
			return tran.gameObject.GetFullPath();
		}

		/// <summary>
		/// ** not really working yet **
		/// Checks if a GameObject has been destroyed.
		/// </summary>
		/// <param name="gameObject">GameObject reference to check for destructedness</param>
		/// <returns>If the game object has been marked as destroyed by UnityEngine</returns>
		public static bool IsDestroyed( this GameObject gameObject ) {
			// UnityEngine overloads the == opeator for the GameObject type
			// and returns null when the object has been destroyed, but 
			// actually the object is still there but has not been cleaned up yet
			// if we test both we can determine if the object has been destroyed.
			return gameObject == null && !ReferenceEquals( gameObject, null );
		}

		/// <summary>
		/// get the object no matter activeSelf is ON or OFF
		/// * recommended to be used within Awake()
		/// </summary>
		/// <returns>GameObject</returns>
		/// <param name="path">Path; e.g. "Camera/MenuPanel/GachaResult"</param>
		public static GameObject BrutalFind( this GameObject root, string path, bool printError = true ) {
			Transform tran = root.transform.BrutalFind( path, printError );
			if( tran == null ) { return null; }
			return tran.gameObject;
		}

		/// <summary>
		/// get the object no matter activeSelf is ON or OFF
		/// * recommended to be used within Awake()
		/// </summary>
		/// <returns>Transform</returns>
		/// <param name="path">Path; e.g. "Camera/MenuPanel/GachaResult"</param>
		public static Transform BrutalFind( this Transform root, string path, bool printError = true ) {
			Transform child = root.Find( path );
			if( child != null ) { return child; }

			string[] arrayPath = path.Split( '/' );
			//Debug.Log(path.Length + " : " + arrayPath.ToString() );
			child = root.BrutalFind( arrayPath );
			#if UNITY_EDITOR
			if( printError && child == null ) {
				Debug.LogError( "\"" + GetFullPath( root ) + "/" + path + "\" doesn't exist" );
			}
			#endif
			//Debug.Log( "finally we have : " + ( child != null ? child.GetComponent<UIPanel>().gameObject.name : "no child" ) );
			return child;
		}
		static Transform BrutalFind( this Transform root, string[] arrayPath ) {
			if( arrayPath == null || arrayPath.Length == 0 ) { return null; }
			//Debug.Log( "try to find " + arrayPath[0] );

			Transform theChild = null;
			for( int i = 0, len = root.childCount; i < len; i++ ) {
				Transform t = root.GetChild( i );
				if( t.name == arrayPath[0] ) {
					theChild = t;
					break;
				}
			}
			// do some search
			if( theChild != null ) {	// found what we want,
				// and some works remind
				int len = arrayPath.Length - 1;
				if( 0 < len ) {
					string[] childPath = new string[len];
					for( int i = 0; i < len; i++ ) { childPath[i] = arrayPath[i + 1]; }

					Transform theGrandchild = theChild.BrutalFind( childPath );
					if( theGrandchild != null ) {
						return theGrandchild;
					}
				} else {
					return theChild;
				}
			}
			return null;
		}

		public static Rect GetScreenRect( this RectTransform rectTransform, Canvas canvas = null ) {
			Vector3[] corners = new Vector3[4];
			Vector3[] screenCorners = new Vector3[2];
			
			//rectTransform.GetWorldCorners( corners );
			//if( canvas == null ) {
			//	Transform tran = rectTransform.parent;
			//	do {
			//		canvas = tran.GetComponent<Canvas>();
			//		tran = tran.parent;
			//	} while( canvas == null && tran != null );
			//}
			if( canvas != null ) {
				if( canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace ) {
					screenCorners[0] = RectTransformUtility.WorldToScreenPoint( canvas.worldCamera, corners[1] );
					screenCorners[1] = RectTransformUtility.WorldToScreenPoint( canvas.worldCamera, corners[3] );
				} else {
					screenCorners[0] = RectTransformUtility.WorldToScreenPoint( null, corners[1] );
					screenCorners[1] = RectTransformUtility.WorldToScreenPoint( null, corners[3] );
				}
			} else {
				screenCorners[0] = RectTransformUtility.WorldToScreenPoint( null, corners[1] );
				screenCorners[1] = RectTransformUtility.WorldToScreenPoint( null, corners[3] );
			}
			
			screenCorners[0].y = Screen.height - screenCorners[0].y;
			screenCorners[1].y = Screen.height - screenCorners[1].y;
			
			return new Rect( screenCorners[0], screenCorners[1] - screenCorners[0] );
		}

		public static Bounds GetBounds( this RectTransform rt, RectTransform tagSpace = null ) { // in world space or in target's local space
			Vector3 vec = rt.TransformPoint( rt.rect.center );
			if( tagSpace != null ) {
				vec = tagSpace.InverseTransformPoint( vec );
			}
			Vector3 size = rt.rect.size;
			return new Bounds( vec, size.XY( size.x * rt.localScale.x, size.y * rt.localScale.y ) );
		}

		public static float GetHeight( this RectTransform rt ) { // in world space or in target's local space
			return rt.rect.size.y;
		}
		public static void SetHeight( this RectTransform rt, float val ) { // in world space or in target's local space
			rt.sizeDelta = rt.sizeDelta.Y( val );
		}
		public static float GetWidth( this RectTransform rt ) { // in world space or in target's local space
			return rt.rect.size.x;
		}
		public static Vector2 GetSize( this RectTransform rt ) { // in world space or in target's local space
			return rt.rect.size;
		}

		public static void SetAnchoredPositionTo( this RectTransform rt, RectTransform tagRt ) { // in world space or in target's local space
			Vector3 pos = tagRt.GetBounds( rt.parent.GetComponent<RectTransform>() ).center;
			if( rt.IsAnchoredTop() ) {
				pos.y += rt.GetHeight() / 2;
			}
			rt.anchoredPosition3D = pos;
		}

		public static Vector3 GetAnchoredPositionFromTagrget( this RectTransform rt, RectTransform tagRt ) { // in world space or in target's local space
			Vector3 pos = tagRt.GetBounds( rt.parent.GetComponent<RectTransform>() ).center;
			if( rt.IsAnchoredTop() ) {
				pos.y += rt.GetHeight() / 2;
			}
			return pos;
		}

		public static Vector2 CalcRectTranSize( this RectTransform rt ) { // in world space or in target's local space
			float w = 0f;
			float h = 0f;

			UnityEngine.UI.LayoutElement le = rt.GetComponent<UnityEngine.UI.LayoutElement>();

			if( le != null && le.preferredHeight != -1 ) {
				h = le.preferredHeight;
			}

			if( le != null && le.preferredWidth != -1 ) {
				w = le.preferredWidth;
			}

			if( w == 0f || h == 0f ) {
				Vector3 size = RectTransformUtility.CalculateRelativeRectTransformBounds( rt ).size;
				if( w == 0f ) { w = size.x; }
				if( h == 0f ) { h = size.y; }
			}

			#if MAKE_SURE_DATA_IDENTICAL
			rt.sizeDelta = new Vector2( w, h );
			return rt.sizeDelta;
			#else
			return new Vector2( w, h );
			#endif
		}


		public static void HierarchySelect( this GameObject go ) {
			#if UNITY_EDITOR
			UnityEditor.Selection.activeGameObject = go;
			#endif
		}

		public static int IndexOfNth( string str, string find, int nth, int sidx = 0 ) {
			int s = -1;
			int cLen = find.Length;

			for( int i = 0; i < nth + 1; i++ ) {
				//Log( "["+i+"]" + sidx );
				s = str.IndexOf( find, sidx );

				if( s == -1 ) { break; } else {
					sidx = s + cLen;
				}
			}

			return s;
		}


		public static void SetTagsAndFlags( this GameObject go, string tag, HideFlags hideFlags = HideFlags.None, bool includeChild = true ) {
			go.transform.SetTagsAndFlags( tag, hideFlags, includeChild );
		}
		public static void SetTagsAndFlags( this Transform tran, string tag, HideFlags hideFlags = HideFlags.None, bool includeChild = true ) {
			tran.tag = tag;
			tran.hideFlags = hideFlags;
			tran.gameObject.hideFlags = hideFlags;
			if( includeChild ) {
				for( int i = 0, len = tran.childCount; i < len; i++ ) {
					tran.GetChild( i ).SetTagsAndFlags( tag, hideFlags );
				}
			}
		}

		public static void SetTags( this GameObject go, string tag, bool includeChild = true ) {
			go.transform.SetTags( tag, includeChild );
		}
		public static void SetTags( this Transform tran, string tag, bool includeChild = true ) {
			tran.tag = tag;
			if( includeChild ) {
				for( int i = 0, len = tran.childCount; i < len; i++ ) {
					tran.GetChild( i ).SetTags( tag );
				}
			}
		}

		public static void SetLayers( this Transform tran, string layer, bool includeChild = true ) {
			tran.gameObject.SetLayers( layer, includeChild );
		}
		public static void SetLayers( this GameObject go, string layer, bool includeChild = true ) {
			go.layer = LayerMask.NameToLayer( layer );
			if( includeChild ) {
				Transform tran = go.transform;
				for( int i = 0, len = tran.childCount; i < len; i++ ) {
					tran.GetChild( i ).gameObject.SetLayers( layer );
				}
			}
		}
		public static void SetTagsAndLayers( this Transform tran, string tag, string layer, bool includeChild = true ) {
			tran.gameObject.SetTagsAndLayers( tag, layer, includeChild );
		}
		public static void SetTagsAndLayers( this GameObject go, string tag, string layer, bool includeChild = true ) {
			go.layer = LayerMask.NameToLayer( layer );
			go.tag = tag;
			if( includeChild ) {
				Transform tran = go.transform;
				for( int i = 0, len = tran.childCount; i < len; i++ ) {
					tran.GetChild( i ).gameObject.SetTagsAndLayers( tag, layer );
				}
			}
		}

		public static void SetTopBtmLftRgt( this RectTransform rt, float top, float btm, float lft, float rgt ) {
			rt.offsetMin = new Vector2( lft, btm );
			rt.offsetMax = new Vector2( -rgt, -top );
		}

		public static void AscendingByChildName( this List<Transform> trans ) {
			if( trans == null ) { return; }
			trans.Sort( delegate ( Transform t1, Transform t2 ) {
				return (new CaseInsensitiveComparer()).Compare( t1.name, t2.name );
			} );
		}


		public static void SetBit( ref int val, int idx, bool onoff ) {
			idx = 1 << idx;
			if( onoff ) {
				val |= idx;
			} else {
				val &= ~idx;
			}
		}

		public static bool GetBit( int val, int idx ) {
			idx = 1 << idx;
			return (val & idx) == idx;
		}

		public static Transform GetPrimitive( string name, PrimitiveType type, Transform parent, Vector3 pos, Vector3 rot, Vector3 scale, int layer = 0, Material mat = null ) {
			GameObject go = null;
			if( parent == null ) {
				go = GameObject.Find( name );
			} else {
				Transform foundTran = parent.BrutalFind( name, false );
				if( foundTran != null ) {
					go = foundTran.gameObject;
				}
			}
			if( go != null ) {
				return go.transform;
			}
			Transform tran;
			if( (int)type == -1 ) {
				tran = new GameObject().transform;
			} else {
				tran = GameObject.CreatePrimitive( type ).transform;
			}
			tran.name = name;
			tran.parent = parent;
			if( mat != null ) {
				tran.GetComponent<Renderer>().material = mat;
			}
			tran.localPosition = pos;
			tran.localRotation = Quaternion.Euler( rot );
			tran.localScale = scale;
			tran.gameObject.layer = layer;
			return tran;
		}

		//https://forum.unity.com/threads/keep-gameobject-upright.38542/ time-based vertical attractor from the Havok physics code for vehicles.
		public static bool CreateUprightTorque( this Rigidbody rig, out Quaternion outQuat, float exeThreshold = 30f, float rotSpd = 5f ) {
			// Get our local Y+ vector (relative to world)
			Vector3 localUp = rig.transform.up;
			// Now build a rotation taking us from our current Y+ vector towards the actual world Y+
			Quaternion vertical = Quaternion.FromToRotation( localUp, Vector3.up ) * rig.rotation;
			// How far are we tilted off the ideal Y+ world rotation?
			float angleVerticalDiff = Quaternion.Angle( rig.rotation, vertical );
			if( angleVerticalDiff > exeThreshold ) // Greater than 30 degrees, start the vertical attractor
			{
				// Slerp blend based on our current rotation
				outQuat = Quaternion.Slerp( rig.rotation, vertical, Time.deltaTime * rotSpd );
				//rig.MoveRotation( Quaternion.Slerp( rig.rotation, vertical, Time.deltaTime * rotSpd ) );
				return true;
			}
			outQuat = Quaternion.identity;
			return false;
		}

	}





	class SimplePathFinder {
		#if UNITY_EDITOR
		static bool mChkMode = true;
		#endif

		public static void Calc( Vector3 s, Vector3 e, float castR, float avoidOffset, float walkDist, out List<Vector3> path, out Vector3 nextPos, ref bool isSurrounded, float smoothness = 0f, int layer = Physics.DefaultRaycastLayers, string tag = null ) {
			path = new List<Vector3>();
			Debug.DrawLine( s, e, Color.blue );

			Vector3 dir = e - s;
			RaycastHit[] hits = Physics.SphereCastAll( s, castR, dir.normalized, dir.magnitude, layer );
			int nofHits = hits.Length;

			#if PATH_FIND_DBG_DRAW && UNITY_EDITOR
			HiroVlogExt.DrawSphereCast( s, castR, dir, dir.magnitude, HiroClr.purple * .3f );
			HiroVlogExt.DrawText( (s + e) / 2f + Vector3.up * 2f, nofHits.ToString(), HiroClr.purple );
			#endif

			if( 0 < nofHits ) {
				#if UNITY_EDITOR
				if( mChkMode ) {
					BoundingBoxMethod( ref s, ref e, hits, nofHits, avoidOffset, out isSurrounded, ref path, ref tag );
				} else {
					IndividualSphereMethod( ref s, ref e, hits, nofHits, avoidOffset, out isSurrounded, ref path, ref tag );
				}
				if( Input.GetKeyDown( KeyCode.Space ) ) { mChkMode = !mChkMode; }
				#else
				BoundingBoxMethod( ref s, ref e, hits, nofHits, avoidOffset, out isSurrounded, ref path, ref tag );
				#endif

				bool makeSmoothCurve = 0f < smoothness;
				#if PATH_FIND_DBG_DRAW && UNITY_EDITOR
				int plen = path.Count - 1;
				for( int i = 0; i < plen; i++ ) {
					float alpha = .3f + ( 1f - ((float)i / (float)plen) ) * .57f;
					Debug.DrawLine( path[i], path[i + 1], Color.green.A( makeSmoothCurve ? alpha * .3f : alpha ) );
				}
				#endif
				if( makeSmoothCurve ) {
					#if false
					int preLen = path.Count;
					path = HiroMathExt.MakeSmoothCurve( path, smoothness );
					HiroExt.ClearLog();
					Debug.Log( preLen + " > " + path.Count );
					#else
					path = HiroMathExt.MakeSmoothCurve( path, smoothness );
					#endif

					#if PATH_FIND_DBG_DRAW && UNITY_EDITOR
					plen = path.Count - 1;
					for( int i = 0; i < plen; i++ ) {
						float alpha = .3f + (1f - ((float)i / (float)plen)) * .57f;
						Debug.DrawLine( path[i], path[i + 1], Color.green.A( alpha ) );
					}
					#endif
				}
				//Debug.DrawLine( path[plen], path[i + 1], Color.green );
			}

			if( 0 < path.Count ) {
				nextPos = path[path.Count - 1];
				for( int i = 1, len = path.Count; i < len; i++ ) {
					nextPos = path[i];
					float d = (nextPos - s).HorizontalDistance();
					if( walkDist <= (nextPos - s).HorizontalDistance() ) {
						break;
					}
				}

			} else {
				nextPos = e;
			}

			#if PATH_FIND_DBG_DRAW && UNITY_EDITOR
			HiroVlogExt.DrawWiredArc( nextPos, Vector3.up, Vector3.forward, 360f, .5f, Color.red );
			HiroVlogExt.DrawText( nextPos, "Next", Color.red, 0f, TextAnchor.MiddleCenter );
			#endif
		}


		static void BoundingBoxMethod( ref Vector3 s, ref Vector3 e, RaycastHit[] hits, int nofHits, float avoidOffset, out bool isSurrounded, ref List<Vector3> path, ref string tag ) {
			Bounds b = new Bounds();
			bool aHit = false;
			for( int i = 0; i < nofHits; i++ ) {
				RaycastHit hit = hits[i];
				if( tag == null || hit.transform.tag == tag ) {
					#if PATH_FIND_DBG_DRAW && UNITY_EDITOR
					Debug.DrawLine( i == 0 ? s : hits[i - 1].point, hit.point, Color.red );
					Debug.DrawLine( hit.point, hit.normal, Color.yellow );
					#endif

					if( !aHit ) {
						aHit = true;
						b = hit.collider.bounds;
					} else {
						b.Encapsulate( hit.collider.bounds );
					}
				}
			}
			#if PATH_FIND_DBG_DRAW && UNITY_EDITOR
			Debug.DrawLine( hits[nofHits - 1].point, e, Color.red );
			HiroVlogExt.DrawBound( b, Color.red );
			#endif

			Bounds safeB = new Bounds( b.center, b.size );
			{	// find safe bound
				safeB.min -= new Vector3( avoidOffset, 0f, avoidOffset );
				safeB.max += new Vector3( avoidOffset, 0f, avoidOffset );
				#if PATH_FIND_DBG_DRAW && UNITY_EDITOR
				HiroVlogExt.DrawBound( safeB, Color.green );
				#endif
			}

			isSurrounded = b.Contains( s );
			//if( safeB.Contains( e ) ) {	// destination within bound
			//	path.Add( s );
			//	path.Add( e );
			//	return;
			//}

			float sDeg;
			float eDeg;
			float incr;
			int nofPts = 10;
			{
				#if PATH_FIND_DBG_DRAW && UNITY_EDITOR
				Debug.DrawLine( safeB.center + Vector3.up, s + Vector3.up, Color.green );
				Debug.DrawLine( safeB.center + Vector3.up, e + Vector3.up, Color.green );
				#endif
				Vector3 vec = safeB.center - s;
				sDeg = Mathf.Atan2( vec.x, vec.z ) * Mathf.Rad2Deg;
				//sDeg = HiroMathExt.ClampAngle( sDeg );

				vec = safeB.center - e;
				eDeg = Mathf.Atan2( vec.x, vec.z ) * Mathf.Rad2Deg;
				//eDeg = HiroMathExt.ClampAngle( eDeg );

				incr = HiroMathExt.ShortestIncrement( sDeg, eDeg );

				#if PATH_FIND_DBG_DRAW && UNITY_EDITOR
				//HiroVlogExt.DrawText( (s + e) / 2f + new Vector3( 2f, 2f, 0f ), sDeg.ToString( "F2" ) + "->" + eDeg.ToString( "F2" ) + " : " + incr.ToString( "F2" ), HiroVlogExt.purple );
				HiroVlogExt.DrawText( new Vector3( 2f, 2f, 0f ), sDeg.ToString( "F2" ) + "->" + eDeg.ToString( "F2" ) + " : " + incr.ToString( "F2" ) + "(" + (incr / (float)nofPts).ToString( "F2" ) + ")", HiroClr.purple );
				#endif

				incr /= (float)nofPts;
			}

			path.Add( s );

			Vector3 fwd = Vector3.forward * Mathf.Max( safeB.size.x, safeB.size.z ) / 2f;
			for( int i = 1; i < nofPts; i++ ) {
				float d = sDeg + incr * i;
				Vector3 pt = safeB.center - Quaternion.AngleAxis( d, Vector3.up ) * fwd;
				path.Add( pt );

				#if PATH_FIND_DBG_DRAW && UNITY_EDITOR
				Debug.DrawRay( pt, Vector3.up, (i == 1 ? new Color( .7f, .7f, 0f ) : new Color( 0f, .4f, 0f )) );
				HiroVlogExt.DrawText( pt, d.ToString( "F2" ), (i == 1 ? new Color( .7f, .7f, 0f ) : new Color( 0f, .4f, 0f )) );
				#endif
			}

			path.Add( e );
		}


		class SphereData {
			public Vector3 pos;
			public float radius;
			public List<int> mCollideIdx = new List<int>();

			public bool isIntercept( SphereData sd ) {
				float dist = (sd.pos - pos).HorizontalDistance();
				return dist < radius + sd.radius;
			}
		}
		static void IndividualSphereMethod( ref Vector3 s, ref Vector3 e, RaycastHit[] hits, int nofHits, float avoidOffset, out bool isSurrounded, ref List<Vector3> path, ref string tag ) {
			isSurrounded = false;	// not yet figured it out how to do

			List<SphereData> scs = new List<SphereData>();
			for( int i = 0; i < nofHits; i++ ) {
				RaycastHit hit = hits[i];
				if( tag == null || hit.transform.tag == tag ) {
					#if PATH_FIND_DBG_DRAW && UNITY_EDITOR
					if( i == 0 ) { Debug.DrawLine( s, hit.point, Color.red ); }
					else { Debug.DrawLine( hits[i - 1].point, hit.point, Color.red ); }
					Debug.DrawLine( hit.point, hit.normal, Color.yellow );
					#endif

					SphereCollider sc = hit.transform.GetComponent<SphereCollider>();
					if( sc != null ) {
						scs.Add( new SphereData() { pos = sc.transform.position, radius = sc.radius/* + avoidOffset*/ } );
					}
					BoxCollider bc = hit.transform.GetComponent<BoxCollider>();
					if( bc != null ) {
						scs.Add( new SphereData() { pos = bc.transform.position, radius = Mathf.Sqrt( bc.bounds.size.x * bc.bounds.size.x + bc.bounds.size.z * bc.bounds.size.z ) / 2f /*+ avoidOffset*/ } );
					}
				}
			}

			//if( 1 < scs.Count ) {
			//	for( int i = 0, len = scs.Count; i < len; i++ ) {
			//		for( int j = i+1; j < len; j++ ) {
			//			if( scs[i].isIntercept( scs[j] ) ) {
			//				scs[i].mCollideIdx.Add( j );
			//				scs[j].mCollideIdx.Add( i );
			//			}
			//		}
			//	}
			//	HiroExt.ClearLog();
			//	for( int i = 0, len = scs.Count; i < len; i++ ) {
			//		int[] cs = scs[i].mCollideIdx.ToArray();
			//		HiroVlogExt.DrawText( scs[i].pos + Vector3.up*10f, i.ToString(), Color.yellow );
			//		Debug.Log( "scs[" + i + "] collided wz " + string.Join( ",", System.Array.ConvertAll( cs, element => element.ToString() ) ) );
			//	}
			//}

			#if PATH_FIND_DBG_DRAW && UNITY_EDITOR
			Debug.DrawLine( hits[nofHits - 1].point, e, Color.red );
			#endif

			//path.Add( s );
			//path.Add( nxSph.pos );
			//path.Add( s );
			//path.Add( e );

			int scsLen = scs.Count;

			#if PATH_FIND_DBG_MSG && UNITY_EDITOR
			HiroExt.ClearLog();
			Debug.Log( "scsLen(" + scsLen + ")" );
			#endif

			if( 0 < scsLen ) {	// find the closest pos
				if( scsLen == 1 ) {
					List<Vector3> midPath = new List<Vector3>();
					FindMidPath( s, e, scs[0], ref midPath );
					path.Add( s );
					path.AddRange( midPath );
					path.Add( e );
				} else {

					path.Add( s );
					#if PATH_FIND_DBG_MSG && UNITY_EDITOR
					Debug.Log( "addS(" + s.ToPrintable() + ")" );
					#endif
					Vector3 sPt = s;

					int safeCnt = 20;
					do {
						List<Vector3> midPath = new List<Vector3>();
						SphereData sph = null;
						float dist = 9999f;
						for( int i = 0; i < scsLen; i++ ) {
							SphereData sc = scs[i];
							float d = HiroExt.HorizontalDistance( sc.pos - sPt );
							if( d < dist ) {
								dist = d;
								sph = sc;
							}
						}

						scs.Remove( sph );	// remove 1st pos
						scsLen = scs.Count;
						#if PATH_FIND_DBG_DRAW && UNITY_EDITOR
						HiroVlogExt.DrawWiredSphere( sph.pos, sph.radius, Color.red );
						#endif

						SphereData nxSph = null;
						{
							dist = 9999f;
							for( int i = 0; i < scsLen; i++ ) {
								SphereData sc = scs[i];
								float d = HiroExt.HorizontalDistance( sc.pos - sph.pos );
								if( d < dist ) {
									dist = d;
									nxSph = sc;
								}
							}
						}

						#if PATH_FIND_DBG_MSG && UNITY_EDITOR
						//Debug.DrawLine( s, HiroMathExt.AbsPosFromPt2( s, sph.pos, sph.radius ), Color.green );
						Debug.Log( "addRange; scsLen(" + scsLen + "), nxSph(" + nxSph + "), safeCnt(" + safeCnt + ")" );
						#endif

						if( nxSph != null ) {	// have next point
							FindMidPath( sPt, nxSph.pos, sph, ref midPath );
							path.AddRange( midPath );
						} else {
							FindMidPath( sPt, e, sph, ref midPath );
							path.AddRange( midPath );
						}
						sPt = sph.pos;

						safeCnt--;
					} while( 0 < scsLen );
					
					#if PATH_FIND_DBG_MSG && UNITY_EDITOR
					Debug.Log( "addE(" + e.ToPrintable() + ")" );
					#endif
					path.Add( e );
				}
			}
		}

		static void FindMidPath( Vector3 s, Vector3 e, SphereData sph, ref List<Vector3> midPath ) {
			float sDeg;
			float eDeg;
			float incr;
			int nofPts = 10;
			{
				Vector3 vec = sph.pos - s;
				sDeg = Mathf.Atan2( vec.x, vec.z ) * Mathf.Rad2Deg;
				//sDeg = HiroMathExt.ClampAngle( sDeg );

				vec = sph.pos - e;
				eDeg = Mathf.Atan2( vec.x, vec.z ) * Mathf.Rad2Deg;
				//eDeg = HiroMathExt.ClampAngle( eDeg );

				incr = HiroMathExt.ShortestIncrement( sDeg, eDeg );

				#if PATH_FIND_DBG_DRAW && UNITY_EDITOR	
				//HiroVlogExt.DrawText( (s + e) / 2f + new Vector3( 2f, 2f, 0f ), sDeg.ToString( "F2" ) + "->" + eDeg.ToString( "F2" ) + " : " + incr.ToString( "F2" ), HiroVlogExt.purple );
				HiroVlogExt.DrawText( new Vector3( 2f, 2f, 0f ), sDeg.ToString( "F2" ) + "->" + eDeg.ToString( "F2" ) + " : " + incr.ToString( "F2" ) + "(" + (incr / (float)nofPts).ToString( "F2" ) + ")", HiroClr.purple );
				#endif

				incr /= (float)(nofPts - 1);
			}

			Vector3 fwd = Vector3.forward * sph.radius;
			for( int i = 0; i < nofPts; i++ ) {
				float d = sDeg + incr * i;
				Vector3 pt = sph.pos - Quaternion.AngleAxis( d, Vector3.up ) * fwd;
				midPath.Add( pt );

				#if PATH_FIND_DBG_DRAW && UNITY_EDITOR
				Debug.DrawRay( pt, Vector3.up, (i == 1 ? new Color( .7f, .7f, 0f ) : new Color( 0f, .4f, 0f )) );
				HiroVlogExt.DrawText( pt, d.ToString( "F2" ), (i == 1 ? new Color( .7f, .7f, 0f ) : new Color( 0f, .4f, 0f )) );
				#endif
			}
		}


	}








	/// <summary>
	/// start of Control Extensions
	/// </summary>

	//todo: need to port from Fortisia
	public class GamepadInput {
	}

	public class HiroInput {
		static TouchCreator mCreator;

		public static List<Touch> touches {
			get {
				List<Touch> touches = new List<Touch>();
				touches.AddRange( Input.touches );

				#if UNITY_EDITOR
				if( Input.GetMouseButtonDown( 0 ) ) {
					if( mCreator == null ) { mCreator = new TouchCreator(); }
					//Debug.Log( "<color=red>Down</color>" );
					mCreator.phase = TouchPhase.Began;
					mCreator.deltaPosition = new Vector2( 0, 0 );
					mCreator.position = new Vector2( Input.mousePosition.x, Input.mousePosition.y );
					mCreator.fingerId = 0;

				} else if( Input.GetMouseButtonUp( 0 ) ) {
					if( mCreator != null ) {
						//Debug.Log( "<color=green>Up</color>" );
						mCreator.phase = TouchPhase.Ended;
						Vector2 newPosition = new Vector2( Input.mousePosition.x, Input.mousePosition.y );
						mCreator.deltaPosition = newPosition - mCreator.position;
						mCreator.position = newPosition;
						mCreator.fingerId = 0;
					}

				} else if( Input.GetMouseButton( 0 ) ) {
					if( mCreator != null ) {
						//Debug.Log( "<color=yellow>Move</color>" );
						mCreator.phase = TouchPhase.Moved;
						Vector2 newPosition = new Vector2( Input.mousePosition.x, Input.mousePosition.y );
						mCreator.deltaPosition = newPosition - mCreator.position;
						mCreator.position = newPosition;
						mCreator.fingerId = 0;
					}

				} else if( mCreator != null ) {
					mCreator = null;

				}

				if( mCreator != null ) {
					touches.Add( mCreator.Create() );
				}
				#endif

				return touches;
			}
		}


		public class TouchGesture {
			float mMinSwipeDist = 100f;
			float mMaxSwipeTime = 1f;

			float mSwipeStartTime;
			bool mCouldBeSwipe;
			Vector2 mStartPos;
			int mStationaryForFrames;
			TouchPhase mLastPhase;

			public TouchGesture( float minSwipeDist, float maxSwipeTime ) {
				mMinSwipeDist = minSwipeDist;
				mMaxSwipeTime = maxSwipeTime;
			}

			delegate void VerdictDlg( Touch touch );
			void Check( VerdictDlg VerdictCb ) {
				List<Touch> touches = HiroInput.touches;
				//Debug.Log( "Input.touches(" + Input.touches.Length + "), touches(" + touches.Count + ")" );

				for( int i = 0, len = touches.Count; i < len; i++ ) {
					Touch touch = touches[i];

					switch( touch.phase ) {
						case TouchPhase.Began: //The finger first touched the screen, could be(come) a swipe
							mCouldBeSwipe = true;
							mStartPos = touch.position;  //Position where the touch started
							mSwipeStartTime = Time.time; //The time it started
							mStationaryForFrames = 0;
							break;
						case TouchPhase.Stationary: //Is the touch stationary? No swipe for you then!
							if( isContinouslyStationary( 6 ) ) {
								mCouldBeSwipe = false;
							}
							break;
						case TouchPhase.Ended:
							if( isASwipe( touch ) ) {
								mCouldBeSwipe = false; //<-- Otherwise this part would be called over and over again.
								VerdictCb( touch );
							}
							break;
					}
					mLastPhase = touch.phase;
				}

			}
			public void CheckVerticleSwipes( System.Action onUpSwipe, System.Action onDownSwipe ) {
				Check( delegate( Touch touch ) {
					float distY = touch.position.y - mStartPos.y;
					if( Mathf.Sign( distY ) == 1f ) {  //Swipe-direction, either 1 or -1.
						onUpSwipe();
					} else {
						onDownSwipe();
					}
				} );
			}
			public void CheckHorizontalSwipes( System.Action onLeftSwipe, System.Action onRightSwipe ) {
				Check( delegate( Touch touch ) {
					float distX = touch.position.x - mStartPos.x;
					if( Mathf.Sign( distX ) == 1f ) {  //Swipe-direction, either 1 or -1.
						onRightSwipe(); //Right-swipe
					} else {
						onLeftSwipe(); //Left-swipe
					}
				} );
			}
			public void CheckSwipes( System.Action onLeftSwipe, System.Action onRightSwipe, System.Action onUpSwipe, System.Action onDownSwipe ) {
				Check( delegate( Touch touch ) {
					float distX = touch.position.x - mStartPos.x;
					float distY = touch.position.y - mStartPos.y;
					if( Mathf.Abs( distY ) < Mathf.Abs( distX ) ) {
						if( Mathf.Sign( distX ) == 1f ) {  //Swipe-direction, either 1 or -1.
							if( onRightSwipe != null ) {
								onRightSwipe(); //Right-swipe
							}
						} else {
							if( onLeftSwipe != null ) {
								onLeftSwipe(); //Left-swipe
							}
						}
					} else {
						if( Mathf.Sign( distY ) == 1f ) {  //Swipe-direction, either 1 or -1.
							if( onUpSwipe != null ) {
								onUpSwipe();
							}
						} else {
							if( onDownSwipe != null ) {
								onDownSwipe();
							}
						}
					}

				} );
			}
			bool isContinouslyStationary( int frames ) {
				if( mLastPhase == TouchPhase.Stationary ) {
					mStationaryForFrames++;
				} else {	// reset back to 1
					mStationaryForFrames = 1;
				}
				return mStationaryForFrames > frames;
			}
			bool isASwipe( Touch touch ) {
				float swipeTime = Time.time - mSwipeStartTime; //Time the touch stayed at the screen till now.
				float swipeDist = Mathf.Abs( touch.position.x - mStartPos.x ); //Swipe distance
				return mCouldBeSwipe && swipeTime < mMaxSwipeTime && swipeDist > mMinSwipeDist;
			}
		}

		public class TouchCreator2 {	// speed?
			static Dictionary<string, System.Reflection.FieldInfo> mFields;

			public float deltaTime;
			public int tapCount;
			public TouchPhase phase;
			public Vector2 deltaPosition;
			public int fingerId;
			public Vector2 position;
			public Vector2 rawPosition;

			public Touch Create() {
				object touch = new Touch();
				mFields["m_TimeDelta"].SetValue( touch, deltaTime );
				mFields["m_TapCount"].SetValue( touch, tapCount );
				mFields["m_Phase"].SetValue( touch, phase );
				mFields["m_PositionDelta"].SetValue( touch, deltaPosition );
				mFields["m_FingerId"].SetValue( touch, fingerId );
				mFields["m_Position"].SetValue( touch, position );
				mFields["m_RawPosition"].SetValue( touch, rawPosition );
				return (Touch)touch;
			}

			public TouchCreator2() {
			}

			static TouchCreator2() {
				mFields = new Dictionary<string, System.Reflection.FieldInfo>();
				foreach( var f in typeof( Touch ).GetFields( System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic ) ) {
					mFields.Add( f.Name, f );
					//Debug.Log( "name: " + f.Name );
				}
			}
		}

		public class TouchCreator { // less memory?
			static Dictionary<string, System.Reflection.FieldInfo> mFields;
			object mTouch;

			public float deltaTime {
				get { return ((Touch)mTouch).deltaTime; }
				set { mFields["m_TimeDelta"].SetValue( mTouch, value ); }
			}
			public int tapCount {
				get { return ((Touch)mTouch).tapCount; }
				set { mFields["m_TapCount"].SetValue( mTouch, value ); }
			}
			public TouchPhase phase {
				get { return ((Touch)mTouch).phase; }
				set { mFields["m_Phase"].SetValue( mTouch, value ); }
			}
			public Vector2 deltaPosition {
				get { return ((Touch)mTouch).deltaPosition; }
				set { mFields["m_PositionDelta"].SetValue( mTouch, value ); }
			}
			public int fingerId {
				get { return ((Touch)mTouch).fingerId; }
				set { mFields["m_FingerId"].SetValue( mTouch, value ); }
			}
			public Vector2 position {
				get { return ((Touch)mTouch).position; }
				set { mFields["m_Position"].SetValue( mTouch, value ); }
			}
			public Vector2 rawPosition {
				get { return ((Touch)mTouch).rawPosition; }
				set { mFields["m_RawPosition"].SetValue( mTouch, value ); }
			}

			public Touch Create() {
				return (Touch)mTouch;
			}

			public TouchCreator() {
				mTouch = new Touch();
			}

			static TouchCreator() {
				mFields = new Dictionary<string, System.Reflection.FieldInfo>();
				foreach( var f in typeof( Touch ).GetFields( System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic ) ) {
					mFields.Add( f.Name, f );
					//Debug.Log( "name: " + f.Name );
				}
			}
		}




	}





	public class ObjectDumper {
		private int _level;
		private readonly int _indentSize;
		private readonly System.Text.StringBuilder _stringBuilder;
		private readonly List<int> _hashListOfFoundElements;

		private ObjectDumper( int indentSize ) {
			_indentSize = indentSize;
			_stringBuilder = new System.Text.StringBuilder();
			_hashListOfFoundElements = new List<int>();
		}

		public static string Dump( object element ) {
			return Dump( element, 2 );
		}

		public static string Dump( object element, int indentSize ) {
			var instance = new ObjectDumper( indentSize );
			return instance.DumpElement( element );
		}

		private string DumpElement( object element, int idx = -1 ) {
			try {
				if( element == null || element is System.ValueType || element is string ) {
					if( idx == -1 ) {
						Write( FormatValue( element ) + ", " );
					} else {
						Write( "[" + idx + "]" + FormatValue( element ) + ", " );

					}
				} else {
					var objectType = element.GetType();
					if( !typeof( IEnumerable ).IsAssignableFrom( objectType ) ) {
						Write( "{{{0}}}", objectType.FullName );
						_hashListOfFoundElements.Add( element.GetHashCode() );
						_level++;
					}

					var enumerableElement = element as IEnumerable;
					if( enumerableElement != null ) {
						int i = 0;
						foreach( object item in enumerableElement ) {
							i++;
							if( item is IEnumerable && !(item is string) ) {
								_level++;
								DumpElement( item, i );
								_level--;
							} else {
								if( !AlreadyTouched( item ) )
									DumpElement( item, i );
								else
									Write( "{{{0}}} <-- bidirectional reference found", item.GetType().FullName );
							}
						}
					} else {
						System.Reflection.MemberInfo[] members = element.GetType().GetMembers( System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance );
						foreach( var memberInfo in members ) {
							var fieldInfo = memberInfo as System.Reflection.FieldInfo;
							var propertyInfo = memberInfo as System.Reflection.PropertyInfo;

							if( fieldInfo == null && propertyInfo == null )
								continue;

							System.Type type;
							object value = "unknown";
							if( fieldInfo != null ) {
								type = fieldInfo.FieldType;
								value = fieldInfo.GetValue( element );
							} else {
								type = propertyInfo.PropertyType;
								bool skip = false;
								try {
									if( type == typeof( System.Reflection.MethodInfo ) ) {
										value = memberInfo.Name;
										skip = true;
									} else if( type == typeof( System.Reflection.MethodBase ) ) {
										value = memberInfo.Name;
										skip = true;
									} else if( type == typeof( System.Object ) ) {
										value = memberInfo.Name;
										skip = true;
									} else {
										value = propertyInfo.GetValue( element, null );
									}
								} catch( System.Exception e ) {
									Debug.LogError( "baseType(" + type.BaseType + "), memberType(" + type.MemberType + "), reflectedType(" + type.ReflectedType + ")" );
									Debug.LogError( e );
									skip = true;
								}
								if( skip ) { continue; }
							}

							if( type.IsValueType || type == typeof( string ) ) {
								Write( "<color=yellow>{0}</color>: {1}", memberInfo.Name, FormatValue( value ) + ";" );
							} else {
								var isEnumerable = typeof( IEnumerable ).IsAssignableFrom( type );
								Write( "<color=yellow>{0}</color>: {1}", memberInfo.Name, isEnumerable ? "..." : "{ }" );

								var alreadyTouched = !isEnumerable && AlreadyTouched( value );
								_level++;
								if( !alreadyTouched )
									DumpElement( value );
								else
									Write( "{{{0}}} <-- bidirectional reference found", value.GetType().FullName );
								_level--;
							}
						}
					}

					if( !typeof( IEnumerable ).IsAssignableFrom( objectType ) ) {
						_level--;
					}
				}

				return _stringBuilder.ToString();
			} catch( System.Exception e ) {
				Debug.LogError( e.Message + e.StackTrace.ToString() );
				return e.Message + e.StackTrace.ToString();
			}
		}

		private bool AlreadyTouched( object value ) {
			if( value == null )
				return false;

			var hash = value.GetHashCode();
			for( var i = 0; i < _hashListOfFoundElements.Count; i++ ) {
				if( _hashListOfFoundElements[i] == hash )
					return true;
			}
			return false;
		}

		private void Write( string value, params object[] args ) {
			var space = new string( ' ', _level * _indentSize );

			if( args != null )
				value = string.Format( value, args );

			_stringBuilder.AppendLine( space + value );
		}

		private string FormatValue( object o ) {
			if( o == null )
				return ("null");

			if( o is System.DateTime )
				return (((System.DateTime)o).ToShortDateString());

			if( o is string )
				return string.Format( "\"{0}\"", o );

			if( o is char && (char)o == '\0' )
				return string.Empty;

			if( o is System.ValueType )
				return (o.ToString());

			if( o is IEnumerable )
				return ("...");

			return ("{ }");
		}
	}
}











public class Vec3Lerp {
	Vector3 mNow;
	Vector3 mStart;
	Vector3 mTag;
	float mDelta = 1f;
	public Vec3Lerp( Vector3 ini ) { mTag = mStart = mNow = ini; }
	public Vec3Lerp() { mTag = mStart = mNow = Vector3.zero; }

	public float delta { get { return mDelta; } }
	public Vector3 val { get { return mNow; } }

	public Vector3 Update( Vector3 newVal, float spd = .3f ) {

		#if false
		if( mTag != newVal ) {
			mTag = newVal;
			mDelta = Time.deltaTime;  // reset
			mStart = mNow;
		} else {
			if( mDelta < .3f ) {
				mDelta += Time.deltaTime;
				if( .3f < mDelta ) { mDelta = .3f; }
			}
		}
		// currFrm, startValm, endVal, totalFrmCost
		//c = this.eq( sf, s, (e - s), ef );
		mNow.x = CustomEase.EaseFunc.QuadOut( mDelta, mStart.x, newVal.x - mStart.x, .3f );
		mNow.y = CustomEase.EaseFunc.QuadOut( mDelta, mStart.y, newVal.y - mStart.y, .3f );
		mNow.z = CustomEase.EaseFunc.QuadOut( mDelta, mStart.z, newVal.z - mStart.z, .3f );

		return mNow;

		#else
		if( mNow == newVal ) { return mNow; }

		//string dbg = "";
		if( mTag != newVal ) {
			mTag = newVal;
			mDelta = Time.deltaTime * spd;	// reset
			//dbg = "<color=red>reset</color>";
		} else {
			mDelta += Time.deltaTime * spd;
			//dbg = "update";
		}
		//dbg += "from(" + mNow.ToString( "F2" ) + ")";

		if( 1f < mDelta ) {
			mDelta = 1f;
			mTag = mNow = newVal;
		} else {
			mNow = Vector3.Lerp( mNow, newVal, mDelta );
			if( (newVal - mNow).magnitude < .01f ) { mDelta = 1f; mTag = mNow = newVal; }	// can end it...
		}
		//Debug.Log( dbg + ", to(" + mNow.ToString( "F2" ) + ")\ndelta(" + mDelta.ToString( "F2" ) + ")" );
		return mNow;

		#endif
	}
}

class Vec2Lerp {
	Vector2 mNow;
	Vector2 mTag;
	float mDelta = 1f;
	public Vec2Lerp( Vector2 ini ) { mTag = mNow = ini; }
	public Vec2Lerp() { mTag = mNow = Vector2.zero; }

	public float delta { get { return mDelta; } }
	public Vector2 val { get { return mNow; } }

	public Vector2 Update( Vector2 newVal, float spd = .3f ) {
		if( mNow == newVal ) { return mNow; }

		//string dbg = "";
		if( mTag != newVal ) {
			mTag = newVal;
			mDelta = Time.deltaTime * spd;  // reset
											//dbg = "<color=red>reset</color>";
		} else {
			mDelta += Time.deltaTime * spd;
			//dbg = "update";
		}
		//dbg += "from(" + mNow.ToString( "F2" ) + ")";

		if( 1f < mDelta ) {
			mDelta = 1f;
			mTag = mNow = newVal;
		} else {
			mNow = Vector2.Lerp( mNow, newVal, mDelta );
			if( (newVal - mNow).magnitude < .01f ) { mDelta = 1f; mTag = mNow = newVal; }   // can end it...
		}
		//Debug.Log( dbg + ", to(" + mNow.ToString( "F2" ) + ")\ndelta(" + mDelta.ToString( "F2" ) + ")" );
		return mNow;
	}
}


public class FloatLerp {
	float mNow;
	float mTag;
	float mDelta = 1f;
	public FloatLerp( float ini = 0f ) { mTag = mNow = ini; }

	public float delta { get { return mDelta; } }
	public float val { get { return mNow; } }

	public float Update( float newVal, float spd = .3f ) {
		if( mNow == newVal ) { return mNow; }

		//string dbg = "";
		if( mTag != newVal ) {
			mTag = newVal;
			mDelta = Time.deltaTime * spd;  // reset
											//dbg = "<color=red>reset</color>";
		} else {
			mDelta += Time.deltaTime * spd;
			//dbg = "update";
		}
		//dbg += "from(" + mNow.ToString( "F2" ) + ")";

		if( 1f < mDelta ) {
			mDelta = 1f;
			mTag = mNow = newVal;
		} else {
			mNow = Mathf.Lerp( mNow, newVal, mDelta );
			if( newVal - mNow < .01f ) { mDelta = 1f; mTag = mNow = newVal; }   // can end it...
		}
		//Debug.Log( dbg + ", to(" + mNow.ToString( "F2" ) + ")\ndelta(" + mDelta.ToString( "F2" ) + ")" );
		return mNow;
	}
}


public class QuatLerp {
	Quaternion mNow;
	Quaternion mTag;
	float mDelta = 1f;
	public QuatLerp() { mTag = mNow = Quaternion.identity; }
	public QuatLerp( Quaternion ini ) { mTag = mNow = ini; }

	public Quaternion Update( Quaternion newVal, float spd = .3f ) {
		if( mNow == newVal ) { return mNow; }

		//string dbg = "";
		if( mTag != newVal ) {
			mTag = newVal;
			mDelta = Time.deltaTime * spd;  // reset
											//dbg = "<color=red>reset</color>";
		} else {
			mDelta += Time.deltaTime * spd;
			//dbg = "update";
		}
		//dbg += "from(" + mNow.ToString( "F2" ) + ")";

		if( 1f < mDelta ) {
			mDelta = 1f;
			mTag = mNow = newVal;
		} else {
			mNow = Quaternion.Lerp( mNow, newVal, mDelta );
			if( Quaternion.Angle( newVal, mNow ) < .01f ) { mDelta = 1f; mTag = mNow = newVal; }   // can end it...
		}
		//Debug.Log( dbg + ", to(" + mNow.ToString( "F2" ) + ")\ndelta(" + mDelta.ToString( "F2" ) + ")" );
		return mNow;
	}

	public Quaternion val { get { return mNow; } }
}



public class XmlReadWrite {
	static string path = Application.dataPath + "/Application/Resources/IkAniData/";

	public static void Save<T>( string fileName, T t ) {
		XmlSerializer xmls = new XmlSerializer( typeof( T ) );

		StringWriter sw = new StringWriter();
		xmls.Serialize( sw, t );
		string xml = sw.ToString();

		if( !Directory.Exists( path ) ) {
			Directory.CreateDirectory( path );
		}
		File.WriteAllText( path + fileName, xml );
	}

	public static T Load<T>( string fileName ) {
		XmlSerializer xmls = new XmlSerializer( typeof( T ) );
		fileName = path + fileName;

		if( File.Exists( fileName ) ) {
			string xml = File.ReadAllText( fileName );
			return (T)xmls.Deserialize( new StringReader( xml ) );
		}
		return default( T );
	}
}




#if UNITY_EDITOR
class TestCls3 {
	public List<int> il3 = new List<int>(){
				333, 333, 333
			};

}
class TestCls2 {
	public TestCls3 mTestCls3 = new TestCls3();
	public TestCls3 mTestCls3Null = null;
	public List<int> il = new List<int>(){
				123, 123,124, 5656
			};

	public List<string> sa = new List<string>(){
				"ASD", "$#%^", "F", "%^U",
			};
}

class TestCls {
	public string ObjectViewer;
	public long xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx;
	public string mGreetMsg = null;
	public int xxxxxxxxxxxxxxxxxxzxxxxxxxxxxxxxxxxxxxxx;

	public int i = 234;
	public int[] ia = new int[]{
		1, 2,245, 7,578, 7,8
	};

	public string s = "@#$";
	public string[] sa = new string[]{
		"ASD", "$#%^", "F", "%^U",
	};

	public TestCls2[] mTestCls2s = new TestCls2[]{
		new TestCls2(),
		new TestCls2(),
	};
}



public class ObjectViewer : ObjectViewerBase {
	const string EditorName = "ObjectViewer";

	[MenuItem( "Hotkey/" + EditorName + " %j" )]
	static void Init() { EditorWindow.GetWindow( type ); }

	static System.Type type { get { return System.Type.GetType( EditorName ); } }
	static string mKey = EditorName + ".isVisible";
	protected static bool isVisible { get { return EditorPrefs.GetBool( mKey, false ); } set { EditorPrefs.SetBool( mKey, value ); } }
	protected static EditorWindow win { get { return isVisible ? EditorWindow.GetWindow( type ) : null; } }

	static string mJson;
	static List<string> mJsons;

	public static string json {
		set { ObjectViewerBase.json( win, ref value, ref mJson, ref mJsons ); }
	}
	public static object dumpObj {
		set { 
			if( isVisible == true)
				json = HiroExtensions.ObjectDumper.Dump( value );
		}
	}

	void Awake() {
		isVisible = true;
		_Awake( win, ref mJson, ref mJsons );
	}
	void OnDestroy() { isVisible = false; }
	void OnGUI() {
		ObjectViewerBase.UpdateGUI( Event.current, ref mScrollPos, ref mJson, ref mJsons, ref mStyle, this, ref mIsMouseDown );
	}
}

public class ObjectViewer2 : ObjectViewerBase {
	const string EditorName = "ObjectViewer2";

	[MenuItem( "Hotkey/" + EditorName )]
	static void Init() { EditorWindow.GetWindow( type ); }

	static System.Type type { get { return System.Type.GetType( EditorName ); } }
	static string mKey = EditorName + ".isVisible";
	protected static bool isVisible { get { return EditorPrefs.GetBool( mKey, false ); } set { EditorPrefs.SetBool( mKey, value ); } }
	protected static EditorWindow win { get { return isVisible ? EditorWindow.GetWindow( type ) : null; } }

	static string mJson;
	static List<string> mJsons;

	public static string json {
		set { ObjectViewerBase.json( win, ref value, ref mJson, ref mJsons ); }
	}
	public static object dumpObj {
		set { json = HiroExtensions.ObjectDumper.Dump( value ); }
	}

	void Awake() {
		isVisible = true;
		_Awake( win, ref mJson, ref mJsons );
	}
	void OnDestroy() { isVisible = false; }
	void OnGUI() {
		ObjectViewerBase.UpdateGUI( Event.current, ref mScrollPos, ref mJson, ref mJsons, ref mStyle, this, ref mIsMouseDown );
	}
}

public class ObjectViewerBase : EditorWindow {
	protected Vector2 mScrollPos = Vector2.zero;
	protected GUIStyle mStyle = null;
	protected bool mIsMouseDown = false;


	protected void _Awake( EditorWindow win, ref string mJson, ref List<string> mJsons ) {
		mStyle = new GUIStyle() { richText = true, margin = new RectOffset( 0, 0, 0, 0 ), padding = new RectOffset( 0, 0, 0, 0 ) };

		TestCls d = new TestCls() {
			ObjectViewer = "",
			xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx = 1,
			mGreetMsg = win.name + " welcomes you",
			xxxxxxxxxxxxxxxxxxzxxxxxxxxxxxxxxxxxxxxx = 1,
		};
		string val = HiroExtensions.ObjectDumper.Dump( d, 4 );
		ObjectViewerBase.json( win, ref val, ref mJson, ref mJsons );
	}

	public static void json( EditorWindow mWin, ref string str, ref string mJson, ref List<string> mJsons ) {
		mJson = str.Replace("\r", "");

		mJsons = new List<string>();

		int nth = 50;

		bool cont = true;
		int fs = 12;
		int prvIdx = 0;
		do {
			int idx = HiroExtensions.HiroExt.IndexOfNth( str, "\n", nth, prvIdx );
			if( idx != -1 ) {
				idx += 2;
				if( str.Length < idx ) {
					mJsons.Add( str.Substring( prvIdx ) );
					//strs.Add("skip");
					cont = false;
				} else {
					string json = str.Substring( prvIdx, idx - prvIdx );
					int lidx = json.LastIndexOf( "\n" );
					if( lidx == json.Length - "\n".Length - 1 ) {
						json = json.Substring( 0, lidx );
					}
					mJsons.Add( json );
					prvIdx = idx;
				}
			} else {
				mJsons.Add( str.Substring( prvIdx ) );
				//strs.Add("skip");
				cont = false;
			}

			fs--;
			if( fs < 0 ) {
				cont = false;
			}
		} while( cont );
		if( mWin != null ) {
			mWin.Repaint();
		}
	}
	public static void UpdateGUI( Event evt, ref Vector2 ScrollPos, ref string mJson, ref List<string> mJsons, ref GUIStyle mStyle, EditorWindow mWin, ref bool mIsMouseDown ) {
		if( evt.type == EventType.MouseDown ) {
			mIsMouseDown = true;
			Debug.Log( "down" );
			EditorGUIUtility.systemCopyBuffer = mJson.Replace( "<color=yellow>", "" ).Replace( "<color=red>", "" ).Replace( "</color>", "" );
			mWin.Repaint();

		} else if( evt.type == EventType.MouseUp ) {
			Debug.Log( "up" );
			mIsMouseDown = false;
			mWin.Repaint();
		}

		ScrollPos = EditorGUILayout.BeginScrollView( ScrollPos );

		if( mJsons != null ) {
			for( int i = 0, len = mJsons.Count; i < len; i++ ) {
				//EditorGUILayout.TextArea( "<color=white>" + mJsons[i] + "</color>", mStyle );
				GUILayout.Label( "<color=white>" + mJsons[i] + "</color>", mStyle );
			}
		}

		//GUI.TextArea( new Rect( Vector2.zero, this.maxSize ), json );

		EditorGUILayout.EndScrollView();

		if( mIsMouseDown ) {
			GUI.Box( new Rect( 0, 0, mWin.position.width, mWin.position.height ), "<color=red>>> clipboard</color>", new GUIStyle() { richText = true, alignment = TextAnchor.MiddleCenter } );
		}
	}
}
#endif