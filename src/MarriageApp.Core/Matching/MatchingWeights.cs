namespace MarriageApp.Core.Matching;

/// <summary>
/// Tunable weights and behavior for the matching algorithm, bound from the
/// "Matching" section of appsettings.json. Weights are the maximum points each
/// criterion can contribute; they should sum to 100 for a clean percentage but
/// the algorithm normalizes regardless.
/// </summary>
public class MatchingWeights
{
    public const string SectionName = "Matching";

    public double Age { get; set; } = 20;
    public double Height { get; set; } = 15;
    public double ReligiousCommitment { get; set; } = 20;
    public double Education { get; set; } = 10;
    public double MaritalStatus { get; set; } = 10;
    public double DressCode { get; set; } = 10;
    public double Residence { get; set; } = 10;
    public double FamilyCommitment { get; set; } = 5;

    /// <summary>
    /// Multiplier applied to the total score when a boolean hard constraint
    /// (travel / accepts-divorced / accepts-children) is violated. 0.15 = heavy
    /// penalty but still rankable; set to 0 to zero-out, or flip a constraint to
    /// a strict filter via <see cref="TreatConstraintsAsStrictFilter"/>.
    /// </summary>
    public double HardConstraintPenaltyFactor { get; set; } = 0.15;

    /// <summary>When true, a violated boolean constraint excludes the candidate entirely.</summary>
    public bool TreatConstraintsAsStrictFilter { get; set; } = false;

    /// <summary>
    /// How many cm outside a requested height band still earns partial (linearly
    /// decaying) credit. e.g. with band 165–175 and falloff 10, a 180cm candidate
    /// earns 50% of the height weight; beyond 185cm earns zero.
    /// </summary>
    public int HeightFalloffCm { get; set; } = 10;

    /// <summary>Same idea as <see cref="HeightFalloffCm"/> but for the age band, in years.</summary>
    public int AgeFalloffYears { get; set; } = 3;

    /// <summary>Partial credit (0..1) for an ordered enum (education/commitment) one step off the accepted set.</summary>
    public double AdjacentEnumCredit { get; set; } = 0.5;
}
