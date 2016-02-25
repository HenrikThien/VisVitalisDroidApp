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
    class FinishedPatient
    {
        [JsonProperty("groupname")]
        public string Groupname { get; set; }
        [JsonProperty("date")]
        public string Date { get; set; }
        [JsonProperty("patient")]
        public Patient Patient { get; set; }
    }
}