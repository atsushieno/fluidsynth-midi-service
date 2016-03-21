using System;
using System.IO;
using System.Linq;

using Android.App;
using Android.Content;
using Android.OS;
using Android.OS.Storage;
using Android.Util;
using Android.Widget;
using Java.Interop;
using Commons.Music.Midi;
using Commons.Music.Midi.Mml;

namespace FluidsynthMidiServices
{
	[Activity (Label = "FluidsynthMidiServices", MainLauncher = true, Icon = "@mipmap/icon")]
	public class PlaygroundsMainActivity : Activity
	{
		FluidsynthMidiReceiver recv;
		
		MidiPlayer player;
		
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			SetContentView (Resource.Layout.PlaygroundsMain);
			
			FindViewById<Button> (Resource.Id.openSoundFontManager).Click += delegate {
				this.StartActivity (new Intent (this, typeof (SoundFontProvider.SoundFontProviderMainActivity)));
			};

			FindViewById<Button> (Resource.Id.openFreeStylePad).Click += delegate {
				this.StartActivity (new Intent (this, typeof (RhythmPadActivity)));
			};

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
					if (player == null) {
						string song = songFileOrUrlTextEdit.Text;
						song = string.IsNullOrEmpty (song) ? "escape.mid" : song;
						// if empty, play some song from asset.
						StartNewSong (GetSongData (song));
					}
					playSongButton.Text = "playing...";
					player.PlayAsync ();
				} else {
					playSongButton.Text = "Play song";
					player.PauseAsync ();
					player.Dispose ();
					player = null;
				}
			};
			
			var mmlEditText = FindViewById<EditText> (Resource.Id.editText);
			mmlEditText.Text = new StreamReader (Assets.Open ("wish.mml")).ReadToEnd ();
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
					
					playMmlButton.Text = "playing...";
				} else {
					playMmlButton.Text = "Play MML";
					player.PauseAsync ();
					player.Dispose ();
					player = null;
				}
			};

			// Mount OBBs at bootstrap.
			MidiState.Instance.MountObbs (this);
		}

		protected override void OnDestroy ()
		{
			if (recv != null)
				recv.Close ();
			base.OnDestroy ();
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
			player = new MidiPlayer (music, MidiState.Instance.GetMidiOutput (this));
			player.PlayAsync ();
		}
	}
}

