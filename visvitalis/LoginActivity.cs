using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using Android.Text;
using Android.Text.Style;

namespace visvitalis
{
	[Activity(Label="Mitarbeiter Login", Icon = "@drawable/ic_launcher", Theme = "@style/CustomAppTheme")]
    public class LoginActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
			SetContentView (Resource.Layout.LoginScreen);

			ActionBar.SetDisplayShowHomeEnabled (true);

			ActionBar.SetIcon (GetDrawable (Resource.Drawable.ic_launcher));


			var typeface = Typeface.CreateFromAsset (this.Assets, "fonts/Generica.otf");
			var loginTitle = FindViewById<TextView> (Resource.Id.textView1);

			loginTitle.SetTypeface (typeface, TypefaceStyle.Normal);
			loginTitle.TextSize = 35;

			var st = new SpannableString("  " + "Vis Vitalis");
			st.SetSpan (new TypefaceSpan ("fonts/Generica.otf"), 0, st.Length (), SpanTypes.ExclusiveExclusive);

			ActionBar.TitleFormatted = st;
        }
    }
}