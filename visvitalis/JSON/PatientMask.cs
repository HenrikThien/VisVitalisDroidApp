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
    public class PatientMask
    { 
        [JsonProperty("einsatz")]
        public PatientOperation PatientOperation { get; set; }

        /// <summary>
        /// Gets the einsaetze by time.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <returns></returns>
        public List<Patient> GetEinsaetzeByTime(string time)
        {
            var patienten = (from i in PatientOperation.Tours
                             where i.Id == time
                             select i.Patients);

            var enumerable = patienten as IList<List<Patient>> ?? patienten.ToList();
            return enumerable.Any() ? enumerable.ElementAt(0).ToList() : null;
        }
    }
}