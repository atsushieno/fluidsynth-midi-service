using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Android.Content.Res;

namespace NFluidsynth
{
	public class AndroidAssetSoundFontLoader : SoundFontLoader
	{
		public AndroidAssetSoundFontLoader (Synth synth, AssetManager assetManager)
			: base (synth)
		{
			this.am = assetManager;
		}

		AssetManager am;
		Dictionary<int, Stream> streams = new Dictionary<int, Stream> ();

		public override IntPtr Load (string filename)
		{
			return Open (filename);
		}

		public override void Free ()
		{
			am.Close ();
		}

		int counter;
		public override IntPtr Open (string filename)
		{
			var stream = am.Open (filename);
			streams [counter] = stream;
			return (IntPtr) counter++;
		}

		public override int Close (IntPtr handle)
		{
			streams [(int)handle].Close ();
			return 0;
		}

		byte [] buffer = new byte [1024];

		public override int Read (IntPtr buf, long count, IntPtr handle)
		{
			if (count > buffer.Length)
				buffer = new byte [count];
			if (count > int.MaxValue)
				throw new NotSupportedException ();
			int ret = streams [(int) handle].Read (buffer, 0, (int) count);
			Marshal.Copy (buffer, 0, buf, ret);
			return ret;
		}

		public override int Seek (IntPtr handle, long position, int origin)
		{
			var ret = streams [(int)handle].Seek (position, (SeekOrigin)origin);
			if (ret > int.MaxValue)
				throw new InvalidOperationException ("Stream seek past int max size, which is unsupported.");
			return (int) ret;
		}

		public override int Tell (IntPtr handle)
		{
			var ret = streams [(int)handle].Position;
			if (ret > int.MaxValue)
				throw new InvalidOperationException ("Stream position past int max size, which is unsupported.");
			return (int)ret;
		}
	}
}
