using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Media.Midi;
using Android.Runtime;

namespace Commons.Music.Midi.AndroidMidiAccess
{
	public class MidiAccess : IMidiAccess
	{
		MidiManager midi_manager;
		
		public MidiAccess (Context context)
		{
			midi_manager = context.GetSystemService (Context.MidiService).JavaCast<MidiManager> ();
		}
		
		public IEnumerable<IMidiPortDetails> Inputs {
			get { return midi_manager.GetDevices ().SelectMany (d => d.GetPorts ().Where (p => p.Type == MidiPortType.Input).Select (p => new MidiPortDetails (d, p))); }
		}

		public IEnumerable<IMidiPortDetails> Outputs {
			get { return midi_manager.GetDevices ().SelectMany (d => d.GetPorts ().Where (p => p.Type == MidiPortType.Output).Select (p => new MidiPortDetails (d, p))); }
		}

		// FIXME: left unsupported...
		public event EventHandler<MidiConnectionEventArgs> StateChanged;

		List<MidiDevice> open_devices = new List<MidiDevice> ();

		public Task<IMidiInput> OpenInputAsync (string portId)
		{
			var ip = (MidiPortDetails) Inputs.First (i => i.Id == portId);
			var dev = open_devices.FirstOrDefault (d => ip.Device.Id == d.Info.Id);
			var l = new OpenDeviceListener (this, dev, ip);
			return l.OpenInputAsync (CancellationToken.None);			
		}

		public Task<IMidiOutput> OpenOutputAsync (string portId)
		{
			throw new NotImplementedException ();
		}

		class OpenDeviceListener : Java.Lang.Object, MidiManager.IOnDeviceOpenedListener
		{
			MidiAccess parent;
			MidiDevice device;
			MidiPortDetails port_to_open;
			ManualResetEventSlim wait;
			
			public OpenDeviceListener (MidiAccess parent, MidiDevice device, MidiPortDetails portToOpen)
			{
				this.parent = parent;
				port_to_open = portToOpen;
			}
			
			public Task<IMidiInput> OpenInputAsync (CancellationToken token)
			{
				return OpenAsync (token, dev => (IMidiInput) new MidiInput (port_to_open, device.OpenOutputPort (port_to_open.Port.PortNumber)));
			}
			
			public Task<IMidiOutput> OpenOutputAsync (CancellationToken token)
			{
				return OpenAsync (token, dev => (IMidiOutput) new MidiOutput (port_to_open, device.OpenInputPort (port_to_open.Port.PortNumber)));
			}
			
			Task<T> OpenAsync<T> (CancellationToken token, Func<MidiDevice, T> resultCreator)
			{
				return Task.Run (delegate {
					wait = new ManualResetEventSlim ();
					if (device == null) {
						parent.midi_manager.OpenDevice (port_to_open.Device, this, null);
						wait.Wait (token);
						wait.Reset ();
					}
					return resultCreator (device);
				});
			}
			
			public void OnDeviceOpened (MidiDevice device)
			{
				parent.open_devices.Add (device);
				wait.Set ();
			}
		}
	}

	public class MidiPortDetails : IMidiPortDetails
	{
		MidiDeviceInfo device;
		MidiDeviceInfo.PortInfo port;
		
		public MidiPortDetails (MidiDeviceInfo device, MidiDeviceInfo.PortInfo port)
		{
			this.device = device;
			this.port = port;
		}
		
		public MidiDeviceInfo Device {
			get { return device; }
		}
		
		public MidiDeviceInfo.PortInfo Port {
			get { return port; }
		}
		
		public string Id {
			get { return "device" + device.Id + "_port" + port.PortNumber; }
		}

		public string Manufacturer {
			get { return device.Properties.GetString (MidiDeviceInfo.PropertyManufacturer); }
		}

		public string Name {
			get { return port.Name; }
		}

		public string Version {
			get { return device.Properties.GetString (MidiDeviceInfo.PropertyVersion); }
		}
	}

	public class MidiPort : IMidiPort
	{
		MidiPortDetails details;
		MidiPortConnectionState connection;
		Action on_close;
		
		protected MidiPort (MidiPortDetails details, Action onClose)
		{
			this.details = details;
			on_close = onClose;
			connection = MidiPortConnectionState.Open;
		}
		
		public MidiPortConnectionState Connection {
			get { return connection; }
		}

		public IMidiPortDetails Details {
			get { return details; }
		}

		public MidiPortDeviceState State {
			get { return MidiPortDeviceState.Connected; }
		}
		
		public event EventHandler StateChanged;

		public Task CloseAsync ()
		{
			return Task.Run (() => { Close (); });
		}

		public void Dispose ()
		{
			Close ();
		}
		
		internal virtual void Close ()
		{
			on_close ();
			connection = MidiPortConnectionState.Closed;
			StateChanged (this, EventArgs.Empty);
		}
	}
	
	public class MidiInput : MidiPort, IMidiInput
	{
		MidiOutputPort port;
		Receiver receiver;
		
		public MidiInput (MidiPortDetails details, MidiOutputPort port)
			: base (details, () => port.Close ())
		{
			this.port = port;
			receiver = new Receiver (this);
			port.Connect (receiver);
		}
		
		internal override void Close ()
		{
			port.Disconnect (receiver);
			base.Close ();
		}
		
		class Receiver : MidiReceiver
		{
			MidiInput parent;
			
			public Receiver (MidiInput parent)
			{
				this.parent = parent;
			}
			
			public override void OnSend (byte [] msg, int offset, int count, long timestamp)
			{
				if (parent.MessageReceived != null)
					parent.MessageReceived (this, new MidiReceivedEventArgs () {
						Data = offset == 0 && msg.Length == count ? msg : msg.Skip (offset).Take (count).ToArray (),
						Timestamp = timestamp });
			}
		}

		public event EventHandler<MidiReceivedEventArgs> MessageReceived;
	}

	public class MidiOutput : MidiPort, IMidiOutput
	{
		MidiInputPort port;

		public MidiOutput (MidiPortDetails details, MidiInputPort port)
			: base (details, () => port.Close ())
		{
			this.port = port;
		}

		public Task SendAsync (byte [] mevent, int offset, int length, long timestamp)
		{
			// We could return Task.Run (), but it is stupid to create a Task instance for that on every call to this method.
			port.Send (mevent, offset, length, timestamp);
			return Task.FromResult (string.Empty);
		}
	}
}

