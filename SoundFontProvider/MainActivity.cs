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

namespace SoundFontProvider
{
	[Activity (Label = "SoundFontProvider", MainLauncher = true, Icon = "@mipmap/icon", Theme="@style/Theme.Styled")]
	public class MainActivity : AppCompatActivity
	{
		const string predefined_temp_path = "/data/local/tmp/name.atsushieno.soundfontprovider";

		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

			string key = "SoundFontProviderStorage";
			var sp = this.GetSharedPreferences (key, default (FileCreationMode));

			var model = ApplicationModel.Instance;
			model.Initialize (sp.GetString ("settings", string.Empty), s => {
				var e = sp.Edit ();
				e.PutString (key, s);
				e.Commit ();
			});

			if (ObbDir != null && !model.Settings.SearchPaths.Contains (ObbDir.AbsolutePath))
				model.AddSearchPaths (ObbDir.AbsolutePath);
			if (!model.Settings.SearchPaths.Contains (predefined_temp_path))
				model.AddSearchPaths (predefined_temp_path);

			foreach (var path in model.Settings.SearchPaths)
				model.LoadFromDirectory (path);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			//SetSupportActionBar (FindViewById<Android.Support.V7.Widget.Toolbar> (Resource.Id.mainToolbar));

			var vp = FindViewById<ViewPager> (Resource.Id.mainViewPager);
			var vpa = new TheViewPagerAdapter (SupportFragmentManager);
			vpa.Add ("Installed SoundFonts", new InstalledSoundFontFragment ());
			vpa.Add ("SoundFont Folders", new SoundFontFolderListFragment ());
			vpa.Add ("Online Catalogs", new OnlineCatalogFragment ());
			vp.Adapter = vpa;

			FindViewById<TabLayout> (Resource.Id.mainTabLayout).TabMode = TabLayout.ModeScrollable;

			var toolbar = FindViewById<Android.Support.V7.Widget.Toolbar> (Resource.Id.mainToolbar);
			toolbar.InflateMenu (Resource.Menu.toolbar);

			var tabl = FindViewById<TabLayout> (Resource.Id.mainTabLayout);
			tabl.SetupWithViewPager (vp);
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
				rv.SetAdapter (new TheRecyclerViewAdapter ());

				return v;
			}

			class TheRecyclerViewAdapter : RecyclerView.Adapter
			{
				public TheRecyclerViewAdapter ()
				{
					var model = ApplicationModel.Instance;
					data = model.Settings.SearchPaths;
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
					var v = LayoutInflater.From (parent.Context).Inflate (Resource.Layout.MainSoundFontFolderListItem, parent, false);
					var h = new TheViewHolder (v);
					return h;
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
