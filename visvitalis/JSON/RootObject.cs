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
using Newtonsoft.Json;

namespace visvitalis.JSON
{
    public class RootObject
    {
        [JsonProperty("valid")]
        public bool Valid { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("data")]
        public PatientMask PatientMask { get; set; }
        [JsonProperty("weeknr")]
        public string WeekNr { get; set; }
        [JsonProperty("masknr")]
        public string MaskNr { get; set; }
        [JsonProperty("groupname")]
        public string Groupname { get; set; }
    }
}