using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TMX.Editor
{
	public class EditorCoroutine
	{
		public static EditorCoroutine Start( IEnumerator _routine )
		{
			EditorCoroutine coroutine = new EditorCoroutine(_routine);
			coroutine.start();
			return coroutine;
		}

        public static void Stop (EditorCoroutine coroutine)
        {
            coroutine.stop();
        }

        public readonly IEnumerator routine;
		public EditorCoroutine( IEnumerator _routine )
		{
			routine = _routine;
		}

		public void start()
		{
			//Dbg.Msg("start");
			EditorApplication.update += update;
		}
		public void stop()
		{
			//Dbg.Msg("stop");
			EditorApplication.update -= update;
		}

		void update()
		{
			/* NOTE: no need to try/catch MoveNext,
			 * if an IEnumerator throws its next iteration returns false.
			 * Also, Unity probably catches when calling EditorApplication.update.
			 */

			//Dbg.Msg("update");
			if (!routine.MoveNext())
			{
				stop();
			}
		}
	}
}