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
using visvitalis.JSON;

namespace visvitalis.Recycler
{
    internal class RecyclerPatientItem
    {
        private readonly List<Patient> _patients;

        public RecyclerPatientItem(List<Patient> patients)
        {
            _patients = patients;
        }

        public int Count
        {
            get { return _patients.Count; }
        }

        public Patient this[int position]
        {
            get { return _patients[position]; }
        }

        public long GetItemId(int position)
        {
            return position;
        }
    }
}