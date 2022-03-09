﻿using System.Collections.Generic;
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

        [JsonPropertyName("testGroups")]
        public List<Group> TestGroups { get; set; }

        [JsonPropertyName("taskGroups_01")]
        public List<Group> TaskGroups_01 { get; set; }
    }

}
