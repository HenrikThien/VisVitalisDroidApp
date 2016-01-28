using System;
using Android.Views;
using Android.Support.V7.Widget;

namespace visvitalis.Recycler
{
    internal class RecyclerPatientViewAdapter : RecyclerView.Adapter
    {
        public event EventHandler<int> ItemClick;
        public event EventHandler<int> ItemLongClick;

        public RecyclerPatientItem mRecylcerItem;

        public RecyclerPatientViewAdapter(RecyclerPatientItem recyclerItem)
        {
            mRecylcerItem = recyclerItem;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).
                Inflate(Resource.Layout.fragment_card, parent, false);

            RecyclerPatientItemViewHolder vh = new RecyclerPatientItemViewHolder(itemView, OnClick, OnLongClick);
            return vh;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = holder as RecyclerPatientItemViewHolder;

            if (vh == null)
                return;

            vh.TitleTextView.Text = mRecylcerItem[position].PatientTitle;
            vh.PerformancesTextView.Text = mRecylcerItem[position].LeistungAsString();
            vh.ArrivalTextView.Text = mRecylcerItem[position].SecondLine();
            vh.DepartureTextView.Text = mRecylcerItem[position].ThirdLine();
        }

        public override int ItemCount
        {
            get
            {
                return mRecylcerItem.Count;
            }
        }

        void OnClick(int position)
        {
            if (ItemClick != null)
                ItemClick(this, position);
        }
        void OnLongClick(int position)
        {
            if (ItemLongClick != null)
                ItemLongClick(this, position);
        }
    }
}