using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using BirdsiteLive.Common.Interfaces;

namespace BirdsiteLive.DAL.Models
{
    public class SyncTwitterUser : SyncUser
    {
        public int Id { get; set; }
        public long TwitterUserId { get; set; }
        public string Acct { get; set; }
        public string FediAcct { get; set; }

        public long LastTweetPostedId { get; set; }

        public DateTime LastSync { get; set; }

        public int FetchingErrorCount { get; set; } //TODO: update DAL
        public long Followers { get; set; } 
        public int StatusesCount { get; set; }
        public JsonElement ExtraData { get; set; } = new JsonElement();

        public string PreDescriptionHook
        {
            get
            {
                JsonElement hooks;
                if(!ExtraData.TryGetProperty("hooks", out hooks))
                    return "";
                
                JsonElement preDesc;
                if(!hooks.TryGetProperty("preDescription", out preDesc))
                    return "";

                return preDesc.GetString();
            }
        }
        public string PostDescriptionHook 
        {
            get
            {
                JsonElement hooks;
                if(!ExtraData.TryGetProperty("hooks", out hooks))
                    return "";
                
                JsonElement preDesc;
                if(!hooks.TryGetProperty("postDescription", out preDesc))
                    return "";

                return preDesc.GetString();
            }
        }
        public (string, string)[] AdditionnalAttachments 
        {
            get
            {
                JsonElement hooks;
                if(!ExtraData.TryGetProperty("hooks", out hooks))
                    return Array.Empty<(string, string)>();
                
                JsonElement attachements;
                if(!hooks.TryGetProperty("addAttachments", out attachements))
                    return Array.Empty<(string, string)>();

                var finalAtt = new List<(string, string)>();
                foreach (JsonProperty att in attachements.EnumerateObject())
                {
                    finalAtt = finalAtt.Append((att.Name, att.Value.GetString())).ToList();
                }
                return finalAtt.ToArray();
            }
        }
        public string DisclaimerReplacementHook
        {
            get
            {
                JsonElement hooks;
                if(!ExtraData.TryGetProperty("hooks", out hooks))
                    return "";
                
                JsonElement preDesc;
                if(!hooks.TryGetProperty("disclaimerReplacement", out preDesc))
                    return "";

                return preDesc.GetString();
            }
        }
    }
}