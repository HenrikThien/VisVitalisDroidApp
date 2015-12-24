using Newtonsoft.Json;

namespace visvitalis.Networking.Responses
{
    public class LoginResponse
    {
        [JsonProperty("valid")]
        public bool Valid { get; set; }
        [JsonProperty("groupname")]
        public string Groupname { get; set; }
        [JsonProperty("password")]
        public string Password { get; set; }
        [JsonProperty("client_id")]
        public string ClientId { get; set; }
        [JsonProperty("client_secret")]
        public string ClientSecret { get; set; }
    }
}