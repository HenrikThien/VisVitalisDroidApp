using System.Collections.Generic;

using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using visvitalis.Utils;
using System.Threading.Tasks;
using visvitalis.Fragments;
using System;
using visvitalis.Encryption;
using Android.Support.V7.App;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace visvitalis
{
    [Activity(Label = "Nachrichten mit dem Büro", Icon = "@drawable/ic_launcher", Theme = "@style/MyTheme")]
    public class ChatActivity : AppCompatActivity
    {
        private EditText messageET;
        private ListView messagesContainer;
        private Button sendBtn;
        private ChatAdapter adapter;
        private List<ChatMessage> chatHistory;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.ChatLayout);

            if (StaticHolder.SessionHolder == null)
            {
                Toast.MakeText(this, "Der Chat kann nicht geöffnet werden. Die Session ist abgelaufen.", ToastLength.Long).Show();
                Finish();
                return;
            }

            Init();
        }

        async void Init()
        {
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetHomeButtonEnabled(true);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            await LoadSavedMessagesAsync();
        }

        async Task LoadSavedMessagesAsync()
        {
            messagesContainer = (ListView)FindViewById(Resource.Id.messagesContainer);
            messageET = (EditText)FindViewById(Resource.Id.messageEdit);
            sendBtn = (Button)FindViewById(Resource.Id.chatSendButton);

            RelativeLayout container = (RelativeLayout)FindViewById(Resource.Id.container);

            loadDummyHistory();
            sendBtn.Click += SendBtn_Click;

            await Task.Factory.StartNew(() =>
            {

            });
        }

        private async void SendBtn_Click(object sender, EventArgs e)
        {
            string message = messageET.Text.ToString();

            if (String.IsNullOrEmpty(message))
                return;

            var msg = new ChatMessage();
            msg.Id = 122;
            msg.Message = message;
            msg.DateTime = DateTime.Now.ToString();
            msg.IsMe = true;

            ChatEncryption crypto = new ChatEncryption();
            string themessage = await crypto.SendMessage(message);

            messageET.Text = "";
            DisplayMessage(msg);
        }

        void DisplayMessage(ChatMessage message)
        {
            adapter.Add(message);
            adapter.NotifyDataSetChanged();
            scroll();
        }

        private void scroll()
        {
            messagesContainer.SetSelection(messagesContainer.Count - 1);
        }

        private void loadDummyHistory()
        {
            chatHistory = new List<ChatMessage>();

            ChatMessage msg = new ChatMessage();
            msg.Id = 1;
            msg.IsMe = false;
            msg.Message = "Hi, hab da mal eine Frage..";
            msg.DateTime = DateTime.Now.ToString();
            chatHistory.Add(msg);
            ChatMessage msg1 = new ChatMessage();
            msg1.Id = 2;
            msg1.IsMe = false;
            msg1.Message = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.";
            msg1.DateTime = DateTime.Now.ToString();
            chatHistory.Add(msg1);

            adapter = new ChatAdapter(this, new List<ChatMessage>());
            messagesContainer.Adapter = adapter;

            for (int i = 0; i < chatHistory.Count; i++)
            {
                ChatMessage message = chatHistory[i];
                DisplayMessage(message);
            }

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