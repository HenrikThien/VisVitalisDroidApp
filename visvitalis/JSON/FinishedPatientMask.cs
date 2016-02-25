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
    class FinishedPatientMask
    {
        [JsonProperty("patients")]
        public List<FinishedPatient> Patients { get; set; }
    }
}