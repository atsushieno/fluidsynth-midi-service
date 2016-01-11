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
			Modules = new List<MidiModuleDefinition> ();
		}
		
		public IList<MidiModuleDefinition> Modules { get; private set; }
		
		Func<string,Stream> resolve_stream;
		
		public MidiModuleDefinition RegisterOrUpdate (string sf2FileName)
		{
			using (var s = resolve_stream (sf2FileName))
				return RegisterOrUpdate (new SoundFont (s));
		}
		
		public MidiModuleDefinition RegisterOrUpdate (SoundFont sf2)
		{
			var existing = Modules.FirstOrDefault (m => m.Name == sf2.FileInfo.BankName);
			if (existing != null)
				return existing;

			var mod = new MidiModuleDefinition ();
			mod.Name = sf2.FileInfo.BankName;
			
			// only one map for one sf2.
			var map = new MidiInstrumentMap () { Name = sf2.FileInfo.BankName };			
			mod.Instrument.Maps.Add (map);
			
			foreach (var preset in sf2.Presets.OrderBy (p => p.PatchNumber).ThenBy (p => p.Bank)) {
				var prog = map.Programs.FirstOrDefault (p => p.Index == preset.PatchNumber);
				if (prog == null) {
					prog = new MidiProgramDefinition () { Index = preset.PatchNumber, Name = preset.Name };
					map.Programs.Add (prog);
				}
				prog.Banks.Add (new MidiBankDefinition () { Name = preset.Name, Msb = preset.Bank });
			}
			
			Modules.Add (mod);
			
			return mod;
		}
		
		public override MidiModuleDefinition Resolve (string moduleName)
		{
			return Modules.FirstOrDefault (m => m.Name == moduleName);
		}
	}
}

