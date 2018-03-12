using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Media;
using Android.Media.Midi;
using Android.OS.Storage;
using Android.Runtime;
using Commons.Music.Midi;
using Commons.Music.Midi.Mml;
using NFluidsynth;
using NFluidsynth.MidiManager;

namespace FluidsynthMidiServices
{
	public class MidiState
	{
		const string predefined_temp_path = "/data/local/tmp/name.atsushieno.fluidsynthmidideviceservice";

		IMidiAccess acc;
		IMidiOutput output;
		
		public static MidiState Instance { get; private set; }

		static MidiState ()
		{
			Instance = new MidiState ();
		}

		public void MountObbs (Context context)
		{
			if ((int) Android.OS.Build.VERSION.SdkInt >= (int) Android.OS.BuildVersionCodes.Kitkat) {
				var obbMgr = context.GetSystemService (Context.StorageService).JavaCast<StorageManager> ();
				var obbs = context.GetObbDirs ().SelectMany (d => Directory.GetFiles (d.Path, "*.obb"));
				foreach (var obb in obbs.Where (obb => !obbMgr.IsObbMounted (obb)))
					obbMgr.MountObb (obb, null, new ObbListener ());
			}
		}
		
		public IMidiOutput GetMidiOutput (Context context)
		{
			if (acc == null) {
				SetupMidiAccess (context);
				output = acc.OpenOutputAsync (acc.Outputs.First ().Id).Result;
			}
			return output;
		}
		
		void SetupMidiAccess (Context context)
		{
#if TEST_MIDI_API_BASED_ACCESS
			acc = new Commons.Music.Midi.AndroidMidiAccess.MidiAccess (this);
#else
			var acc = new FluidsynthMidiAccess ();
			this.acc = acc;
			acc.HandleNativeError = (messageManaged, messageNative) => {
				Android.Util.Log.Error ("FluidsynthPlayground", messageManaged + " : " + messageNative);
				return true;
			};
			acc.ConfigureSettings += settings => {
				settings [ConfigurationKeys.AudioSampleFormat].StringValue = "16bits"; // float or 16bits
				
				var manager = context.GetSystemService (Context.AudioService).JavaCast<AudioManager> ();
				
				// Note that SynthSampleRate is NOT audio sample rate but *synthesizing* sample rate.
				// So it is kind of wrong assumption that AudioManager.PropertyOutputSampleRate would give the best outcome...
				var sr = double.Parse (manager.GetProperty (AudioManager.PropertyOutputSampleRate));
				settings [ConfigurationKeys.SynthSampleRate].DoubleValue = sr;
				//settings [ConfigurationKeys.SynthSampleRate].DoubleValue = 11025;

				var fpb = double.Parse (manager.GetProperty (AudioManager.PropertyOutputFramesPerBuffer));
				settings [ConfigurationKeys.AudioPeriodSize].IntValue = (int) fpb;
				settings [ConfigurationKeys.SynthThreadSafeApi].IntValue = 0;
			};
			acc.SoundFontLoaderFactories.Add (syn => new AndroidAssetSoundFontLoader (syn, context.Assets));
			SynthAndroidExtensions.GetSoundFonts (acc.SoundFonts, context, predefined_temp_path);
#endif
		}
	}

	class ObbListener : OnObbStateChangeListener
	{
		public override void OnObbStateChange (string path, ObbState state)
		{
			// dummy receiver
		}
	}

	static class SynthAndroidExtensions
	{
		public static void GetSoundFonts (IList<string> soundFonts, Context context, string predefinedTempPath, CancellationToken cancellationToken = default (CancellationToken))
		{
			// OBB support
			if ((int) Android.OS.Build.VERSION.SdkInt >= (int) Android.OS.BuildVersionCodes.Kitkat) {
				var obbMgr = context.GetSystemService (Context.StorageService).JavaCast<StorageManager> ();
				var obbs = context.GetObbDirs ().SelectMany (d => Directory.GetFiles (d.Path, "*.obb"));
				foreach (var obbDir in obbs.Where (d => obbMgr.IsObbMounted (d)).Select (d => obbMgr.GetMountedObbPath (d)))
					foreach (var sf2 in Directory.GetFiles (obbDir, "*.sf2", SearchOption.AllDirectories))
						soundFonts.Add (sf2);
			}

			// Assets
			foreach (var asset in context.Assets.List (""))
				if (asset.EndsWith (".sf2", StringComparison.OrdinalIgnoreCase))
					soundFonts.Add (asset);
#if DEBUG
			// temporary local files for debugging
			if (Directory.Exists (predefinedTempPath))
				foreach (var sf2 in Directory.GetFiles (predefinedTempPath, "*.sf2", SearchOption.AllDirectories))
					if (!soundFonts.Any (_ => Path.GetFileName (_) == Path.GetFileName (sf2)))
						soundFonts.Add (sf2);
#endif
		}
	}


	class AssetOrUrlResolver : StreamResolver
	{
		Context context;
		public AssetOrUrlResolver (Context context)
		{
			this.context = context;
		}
		
		public System.IO.Stream ResolveStream (string url)
		{
			System.IO.Stream stream = null;
			System.Uri uri;
			if (Uri.TryCreate (null, UriKind.RelativeOrAbsolute, out uri) && uri.Scheme != Uri.UriSchemeFile)
				return new HttpClient ().GetStreamAsync (url).Result;
			foreach (var s in new string [] {url, "mugene/mml/" + url}) {
				try {
					return context.Assets.Open (s);
				} catch (Exception) {
					stream = null;
				}
			}
			return stream;
		}
		
		public override TextReader Resolve (string url)
		{
			return new StreamReader (ResolveStream (url));
		}
	}

	class Listener : Java.Lang.Object, MidiManager.IOnDeviceOpenedListener
	{
		public void OnDeviceOpened (MidiDevice device)
		{
			//var port = device.OpenInputPort (device.Info.GetPorts ().First (p => p.Type == MidiPortType.Input).PortNumber);
			//port.Send (new Byte [] { 0xC0, 0x0 }, 0, 2);
			//port.Send (new byte [] { 0x90, 0x64, 110 }, 0, 3);
			//port.Send (new byte [] { 0x80, 0x64, 110 }, 0, 3);
			Console.WriteLine ("Sent noteOn and noteOff");
		}
	}
}
	
