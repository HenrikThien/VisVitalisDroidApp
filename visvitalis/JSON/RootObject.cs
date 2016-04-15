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
        [JsonProperty("is_new")]
        public bool IsNew { get; set; }
        [JsonProperty("data")]
        public List<PatientMask> PatientMask { get; set; }
        [JsonProperty("weeknr")]
        public string WeekNr { get; set; }
        [JsonProperty("masknr")]
        public string MaskNr { get; set; }
        [JsonProperty("groupname")]
        public string Groupname { get; set; }
        //[JsonProperty("datum_start")]
        //public string DatumStart { get; set; } // todo: remove
        //[JsonProperty("datum_end")]
        //public string DatumEnd { get; set; } // todo: remove

        public override string ToString()
        {
            return "Die Maske";
        }

        [JsonIgnore]
        public string PatientsMorgensAsString
        {
            get
            {
                return string.Join(",", PatientMask[0].GetEinsaetzeByTime("morgens"));
            }
        }
        [JsonIgnore]
        public string PatientsAbendsAsString
        {
            get
            {
                return string.Join(",", PatientMask[0].GetEinsaetzeByTime("abends"));
            }
        }
    }
}