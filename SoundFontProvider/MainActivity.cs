using Android.App;
using Android.Widget;
using Android.OS;
using System.IO;
using Commons.Music.Midi.AndroidExtensions;
using System;
using Android.Content;
using System.Linq;
using Android.Support.V4.View;
using Android.Support.V4.App;
using System.Collections.Generic;

using Fragment = Android.Support.V4.App.Fragment;
using Android.Views;
using Android.Support.Design.Widget;
using Android.Support.V7.Widget;
using Android.Runtime;
using Android.Support.V7.App;
using Com.Nononsenseapps.Filepicker;

namespace SoundFontProvider
{
	[Activity (Label = "SoundFontProvider", MainLauncher = true, Icon = "@mipmap/icon", Theme="@style/Theme.Styled")]
	public class MainActivity : AppCompatActivity
	{
		const string predefined_temp_path = "/data/local/tmp/name.atsushieno.soundfontprovider";

		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

			string sharedPreferenceName = "SoundFontProviderStorage";
			string key = "settings";
			var sp = this.GetSharedPreferences (sharedPreferenceName, default (FileCreationMode));

			var model = ApplicationModel.Instance;
			model.Initialize (sp.GetString (key, string.Empty), s => {
				var e = sp.Edit ();
				e.PutString (key, s);
				e.Commit ();
			});

#if DEBUG
			if (!model.Settings.SearchPaths.Contains (predefined_temp_path))
				model.AddSearchPaths (predefined_temp_path);
#endif
			foreach (var path in model.Settings.SearchPaths)
				model.LoadFromDirectory (path);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			var vp = FindViewById<ViewPager> (Resource.Id.mainViewPager);
			var vpa = new TheViewPagerAdapter (SupportFragmentManager);
			vpa.Add ("Installed SoundFonts", new InstalledSoundFontFragment ());
			vpa.Add ("SoundFont Folders", new SoundFontFolderListFragment ());
			vpa.Add ("Online Catalogs", new OnlineCatalogFragment ());
			vp.Adapter = vpa;

			FindViewById<TabLayout> (Resource.Id.mainTabLayout).TabMode = TabLayout.ModeScrollable;

			var toolbar = FindViewById<Android.Support.V7.Widget.Toolbar> (Resource.Id.mainToolbar);
			toolbar.InflateMenu (Resource.Menu.toolbar);
			toolbar.MenuItemClick += (sender, e) => {
				if (e.Item.ItemId == Resource.Id.toolbar_addnewfolder) {
					//var df = new FolderChooserDialogFragment ();
					//df.Show (SupportFragmentManager, "FolderChooser");
					Intent i = new Intent (this, typeof (FilePickerActivity));

					i.PutExtra (FilePickerActivity.ExtraMode, FilePickerActivity.ModeDir);

					// Configure initial directory by specifying a String.
					// You could specify a String like "/storage/emulated/0/", but that can
					// dangerous. Always use Android's API calls to get paths to the SD-card or
					// internal memory.
					i.PutExtra (FilePickerActivity.ExtraStartPath, Android.OS.Environment.ExternalStorageDirectory.Path);

					StartActivityForResult (i, 1234);				}
			};

			var tabl = FindViewById<TabLayout> (Resource.Id.mainTabLayout);
			tabl.SetupWithViewPager (vp);
		}

		protected override void OnActivityResult (int requestCode, Result resultCode, Intent data)
		{
			if (requestCode == 1234 && resultCode == Result.Ok) {
				if (Directory.Exists (data.Data.Path))
					ApplicationModel.Instance.AddSearchPaths (data.Data.Path);
			}
			base.OnActivityResult (requestCode, resultCode, data);
		}

		class FolderChooserDialogFragment : Android.Support.V4.App.DialogFragment
		{
			public override Dialog OnCreateDialog (Bundle savedInstanceState)
			{
				var b = new Android.Support.V7.App.AlertDialog.Builder (Context);
				var entry = new EditText (Context);
				b.SetMessage ("Select a folder whose descendants contain Soundfonts")
				 .SetView (entry)
				 .SetNegativeButton ("Cancel", (sender, e) => {})
				 .SetPositiveButton ("Add", (sender, e) => {
					ApplicationModel.Instance.AddSearchPaths (entry.Text.Trim ());
				});
				return b.Create ();
			}
		}

		class TheViewPagerAdapter : FragmentPagerAdapter
		{
			Android.Support.V4.App.FragmentManager fragmentManager;

			List<KeyValuePair<string, Fragment>> items = new List<KeyValuePair<string, Fragment>> ();

			public TheViewPagerAdapter (Android.Support.V4.App.FragmentManager fragmentManager)
				: base (fragmentManager)
			{
				this.fragmentManager = fragmentManager;
			}

			public override int Count {
				get { return items.Count; }
			}

			public override Fragment GetItem (int position)
			{
				return items [position].Value;
			}

			public void Add (string title, Fragment fragment)
			{
				items.Add (new KeyValuePair<string, Fragment> (title, fragment));
			}

			public override Java.Lang.ICharSequence GetPageTitleFormatted (int position)
			{
				return new Java.Lang.String (items [position].Key);
			}
		}

		class InstalledSoundFontFragment : Fragment
		{
			public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
			{
				var v = inflater.Inflate (Resource.Layout.MainSoundFontList, container, false);

				var rv = v.FindViewById<RecyclerView> (Resource.Id.mainSoundFontList);
				rv.SetLayoutManager (new LinearLayoutManager (Activity.BaseContext));
				rv.SetAdapter (new TheRecyclerViewAdapter ());

				return v;
			}

			class TheRecyclerViewAdapter : RecyclerView.Adapter
			{
				public TheRecyclerViewAdapter ()
				{
					var model = ApplicationModel.Instance;
					data = model.Database.Modules.Select (m => m.Name).ToArray ();
				}

				string [] data;

				public override int ItemCount {
					get { return data.Length; }
				}

				public override void OnBindViewHolder (RecyclerView.ViewHolder holder, int position)
				{
					((TheViewHolder) holder).Text.Text = data [position];
				}

				public override RecyclerView.ViewHolder OnCreateViewHolder (ViewGroup parent, int viewType)
				{
					var v = LayoutInflater.From (parent.Context).Inflate (Resource.Layout.MainSoundFontListItem, parent, false);
					var h = new TheViewHolder (v);
					v.Click += (sender, e) => {
						var intent = new Intent (parent.Context, typeof (SoundFontDetailsActivity));
						intent.PutExtra ("soundfont", data [h.AdapterPosition]);
						parent.Context.StartActivity (intent);
					};
					return h;
				}
			}

			class TheViewHolder : RecyclerView.ViewHolder
			{
				public TheViewHolder (View itemView) : base (itemView)
				{
					if (itemView == null)
						throw new ArgumentNullException (nameof (itemView));
					Text = itemView.FindViewById<TextView> (Resource.Id.mainSoundFontListItemText);
				}

				public TextView Text { get; private set; }
			}
		}

		class SoundFontFolderListFragment : Fragment
		{
			public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
			{
				var v = inflater.Inflate (Resource.Layout.MainSoundFontFolderList, container, false);

				var rv = v.FindViewById<RecyclerView> (Resource.Id.mainSoundFontFolderList);
				rv.SetLayoutManager (new LinearLayoutManager (Activity.BaseContext));
				rv.SetAdapter (new TheRecyclerViewAdapter (Activity));

				return v;
			}

			class TheRecyclerViewAdapter : RecyclerView.Adapter
			{
				public TheRecyclerViewAdapter (FragmentActivity activity)
				{
					this.activity = activity;
					var model = ApplicationModel.Instance;
					data = model.Settings.SearchPaths;
				}

				FragmentActivity activity;

				string [] data;

				public override int ItemCount {
					get { return data.Length; }
				}

				public override void OnBindViewHolder (RecyclerView.ViewHolder holder, int position)
				{
					((TheViewHolder) holder).Text.Text = data [position];
				}

				public override RecyclerView.ViewHolder OnCreateViewHolder (ViewGroup parent, int viewType)
				{
					var v = LayoutInflater.From (parent.Context).Inflate (Resource.Layout.MainSoundFontFolderListItem, parent, false);
					var h = new TheViewHolder (v);
					v.LongClick += (sender, e) => {
						var folder = data [h.AdapterPosition];
						var df = new RemoveFolderDialogFragment (folder);
						df.Show (activity.SupportFragmentManager, "FolderRemoval");
					};
					return h;
				}
			}

			class RemoveFolderDialogFragment : Android.Support.V4.App.DialogFragment
			{
				public RemoveFolderDialogFragment (string folder)
				{
					this.folder = folder;
				}

				string folder;

				public override Dialog OnCreateDialog (Bundle savedInstanceState)
				{
					var b_ = new Android.Support.V7.App.AlertDialog.Builder (Context);
					b_.SetItems (new string [] {"Unregister"}, (sender, ea) => {
						var b = new Android.Support.V7.App.AlertDialog.Builder (Context);
						b.SetMessage (string.Format ("Folder '{0}' will be unregistered.", folder))
						.SetNegativeButton ("Cancel", (o, e) => {})
						.SetPositiveButton ("Remove", (o, e) => {
							ApplicationModel.Instance.RemoveSearchPaths (folder);
						});
						b.Show ();
					});
					return b_.Create ();
				}
			}

			class TheViewHolder : RecyclerView.ViewHolder
			{
				public TheViewHolder (View itemView) : base (itemView)
				{
					if (itemView == null)
						throw new ArgumentNullException (nameof (itemView));
					Text = itemView.FindViewById<TextView> (Resource.Id.mainSoundFontFolderListItemText);
				}

				public TextView Text { get; private set; }
			}
		}

		class OnlineCatalogFragment : Fragment
		{
			public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
			{
				return base.OnCreateView (inflater, container, savedInstanceState);
			}
		}
	}
}
