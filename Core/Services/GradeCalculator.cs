namespace CompSci.Core.Services;

/// <summary>
/// Single source of truth for internship-evaluation scoring math, so the submit and
/// report-grade code paths never compute it two different ways.
/// </summary>
public static class GradeCalculator
{
    /// <summary>The 13 fixed rating criteria sum to at most 52; scaled to a 70-point evaluation score.</summary>
    public static decimal EvaluationScoreFromRawTotal(int rawRatingTotal)
    {
        return Math.Round(rawRatingTotal / 52m * 70m, 2);
    }

    /// <summary>75-100 A, 60-74 B, 50-59 C, 40-49 D, 30-39 E, 0-29 F.</summary>
    public static string GradeFromTotal(decimal totalScore)
    {
        return totalScore switch
        {
            >= 75 => "A",
            >= 60 => "B",
            >= 50 => "C",
            >= 40 => "D",
            >= 30 => "E",
            _ => "F"
        };
    }
}
