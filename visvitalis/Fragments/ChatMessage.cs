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

namespace visvitalis.Fragments
{
    class ChatMessage : Java.Lang.Object
    {
        public long Id { get; set; }
        public bool IsMe { get; set; }
        public string Message { get; set; }
        public long UserId { get; set; }
        public string DateTime { get; set; }
    }
}