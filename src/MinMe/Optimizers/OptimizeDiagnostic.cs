namespace MinMe.Optimizers;

/// <summary>
/// Object containing all diagnostic information related to document optimization.
/// </summary>
public class OptimizeDiagnostic
{
    /// <summary>
    /// List of all errors.
    /// </summary>
    public IList<OptimizeError> Errors { get; set; } = new List<OptimizeError>();
}