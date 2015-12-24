using Newtonsoft.Json;

namespace visvitalis.Networking.Responses
{
    public class ValidMessageResponse
    {
        [JsonProperty("valid")]
        public bool Valid { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}