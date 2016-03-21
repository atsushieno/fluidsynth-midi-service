using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using Java.Util;
using NAudio.SoundFont;

namespace FluidsynthMidiServices.SoundFontProvider
{
	[Activity (Label = "SoundFont Details")]
	class SoundFontDetailsActivity : Activity
	{
		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);
			
			SetContentView (Resource.Layout.SoundFontDetails);
			
			var sf = Intent.GetStringExtra ("soundfont");

			var model = ApplicationModel.Instance;

			var mod = model.Database.Modules.First (m => m.Name == sf);
			var sfFile = model.Files.FirstOrDefault (p => p.Value == mod.Name).Key;
			if (!File.Exists (sfFile)) {
				Toast.MakeText (this, string.Format ("SoundFont file '{0}' was not found. Details not shown", sfFile), ToastLength.Long).Show ();
			} else {
				var sf2 = new SoundFont (new MemoryStream (File.ReadAllBytes (sfFile)));
				
				var infoList = new JavaList<IDictionary<string,object>> ();
				var map = mod.Instrument.Maps.First ();
	
				var labels = new string [] {"BankName", "FileName", "Author", "Copyright", "Comments", "Created", "SFVersion", "ROMVersion", "WaveTableEngine", "Tools", "TargetProduct", "DataROM"};
				var values = new string [] {
					mod.Name,
					sfFile,
					sf2.FileInfo.Author,
					sf2.FileInfo.Copyright,
					sf2.FileInfo.Comments,
					sf2.FileInfo.SoundFontVersion == null ? null : string.Format ("{0}.{1}", sf2.FileInfo.SoundFontVersion.Major, sf2.FileInfo.SoundFontVersion.Minor),
					sf2.FileInfo.ROMVersion == null ? null : string.Format ("{0}.{1}", sf2.FileInfo.ROMVersion.Major, sf2.FileInfo.ROMVersion.Minor),
					sf2.FileInfo.CreationDate,
					sf2.FileInfo.WaveTableSoundEngine,
					sf2.FileInfo.Tools,
					sf2.FileInfo.TargetProduct,
					sf2.FileInfo.DataROM,
					};
				for (int i = 0; i < labels.Length; i++) {
					var items = new JavaDictionary<string,object> ();
					items.Add ("text1", labels [i]);
					items.Add ("text2", values [i]);
					infoList.Add (items);
				}

				var bankList = new JavaList<IDictionary<string,object>> ();
				foreach (var p in map.Programs) {
					foreach (var b in p.Banks) {
						var items = new JavaDictionary<string,object> ();
						items.Add ("text1", string.Format ("{0:D03}.{1:D03}.{2:D03}", p.Index, b.Msb, b.Lsb));
						items.Add ("text2", b.Name);
						bankList.Add (items);
					}
				}
				
				var fromKeys = new string [] {"text1", "text2"};
				var toIds = new int [] { Resource.Id.soundFontDetailFileInfoLabel, Resource.Id.soundFontDetailFileInfoValue };
				
				var lvfi = FindViewById<ListView> (Resource.Id.soundFontDetailsFileInfo);
				lvfi.Adapter = new SimpleAdapter (this, infoList, Resource.Layout.SoundFontFileInfoItem, fromKeys, toIds);
				
				var lvbanks = FindViewById<ListView> (Resource.Id.soundFontDetailsBankList);
				lvbanks.Adapter = new SimpleAdapter (this, bankList, Resource.Layout.SoundFontFileInfoItem, fromKeys, toIds);
			}
		}
	}
}