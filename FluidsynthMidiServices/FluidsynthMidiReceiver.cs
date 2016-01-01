#define MIDI_MANAGER
using System;
using System.IO;
using System.Linq;
using Android.Media.Midi;
using Android.App;
using NFluidsynth;
using Android.Content;
using Java.Interop;
using Android.Media;
using Android.Util;
using NFluidsynth.MidiManager;
using Commons.Music.Midi;

namespace FluidsynthMidiServices
{
	class FluidsynthMidiReceiver : MidiReceiver
	{
		public FluidsynthMidiReceiver (Context context)
		{
#if MIDI_MANAGER
			access = new FluidsynthMidiAccess ();
			access.ConfigureSettings = (settings) => {
#else
			var settings = new Settings ();
#endif
			//settings [ConfigurationKeys.AudioDriver].StringValue = "opensles";
			
			//settings [ConfigurationKeys.SynthParallelRender].IntValue = 0;
			//settings [ConfigurationKeys.SynthThreadsafeApi].IntValue = 0;
			//settings [ConfigurationKeys.AudioPeriods].IntValue = 16;
			//settings [ConfigurationKeys.AudioPeriods].IntValue = 64;

			settings [ConfigurationKeys.AudioSampleFormat].StringValue = "16bits"; // float or 16bits

			// Note that it is NOT audio sample rate but *synthesizing* sample rate.
			// So it is kind of wrong assumption that AudioManager.PropertyOutputSampleRate would give the best outcome...
			var manager = context.GetSystemService (Context.AudioService).JavaCast<AudioManager> ();
			var fpb = double.Parse (manager.GetProperty (AudioManager.PropertyOutputFramesPerBuffer));
			settings [ConfigurationKeys.AudioPeriodSize].IntValue = (int) fpb;
			var sr = double.Parse (manager.GetProperty (AudioManager.PropertyOutputSampleRate));
			settings [ConfigurationKeys.SynthSampleRate].DoubleValue = sr;
#if MIDI_MANAGER
			};
			string sf2Dir = Path.Combine (context.ObbDir.AbsolutePath);
			if (Directory.Exists (sf2Dir))
				foreach (var obbSf2 in Directory.GetFiles (sf2Dir, "*.sf2", SearchOption.AllDirectories))
					access.Soundfonts.Add (obbSf2);
			output = access.OpenOutputAsync (access.Outputs.First ().Id).Result;
#else
			syn = new Synth (settings);
			LoadDefaultSoundFontSpecial (context, syn);

			adriver = new AudioDriver (syn.Settings, syn);
#endif
		}

#if MIDI_MANAGER
		FluidsynthMidiAccess access;
		IMidiOutput output;
#else
		Synth syn;
		AudioDriver adriver;
#endif
		
		void LoadDefaultSoundFontSpecial (Context context, Synth synth)
		{
			string sf2Dir = Path.Combine (context.ObbDir.AbsolutePath);
			if (Directory.Exists (sf2Dir))
				foreach (var obbSf2 in Directory.GetFiles (sf2Dir, "*.sf2", SearchOption.AllDirectories))
					synth.LoadSoundFont (obbSf2, true);
		}

		protected override void Dispose (bool disposing)
		{
#if MIDI_MANAGER
#else
			if (disposing) {
				adriver.Dispose ();
				syn.Dispose ();
			}
#endif
			base.Dispose (disposing);
		}

		public override void OnSend (byte[] msg, int offset, int count, long timestamp)
		{
			try {
				DoSend (msg, offset, count, timestamp);
			} catch (Exception) {
				Log.Error ("FluidsynthMidiService", "Failed to send MIDI message: offset: {0}, count: {1}, timestamp: {2}, message length: {3}",
					offset, count, timestamp, msg.Length);
				Log.Error ("FluidsynthMidiService", "  message: {0}",
					msg.Skip (offset).Take (count).Select (b => b.ToString ("X") + ' '));
				throw;
			}
		}
		
		void DoSend (byte[] msg, int offset, int count, long timestamp)
		{
#if MIDI_MANAGER
			output.SendAsync (msg, offset, count, timestamp);
#else
			// FIXME: consider timestamp.

			int ch = msg [offset] & 0x0F;
			switch (msg [offset] & 0xF0) {
			case 0x80:
				syn.NoteOff (ch, msg [offset + 1]);
				break;
			case 0x90:
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
				synth.PitchBend (ch, msg [offset + 1] + msg [offset + 2] * 0x80);
				break;
			case 0xF0:
				syn.Sysex (new ArraySegment<byte> (msg, offset, count).ToArray (), null);
				break;
			}
#endif
		}
	}
}

