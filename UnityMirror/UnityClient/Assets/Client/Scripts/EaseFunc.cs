using UnityEngine;
using System.Collections.Generic;

namespace CustomEase {
	public enum EaseType {
		Linear = 0,
		LinearRecursive,
		linearRecursive_3over4,
		QuadIn,
		QuadOut,
		QuadInOut,
		QuadOutRecursive,
		QuadInOutRecursive,
		QuadOutRecursive3over4,
		BounceIn,
		BounceOut,
		BounceInOut,
		ElasticInOut,
		ElasticIn,
		ElasticOut,
		BackIn,
		BackOut,
		BackInOut,
		BackOutSmooth,
		BackOutBounce,
		CircIn,
		CircOut,
		CircInOut,
		QuintIn,
		QuintOut,
		QuintInOut,
		ExpoIn,
		ExpoOut,
		ExpoInOut,
		BattlePatternEase,
		ShakeEase2,
		ShakeEase,
		Max,
	}
	
	class EaseFunc {
	
		
		//public static UITweener.Method GetTween( string eqName ){
		//	if( eqName.Length == 0 ) { return UITweener.Method.EaseInOut; }
		//	EaseType easeType = (EaseType)System.Enum.Parse( typeof( EaseType ), eqName );
		//	return GetTween( easeType );
		//}
		
		//public static UITweener.Method GetTween( EaseType easeType ){
		//	switch( easeType ) {
		//	case EaseType.Linear: return UITweener.Method.Linear; break;
		//	case EaseType.BounceIn: return UITweener.Method.BounceIn; break;
		//	case EaseType.BounceOut: return UITweener.Method.BounceOut; break;
		//	case EaseType.QuadIn: return UITweener.Method.EaseIn; break;
		//	case EaseType.QuadOut: return UITweener.Method.EaseOut; break;
		//	case EaseType.QuadInOut: return UITweener.Method.EaseInOut; break;
		//	}
		//	return UITweener.Method.EaseInOut;
		//}

		public static EaseVal.EaseEq GetEq( string eqName ){
			if( eqName.Length == 0 ) { return null; }
			EaseType easeType = (EaseType)System.Enum.Parse( typeof( EaseType ), eqName );
			return GetEq( easeType );
		}
		
		public static EaseVal.EaseEq GetEq( EaseType easeType ){
			switch( easeType ) {
				case EaseType.Linear: return EaseFunc.Linear;
				case EaseType.LinearRecursive: return EaseFunc.LinearRecursive;
				case EaseType.linearRecursive_3over4: return EaseFunc.linearRecursive_3over4;

				case EaseType.QuadIn: return EaseFunc.QuadIn;
				case EaseType.QuadOut: return EaseFunc.QuadOut;
				case EaseType.QuadInOut: return EaseFunc.QuadInOut;
				case EaseType.QuadOutRecursive: return EaseFunc.QuadOutRecursive;
				case EaseType.QuadInOutRecursive: return EaseFunc.QuadInOutRecursive;
				case EaseType.QuadOutRecursive3over4: return EaseFunc.QuadOutRecursive3over4;

				case EaseType.BounceIn: return EaseFunc.BounceIn;
				case EaseType.BounceOut: return EaseFunc.BounceOut;
				case EaseType.BounceInOut: return EaseFunc.BounceInOut;

				case EaseType.ElasticIn: return EaseFunc.ElasticIn;
				case EaseType.ElasticOut: return EaseFunc.ElasticOut;
				case EaseType.ElasticInOut: return EaseFunc.ElasticInOut;

				case EaseType.BackIn: return EaseFunc.BackIn;
				case EaseType.BackOut: return EaseFunc.BackOut;
				case EaseType.BackInOut: return EaseFunc.BackInOut;
				case EaseType.BackOutSmooth: return EaseFunc.BackOutSmooth;
				case EaseType.BackOutBounce: return EaseFunc.BackOutBounce;

				case EaseType.CircIn: return EaseFunc.CircIn;
				case EaseType.CircOut: return EaseFunc.CircOut;
				case EaseType.CircInOut: return EaseFunc.CircInOut;

				case EaseType.QuintIn: return EaseFunc.QuintIn;
				case EaseType.QuintOut: return EaseFunc.QuintOut;
				case EaseType.QuintInOut: return EaseFunc.QuintInOut;

				case EaseType.ExpoIn: return EaseFunc.ExpoIn;
				case EaseType.ExpoOut: return EaseFunc.ExpoOut;
				case EaseType.ExpoInOut: return EaseFunc.ExpoInOut;

				case EaseType.BattlePatternEase: return EaseFunc.BattlePatternEase;
				case EaseType.ShakeEase2: return EaseFunc.ShakeEase2;
				case EaseType.ShakeEase: return EaseFunc.ShakeEase;

				default: return EaseFunc.NoCurve;
			}
		}

		// currFrm, startValm, endVal-startValm, totalFrmCost
		public static float NoCurve( float t, float b, float c, float d ) {
			return 0;
		}
		public static float Linear( float t, float b, float c, float d ) {
			return c * t / d + b;
		}
		public static float LinearRecursive( float t, float b, float c, float d ) {
			return Linear( ( t/d < .5f ? t*2f : (d-t)*2f ), b, c, d );
		}
		public static float linearRecursive_3over4( float t, float b, float c, float d ) {
			return Linear( ( t/d < .75f ? t*1f/.75f : (d-t)*2f ), b, c, d );
		}
	
		
		public static float QuadIn( float t, float b, float c, float d ) {
			return c * ( t /= d ) * t + b;
		}
		public static float QuadOut( float t, float b, float c, float d ) {
			return -c * ( t /= d ) * ( t - 2 ) + b;
		}
		public static float QuadInOut( float t, float b, float c, float d ) {
			if ( ( t /= d / 2 ) < 1 ){ return c / 2 * t * t + b; }
			return -c / 2 * ( ( --t ) * ( t - 2 ) - 1 ) + b;
		}
		public static float QuadOutRecursive( float t, float b, float c, float d ) {
			return QuadOut( ( t/d < .5 ? t*2 : (d-t)*2 ), b, c, d );
		}
		public static float QuadInOutRecursive( float t, float b, float c, float d ) {
			return QuadInOut( ( t/d < .5 ? t*2 : (d-t)*2 ), b, c, d );
		}
		public static float QuadOutRecursive3over4( float t, float b, float c, float d ) {
			return QuadOut( ( t/d < 0.75f ? t*1f/.75f : (d-t)*2f ), b, c, d );
		}

		public static float CubicIn( float t, float b, float c, float d ) {
			t /= d;
			return c*t*t*t + b;
		}
		public static float CubicOut( float t, float b, float c, float d ) {
			t /= d;
			t--;
			return c*(t*t*t + 1) + b;
		}
		public static float CubicInOut( float t, float b, float c, float d ) {
			t /= d/2;
			if (t < 1) return c/2*t*t*t + b;
			t -= 2;
			return c/2*(t*t*t + 2) + b;
		}
	
		
		
		
		public static float BounceIn( float t, float b, float c, float d ) {
			return c - BounceOut( d - t, 0, c, d ) + b;
		}
		public static float BounceOut( float t, float b, float c, float d ) {
			if ( ( t /= d ) < ( 1f / 2.75f ) ) { return c * ( 7.5625f * t * t ) + b; }
			else if ( t < ( 2f / 2.75f ) ) { return c * ( 7.5625f * ( t -= ( 1.5f / 2.75f ) ) * t + .75f ) + b; }
			else if ( t < ( 2.5f / 2.75f ) ) { return c * ( 7.5625f * ( t -= ( 2.25f / 2.75f ) ) * t + .9375f ) + b; }
			else { return c * ( 7.5625f * ( t -= ( 2.625f / 2.75f ) ) * t + .984375f ) + b; }
		}
		public static float BounceInOut( float t, float b, float c, float d ) {
			if ( t < d / 2f ) { return BounceIn( t * 2f, 0f, c, d ) * .5f + b; }
			else { return BounceOut( t * 2f - d, 0f, c, d ) * .5f + c * .5f + b; }
		}
		
		
		public static float ElasticInOut( float t, float b, float c, float d ) {
			if ( ( t /= d / 2f ) == 2f ){ return b + c; }
			float p = d * ( .3f * 1.5f );
			float s = p / 4f;
			if ( t < 1f ){ return -.5f * ( c * Mathf.Pow( 2f, 10f * ( t -= 1f ) ) * Mathf.Sin( ( t * d - s ) * ( 2f * Mathf.PI ) / p ) ) + b; }
			return c * Mathf.Pow( 2f, -10f * ( t -= 1f ) ) * Mathf.Sin( ( t * d - s ) * ( 2f * Mathf.PI ) / p ) * .5f + c + b;
		}
		public static float ElasticIn( float t, float b, float c, float d ) {
			if ( ( t /= d ) == 1f ){ return b + c; }
			float p = d * .3f;
			float s = p / 4f;
			return -( c * Mathf.Pow( 2f, 10f * ( t -= 1f ) ) * Mathf.Sin( ( t * d - s ) * ( 2f * Mathf.PI ) / p ) ) + b;
		}
		public static float ElasticOut( float t, float b, float c, float d ) {
			if ( ( t /= d ) == 1f ){ return b + c; }
			float p = d * .3f;
			float s = p / 4f;
			return ( c * Mathf.Pow( 2f, -10f * t ) * Mathf.Sin( ( t * d - s ) * ( 2f * Mathf.PI ) / p ) + c + b );
		}
		
		public static float BackIn( float t, float b, float c, float d ) {
			return c * ( t /= d ) * t * ( ( 1.70158f + 1f ) * t - 1.70158f ) + b;
		}
		public static float BackOut( float t, float b, float c, float d ) {
			return c * ( ( t = t / d - 1 ) * t * ( ( 1.70158f + 1f ) * t + 1.70158f ) + 1f ) + b;
		}
		public static float BackOutDramatic( float t, float b, float c, float d ) {
			float ts = (t/=d)*t;
			float tc = ts*t;
			return b+c*(-0.600000000000005f*tc*ts + 5.79500000000002f*ts*ts + -5.79000000000002f*tc + -4.40499999999999f*ts + 6f*t);
		}
		public static float BackOutDramatic2( float t, float b, float c, float d ) {
			float ts = (t/=d)*t;
			float tc = ts*t;
			return b+c*(25.55f*tc*ts + -64.9975f*ts*ts + 62.795f*tc + -33.595f*ts + 11.2475f*t);
		}

		public static float BackInOut( float t, float b, float c, float d ) {
			float s = 1.70158f;
			if ( ( t /= d / 2f ) < 1f ){ return c / 2f * ( t * t * ( ( ( s *= ( 1.525f ) ) + 1f ) * t - s ) ) + b; }
			return c / 2f * ( ( t -= 2f ) * t * ( ( ( s *= ( 1.525f ) ) + 1f ) * t + s ) + 2f ) + b;
		}
		
		public static float BackOutSmooth( float t, float b, float c, float d ) {
			var ts=(t/=d)*t;
			var tc=ts*t;
			return b+c*(-3.50000000000001f*tc*ts + 7f*ts*ts + 5.32907051820075f-15f*tc + -8f*ts + 5.5f*t);
		}
		public static float BackOutBounce( float t, float b, float c, float d ) {
			var ts=(t/=d)*t;
			var tc=ts*t;
			return b+c*(-4.50000000000001f*tc*ts + 9f*ts*ts + -0.999999999999995f*tc + -8f*ts + 5.5f*t);
		}
		
		
		
		
		
		
		
		
		
		public static float CircIn( float t, float b, float c, float d ) {
			return -c * ( Mathf.Sqrt( 1 - ( t /= d ) * t ) - 1 ) + b;
		}
		public static float CircOut( float t, float b, float c, float d ) {
			return c * Mathf.Sqrt( 1 - ( t = t / d - 1 ) * t ) + b;
		}
		public static float CircInOut( float t, float b, float c, float d ) {
			if ( ( t /= d / 2 ) < 1 ) { return -c / 2 * ( Mathf.Sqrt( 1 - t * t ) - 1 ) + b; }
			return c / 2 * ( Mathf.Sqrt( 1 - ( t -= 2 ) * t ) + 1 ) + b;
		}
		
		
		public static float QuintIn( float t, float b, float c, float d ) {
			return c * ( t /= d ) * t * t * t * t + b;
		}
		public static float QuintOut( float t, float b, float c, float d ) {
			return c * ( ( t = t / d - 1 ) * t * t * t * t + 1 ) + b;
		}
		public static float QuintInOut( float t, float b, float c, float d ) {
			if ( ( t /= d / 2 ) < 1 ) { return c / 2 * t * t * t * t * t + b; }
			return c / 2 * ( ( t -= 2 ) * t * t * t * t + 2 ) + b;
		}
		
		
		public static float ExpoIn( float t, float b, float c, float d ) {
			return ( t == 0 ) ? b : c * Mathf.Pow( 2, 10 * ( t / d - 1 ) ) + b;
		}
		public static float ExpoOut( float t, float b, float c, float d ) {
			return ( t == d ) ? b + c : c * ( -Mathf.Pow( 2, -10 * t / d ) + 1 ) + b;
		}
		public static float ExpoInOut( float t, float b, float c, float d ) {
			if ( t == 0 ){ return b; }
			if ( t == d ){ return b + c; }
			if ( ( t /= d / 2 ) < 1 ){ return c / 2 * Mathf.Pow( 2, 10 * ( t - 1 ) ) + b; }
			return c / 2 * ( -Mathf.Pow( 2, -10 * --t ) + 2 ) + b;
		}
		public static float ExpoOutRecursive( float t, float b, float c, float d ) {
			return ExpoOut( (t / d < .5 ? t * 2 : (d - t) * 2), b, c, d );
		}
		
		
		public static float BattlePatternEase( float t, float b, float c, float d ) {
			var ts=(t/=d)*t;
			var tc=ts*t;
			return b+c*(-11.7f*tc*ts + 1.65749999999998f*ts*ts + 39.885f*tc + -39.69f*ts + 10.8475f*t);
		}
		
		public static float ShakeEase2( float t, float b, float c, float d ) {
			var ts=(t/=d)*t;
			var tc=ts*t;
			return b+c*(24.9f*tc*ts + -128.5425f*ts*ts + 194.585f*tc + -110.69f*ts + 20.7475f*t);
		}
		
		public static float ShakeEase( float t, float b, float c, float d ) {
			return QuadOut( ( t/d < .5f ? t*2f : (d-t)*2f ), b, c, d );
			//return ShakeEase( ( t/d < .5f ? t*2f : (d-t)*2f ), b, c, d );
		}
	
		
		public static bool SimpleEase( ref float s, float e, float step ) {
			float diff = (e-s)*step;
			if( s != e ) {
				s += diff;
				if( Mathf.Abs( e-s ) < .1f ) {
					s = e;
				}
				return true;
			}
			return false;
		}
	
	
		//public static List<EaseVal> mEaseVals = new List<EaseVal>();
		//static public void UnPauseAllEaseVals() {
		//	int len = EaseFunc.mEaseVals.Count;
		//	if( len == 0 ){ return; }
		//	for( var i = 0; i < len; i++ ) {
		//		EaseFunc.mEaseVals[i].pause = 0f;
		//	}
		//	EaseFunc.mEaseVals.Clear();
		//}
	
	}
	
}