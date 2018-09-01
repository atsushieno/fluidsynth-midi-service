using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Android.Content.Res;
using static NFluidsynth.Native.LibFluidsynth;

namespace NFluidsynth
{
	public class AndroidAssetSoundFontLoader : SoundFontLoader
	{
		public AndroidAssetSoundFontLoader (Settings settings, AssetManager assetManager)
			: base (SfLoader.new_fluid_defsfloader (settings.Handle))
		{
			SetCallbacks (new AssetLoaderCallbacks (assetManager));
		}

		class AssetLoaderCallbacks : SoundFontLoaderCallbacks
		{
			public AssetLoaderCallbacks (AssetManager assetManager)
			{
				this.am = assetManager;
			}

			AssetManager am;

			Dictionary<int, Stream> streams = new Dictionary<int, Stream> ();

			int counter;
			public override IntPtr Open (string filename)
			{
				var stream = am.Open (filename, Access.Random);
				streams [counter] = stream;
				return (IntPtr)counter++;
			}

			public override int Close (IntPtr sfHandle)
			{
				streams [(int)sfHandle].Close ();
				return 0;
			}

			byte [] buffer = new byte [1024];

			public override int Read (IntPtr buf, long count, IntPtr sfHandle)
			{
				if (count > buffer.Length)
					buffer = new byte [count];
				if (count > int.MaxValue)
					throw new NotSupportedException ();
				int ret = streams [(int)sfHandle].Read (buffer, 0, (int)count);
				Marshal.Copy (buffer, 0, buf, ret);
				return ret;
			}

			public override int Seek (IntPtr sfHandle, int offset, SeekOrigin origin)
			{
				var ret = streams [(int)sfHandle].Seek (offset, origin);
				if (ret > int.MaxValue)
					throw new InvalidOperationException ("Stream seek past int max size, which is unsupported.");
				return (int)ret;
			}

			public override int Tell (IntPtr sfHandle)
			{
				var ret = streams [(int)sfHandle].Position;
				if (ret > int.MaxValue)
					throw new InvalidOperationException ("Stream position past int max size, which is unsupported.");
				return (int)ret;
			}
		}
	}
}
