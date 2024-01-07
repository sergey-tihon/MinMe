namespace MinMe.Optimizers;

/// <summary>
/// Error related to the document Optimization.
/// </summary>
public class OptimizeError
{
    /// <summary>
    /// Initializes the <see cref="OptimizeError"/> class.
    /// </summary>
    public OptimizeError(string pointer, string message)
        => (Pointer,Message) = (pointer, message);

    /// <summary>
    /// Message explaining the error.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Pointer to the location of the error.
    /// </summary>
    public string Pointer { get; set; }

    /// <summary>
    /// Gets the string representation of <see cref="OptimizeError"/>.
    /// </summary>
    public override string ToString()
        => "[" + Pointer + "] " + Message;
}