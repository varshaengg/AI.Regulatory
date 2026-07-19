using AI.Regulatory.API.Contracts;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>User notifications — L1.</summary>
public sealed class NotificationsRepository : BaseRepository<UserNotification>
{
    public NotificationsRepository(IOptions<DataOptions> options) : base(options) { }

    protected override bool MatchesId(UserNotification item, string id)
        => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<UserNotification> SeedData() => new[]
    {
        new UserNotification("n1", "success", "Agent completed M2 Summaries for PX-102 DE",           DateTime.UtcNow.AddMinutes(-8)),
        new UserNotification("n2", "warning", "Section 3.2.S.4.2 flagged — low confidence 0.62",      DateTime.UtcNow.AddMinutes(-23)),
        new UserNotification("n3", "error",   "Citation missing in M4 Nonclinical EL-201",           DateTime.UtcNow.AddHours(-1)),
        new UserNotification("n4", "info",    "Template DE v4.2 updated by Sara",                    DateTime.UtcNow.AddHours(-2)),
        new UserNotification("n5", "mention", "Dr. Chen mentioned you in R1 comments",               DateTime.UtcNow.AddHours(-3)),
    };
}
