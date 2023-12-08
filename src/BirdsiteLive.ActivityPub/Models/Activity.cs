using System.Text.Json.Serialization;

namespace BirdsiteLive.ActivityPub
{
    public class Activity
    {
        [JsonIgnore]
        public string context { get; set; }
        // Avoids deserialization of @context, as it may be a JSON object instead of a string (e.g., on Firefish)
        [JsonPropertyName("@context")]
        public string SerializedContext => context;
        public string id { get; set; }
        public string type { get; set; }
        public string actor { get; set; }


        //[JsonPropertyName("object")]
        //public string apObject { get; set; }
    }
}