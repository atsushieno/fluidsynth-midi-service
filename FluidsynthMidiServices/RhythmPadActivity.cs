
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace FluidsynthMidiServices
{
	[Activity (Label = "Sample Rhythm Pad")]
	public class RhythmPadActivity : Activity, ISurfaceHolderCallback2
	{
		bool initialized;
		
		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

			SetContentView (Resource.Layout.RhythmPad);
			
			SurfaceView sv = FindViewById<SurfaceView> (Resource.Id.mainSurface);
			sv.Holder.AddCallback (this);
			sv.Touch += (sender, e) => {
				int index = -1;
				bool on = false;
				switch (e.Event.Action) {
				case MotionEventActions.Down:
					on = true;
					goto case MotionEventActions.PointerIndexMask;
				case MotionEventActions.Up:
					on = false;
					goto case MotionEventActions.PointerIndexMask;
				case MotionEventActions.PointerDown:
					on = true;
					index = e.Event.ActionIndex;
					goto case MotionEventActions.PointerIndexMask;
				case MotionEventActions.PointerUp:
					on = false;
					index = e.Event.ActionIndex;
					goto case MotionEventActions.PointerIndexMask;
				case MotionEventActions.PointerIndexMask:
					var x = index < 0 ? e.Event.RawX : e.Event.GetX (index);
					var y = index < 0 ? e.Event.RawY : e.Event.GetY (index);
					if (8 <= x && x < 7 * 64 + 48 + 8 &&
					    8 <= y && y < 5 * 64 + 48 + 8) {
						int h = (int) ((x - 8) / 64);
						int v = (int) ((y - 8) / 64);
						var output = MidiState.Instance.GetMidiOutput (this);
						if (!initialized) {
							initialized = true;
							output.SendAsync (new Byte [] { 0xB9, 0, 1 }, 0, 3, 0);
							output.SendAsync (new Byte [] { 0xB9, 0x20, 0 }, 0, 3, 0);
							output.SendAsync (new Byte [] { 0xB9, 11, 127 }, 0, 3, 0);
							output.SendAsync (new Byte [] { 0xC9, 0 }, 0, 2, 0);
						}
						output.SendAsync (new byte [] {
							(byte) (on ? 0x99 : 0x89),
							(byte) (0x30 + v * 8 + h),
							(byte) (on ? 120 : 0)}, 0, 3, 0);
					}
					break;
				}
			};
		}
		
		void Redraw (ISurfaceHolder holder)
		{
			var canvas = holder.LockCanvas ();
			var paint = new Paint () { Color = Color.White, StrokeWidth = 2 };
			for (int v = 0; v < 4; v++)
				for (int h = 0; h < 8; h++)
					canvas.DrawRoundRect (8 + h * 64, 8 + v * 64, h * 64 + 48 + 8, v * 64 + 48 + 8, 5, 5, paint);
			holder.UnlockCanvasAndPost (canvas);
		}

		public void SurfaceChanged (ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
		{
			Redraw (holder);
		}

		public void SurfaceCreated (ISurfaceHolder holder)
		{
		}

		public void SurfaceDestroyed (ISurfaceHolder holder)
		{
		}

		public void SurfaceRedrawNeeded (ISurfaceHolder holder)
		{
			Redraw (holder);
		}
	}
}

