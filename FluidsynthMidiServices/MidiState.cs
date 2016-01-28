using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using Android.Content;
using Android.Media;
using Android.Media.Midi;
using Android.Runtime;
using Commons.Music.Midi;
using Commons.Music.Midi.Mml;
using NFluidsynth;
using NFluidsynth.MidiManager;

namespace FluidsynthMidiServices
{
	public class MidiState
	{
		const string predefined_temp_path = "/data/local/tmp/name.atsushieno.soundfontprovider";

		IMidiAccess acc;
		IMidiOutput output;
		
		public static MidiState Instance { get; private set; }

		static MidiState ()
		{
			Instance = new MidiState ();
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
				//var sr = double.Parse (manager.GetProperty (AudioManager.PropertyOutputSampleRate));
				//settings [ConfigurationKeys.SynthSampleRate].DoubleValue = sr;
				settings [ConfigurationKeys.SynthSampleRate].DoubleValue = 11025;
				
				var fpb = double.Parse (manager.GetProperty (AudioManager.PropertyOutputFramesPerBuffer));
				settings [ConfigurationKeys.AudioPeriodSize].IntValue = (int) fpb;
				//settings [ConfigurationKeys.SynthThreadSafeApi].IntValue = 0;
			};
			string sf2Dir = context.ObbDir != null ? Path.Combine (context.ObbDir.AbsolutePath) : null;
			if (sf2Dir != null && Directory.Exists (sf2Dir))
				foreach (var sf2 in Directory.GetFiles (sf2Dir, "*.sf2", SearchOption.AllDirectories))
					acc.Soundfonts.Add (sf2);
#if DEBUG
			foreach (var sf2 in Directory.GetFiles (predefined_temp_path, "*.sf2", SearchOption.AllDirectories))
				if (!acc.Soundfonts.Any (_ => Path.GetFileName (_) == Path.GetFileName (sf2)))
					acc.Soundfonts.Add (sf2);
#endif
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
	
