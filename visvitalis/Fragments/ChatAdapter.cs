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
using Java.Lang;

namespace visvitalis.Fragments
{
    class ChatAdapter : BaseAdapter
    {
        private readonly List<ChatMessage> chatMessages;
        private Context context;

        public ChatAdapter(Context context, List<ChatMessage> chatMessages)
        {
            this.context = context;
            this.chatMessages = chatMessages;
        }

        public override int Count
        {
            get
            {
                return chatMessages.Count;
            }
        }

        public override Java.Lang.Object GetItem(int position)
        {
            if (chatMessages != null)
                return chatMessages.ElementAt(position);
            else
                return null;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            ViewHolder holder;
            ChatMessage chatMessage = (ChatMessage)GetItem(position);
            LayoutInflater vi = (LayoutInflater)context.GetSystemService(Context.LayoutInflaterService);

            if (convertView == null)
            {
                convertView = vi.Inflate(Resource.Layout.chat_activity_list_item, null);
                holder = CreateViewHolder(convertView);
                convertView.Tag = holder;
            }
            else
            {
                holder = (ViewHolder)convertView.Tag;
            }

            bool myMsg = chatMessage.IsMe;//Just a dummy check to simulate whether it me or other sender
            setAlignment(holder, myMsg);
            holder.txtMessage.Text = chatMessage.Message;
            holder.txtInfo.Text = chatMessage.DateTime;

            return convertView;
        }

        public void Add(ChatMessage message)
        {
            chatMessages.Add(message);
        }

        public void Add(List<ChatMessage> messages)
        {
            chatMessages.AddRange(messages);
        }

        private void setAlignment(ViewHolder holder, bool isMe)
        {
            if (!isMe)
            {
                holder.contentWithBG.SetBackgroundResource(Resource.Drawable.out_message_bg);

                LinearLayout.LayoutParams layoutParams = (LinearLayout.LayoutParams)holder.contentWithBG.LayoutParameters;
                layoutParams.Gravity = GravityFlags.Left;
                holder.contentWithBG.LayoutParameters = layoutParams;

                RelativeLayout.LayoutParams lp = (RelativeLayout.LayoutParams)holder.content.LayoutParameters;
                lp.AddRule(LayoutRules.AlignParentRight, 0);
                lp.AddRule(LayoutRules.AlignParentLeft);
                holder.content.LayoutParameters = lp;
                layoutParams = (LinearLayout.LayoutParams)holder.txtMessage.LayoutParameters;
                layoutParams.Gravity = GravityFlags.Left;
                holder.txtMessage.LayoutParameters = layoutParams;

                layoutParams = (LinearLayout.LayoutParams)holder.txtInfo.LayoutParameters;
                layoutParams.Gravity = GravityFlags.Left;
                holder.txtInfo.LayoutParameters = layoutParams;

            }
            else
            {
                holder.contentWithBG.SetBackgroundResource(Resource.Drawable.in_message_bg);

                LinearLayout.LayoutParams layoutParams = (LinearLayout.LayoutParams)holder.contentWithBG.LayoutParameters;
                layoutParams.Gravity = GravityFlags.Right;
                holder.contentWithBG.LayoutParameters = layoutParams;

                RelativeLayout.LayoutParams lp = (RelativeLayout.LayoutParams)holder.content.LayoutParameters;
                lp.AddRule(LayoutRules.AlignParentLeft, 0);
                lp.AddRule(LayoutRules.AlignParentRight);
                holder.content.LayoutParameters = lp;

                layoutParams = (LinearLayout.LayoutParams)holder.txtMessage.LayoutParameters;
                layoutParams.Gravity = GravityFlags.Right;
                holder.txtMessage.LayoutParameters = layoutParams;

                layoutParams = (LinearLayout.LayoutParams)holder.txtInfo.LayoutParameters;
                layoutParams.Gravity = GravityFlags.Right;
                holder.txtInfo.LayoutParameters = layoutParams;
            }
        }

        private ViewHolder CreateViewHolder(View v)
        {
            ViewHolder holder = new ViewHolder();
            holder.txtMessage = (TextView)v.FindViewById(Resource.Id.txtMessage);
            holder.content = (LinearLayout)v.FindViewById(Resource.Id.content);
            holder.contentWithBG = (LinearLayout)v.FindViewById(Resource.Id.contentWithBackground);
            holder.txtInfo = (TextView)v.FindViewById(Resource.Id.txtInfo);
            return holder;
        }
    }

    class ViewHolder : Java.Lang.Object
    {
        public TextView txtMessage;
        public TextView txtInfo;
        public LinearLayout content;
        public LinearLayout contentWithBG;
    }
}