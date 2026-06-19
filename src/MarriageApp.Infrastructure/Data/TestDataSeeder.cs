using MarriageApp.Core.Entities;
using MarriageApp.Core.Enums;
using MarriageApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MarriageApp.Infrastructure.Data;

/// <summary>
/// Seeds realistic sample applicants (grooms + brides) for DEVELOPMENT/testing so the
/// admin match-review screen has data. Idempotent: runs only when no profiles exist.
/// All test accounts share the password "Test#12345".
/// </summary>
public static class TestDataSeeder
{
    public const string TestPassword = "Test#12345";

    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("TestDataSeeder");

        var now = DateTime.UtcNow;

        foreach (var (email, profile) in BuildProfiles(now))
        {
            // Idempotent per account: skip any test user that already exists.
            if (await userManager.FindByEmailAsync(email) is not null) continue;

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = profile.Name,
                CreatedAt = now
            };

            var result = await userManager.CreateAsync(user, TestPassword);
            if (!result.Succeeded)
            {
                logger.LogError("Failed to create test user {Email}: {Errors}",
                    email, string.Join("; ", result.Errors.Select(e => e.Description)));
                continue;
            }
            await userManager.AddToRoleAsync(user, AppRoles.User);

            profile.UserId = user.Id;
            profile.Status = ProfileStatus.PendingMatch;
            profile.CreatedAt = now;
            profile.UpdatedAt = now;
            profile.RecalculateAge(now);
            db.Profiles.Add(profile);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} test profiles (password: {Password}).",
            await db.Profiles.CountAsync(), TestPassword);
    }

    // ---------------------------------------------------------------------------------
    // Sample applicants: 4 grooms + 6 brides with varied attributes so the matching
    // algorithm produces a meaningful spread of scores (great / good / penalized).
    // ---------------------------------------------------------------------------------
    private static IEnumerable<(string Email, Profile Profile)> BuildProfiles(DateTime now)
    {
        // ===================== GROOMS (العرسان) =====================

        yield return ("ahmed.groom@test.local", new Profile
        {
            Gender = Gender.Male,
            Name = "أحمد محمود",
            DateOfBirth = new DateTime(1994, 3, 12),   // ~32
            HeightCm = 178, WeightKg = 82,
            EducationLevel = EducationLevel.University,
            Occupation = "مهندس برمجيات",
            MaritalStatus = MaritalStatus.Single,
            CurrentResidence = "القاهرة",
            FutureMaritalResidence = "القاهرة - مدينة نصر",
            PhoneNumber = "+201001234501",
            ReligiousCommitment = ReligiousCommitment.SeeksKnowledge,
            PersonalityDescription = "هادئ الطباع، أحب القراءة والرياضة، ملتزم بعملي وأسعى لبيت مستقر.",
            IntendsToTravel = false,
            AcceptsDivorcedOrWithChildrenBride = false,
            FamilyDetails = new FamilyDetails
            {
                FatherOccupation = "محاسب",
                MotherOccupationOrEducation = "ربة منزل - مؤهل عالي",
                SiblingsCountAndStudies = "أخ مهندس وأخت طبيبة",
                FamilyCommitmentLevel = FamilyCommitmentLevel.Committed,
                MotherOrSisterPhone = "+201001234511"
            },
            Requirements = new MatchRequirements
            {
                MinAge = 22, MaxAge = 28,
                MinHeightCm = 158, MaxHeightCm = 170,
                AcceptedEducationLevels = EducationLevelFlags.University | EducationLevelFlags.Postgraduate,
                AcceptedReligiousCommitments = ReligiousCommitmentFlags.SeeksKnowledge | ReligiousCommitmentFlags.MemorizesQuran,
                AcceptedDressCodes = DressCodeFlags.Niqab | DressCodeFlags.Abaya,
                AcceptedMaritalStatuses = MaritalStatusFlags.Single,
                AcceptedFamilyCommitmentLevels = FamilyCommitmentLevelFlags.Committed | FamilyCommitmentLevelFlags.VeryCommitted,
                AcceptedResidences = { new RequirementResidence { City = "القاهرة" }, new RequirementResidence { City = "الجيزة" } },
                RequiresTravelWillingness = false,
                AcceptsDivorced = false,
                AcceptsWithChildren = false,
                OtherConditions = "أفضل أسرة ملتزمة ومحافظة."
            }
        });

        yield return ("omar.groom@test.local", new Profile
        {
            Gender = Gender.Male,
            Name = "عمر السيد",
            DateOfBirth = new DateTime(1991, 7, 25),   // ~35
            HeightCm = 172, WeightKg = 90,
            EducationLevel = EducationLevel.Postgraduate,
            Occupation = "طبيب أسنان",
            MaritalStatus = MaritalStatus.Divorced,
            CurrentResidence = "الإسكندرية",
            FutureMaritalResidence = "الإسكندرية - سموحة",
            PhoneNumber = "+201001234502",
            ReligiousCommitment = ReligiousCommitment.MemorizesQuran,
            PersonalityDescription = "اجتماعي وطموح، سبق لي الزواج وأبحث عن بداية جديدة بنية صادقة.",
            IntendsToTravel = true,
            AcceptsDivorcedOrWithChildrenBride = true,
            FamilyDetails = new FamilyDetails
            {
                FatherOccupation = "تاجر",
                MotherOccupationOrEducation = "مدرّسة",
                SiblingsCountAndStudies = "ثلاثة إخوة جامعيون",
                FamilyCommitmentLevel = FamilyCommitmentLevel.VeryCommitted,
                MotherOrSisterPhone = "+201001234512"
            },
            Requirements = new MatchRequirements
            {
                MinAge = 25, MaxAge = 33,
                MinHeightCm = 155, MaxHeightCm = 175,
                AcceptedEducationLevels = EducationLevelFlags.Diploma | EducationLevelFlags.University | EducationLevelFlags.Postgraduate,
                AcceptedReligiousCommitments = ReligiousCommitmentFlags.MemorizesQuran | ReligiousCommitmentFlags.SeeksKnowledge,
                AcceptedDressCodes = DressCodeFlags.Niqab,
                AcceptedMaritalStatuses = MaritalStatusFlags.Single | MaritalStatusFlags.Divorced | MaritalStatusFlags.Widowed,
                AcceptedFamilyCommitmentLevels = FamilyCommitmentLevelFlags.VeryCommitted | FamilyCommitmentLevelFlags.Committed,
                RequiresTravelWillingness = true,   // hard-ish constraint: bride must accept travel
                AcceptsDivorced = true,
                AcceptsWithChildren = true,
                OtherConditions = "النية للسفر للخليج لفترة عمل."
            }
        });

        yield return ("mostafa.groom@test.local", new Profile
        {
            Gender = Gender.Male,
            Name = "مصطفى عبد الرحمن",
            DateOfBirth = new DateTime(1998, 1, 8),    // ~28
            HeightCm = 183, WeightKg = 77,
            EducationLevel = EducationLevel.Secondary,
            Occupation = "صاحب ورشة نجارة",
            MaritalStatus = MaritalStatus.Single,
            CurrentResidence = "الجيزة",
            FutureMaritalResidence = "الجيزة - فيصل",
            PhoneNumber = "+201001234503",
            ReligiousCommitment = ReligiousCommitment.ListensToLessons,
            PersonalityDescription = "عملي وبسيط، أحب شغلي وبجتهد عشان أكوّن بيت محترم.",
            IntendsToTravel = false,
            AcceptsDivorcedOrWithChildrenBride = false,
            FamilyDetails = new FamilyDetails
            {
                FatherOccupation = "نجار",
                MotherOccupationOrEducation = "ربة منزل",
                SiblingsCountAndStudies = "أخوان يعملان بالتجارة",
                FamilyCommitmentLevel = FamilyCommitmentLevel.SomewhatCommitted,
                MotherOrSisterPhone = "+201001234513"
            },
            Requirements = new MatchRequirements
            {
                MinAge = 20, MaxAge = 26,
                MinHeightCm = 150, MaxHeightCm = 168,
                AcceptedEducationLevels = EducationLevelFlags.BelowSecondary | EducationLevelFlags.Secondary | EducationLevelFlags.Diploma,
                AcceptedReligiousCommitments = ReligiousCommitmentFlags.ListensToLessons | ReligiousCommitmentFlags.NoMixing,
                AcceptedDressCodes = DressCodeFlags.Abaya | DressCodeFlags.SkirtsAndDresses,
                AcceptedMaritalStatuses = MaritalStatusFlags.Single,
                AcceptedResidences = { new RequirementResidence { City = "الجيزة" } },
                RequiresTravelWillingness = false,
                AcceptsDivorced = false,
                AcceptsWithChildren = false
            }
        });

        yield return ("youssef.groom@test.local", new Profile
        {
            Gender = Gender.Male,
            Name = "يوسف الشريف",
            DateOfBirth = new DateTime(1989, 11, 2),   // ~36
            HeightCm = 175, WeightKg = 85,
            EducationLevel = EducationLevel.University,
            Occupation = "محاسب قانوني",
            MaritalStatus = MaritalStatus.Widowed,
            CurrentResidence = "المنصورة",
            FutureMaritalResidence = "المنصورة",
            PhoneNumber = "+201001234504",
            ReligiousCommitment = ReligiousCommitment.SeeksKnowledge,
            PersonalityDescription = "أرمل ولديّ طفل، أبحث عن زوجة صالحة تكون أمًّا حنونة بإذن الله.",
            IntendsToTravel = false,
            AcceptsDivorcedOrWithChildrenBride = true,
            FamilyDetails = new FamilyDetails
            {
                FatherOccupation = "موظف حكومي متقاعد",
                MotherOccupationOrEducation = "ربة منزل",
                SiblingsCountAndStudies = "أخت متزوجة وأخ مهندس",
                FamilyCommitmentLevel = FamilyCommitmentLevel.Committed,
                MotherOrSisterPhone = "+201001234514"
            },
            Requirements = new MatchRequirements
            {
                MinAge = 26, MaxAge = 36,
                AcceptedEducationLevels = EducationLevelFlags.Secondary | EducationLevelFlags.Diploma | EducationLevelFlags.University,
                AcceptedReligiousCommitments = ReligiousCommitmentFlags.SeeksKnowledge | ReligiousCommitmentFlags.MemorizesQuran | ReligiousCommitmentFlags.ListensToLessons,
                AcceptedDressCodes = DressCodeFlags.Niqab | DressCodeFlags.Abaya | DressCodeFlags.SkirtsAndDresses,
                AcceptedMaritalStatuses = MaritalStatusFlags.Single | MaritalStatusFlags.Divorced | MaritalStatusFlags.Widowed,
                RequiresTravelWillingness = false,
                AcceptsDivorced = true,
                AcceptsWithChildren = true,
                OtherConditions = "تقبل برعاية طفلي وتعامله كابنها."
            }
        });

        // ===================== BRIDES (العرائس) =====================

        yield return ("fatma.bride@test.local", new Profile
        {
            Gender = Gender.Female,
            Name = "فاطمة حسن",
            DateOfBirth = new DateTime(2000, 5, 20),   // ~26
            HeightCm = 163,
            EducationLevel = EducationLevel.University,
            Occupation = "صيدلانية",
            MaritalStatus = MaritalStatus.Single,
            CurrentResidence = "القاهرة",
            PhoneNumber = "+201001234601",
            ReligiousCommitment = ReligiousCommitment.MemorizesQuran,
            PersonalityDescription = "حافظة للقرآن والحمد لله، هادئة وأحب البيت والاستقرار.",
            DressCode = DressCode.Niqab,
            AcceptsTravel = true,
            AllowsOnlineViewing = false,
            AcceptsDivorcedOrWithChildrenGroom = false,
            FamilyDetails = new FamilyDetails
            {
                FatherOccupation = "مدرّس",
                MotherOccupationOrEducation = "ربة منزل - ليسانس آداب",
                SiblingsCountAndStudies = "أخ جامعي وأختان في الثانوية",
                FamilyCommitmentLevel = FamilyCommitmentLevel.VeryCommitted
            },
            Requirements = new MatchRequirements
            {
                MinAge = 27, MaxAge = 35,
                MinHeightCm = 170, MaxHeightCm = 190,
                AcceptedEducationLevels = EducationLevelFlags.University | EducationLevelFlags.Postgraduate,
                AcceptedReligiousCommitments = ReligiousCommitmentFlags.SeeksKnowledge | ReligiousCommitmentFlags.MemorizesQuran,
                AcceptedMaritalStatuses = MaritalStatusFlags.Single,
                AcceptedFamilyCommitmentLevels = FamilyCommitmentLevelFlags.Committed | FamilyCommitmentLevelFlags.VeryCommitted,
                RequiresTravelWillingness = false,
                AcceptsDivorced = false,
                AcceptsWithChildren = false
            }
        });

        yield return ("aisha.bride@test.local", new Profile
        {
            Gender = Gender.Female,
            Name = "عائشة إبراهيم",
            DateOfBirth = new DateTime(2001, 9, 14),   // ~24
            HeightCm = 166,
            EducationLevel = EducationLevel.University,
            Occupation = "مدرّسة لغة عربية",
            MaritalStatus = MaritalStatus.Single,
            CurrentResidence = "القاهرة",
            PhoneNumber = "+201001234602",
            ReligiousCommitment = ReligiousCommitment.SeeksKnowledge,
            PersonalityDescription = "طالبة علم، أحب التدريس والأطفال، اجتماعية ومرتبة.",
            DressCode = DressCode.Abaya,
            AcceptsTravel = false,
            AllowsOnlineViewing = true,
            AcceptsDivorcedOrWithChildrenGroom = false,
            FamilyDetails = new FamilyDetails
            {
                FatherOccupation = "مهندس",
                MotherOccupationOrEducation = "طبيبة",
                SiblingsCountAndStudies = "أخ في كلية الهندسة",
                FamilyCommitmentLevel = FamilyCommitmentLevel.Committed
            },
            Requirements = new MatchRequirements
            {
                MinAge = 26, MaxAge = 34,
                MinHeightCm = 172, MaxHeightCm = 190,
                AcceptedEducationLevels = EducationLevelFlags.University | EducationLevelFlags.Postgraduate,
                AcceptedReligiousCommitments = ReligiousCommitmentFlags.SeeksKnowledge | ReligiousCommitmentFlags.MemorizesQuran,
                AcceptedMaritalStatuses = MaritalStatusFlags.Single,
                AcceptedResidences = { new RequirementResidence { City = "القاهرة" } },
                RequiresTravelWillingness = false,
                AcceptsDivorced = false,
                AcceptsWithChildren = false,
                OtherConditions = "أفضل البقاء في القاهرة قرب أهلي."
            }
        });

        yield return ("mariam.bride@test.local", new Profile
        {
            Gender = Gender.Female,
            Name = "مريم عادل",
            DateOfBirth = new DateTime(1996, 2, 3),    // ~30
            HeightCm = 160,
            EducationLevel = EducationLevel.Postgraduate,
            Occupation = "معيدة بكلية العلوم",
            MaritalStatus = MaritalStatus.Divorced,
            CurrentResidence = "الإسكندرية",
            PhoneNumber = "+201001234603",
            ReligiousCommitment = ReligiousCommitment.MemorizesQuran,
            PersonalityDescription = "مطلقة بدون أطفال، باحثة علمية، أقدّر الصراحة والوضوح.",
            DressCode = DressCode.Niqab,
            AcceptsTravel = true,
            AllowsOnlineViewing = false,
            AcceptsDivorcedOrWithChildrenGroom = true,
            FamilyDetails = new FamilyDetails
            {
                FatherOccupation = "صيدلي",
                MotherOccupationOrEducation = "ربة منزل",
                SiblingsCountAndStudies = "أختان جامعيتان",
                FamilyCommitmentLevel = FamilyCommitmentLevel.VeryCommitted
            },
            Requirements = new MatchRequirements
            {
                MinAge = 30, MaxAge = 40,
                AcceptedEducationLevels = EducationLevelFlags.University | EducationLevelFlags.Postgraduate,
                AcceptedReligiousCommitments = ReligiousCommitmentFlags.MemorizesQuran | ReligiousCommitmentFlags.SeeksKnowledge,
                AcceptedMaritalStatuses = MaritalStatusFlags.Single | MaritalStatusFlags.Divorced | MaritalStatusFlags.Widowed,
                RequiresTravelWillingness = false,
                AcceptsDivorced = true,
                AcceptsWithChildren = true
            }
        });

        yield return ("nour.bride@test.local", new Profile
        {
            Gender = Gender.Female,
            Name = "نور خالد",
            DateOfBirth = new DateTime(2003, 12, 1),   // ~22
            HeightCm = 158,
            EducationLevel = EducationLevel.Diploma,
            Occupation = "خياطة في مشغل",
            MaritalStatus = MaritalStatus.Single,
            CurrentResidence = "الجيزة",
            PhoneNumber = "+201001234604",
            ReligiousCommitment = ReligiousCommitment.ListensToLessons,
            PersonalityDescription = "بنت بلد جدعة، شاطرة في الشغل والبيت، وبحب العيلة.",
            DressCode = DressCode.Abaya,
            AcceptsTravel = false,
            AllowsOnlineViewing = true,
            AcceptsDivorcedOrWithChildrenGroom = false,
            FamilyDetails = new FamilyDetails
            {
                FatherOccupation = "سائق",
                MotherOccupationOrEducation = "ربة منزل",
                SiblingsCountAndStudies = "ثلاثة إخوة في مراحل دراسية مختلفة",
                FamilyCommitmentLevel = FamilyCommitmentLevel.SomewhatCommitted
            },
            Requirements = new MatchRequirements
            {
                MinAge = 24, MaxAge = 32,
                AcceptedEducationLevels = EducationLevelFlags.Secondary | EducationLevelFlags.Diploma | EducationLevelFlags.University,
                AcceptedReligiousCommitments = ReligiousCommitmentFlags.ListensToLessons | ReligiousCommitmentFlags.SeeksKnowledge,
                AcceptedMaritalStatuses = MaritalStatusFlags.Single,
                AcceptedResidences = { new RequirementResidence { City = "الجيزة" }, new RequirementResidence { City = "القاهرة" } },
                RequiresTravelWillingness = false,
                AcceptsDivorced = false,
                AcceptsWithChildren = false
            }
        });

        yield return ("salma.bride@test.local", new Profile
        {
            Gender = Gender.Female,
            Name = "سلمى فؤاد",
            DateOfBirth = new DateTime(1993, 6, 17),   // ~33
            HeightCm = 170,
            EducationLevel = EducationLevel.University,
            Occupation = "محاسبة",
            MaritalStatus = MaritalStatus.Widowed,
            CurrentResidence = "المنصورة",
            PhoneNumber = "+201001234605",
            ReligiousCommitment = ReligiousCommitment.SeeksKnowledge,
            PersonalityDescription = "أرملة ولديّ طفلة، صبورة ومتفائلة، وأتمنى بيتًا يجمعنا على خير.",
            DressCode = DressCode.Abaya,
            AcceptsTravel = false,
            AllowsOnlineViewing = false,
            AcceptsDivorcedOrWithChildrenGroom = true,
            FamilyDetails = new FamilyDetails
            {
                FatherOccupation = "مزارع",
                MotherOccupationOrEducation = "ربة منزل",
                SiblingsCountAndStudies = "أخ يعمل بالخارج وأخت متزوجة",
                FamilyCommitmentLevel = FamilyCommitmentLevel.Committed
            },
            Requirements = new MatchRequirements
            {
                MinAge = 32, MaxAge = 45,
                AcceptedEducationLevels = EducationLevelFlags.Diploma | EducationLevelFlags.University | EducationLevelFlags.Postgraduate,
                AcceptedReligiousCommitments = ReligiousCommitmentFlags.SeeksKnowledge | ReligiousCommitmentFlags.MemorizesQuran | ReligiousCommitmentFlags.ListensToLessons,
                AcceptedMaritalStatuses = MaritalStatusFlags.Single | MaritalStatusFlags.Divorced | MaritalStatusFlags.Widowed,
                AcceptedResidences = { new RequirementResidence { City = "المنصورة" } },
                RequiresTravelWillingness = false,
                AcceptsDivorced = true,
                AcceptsWithChildren = true,
                OtherConditions = "يقبل بابنتي ويعاملها معاملة طيبة."
            }
        });

        yield return ("hagar.bride@test.local", new Profile
        {
            Gender = Gender.Female,
            Name = "هاجر سامي",
            DateOfBirth = new DateTime(1999, 8, 30),   // ~26
            HeightCm = 168,
            EducationLevel = EducationLevel.University,
            Occupation = "مصممة جرافيك (عن بُعد)",
            MaritalStatus = MaritalStatus.Single,
            CurrentResidence = "القاهرة",
            PhoneNumber = "+201001234606",
            ReligiousCommitment = ReligiousCommitment.SeeksKnowledge,
            PersonalityDescription = "مبدعة وبسيطة، أحب الرسم والقراءة، ومستعدة للسفر مع زوجي.",
            DressCode = DressCode.Niqab,
            AcceptsTravel = true,
            AllowsOnlineViewing = false,
            AcceptsDivorcedOrWithChildrenGroom = true,
            FamilyDetails = new FamilyDetails
            {
                FatherOccupation = "تاجر أقمشة",
                MotherOccupationOrEducation = "خياطة",
                SiblingsCountAndStudies = "أخت في الجامعة وأخ في الإعدادية",
                FamilyCommitmentLevel = FamilyCommitmentLevel.Committed
            },
            Requirements = new MatchRequirements
            {
                MinAge = 27, MaxAge = 38,
                MinHeightCm = 168, MaxHeightCm = 188,
                AcceptedEducationLevels = EducationLevelFlags.University | EducationLevelFlags.Postgraduate | EducationLevelFlags.Diploma,
                AcceptedReligiousCommitments = ReligiousCommitmentFlags.SeeksKnowledge | ReligiousCommitmentFlags.MemorizesQuran,
                AcceptedMaritalStatuses = MaritalStatusFlags.Single | MaritalStatusFlags.Divorced | MaritalStatusFlags.Widowed,
                RequiresTravelWillingness = false,
                AcceptsDivorced = true,
                AcceptsWithChildren = true
            }
        });
    }
}
