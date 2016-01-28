
using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;

namespace visvitalis
{
    [Activity(Label = "Neuen Einsatz erstellen", Icon = "@drawable/ic_launcher", Theme = "@style/MyTheme")]
    public class CreateNewEntryActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            OverridePendingTransition(Resource.Animation.from_left, Resource.Animation.hold);
            SetContentView(Resource.Layout.CreateNewEntryLayout);

            InitActionBar();
        }

        void InitActionBar()
        {
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetIcon(null);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetHomeButtonEnabled(true);
        }

        protected override void OnPause()
        {
            OverridePendingTransition(Resource.Animation.hold, Resource.Animation.to_left);
            base.OnPause();
        }
     
        public override bool OnOptionsItemSelected(IMenuItem menu)
        {
            switch (menu.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    return true;
                default:
                    return base.OnOptionsItemSelected(menu);
            }
        }
    }
}