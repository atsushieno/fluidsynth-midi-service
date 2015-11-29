using System;
using Android.Media.Midi;
using Android.App;
using NFluidsynth;
using Android.Content;

namespace FluidsynthMidiServices
{
	[Service (Label = "Fluidsynth MIDI Device Service")]
	[IntentFilter (new string [] { MidiDeviceService.ServiceInterface })]
	[MetaData (MidiDeviceService.ServiceInterface, Resource = "@xml/device_info")]
	public class FluidsynthMidiDeviceService : MidiDeviceService
	{
		// default constructor for normal service instance
		public FluidsynthMidiDeviceService ()
		{
		}

		// for debugging
		public FluidsynthMidiDeviceService (Context context)
		{
			if (fluidsynth_receiver == null)
				fluidsynth_receiver = new FluidsynthMidiReceiver (context);
		}

		FluidsynthMidiReceiver fluidsynth_receiver;

		public override void OnCreate ()
		{
			fluidsynth_receiver = new FluidsynthMidiReceiver (ApplicationContext);
		}

		public override MidiReceiver[] OnGetInputPortReceivers ()
		{
			return new MidiReceiver [] {fluidsynth_receiver };
		}
	}
}

