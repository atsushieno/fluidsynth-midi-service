using System;
using System.Threading;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Media.Midi;
using System.Linq;
using Commons.Music.Midi;
using NFluidsynth;
using Android.Media;
using Commons.Music.Midi.Mml;
using System.IO;
using NFluidsynth.MidiManager;

namespace FluidsynthMidiServices
{
	[Activity (Label = "FluidsynthMidiServices", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		FluidsynthMidiReceiver recv;
		FluidsynthMidiAccess acc;
		
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			SetContentView (Resource.Layout.Main);
			
			acc = new FluidsynthMidiAccess ();
			acc.HandleNativeError = (messageManaged, messageNative) => {
				Android.Util.Log.Error ("FluidsynthPlayground", messageManaged + " : " + messageNative);
				return true;
			};
			acc.ConfigureSettings += settings => {
				settings [ConfigurationKeys.AudioSampleFormat].StringValue = "16bits";
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
			
			var playChordButton = FindViewById<Button> (Resource.Id.playChord);
			bool noteOn = false;
			
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
				playChordButton.Text = noteOn ? "playing" : "off";
			};
			
			var music = SmfMusic.Read (this.Assets.Open ("rain.mid"));
			MidiPlayer player = null;
			var playSongButton = FindViewById<Button> (Resource.Id.playSong);
			playSongButton.Click += delegate {
				if (player == null || player.State == PlayerState.Paused || player.State == PlayerState.Stopped) {
					if (player == null)
						player = new MidiPlayer (music, acc);
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
			MidiPlayer player2 = null;
			playMmlButton.Click += delegate {
				if (player2 == null || player2.State == PlayerState.Paused || player2.State == PlayerState.Stopped) {
					var compiler = new MmlCompiler ();
					compiler.Resolver = new AssetResolver (this);
					var midiStream = new MemoryStream ();
					var source = new MmlInputSource ("", new StringReader (mmlEditText.Text));
					try {
						compiler.Compile (false, Enumerable.Repeat (source, 1).ToArray (), null, midiStream, false);
					} catch (MmlException ex) {
						Android.Util.Log.Error ("FluidsynthPlayground", ex.ToString ());
						Toast.MakeText (this, ex.Message, ToastLength.Long).Show ();
						return;
					}
					
					var music2 = SmfMusic.Read (new MemoryStream (midiStream.ToArray ()));
					player2 = new MidiPlayer (music2, acc);
					
					playMmlButton.Text = "playing";
					player2.PlayAsync ();
				} else {
					playMmlButton.Text = "stopped";
					player2.PauseAsync ();
					player2.Dispose ();
				}
			};
		}

		class AssetResolver : StreamResolver
		{
			Context context;
			public AssetResolver (Context context)
			{
				this.context = context;
			}
			
			public override TextReader Resolve (string uri)
			{
				return new StreamReader (context.Assets.Open (uri));
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


