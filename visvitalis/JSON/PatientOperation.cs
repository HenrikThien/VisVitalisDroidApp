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
    public class PatientOperation
    {
        [JsonProperty("datum")]
        public string MaskDate { get; set; }
        [JsonProperty("ma")]
        public string WorkerToken { get; set; }
        [JsonProperty("touren")]
        public List<Touren> Tours { get; set; }
        [JsonIgnore]
        public string MaskNr { get; set; }
    }
}