using System;
using BirdsiteLive.Twitter.Models;

namespace BirdsiteLive.Common.Interfaces;

public interface SocialMediaPost
{
    public long Id { get; set; }
    public SocialMediaUser Author { get; set; }
    public SocialMediaUser OriginalAuthor { get; set; }
    public string MessageContent { get; set; }
    public bool IsRetweet { get; set; } 
    public long RetweetId { get; set; } 
    public long? InReplyToStatusId { get; set; }
    public string InReplyToAccount { get; set; }
    public DateTime CreatedAt { get; set; }
    public ExtractedMedia[] Media { get; set; }
}