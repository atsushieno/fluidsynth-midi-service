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

namespace FluidsynthMidiServices
{
	[Activity (Label = "FluidsynthMidiServices", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		int count = 1;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			// Get our button from the layout resource,
			// and attach an event to it
			Button button = FindViewById<Button> (Resource.Id.myButton);
			var recv = new FluidsynthMidiReceiver (this);
			recv.OnSend (new Byte [] { 0xB0, 7, 127 }, 0, 3, 0);
			recv.OnSend (new Byte [] { 0xB0, 11, 127 }, 0, 3, 0);
			recv.OnSend (new Byte [] { 0xC0, 30 }, 0, 2, 0);
			bool noteOn = false;
			
			button.Click += delegate {
				var midiService = this.GetSystemService (MidiService).JavaCast<MidiManager> ();
				var devs = midiService.GetDevices ();
				Console.WriteLine ("!!!! {0} devices.", devs.Length);
				/*
				foreach (var dev in devs) {
					Console.WriteLine ("!!!! {0}, {1}, IN: {2}, Out: {3}, {4}, {5}", dev.Id, dev.Type, dev.InputPortCount, dev.OutputPortCount,
							   dev.Properties.GetString (MidiDeviceInfo.PropertyName),
							   dev.Properties.GetString (MidiDeviceInfo.PropertyProduct));

					midiService.OpenDevice (dev, new Listener (), null);
				}
				*/
				if (noteOn) {
					recv.OnSend (new Byte [] { 0x80, 0x30, 0x78 }, 0, 3, 0);
					recv.OnSend (new Byte [] { 0x80, 0x39, 0x78 }, 0, 3, 0);
				} else {
					recv.OnSend (new Byte [] { 0x90, 0x30, 0x60 }, 0, 3, 0);
					recv.OnSend (new Byte [] { 0x90, 0x39, 0x60 }, 0, 3, 0);
				}
				noteOn = !noteOn;
				button.Text = noteOn ? "playing" : "off";
			};
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


