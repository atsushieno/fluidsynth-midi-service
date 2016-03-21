using System.IO;
using Commons.Music.Midi.AndroidExtensions;
using System;
using System.Linq;
using System.Collections.Generic;

namespace FluidsynthMidiServices.SoundFontProvider
{
	public class ApplicationModel
	{
		public static ApplicationModel Instance { get; set; } = new ApplicationModel ();
		
		public ApplicationModel ()
		{
			Database = new SoundFontMidiModuleDatabase (file => File.OpenRead (file));
			Files = new Dictionary<string, string> ();
		}
		
		public ApplicationSettings Settings { get; private set; }
		
		public Dictionary<string,string> Files { get; set; }
		public SoundFontMidiModuleDatabase Database { get; set; }
		
		public void LoadFromDirectory (string path)
		{
			if (Directory.Exists (path)) {
				foreach (var sf2 in Directory.GetFiles (path, "*.sf2", SearchOption.AllDirectories)) {
					var mod = Database.RegisterOrUpdate (sf2);
					Files [Path.GetFullPath (sf2)] = mod.Name;
				}
			}
		}

		public void Initialize (string settings, Action<string> save)
		{
			Settings = ApplicationSettings.Create (settings, save);
		}

		public void AddSearchPaths (params string [] newPaths)
		{
			Settings.SearchPaths = Settings.SearchPaths.Concat (newPaths).Distinct ().ToArray ();
		}

		internal void RemoveSearchPaths (params string [] folders)
		{
			Settings.SearchPaths = Settings.SearchPaths.Except (folders).ToArray ();
		}
	}
		
	public class ApplicationSettings
	{
		Action<string> save;
		
		string [] searchpaths;
		public string [] SearchPaths {
			get { return searchpaths; }
			set {
				searchpaths = value;
				Save ();
			}
		}

		public static ApplicationSettings Create (string settings, Action<string> save)
		{
			return new ApplicationSettings () {
				searchpaths = settings.Split (new char [] {'\n'}, StringSplitOptions.RemoveEmptyEntries).Select (s => s.Trim ()).ToArray (),
				save = save
				};
		}
		
		public void Save ()
		{
			save (string.Join ("\n", SearchPaths));
		}
	}
}
