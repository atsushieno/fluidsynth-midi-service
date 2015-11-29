using System;
using System.IO;
using System.Linq;
using Android.Media.Midi;
using Android.App;
using NFluidsynth;
using Android.Content;

namespace FluidsynthMidiServices
{
	class FluidsynthMidiReceiver : MidiReceiver
	{
		public FluidsynthMidiReceiver (Context context)
		{
			var settings = new Settings ();

			//settings [ConfigurationKeys.AudioDriver].StringValue = "opensles";
			
			//settings [ConfigurationKeys.SynthParallelRender].IntValue = 0;
			//settings [ConfigurationKeys.SynthThreadsafeApi].IntValue = 0;
			//settings [ConfigurationKeys.AudioPeriods].IntValue = 16;
			//settings [ConfigurationKeys.AudioPeriods].IntValue = 64;

			// This is required.
			settings [ConfigurationKeys.AudioSampleFormat].StringValue = "16bits";

			// 44.1KHz seems too much. 22.05 works on kvm-based emulator on Ubuntu.
			//settings [ConfigurationKeys.SynthSampleRate].DoubleValue = 44100;
			//settings [ConfigurationKeys.SynthSampleRate].DoubleValue = 22050;
			settings [ConfigurationKeys.SynthSampleRate].DoubleValue = 11025;

			// This number seems good for Android (or my kvm-based emulator on Ubuntu).
			settings [ConfigurationKeys.AudioPeriodSize].IntValue = 512; // like Windows
			//settings [ConfigurationKeys.AudioPeriodSize].IntValue = 64;

			syn = new Synth (settings);
			LoadDefaultSoundFontSpecial (context, syn);

			/*
			var files = args.Where (a => Synth.IsMidiFile (a));
			if (files.Any ()) {
				foreach (var arg in files) {
					using (var player = new Player (syn)) {
						using (var adriver = new AudioDriver (syn.Settings, syn)) {
							player.Add (arg);
							player.Play ();
							player.Join ();
						}
					}
				}
			} else*/ {
				//using (var adriver = new AudioDriver (syn.Settings, syn)) {
				adriver = new AudioDriver (syn.Settings, syn);
					//syn.NoteOn (0, 60, 100);
					//syn.NoteOff (0, 60);
				//}
			}
		}

		Synth syn;
		AudioDriver adriver;

		void LoadDefaultSoundFontSpecial (Context context, Synth synth)
		{
			string appDir = context.ApplicationInfo.DataDir;
			Console.WriteLine ("!!!!!!!!!!!!!!!" + appDir);
			Console.WriteLine (string.Join ("\n",
			                                Directory.GetFileSystemEntries (appDir, "*", SearchOption.AllDirectories)));
			Console.WriteLine ("!!!!!!!!!!!!!!!" + appDir);
			Console.WriteLine (string.Join ("\n", context.ApplicationContext.FileList ()));
			synth.LoadSoundFont ("/sdcard/tmp/FluidR3_GM.sf2", true);
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				adriver.Dispose ();
				syn.Dispose ();
			}
			base.Dispose (disposing);
		}

		public override void OnSend (byte[] msg, int offset, int count, long timestamp)
		{
			try {
				DoSend (msg, offset, count, timestamp);
			} catch (Exception) {
				Android.Util.Log.Error ("FluidsynthMidiService", "Failed to send MIDI message: offset: {0}, count: {1}, timestamp: {2}, message length: {3}",
					offset, count, timestamp, msg.Length);
				Android.Util.Log.Error ("FluidsynthMidiService", "  message: {0}",
					msg.Skip (offset).Take (count).Select (b => b.ToString ("X") + ' '));
				throw;
			}
		}
		
		void DoSend (byte[] msg, int offset, int count, long timestamp)
		{
			// FIXME: consider timestamp.

			int ch = msg [offset] & 0x0F;
			switch (msg [offset] & 0xF0) {
			case 0x80:
				syn.NoteOff (ch, msg [offset + 1]);
				break;
			case 0x90:
				Console.WriteLine ("NoteOn: " + string.Join (":", msg.Skip (offset).Take (count).Select (b => string.Format ("{0:X02}", b))));
				if (msg [offset + 2] == 0)
					syn.NoteOff (ch, msg [offset + 1]);
				else
					syn.NoteOn (ch, msg [offset + 1], msg [offset + 2]);
				break;
			case 0xA0:
				// No PAf in fluidsynth?
				break;
			case 0xB0:
				syn.CC (ch, msg [offset + 1], msg [offset + 2]);
				break;
			case 0xC0:
				syn.ProgramChange (ch, msg [offset + 1]);
				break;
			case 0xD0:
				syn.ChannelPressure (ch, msg [offset + 1]);
				break;
			case 0xE0:
				syn.PitchBend (ch, (msg [offset + 1] << 14) + msg [offset + 2]);
				break;
			case 0xF0:
				syn.Sysex (new ArraySegment<byte> (msg, offset, count).ToArray (), null);
				break;
			}
		}
	}
}

