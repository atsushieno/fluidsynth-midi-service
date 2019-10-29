#define NATIVE_ASSET_SFLOADER
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Android.Content.Res;
using Android.Runtime;

namespace NFluidsynth
{
	public class AndroidNativeAssetSoundFontLoader : SoundFontLoader
	{
		static T DoAndThen<T> (Action action, Func<T> then)
		{
			action ();
			return then ();
		}

		public AndroidNativeAssetSoundFontLoader (Settings settings, AssetManager assetManager)
			: base (DoAndThen (() => set_asset_manager_context (JNIEnv.Handle, IntPtr.Zero, assetManager.Handle), () => new_fluid_android_asset_sfloader (settings.Handle, IntPtr.Zero)))
		{
			this.asset_manager = GCHandle.Alloc (asset_manager);
		}

		GCHandle asset_manager;

		public override void Dispose ()
		{
			if (asset_manager.IsAllocated)
				asset_manager.Free ();
			base.Dispose ();
		}

		[DllImport ("fluidsynth-assetloader", EntryPoint = "Java_fluidsynth_androidextensions_NativeHandler_setAssetManagerContext")]
		static extern void set_asset_manager_context (IntPtr jniEnv, IntPtr __this, IntPtr assetManager);

		[DllImport ("fluidsynth-assetloader")]
		static extern IntPtr new_fluid_android_asset_sfloader (IntPtr settings, IntPtr nativeAssetManager);
	}

	public class AndroidAssetSoundFontLoader : SoundFontLoader
	{
		public AndroidAssetSoundFontLoader (Settings settings, AssetManager assetManager)
			: base (SoundFontLoader.NewDefaultSoundFontLoader(settings).Handle)
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

			public override IntPtr Open (string filename)
			{
				var stream = am.Open (filename, Access.Random);
				// it is ugly, but since Xamarin.Android does not offer
				// seekable stream via AssetManager, we first load everything in memory and then store it as MemoryStream.
				var ms = new MemoryStream (1048576);
				stream.CopyTo (ms);
				stream.Close ();
				streams [streams.Count + 1] = ms;
				ms.Position = 0;
				return (IntPtr) (streams.Count);
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
