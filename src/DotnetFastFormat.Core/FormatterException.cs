namespace DotnetFastFormat.Core;

/// <summary>The formatter refused to format a file; the caller must leave the file untouched.</summary>
public class FormatterException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="FormatterException"/> class.</summary>
    public FormatterException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FormatterException"/> class.</summary>
    /// <param name="message">What went wrong.</param>
    public FormatterException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FormatterException"/> class.</summary>
    /// <param name="message">What went wrong.</param>
    /// <param name="innerException">The underlying failure.</param>
    public FormatterException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
