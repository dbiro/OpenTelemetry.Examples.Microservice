using System;
using Newtonsoft.Json;

namespace WebApi.GitHub
{
    public class GitHubUser
    {
        public string Name { get; set; }
        public string Blog { get; set; }

        // This is deserialized using Json.NET, so use attributes as necessary
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
