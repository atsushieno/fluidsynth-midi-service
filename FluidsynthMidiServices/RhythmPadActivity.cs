﻿
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
		int size;
		
		const int pad_size_denom = 10;
		const int round_size_denom = 16;
		const int num_cols = 8;
		const int num_rows = 8;
		
		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

			SetContentView (Resource.Layout.RhythmPad);

			SurfaceView sv = FindViewById<SurfaceView> (Resource.Id.mainSurface);
			sv.Holder.AddCallback (this);

			sv.Touch += (sender, e) => {
				int index = -1;
				bool on = false;
				switch ((MotionEventActions) ((int) e.Event.Action & (int) MotionEventActions.Mask)) {
				// single touch...
				case MotionEventActions.Down:
					on = true;
					goto case MotionEventActions.PointerIndexMask;
				case MotionEventActions.Up:
					on = false;
					goto case MotionEventActions.PointerIndexMask;
				// it never fires for the first touch... .Down is raised instead :/
				case MotionEventActions.PointerDown:
					on = true;
					index = e.Event.ActionIndex;
					goto case MotionEventActions.PointerIndexMask;
				case MotionEventActions.PointerUp:
					on = false;
					index = e.Event.ActionIndex;
					goto case MotionEventActions.PointerIndexMask;
				case MotionEventActions.PointerIndexMask:
					var padding = size / pad_size_denom;
					var x = index < 0 ? e.Event.GetX () : e.Event.GetX (index);
					var y = index < 0 ? e.Event.GetY () : e.Event.GetY (index);
					if (padding <= x && x < num_cols * size - padding * 2 &&
					    padding <= y && y < num_rows * size - padding * 2) {
						int h = (int) ((x - padding) / size);
						int v = (int) ((y - padding) / size);
						var output = MidiState.Instance.GetMidiOutput (this);
						if (!initialized) {
							initialized = true;
							output.Send (new Byte [] { 0xB9, 0, 1 }, 0, 3, 0);
							output.Send (new Byte [] { 0xB9, 0x20, 0 }, 0, 3, 0);
							output.Send (new Byte [] { 0xB9, 11, 127 }, 0, 3, 0);
							output.Send (new Byte [] { 0xC9, 0 }, 0, 2, 0);
						}
						output.Send (new byte [] {
							(byte) (on ? 0x99 : 0x89),
							(byte) (0x20 + v * 8 + h),
							(byte) (on ? 120 : 0)}, 0, 3, 0);
					}
					break;
				default:
					break;
				}
			};
		}
		
		void Redraw (ISurfaceHolder holder)
		{
			int padding = size / pad_size_denom;
			int round = size / round_size_denom;
			var canvas = holder.LockCanvas ();
			var paint = new Paint () { Color = Color.White, StrokeWidth = 2 };
			size = (int) Math.Min (canvas.Width / num_cols, canvas.Height / num_rows);
			for (int v = 0; v < num_cols; v++)
				for (int h = 0; h < num_rows; h++)
					canvas.DrawRoundRect (padding + h * size, padding + v * size, (h + 1) * size - 2 * padding, (v + 1) * size - 2 * padding, round, round, paint);
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

