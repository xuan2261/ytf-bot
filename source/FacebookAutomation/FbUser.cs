using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FacebookAutomation
{
    public class FbUser
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("groups")]
        public List<GroupInfo> Groups { get; set; }

    }

    public class GroupInfo
    {
        [JsonPropertyName("groupName")]
        public string GroupName { get; set; }

        [JsonPropertyName("groupId")]
        public string GroupId { get; set; }
    }
}
