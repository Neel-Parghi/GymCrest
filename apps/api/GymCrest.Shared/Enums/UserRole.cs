using System.Text.Json.Serialization;

namespace GymCrest.Shared.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UserRole
    {
        SuperAdmin = 1,
        GymOwner = 2,
        Trainer = 3,
        Member = 4,
        Staff = 5,
        User = 6
    }
}
