using System;
using System.Collections.Generic;
using Android.App;
using Android.Runtime;
using Java.IO;
using Object = Java.Lang.Object;
using Uri = Android.Net.Uri;

namespace Com.Nononsenseapps.Filepicker {
	public partial class FilePickerFragment {
		public override void OnLoadFinished (Android.Support.V4.Content.Loader p0, Java.Lang.Object p1)
		{
			OnLoadFinished (p0, p1.JavaCast<Android.Support.V7.Util.SortedList> ());
		}
	}

	[Activity (Name = "com.nononsenseapps.filepicker.FilePickerActivity",
	           Label = "@string/app_name",
	           Theme = "@style/FilePickerTheme")]
	[IntentFilter (new string [] {"android.intent.action.GET_CONTENT"},
	               Categories = new string [] {"android.intent.category.DEFAULT"})]
	public partial class FilePickerActivity
	{
	}
}
