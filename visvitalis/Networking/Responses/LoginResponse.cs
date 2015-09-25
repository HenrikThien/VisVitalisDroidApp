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

namespace visvitalis.Networking.Responses
{
    public class LoginResponse
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public AccessTokenResponse AccessTokenResponse { get; set; }
    }
}