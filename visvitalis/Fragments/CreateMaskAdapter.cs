using System.Collections.Generic;
using Android.Views;
using Android.Widget;
using visvitalis.JSON;
using Android.Util;

namespace visvitalis.Fragments
{
    internal class CreateMaskAdapter : BaseAdapter<RootObject>
    {
        private readonly LayoutInflater _inflater;
        private readonly List<RootObject> _patients;

        public CreateMaskAdapter(List<RootObject> list, LayoutInflater inflater)
        {
            _patients = list;
            _inflater = inflater;
        }

        public override RootObject this[int position]
        {
            get { return _patients[position]; }
        }

        public override int Count
        {
            get { return _patients.Count; }
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = convertView ?? _inflater.Inflate(Resource.Layout.simple_patient_list_view, parent, false);

            if (this[position] == null)
            {
                return view;
            }

            var line1 = this[position].ToString();
            var line2 = "A: " + this[position].PatientsMorgensAsString;
            var line3 = "V: " + this[position].PatientsAbendsAsString;

            var text1 = view.FindViewById<TextView>(Resource.Id.line_a);
            var text2 = view.FindViewById<TextView>(Resource.Id.line_b);
            var text3 = view.FindViewById<TextView>(Resource.Id.line_c);

            text1.Text = line1;
            text2.Text = line2;
            text3.Text = line3;

            return view;
        }
    }
}