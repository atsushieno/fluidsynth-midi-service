using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Commons.Music.Midi;
using NAudio.SoundFont;

namespace Commons.Music.Midi.AndroidExtensions
{
	public class SoundFontMidiModuleDatabase : MidiModuleDatabase
	{
		public SoundFontMidiModuleDatabase (Func<string,Stream> resolveStream)
		{
			if (resolveStream == null)
				throw new ArgumentNullException ("resolveStream");
			this.resolve_stream = resolveStream;
		}
		
		public IList<MidiModuleDefinition> Modules { get; private set; }
		
		Func<string,Stream> resolve_stream;
		
		public void RegisterOrUpdate (string sf2FileName)
		{
			using (var s = resolve_stream (sf2FileName))
				RegisterOrUpdate (new SoundFont (s));
		}
		
		public void RegisterOrUpdate (SoundFont sf2)
		{
			var mod = new MidiModuleDefinition ();
			mod.Name = sf2.FileInfo.BankName;
			// only one map for one sf2.
			var map = new MidiInstrumentMap () { Name = sf2.FileInfo.BankName };
			mod.Instrument.Maps.Add (map);
			foreach (var preset in sf2.Presets) {
				var prog = map.Programs.FirstOrDefault (p => p.Index == preset.PatchNumber);
				if (prog == null) {
					prog = new MidiProgramDefinition () { Index = preset.PatchNumber, Name = preset.Name };
					map.Programs.Add (prog);
				}
				prog.Banks.Add (new MidiBankDefinition () { Name = preset.Name, Msb = preset.Bank });
			}
		}
		
		public override MidiModuleDefinition Resolve (string moduleName)
		{
			return Modules.FirstOrDefault (m => m.Name == moduleName);
		}
	}
}

