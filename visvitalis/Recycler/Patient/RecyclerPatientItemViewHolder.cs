using System;
using Android.Views;
using Android.Widget;
using Android.Support.V7.Widget;

namespace visvitalis.Recycler
{
    internal class RecyclerPatientItemViewHolder : RecyclerView.ViewHolder
    {
        public TextView TitleTextView { get; private set; }
        public TextView PerformancesTextView { get; private set; }
        public TextView ArrivalTextView { get; private set; }
        public TextView DepartureTextView { get; private set; }

        public RecyclerPatientItemViewHolder(View view, Action<int> listener, Action<int> listenerLong) : base(view)
        {
            TitleTextView = view.FindViewById<TextView>(Resource.Id.titleTextView);
            PerformancesTextView = view.FindViewById<TextView>(Resource.Id.performancesTextView);
            ArrivalTextView = view.FindViewById<TextView>(Resource.Id.arrivalTextView);
            DepartureTextView = view.FindViewById<TextView>(Resource.Id.departureTextView);

            view.Click += (sender, e) => listener(Position);
            view.LongClick += (sender, e) => listenerLong(Position);
        }
    }
}