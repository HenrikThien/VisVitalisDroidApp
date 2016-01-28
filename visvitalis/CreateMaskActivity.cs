using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using visvitalis.Fragments;
using visvitalis.JSON;
using visvitalis.Utils;
using Android.Support.V7.App;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace visvitalis
{
    [Activity(Label = "Bestehende Masken benutzen", Icon = "@drawable/ic_launcher", Theme = "@style/MyTheme")]
    public class CreateMaskActivity : AppCompatActivity
    {
        private ListView masksListView;
        private CreateMaskAdapter masksAdapter;
            
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            OverridePendingTransition(Resource.Animation.from_left, Resource.Animation.hold);
            SetContentView(Resource.Layout.CreateMaskLayout);

            masksListView = FindViewById<ListView>(Resource.Id.masksListView);
            masksListView.ChoiceMode = ChoiceMode.Single;

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            FillListView();
        }

        protected override void OnPause()
        {
            OverridePendingTransition(Resource.Animation.hold, Resource.Animation.to_left);
            base.OnPause();
        }

        async void FillListView()
        {
            var allMasks = await LoadFilesAsync();
            var inflater = Application.Context.GetSystemService(LayoutInflaterService) as LayoutInflater;

            masksAdapter = new CreateMaskAdapter(allMasks, inflater);
            masksListView.Adapter = masksAdapter;
        }

        async Task<List<RootObject>> LoadFilesAsync()
        {
            var allMasks = new List<RootObject>();

            var path = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, AppConstants.DataFolder, "futuremasks");

            foreach (var file in Directory.GetFiles(path))
            {
                using (var reader = new StreamReader(file))
                {
                    var content = await reader.ReadToEndAsync();

                    try
                    {
                        var mask = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<RootObject>(content));
                        allMasks.Add(mask);
                    }
                    catch
                    {
                    }
                }
            }

            return allMasks;
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            return base.OnPrepareOptionsMenu(menu);
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