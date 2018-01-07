//#define MIDI_MANAGER
using System;
using System.Collections.Generic;
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
		const string predefined_temp_path = "/data/local/tmp/name.atsushieno.fluidsynthmidideviceservice";

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

			var manager = context.GetSystemService (Context.AudioService).JavaCast<AudioManager> ();
			
			// Note that it is NOT audio sample rate but *synthesizing* sample rate.
			// So it is kind of wrong assumption that AudioManager.PropertyOutputSampleRate would give the best outcome...
			//var sr = double.Parse (manager.GetProperty (AudioManager.PropertyOutputSampleRate));
			//settings [ConfigurationKeys.SynthSampleRate].DoubleValue = sr;
			settings [ConfigurationKeys.SynthSampleRate].DoubleValue = 11025;
			
			var fpb = double.Parse (manager.GetProperty (AudioManager.PropertyOutputFramesPerBuffer));
			settings [ConfigurationKeys.AudioPeriodSize].IntValue = (int) fpb;
#if MIDI_MANAGER
			};
			SynthAndroidExtensions.GetSoundFonts (access.SoundFonts, context, predefined_temp_path);
			output = access.OpenOutputAsync (access.Outputs.First ().Id).Result;
#else
			syn = new Synth (settings);
			var sfs = new List<string> ();
			SynthAndroidExtensions.GetSoundFonts (sfs, context, predefined_temp_path);

			asset_stream_loader = new AndroidAssetStreamLoader (context.Assets);
			asset_sfloader = new SoundFontLoader (syn, asset_stream_loader);
			syn.AddSoundFontLoader (asset_sfloader);
			foreach (var sf in sfs)
				syn.LoadSoundFont (sf, false);

			adriver = new AudioDriver (syn.Settings, syn);
#endif
		}

#if MIDI_MANAGER
		FluidsynthMidiAccess access;
		IMidiOutput output;
#else
		Synth syn;
		AudioDriver adriver;
		AndroidAssetStreamLoader asset_stream_loader;
		SoundFontLoader asset_sfloader;
#endif
		public bool IsDisposed { get; private set; }

		protected override void Dispose (bool disposing)
		{
#if MIDI_MANAGER
#else
			if (disposing) {
				if (asset_sfloader != null)
					asset_sfloader.Dispose ();
				if (asset_stream_loader != null)
					asset_stream_loader.Dispose ();
				adriver.Dispose ();
				syn.Dispose ();
				IsDisposed = true;
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
			output.Send (msg, offset, count, timestamp);
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
				syn.PitchBend (ch, msg [offset + 1] + msg [offset + 2] * 0x80);
				break;
			case 0xF0:
				syn.Sysex (new ArraySegment<byte> (msg, offset, count).ToArray (), null);
				break;
			}
#endif
		}
	}
}

