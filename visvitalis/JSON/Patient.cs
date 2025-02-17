using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace visvitalis.JSON
{
    public class Patient
    {
        [JsonProperty("nr")]
        public int Nr { get; set; }
        [JsonProperty("patient")]
        public string PatientName { get; set; }
        [JsonProperty("km")]
        public string Km { get; set; }
        [JsonProperty("ank")]
        public string Arrival { get; set; }
        [JsonProperty("abf")]
        public string Departure { get; set; }
        [JsonProperty("mission")]
        public string Mission { get; set; }
        [JsonProperty("leistung")]
        public List<string> Performances { get; set; }
        [JsonProperty("ma")]
        public string WorkerToken { get; set; }
        [JsonProperty("state")]
        public string ServerState { get; set; }
        [JsonProperty("order")]
        public int Order { get; set; }

        public string LeistungAsString()
        {
            var builder = new StringBuilder();
            foreach (var lst in Performances)
            {
                builder.Append(lst + " ");
            }
            return builder.ToString().TrimEnd();
        }

        public override string ToString()
        {
            return Nr + " - " + PatientName + " | " + LeistungAsString();
        }

        [JsonIgnore]
        public string PatientTitle
        {
            get { return Nr + " - " + PatientName; }
        }

        public string GetPatientArrival()
        {
            var builder = new StringBuilder();
            if (!string.IsNullOrEmpty(Arrival))
            {
                builder.Append("Ankunftszeit: " + Arrival + "");
            }
            else
            {
                builder.Append("Ankunft: Noch nicht eingetragen, bitte klicken!");
            }
            return builder.ToString();
        }

        public string GetPatientDeparture()
        {
            var builder = new StringBuilder();
            if (!string.IsNullOrEmpty(Departure))
            {
                builder.Append("Abfahrtszeit: " + Departure + "");
            }
            else
            {
                builder.Append("Abfahrt: Noch nicht eingetragen, bitte klicken!");
            }
            return builder.ToString();
        }
    }
}