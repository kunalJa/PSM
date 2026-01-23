using Postgrest.Attributes;
using Postgrest.Models;
using UnityEngine.Scripting;

[Preserve]
[Table("profiles")]
public class Profile : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; }

    [Column("username")]
    public string Username { get; set; }

    [Column("updated_at")]
    public string UpdatedAt { get; set; }
}

[Preserve]
[Table("user_secrets")]
public class UserSecret : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; }

    [Column("email")]
    public string Email { get; set; }

    [Column("created_at")]
    public string CreatedAt { get; set; }
}

[Preserve]
[Table("posts")]
public class Post : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; }

    [Column("author_id")]
    public string AuthorId { get; set; }

    [Column("content_text")]
    public string ContentText { get; set; }

    [Column("media_url")]
    public string MediaUrl { get; set; }

    [Column("media_type")]
    public string MediaType { get; set; }

    [Column("created_at")]
    public string CreatedAt { get; set; }
}

[Preserve]
[Table("friendships")]
public class Friendship : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("user_id")]
    public string UserId { get; set; }

    [Column("friend_id")]
    public string FriendId { get; set; }

    [Column("created_at")]
    public string CreatedAt { get; set; }
}

[Preserve]
[Table("read_receipts")]
public class ReadReceipt : BaseModel
{
    [Column("user_id")]
    public string UserId { get; set; }

    [Column("post_id")]
    public string PostId { get; set; }

    [Column("opened_at")]
    public string OpenedAt { get; set; }
}
