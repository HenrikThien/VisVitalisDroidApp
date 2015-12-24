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
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace visvitalis.JSON
{
    public class Touren
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("patient")]
        public List<Patient> Patients { get; set; }
    }
}