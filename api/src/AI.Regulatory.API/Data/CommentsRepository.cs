using AI.Regulatory.API.Contracts;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>Reviewer comments — R1.</summary>
public sealed class CommentsRepository : BaseRepository<Comment>
{
    public CommentsRepository(IOptions<DataOptions> options) : base(options) { }

    protected override bool MatchesId(Comment item, string id)
        => item.Id.ToString() == id;

    protected override IEnumerable<Comment> SeedData() => new[]
    {
        new Comment(1, "3.2.S.4.2", "Dr. Anna Chen", "DC", DateTime.UtcNow.AddHours(-2),
            "The UV detection wavelength should reference the validated specification in §3.2.S.4.1 explicitly. Please confirm alignment.", false),
        new Comment(2, "3.2.S.4.2", "Dr. Anna Chen", "DC", DateTime.UtcNow.AddHours(-1),
            "LOD calculation methodology — confirm ICH Q2(R2) §4.4.1 is cited correctly in the final version.", false),
        new Comment(3, "3.2.P.2",   "Dr. Anna Chen", "DC", DateTime.UtcNow.AddDays(-1),
            "Pharmaceutical development rationale for excipient selection is missing. Needs a supporting paragraph.", true),
    };
}
