using Newtonsoft.Json;

namespace visvitalis.JSON
{
    public class ResponseMessage
    {
        [JsonProperty("valid")]
        public bool Valid { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("newest_version")]
        public int NewestVersion { get; set; }
    }
}