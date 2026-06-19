using MarriageApp.Core.Entities;
using MarriageApp.Core.Enums;
using MarriageApp.Core.Matching;
using MarriageApp.Infrastructure.Data;
using MarriageApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace MarriageApp.Tests;

/// <summary>
/// Unit tests for the matching algorithm, exercising range scoring, multi-select set
/// membership (incl. "no preference"), adjacency credit, and the soft hard-constraint penalty.
/// Uses the EF Core InMemory provider so we test the real service end-to-end.
/// </summary>
public class MatchingServiceTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static MatchingService NewService(AppDbContext db, MatchingWeights? w = null) =>
        new(db, Options.Create(w ?? new MatchingWeights()));

    // A baseline groom seeking a fairly open set of bride attributes.
    private static Profile MakeGroom(int id = 1) => new()
    {
        Id = id,
        UserId = $"groom-{id}",
        Gender = Gender.Male,
        Name = "عريس",
        Age = 30,
        HeightCm = 178,
        EducationLevel = EducationLevel.University,
        MaritalStatus = MaritalStatus.Single,
        CurrentResidence = "القاهرة",
        PhoneNumber = "0100",
        ReligiousCommitment = ReligiousCommitment.SeeksKnowledge,
        IntendsToTravel = true,
        Status = ProfileStatus.PendingMatch,
        FamilyDetails = new FamilyDetails { FamilyCommitmentLevel = FamilyCommitmentLevel.Committed },
        Requirements = new MatchRequirements
        {
            MinAge = 22, MaxAge = 28,
            MinHeightCm = 160, MaxHeightCm = 172,
            AcceptedEducationLevels = EducationLevelFlags.University | EducationLevelFlags.Postgraduate,
            AcceptedReligiousCommitments = ReligiousCommitmentFlags.SeeksKnowledge | ReligiousCommitmentFlags.MemorizesQuran,
            AcceptedDressCodes = DressCodeFlags.Niqab | DressCodeFlags.Abaya,
            AcceptedMaritalStatuses = MaritalStatusFlags.Single,
        }
    };

    private static Profile MakeBride(int id, int age, int height, EducationLevel edu,
        ReligiousCommitment commit, DressCode dress, MaritalStatus marital = MaritalStatus.Single,
        bool acceptsTravel = true) => new()
    {
        Id = id,
        UserId = $"bride-{id}",
        Gender = Gender.Female,
        Name = $"عروس {id}",
        Age = age,
        HeightCm = height,
        EducationLevel = edu,
        MaritalStatus = marital,
        CurrentResidence = "القاهرة",
        PhoneNumber = "0101",
        ReligiousCommitment = commit,
        DressCode = dress,
        AcceptsTravel = acceptsTravel,
        Status = ProfileStatus.PendingMatch,
        FamilyDetails = new FamilyDetails { FamilyCommitmentLevel = FamilyCommitmentLevel.Committed },
        // Open requirements so the backward direction scores ~100 and isolates the forward test.
        Requirements = new MatchRequirements()
    };

    [Fact]
    public async Task PerfectCandidate_ScoresNear100()
    {
        using var db = NewDb();
        var groom = MakeGroom();
        var perfect = MakeBride(2, age: 25, height: 165, edu: EducationLevel.University,
            commit: ReligiousCommitment.SeeksKnowledge, dress: DressCode.Niqab);
        db.AddRange(groom, perfect);
        await db.SaveChangesAsync();

        var top = await NewService(db).GetTopMatchesAsync(groom.Id);

        Assert.Single(top);
        Assert.True(top[0].Percentage >= 99, $"expected ~100 but got {top[0].Percentage}");
        Assert.Empty(top[0].ViolatedConstraints);
    }

    [Fact]
    public async Task OutOfRangeHeight_ScoresLowerButPartialWithinFalloff()
    {
        using var db = NewDb();
        var groom = MakeGroom(); // wants height 160–172
        // 176 is 4cm above the band; with falloff 10 it earns 60% of the height weight.
        var slightlyTall = MakeBride(2, age: 25, height: 176, edu: EducationLevel.University,
            commit: ReligiousCommitment.SeeksKnowledge, dress: DressCode.Niqab);
        // 200 is far outside -> 0 height points.
        var tooTall = MakeBride(3, age: 25, height: 200, edu: EducationLevel.University,
            commit: ReligiousCommitment.SeeksKnowledge, dress: DressCode.Niqab);
        db.AddRange(groom, slightlyTall, tooTall);
        await db.SaveChangesAsync();

        var top = await NewService(db).GetTopMatchesAsync(groom.Id);

        // Slightly tall ranks above too tall.
        Assert.Equal(slightlyTall.Id, top[0].CandidateProfileId);
        Assert.True(top[0].Percentage > top[1].Percentage);
    }

    [Fact]
    public async Task EmptyPreferenceSet_CountsAsFullCredit()
    {
        using var db = NewDb();
        var groom = MakeGroom();
        // Clear the education preference entirely -> "no preference" -> any education scores full.
        groom.Requirements!.AcceptedEducationLevels = EducationLevelFlags.None;
        var lowEdu = MakeBride(2, age: 25, height: 165, edu: EducationLevel.BelowSecondary,
            commit: ReligiousCommitment.SeeksKnowledge, dress: DressCode.Niqab);
        db.AddRange(groom, lowEdu);
        await db.SaveChangesAsync();

        var top = await NewService(db).GetTopMatchesAsync(groom.Id);

        var eduCriterion = top[0].Breakdown.First(c => c.Criterion == "المؤهل");
        Assert.Equal(eduCriterion.Weight, eduCriterion.Earned); // full credit
    }

    [Fact]
    public async Task AdjacentReligiousCommitment_GetsPartialCredit()
    {
        using var db = NewDb();
        var groom = MakeGroom(); // accepts SeeksKnowledge | MemorizesQuran
        // Candidate is ListensToLessons — one step below SeeksKnowledge -> adjacency credit.
        var adjacent = MakeBride(2, age: 25, height: 165, edu: EducationLevel.University,
            commit: ReligiousCommitment.ListensToLessons, dress: DressCode.Niqab);
        db.AddRange(groom, adjacent);
        await db.SaveChangesAsync();

        var top = await NewService(db).GetTopMatchesAsync(groom.Id);
        var commitCriterion = top[0].Breakdown.First(c => c.Criterion == "مستوى الالتزام");

        Assert.True(commitCriterion.Earned > 0 && commitCriterion.Earned < commitCriterion.Weight,
            $"expected partial credit, got {commitCriterion.Earned}/{commitCriterion.Weight}");
    }

    [Fact]
    public async Task TravelConstraintViolation_AppliesHeavyPenalty()
    {
        using var db = NewDb();
        var groom = MakeGroom();
        groom.Requirements!.RequiresTravelWillingness = true;

        var willing = MakeBride(2, age: 25, height: 165, edu: EducationLevel.University,
            commit: ReligiousCommitment.SeeksKnowledge, dress: DressCode.Niqab, acceptsTravel: true);
        var notWilling = MakeBride(3, age: 25, height: 165, edu: EducationLevel.University,
            commit: ReligiousCommitment.SeeksKnowledge, dress: DressCode.Niqab, acceptsTravel: false);
        db.AddRange(groom, willing, notWilling);
        await db.SaveChangesAsync();

        var top = await NewService(db).GetTopMatchesAsync(groom.Id);
        var willingScore = top.First(t => t.CandidateProfileId == willing.Id);
        var notWillingScore = top.First(t => t.CandidateProfileId == notWilling.Id);

        Assert.Contains("الطرف الآخر غير مستعد للسفر", notWillingScore.ViolatedConstraints);
        Assert.True(notWillingScore.Percentage < willingScore.Percentage);
    }

    [Fact]
    public async Task StrictFilterMode_ExcludesViolatingCandidate()
    {
        using var db = NewDb();
        var weights = new MatchingWeights { TreatConstraintsAsStrictFilter = true };
        var groom = MakeGroom();
        groom.Requirements!.RequiresTravelWillingness = true;

        var notWilling = MakeBride(2, age: 25, height: 165, edu: EducationLevel.University,
            commit: ReligiousCommitment.SeeksKnowledge, dress: DressCode.Niqab, acceptsTravel: false);
        db.AddRange(groom, notWilling);
        await db.SaveChangesAsync();

        var top = await NewService(db, weights).GetTopMatchesAsync(groom.Id);

        Assert.Empty(top); // strict filter removes the violating candidate entirely
    }

    [Fact]
    public async Task ReturnsAtMostTopN_SortedDescending()
    {
        using var db = NewDb();
        var groom = MakeGroom();
        db.Add(groom);
        for (int i = 2; i <= 10; i++)
        {
            db.Add(MakeBride(i, age: 20 + i, height: 160 + i, edu: EducationLevel.University,
                commit: ReligiousCommitment.SeeksKnowledge, dress: DressCode.Niqab));
        }
        await db.SaveChangesAsync();

        var top = await NewService(db).GetTopMatchesAsync(groom.Id, take: 5);

        Assert.Equal(5, top.Count);
        for (int i = 1; i < top.Count; i++)
            Assert.True(top[i - 1].Percentage >= top[i].Percentage);
    }
}
