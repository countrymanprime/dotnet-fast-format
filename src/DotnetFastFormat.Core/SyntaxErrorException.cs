namespace DotnetFastFormat.Core;

/// <summary>The input has syntax errors, so it is not formatted.</summary>
public sealed class SyntaxErrorException : FormatterException
{
    /// <summary>Initializes a new instance of the <see cref="SyntaxErrorException"/> class.</summary>
    public SyntaxErrorException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="SyntaxErrorException"/> class.</summary>
    /// <param name="message">The first diagnostic, with its position.</param>
    public SyntaxErrorException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="SyntaxErrorException"/> class.</summary>
    /// <param name="message">The first diagnostic, with its position.</param>
    /// <param name="innerException">The underlying failure.</param>
    public SyntaxErrorException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
