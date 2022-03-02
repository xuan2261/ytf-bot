using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FacebookAutomation
{
    public class Group
    {
        [JsonPropertyName("groupName")]
        public string GroupName { get; set; }

        [JsonPropertyName("groupId")]
        public string GroupId { get; set; }
    }

    public class FacebookConfig
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("pw")]
        public string Pw { get; set; }

        [JsonPropertyName("groups")]
        public List<Group> Groups { get; set; }
    }

}
