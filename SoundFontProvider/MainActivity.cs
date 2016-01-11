using Android.App;
using Android.Widget;
using Android.OS;
using System.IO;
using Commons.Music.Midi.AndroidExtensions;
using System;
using Android.Content;
using System.Linq;

namespace SoundFontProvider
{
	[Activity (Label = "SoundFontProvider", MainLauncher = true, Icon = "@mipmap/icon")]
	public class MainActivity : Activity
	{
		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);
			
			string key = "SoundFontProviderStorage";
			var sp = this.GetSharedPreferences (key, default (FileCreationMode));
			
			var model = ApplicationModel.Instance;
			model.Initialize (sp.GetString ("settings", string.Empty), s => {
				var e = sp.Edit ();
				e.PutString (key, s);
				e.Commit ();
				});

			if (!model.Settings.SearchPaths.Contains (ObbDir.AbsolutePath))
				model.AddSearchPaths (ObbDir.AbsolutePath);
				
			foreach (var path in model.Settings.SearchPaths)
				model.LoadFromDirectory (path);
				
			var lv = FindViewById<ListView> (Resource.Id.mainSoundFontList);
			var data = model.Database.Modules.Select (m => m.Name).ToArray ();
			lv.Adapter = new ArrayAdapter<string> (this, Android.Resource.Layout.SimpleListItem1, data);
			lv.ItemClick += (sender, e) => {
				var intent = new Intent (this, typeof (SoundFontDetailsActivity));
				intent.PutExtra ("soundfont", data [e.Position]);
				this.StartActivity (intent);
			};
		}
	}
}
