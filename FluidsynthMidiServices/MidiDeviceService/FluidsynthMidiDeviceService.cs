using System;
using Android.Media.Midi;
using Android.App;
using NFluidsynth;
using Android.Content;

namespace FluidsynthMidiServices
{
	[Service (Label = "Fluidsynth MIDI Device Service", Permission = Android.Manifest.Permission.BindMidiDeviceService)]
	[IntentFilter (new string [] { MidiDeviceService.ServiceInterface })]
	[MetaData (MidiDeviceService.ServiceInterface, Resource = "@xml/device_info")]
	public class FluidsynthMidiDeviceService : MidiDeviceService
	{
		// default constructor for normal service instance
		public FluidsynthMidiDeviceService ()
		{
			MidiState.Instance.MountObbs (this);
		}

		FluidsynthMidiReceiver fluidsynth_receiver;

		public override void OnCreate ()
		{
		}

		public override MidiReceiver[] OnGetInputPortReceivers ()
		{
			if (fluidsynth_receiver == null || fluidsynth_receiver.IsDisposed)
				fluidsynth_receiver = new FluidsynthMidiReceiver (this);
			return new MidiReceiver [] {fluidsynth_receiver };
		}
	}
}

