using UnityEngine;

namespace CustomEase {
	public class EaseVal {
		public delegate float EaseEq( float t, float b, float c, float d );
	
		EaseEq eq = EaseFunc.QuadInOut;
	
		float c = 0f;
        /// <summary>
        /// スタート
        /// </summary>
		float s = 0f;
        /// <summary>
        /// エンド
        /// </summary>
		float e = 0f;
        /// <summary>
        /// スタートフレーム
        /// </summary>
		float sf = 0f;
        /// <summary>
        /// エンドフレーム
        /// </summary>
		float ef = 0f;
		bool isRecursive = false;
		// pause when new action (to, set) is asigned. and then in the next frame, we will un-pause all the actions at once
		// else we could have started the animation too soon if the target object is right below the object that launches it
		public float pause = 0f;
		public float timeTaking { get { return ef; } }
		public EaseVal( float initVal, EaseEq eq = null ) {
			Init(initVal);
			if( eq != null ) {
				this.eq = eq;
				isRecursive = eq.Method.Name.IndexOf( "ecursive" ) != -1;
			}
		}
		
		public void Init( float val ){
			Set(val);
			c = val;
			sf = this.ef = 0.001f;
			// pause = true;
			// easeFunc.ease_vals.push( this );
		}
		public void Set( float val ){
			s = e = val;
			ef = 0.001f;
			sf = 0.0009999f;
			// pause = true;
			// easeFunc.ease_vals.push( this );
		}
		public float Get(){
			return c;
		}
		public void FromTo( float from, float to, float frameTaking, EaseEq eq = null ){
			Set( from );
			To( to, frameTaking, eq );
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="val"></param>
        /// <param name="frameTaking"></param>
        /// <param name="eq"></param>
		public void To( float val, float frameTaking, EaseEq eq = null ){
			if( frameTaking < 0f ) {	//if frame taking is 0, use set instead
				Init( val );
				return;
			} else if( frameTaking == 0f ) {	//if frame taking is 0, use set instead
				Set( val );
				return;
			}// else if( frame_taking == undefined ) { frame_taking = 0.0005f; }
			if( ef == 0.001f && sf == 0.0009999f ) {	// havn't step set yet
				s = e;
				c = s;
			}
			if( eq != null ) {
				this.eq = eq;
				isRecursive = eq.Method.Name.IndexOf( "ecursive" ) != -1;
			}
			s = c;
			
			e = val;
			if(c == val) {
				ef = 0.001f;
				sf = 0.0009999f;
			} else {
				sf = 0f;
				ef = frameTaking;
			}
			if( frameTaking < 0.0005f ) {
				pause = -999f;
				//EaseFunc.mEaseVals.Add( this );
			}
		}
		
		public bool isPaused{
			get{
				if( 0 < pause ) { return true; }
				else if( pause == -999f ) { return true; }
				return false;
			}
		}
		public bool Step(){
			if( 0 < pause ) { pause -= Time.deltaTime; return true; }
			else if( pause == -999f ) { return true; }
			//if( !Mathf.Approximately( sf, ef ) ) {
			if( sf != ef ) {
				sf += Time.deltaTime;
				if( ef <= sf ) {
					sf = ef;
					
					if( isRecursive ){ c = s; }
					else { c = e; }
					
				} else {
					c = this.eq( sf, s, (e-s), ef );
				}
				return true;	// stilll need update
				// } else {	// commented out because of the return easing
				// if( this.c != this.e ) {
				// this.c = this.e;
				// return true;
				// }
			}
			return false;	// need no update
		}
		
		public void SetEaseEquation( EaseEq eq ){
			if( eq == null ) {
				this.eq = EaseFunc.QuadInOut;
				isRecursive = false;
			} else {
				this.eq = eq;
				isRecursive = eq.Method.Name.IndexOf( "ecursive" ) != -1;
			}
		}

		public float GetDirectionalProgress(){
			if( e < s ) {
				return (ef-sf) / ef;
			}
			return sf / ef;
		}
		public float GetFrom(){
			return s;
		}
		public float GetTo(){
			return e;
		}
		public float GetProgress(){
			return sf / ef;
//			float diff = s - e;
//			if( diff == 0 ) { return 0; }
//			else {
//				return ( diff - ( this.c - this.e ) ) / diff;
//			}
		}
	}

}