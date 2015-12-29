using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;

using Android.App;
using Android.Content;
using Android.Media;
using Android.Media.Midi;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Commons.Music.Midi;
using Commons.Music.Midi.Mml;
using NFluidsynth;
using NFluidsynth.MidiManager;

namespace FluidsynthMidiServices
{
	[Activity (Label = "FluidsynthMidiServices", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		FluidsynthMidiReceiver recv;
		FluidsynthMidiAccess acc;
		MidiPlayer player = null;
		
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			SetContentView (Resource.Layout.Main);
			
			var playChordButton = FindViewById<Button> (Resource.Id.playChord);
			bool noteOn = false;
			if (Android.OS.Build.VERSION.SdkInt < BuildVersionCodes.M)
				playChordButton.Enabled = false; // MIDI API not supported.
			
			playChordButton.Click += delegate {
				//var midiService = this.GetSystemService (MidiService).JavaCast<MidiManager> ();
				//var devs = midiService.GetDevices ();
				
				if (recv == null) {
					recv = new FluidsynthMidiReceiver (this);
					recv.OnSend (new Byte [] { 0xB0, 7, 127 }, 0, 3, 0);
					recv.OnSend (new Byte [] { 0xB0, 11, 127 }, 0, 3, 0);
					recv.OnSend (new Byte [] { 0xC0, 30 }, 0, 2, 0);
				}
				if (noteOn) {
					recv.OnSend (new Byte [] { 0x80, 0x30, 0x78 }, 0, 3, 0);
					recv.OnSend (new Byte [] { 0x80, 0x39, 0x78 }, 0, 3, 0);
				} else {
					recv.OnSend (new Byte [] { 0x90, 0x30, 0x60 }, 0, 3, 0);
					recv.OnSend (new Byte [] { 0x90, 0x39, 0x60 }, 0, 3, 0);
				}
				noteOn = !noteOn;
				playChordButton.Text = noteOn ? "playing" : "Test Android MIDI API";
			};
			
			var songFileOrUrlTextEdit = FindViewById<EditText> (Resource.Id.songFileNameOrUrlEditText);
			var playSongButton = FindViewById<Button> (Resource.Id.playSong);
			playSongButton.Click += delegate {
				if (player == null || player.State == PlayerState.Paused || player.State == PlayerState.Stopped) {
					if (player == null)
						StartNewSong (GetSongData (songFileOrUrlTextEdit.Text ?? "escape.mid")); // if empty, play some song from asset.
					playSongButton.Text = "playing";
					player.PlayAsync ();
				} else {
					playSongButton.Text = "paused";
					player.PauseAsync ();
				}
			};
			
			var mmlEditText = FindViewById<EditText> (Resource.Id.editText);
			mmlEditText.Text = new StreamReader (Assets.Open ("rain.mml")).ReadToEnd ();
			var playMmlButton = FindViewById<Button> (Resource.Id.playMML);
			playMmlButton.Click += delegate {
				if (player == null) {
					SmfMusic song;
					try {
						song = CompileMmlToSong (mmlEditText.Text);
					} catch (MmlException ex) {
						Log.Error ("FluidsynthPlayground", ex.ToString ());
						Toast.MakeText (this, ex.Message, ToastLength.Long).Show ();
						return;
					}
					
					StartNewSong (song);
					
					playMmlButton.Text = "playing";
				} else {
					playMmlButton.Text = "stopped";
					player.PauseAsync ();
					player.Dispose ();
					player = null;
				}
			};
		}
		
		SmfMusic GetSongData (string url)
		{
			return SmfMusic.Read (new AssetOrUrlResolver (this).ResolveStream (url));
		}
		
		SmfMusic CompileMmlToSong (string mml)
		{
			var compiler = new MmlCompiler ();
			compiler.Resolver = new AssetOrUrlResolver (this);
			var midiStream = new MemoryStream ();
			var source = new MmlInputSource ("", new StringReader (mml));
			compiler.Compile (false, Enumerable.Repeat (source, 1).ToArray (), null, midiStream, false);
			return SmfMusic.Read (new MemoryStream (midiStream.ToArray ()));
		}
		
		void StartNewSong (SmfMusic music)
		{
			if (player != null)
				player.Dispose ();
			if (acc == null)
				SetupMidiAccess ();
			player = new MidiPlayer (music, acc);
			player.PlayAsync ();
		}
		
		void SetupMidiAccess ()
		{
			acc = new FluidsynthMidiAccess ();
			acc.HandleNativeError = (messageManaged, messageNative) => {
				Android.Util.Log.Error ("FluidsynthPlayground", messageManaged + " : " + messageNative);
				return true;
			};
			acc.ConfigureSettings += settings => {
				settings [ConfigurationKeys.AudioSampleFormat].StringValue = "16bits"; // float or 16bits
				// Note that it is NOT audio sample rate but *synthesizing* sample rate.
				// So it is kind of wrong assumption that AudioManager.PropertyOutputSampleRate would give the best outcome...
				var manager = GetSystemService (Context.AudioService).JavaCast<AudioManager> ();
				var sr = double.Parse (manager.GetProperty (AudioManager.PropertyOutputSampleRate));
				settings [ConfigurationKeys.SynthSampleRate].DoubleValue = sr;
				var fpb = double.Parse (manager.GetProperty (AudioManager.PropertyOutputFramesPerBuffer));
				settings [ConfigurationKeys.AudioPeriodSize].IntValue = (int) fpb;
				//settings [ConfigurationKeys.SynthThreadSafeApi].IntValue = 0;
			};
			string default_soundfont = "/sdcard/tmp/FluidR3_GM.sf2";
			acc.Soundfonts.Add (default_soundfont);
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
}

