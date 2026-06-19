using MarriageApp.Core.Entities;
using MarriageApp.Core.Enums;
using MarriageApp.Web.ViewModels;

namespace MarriageApp.Web.Mapping;

/// <summary>
/// Maps between <see cref="ProfileFormViewModel"/> and the domain entities, including the
/// conversion of checkbox-group selections (List of enum) into [Flags] bitmasks and back.
/// </summary>
public static class ProfileMapper
{
    // ---- List<enum> -> [Flags] mask (OR each value's bit) ----
    private static TFlags Combine<TEnum, TFlags>(IEnumerable<TEnum> values, Func<TEnum, TFlags> toFlag)
        where TFlags : struct, Enum
    {
        int mask = 0;
        foreach (var v in values) mask |= Convert.ToInt32(toFlag(v));
        return (TFlags)Enum.ToObject(typeof(TFlags), mask);
    }

    // ---- [Flags] mask -> List<enum> (test each single-value enum's bit) ----
    private static List<TEnum> Expand<TEnum, TFlags>(TFlags mask, Func<TEnum, TFlags> toFlag)
        where TEnum : struct, Enum
        where TFlags : struct, Enum
    {
        int m = Convert.ToInt32(mask);
        var result = new List<TEnum>();
        foreach (var v in Enum.GetValues<TEnum>())
        {
            if ((m & Convert.ToInt32(toFlag(v))) != 0) result.Add(v);
        }
        return result;
    }

    /// <summary>Applies form values onto a (new or existing) profile graph.</summary>
    public static void Apply(ProfileFormViewModel vm, Profile profile, DateTime now)
    {
        profile.Gender = vm.Gender;
        profile.Name = vm.Name;
        profile.DateOfBirth = vm.DateOfBirth;
        profile.RecalculateAge(now);
        profile.HeightCm = vm.HeightCm;
        profile.EducationLevel = vm.EducationLevel;
        profile.Occupation = vm.Occupation;
        profile.MaritalStatus = vm.MaritalStatus;
        profile.CurrentResidence = vm.CurrentResidence;
        profile.PhoneNumber = vm.PhoneNumber;
        profile.ReligiousCommitment = vm.ReligiousCommitment;
        profile.PersonalityDescription = vm.PersonalityDescription;
        profile.HasHealthCondition = vm.HasHealthCondition;
        profile.HealthConditionDescription = vm.HasHealthCondition ? vm.HealthConditionDescription : null;

        if (vm.Gender == Gender.Female)
        {
            profile.DressCode = vm.DressCode;
            profile.AcceptsTravel = vm.AcceptsTravel;
            profile.AllowsOnlineViewing = vm.AllowsOnlineViewing;
            profile.AcceptsDivorcedOrWithChildrenGroom = vm.AcceptsDivorcedOrWithChildrenGroom;
            // null out male-only fields
            profile.WeightKg = null; profile.FutureMaritalResidence = null;
            profile.IntendsToTravel = null; profile.AcceptsDivorcedOrWithChildrenBride = null;
        }
        else
        {
            profile.WeightKg = vm.WeightKg;
            profile.FutureMaritalResidence = vm.FutureMaritalResidence;
            profile.IntendsToTravel = vm.IntendsToTravel;
            profile.AcceptsDivorcedOrWithChildrenBride = vm.AcceptsDivorcedOrWithChildrenBride;
            profile.DressCode = null; profile.AcceptsTravel = null;
            profile.AllowsOnlineViewing = null; profile.AcceptsDivorcedOrWithChildrenGroom = null;
        }

        // ---- Family ----
        profile.FamilyDetails ??= new FamilyDetails();
        profile.FamilyDetails.FatherOccupation = vm.FatherOccupation;
        profile.FamilyDetails.MotherOccupationOrEducation = vm.MotherOccupationOrEducation;
        profile.FamilyDetails.SiblingsCountAndStudies = vm.SiblingsCountAndStudies;
        profile.FamilyDetails.FamilyCommitmentLevel = vm.FamilyCommitmentLevel;
        profile.FamilyDetails.MotherOrSisterPhone = vm.MotherOrSisterPhone;

        // ---- Requirements ----
        profile.Requirements ??= new MatchRequirements();
        var req = profile.Requirements;
        req.MinAge = vm.MinAge; req.MaxAge = vm.MaxAge;
        req.MinHeightCm = vm.MinHeightCm; req.MaxHeightCm = vm.MaxHeightCm;
        req.AcceptedEducationLevels = Combine<EducationLevel, EducationLevelFlags>(vm.AcceptedEducationLevels, e => e.ToFlag());
        req.AcceptedReligiousCommitments = Combine<ReligiousCommitment, ReligiousCommitmentFlags>(vm.AcceptedReligiousCommitments, e => e.ToFlag());
        req.AcceptedDressCodes = Combine<DressCode, DressCodeFlags>(vm.AcceptedDressCodes, e => e.ToFlag());
        req.AcceptedMaritalStatuses = Combine<MaritalStatus, MaritalStatusFlags>(vm.AcceptedMaritalStatuses, e => e.ToFlag());
        req.AcceptedFamilyCommitmentLevels = Combine<FamilyCommitmentLevel, FamilyCommitmentLevelFlags>(vm.AcceptedFamilyCommitmentLevels, e => e.ToFlag());
        req.RequiresTravelWillingness = vm.RequiresTravelWillingness;
        // The bride's "accept a divorced groom" form answer is the canonical AcceptsDivorced preference.
        req.AcceptsDivorced = vm.Gender == Gender.Female
            ? (vm.AcceptsDivorcedOrWithChildrenGroom ?? vm.AcceptsDivorced)
            : (vm.AcceptsDivorcedOrWithChildrenBride ?? vm.AcceptsDivorced);
        req.AcceptsWithChildren = vm.AcceptsWithChildren;
        req.OtherConditions = vm.OtherConditions;

        // Replace accepted residence cities from the CSV input.
        req.AcceptedResidences.Clear();
        foreach (var city in SplitCsv(vm.AcceptedResidencesCsv))
            req.AcceptedResidences.Add(new RequirementResidence { City = city });

        profile.UpdatedAt = now;
    }

    /// <summary>Builds an edit view model from an existing profile graph.</summary>
    public static ProfileFormViewModel ToViewModel(Profile p)
    {
        var req = p.Requirements;
        var vm = new ProfileFormViewModel
        {
            ProfileId = p.Id,
            Gender = p.Gender,
            Name = p.Name,
            DateOfBirth = p.DateOfBirth,
            HeightCm = p.HeightCm,
            EducationLevel = p.EducationLevel,
            Occupation = p.Occupation,
            MaritalStatus = p.MaritalStatus,
            CurrentResidence = p.CurrentResidence,
            PhoneNumber = p.PhoneNumber,
            ReligiousCommitment = p.ReligiousCommitment,
            PersonalityDescription = p.PersonalityDescription,
            HasHealthCondition = p.HasHealthCondition,
            HealthConditionDescription = p.HealthConditionDescription,
            DressCode = p.DressCode,
            AcceptsTravel = p.AcceptsTravel,
            AllowsOnlineViewing = p.AllowsOnlineViewing,
            AcceptsDivorcedOrWithChildrenGroom = p.AcceptsDivorcedOrWithChildrenGroom,
            WeightKg = p.WeightKg,
            FutureMaritalResidence = p.FutureMaritalResidence,
            IntendsToTravel = p.IntendsToTravel,
            AcceptsDivorcedOrWithChildrenBride = p.AcceptsDivorcedOrWithChildrenBride,
            FatherOccupation = p.FamilyDetails?.FatherOccupation,
            MotherOccupationOrEducation = p.FamilyDetails?.MotherOccupationOrEducation,
            SiblingsCountAndStudies = p.FamilyDetails?.SiblingsCountAndStudies,
            FamilyCommitmentLevel = p.FamilyDetails?.FamilyCommitmentLevel ?? FamilyCommitmentLevel.Committed,
            MotherOrSisterPhone = p.FamilyDetails?.MotherOrSisterPhone,
            MinAge = req?.MinAge,
            MaxAge = req?.MaxAge,
            MinHeightCm = req?.MinHeightCm,
            MaxHeightCm = req?.MaxHeightCm,
            RequiresTravelWillingness = req?.RequiresTravelWillingness ?? false,
            AcceptsDivorced = req?.AcceptsDivorced ?? false,
            AcceptsWithChildren = req?.AcceptsWithChildren ?? false,
            OtherConditions = req?.OtherConditions,
            AcceptedResidencesCsv = req is null ? null : string.Join("، ", req.AcceptedResidences.Select(r => r.City))
        };

        if (req is not null)
        {
            vm.AcceptedEducationLevels = Expand<EducationLevel, EducationLevelFlags>(req.AcceptedEducationLevels, e => e.ToFlag());
            vm.AcceptedReligiousCommitments = Expand<ReligiousCommitment, ReligiousCommitmentFlags>(req.AcceptedReligiousCommitments, e => e.ToFlag());
            vm.AcceptedDressCodes = Expand<DressCode, DressCodeFlags>(req.AcceptedDressCodes, e => e.ToFlag());
            vm.AcceptedMaritalStatuses = Expand<MaritalStatus, MaritalStatusFlags>(req.AcceptedMaritalStatuses, e => e.ToFlag());
            vm.AcceptedFamilyCommitmentLevels = Expand<FamilyCommitmentLevel, FamilyCommitmentLevelFlags>(req.AcceptedFamilyCommitmentLevels, e => e.ToFlag());
        }
        return vm;
    }

    private static IEnumerable<string> SplitCsv(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? Enumerable.Empty<string>()
            : csv.Split(new[] { ',', '،', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                 .Distinct();
}
