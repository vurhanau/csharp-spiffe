namespace Spiffe.Tests;

internal static class TestConstants
{
    internal static readonly SpiffeTrustDomain td = SpiffeTrustDomain.FromString("trustdomain");

    internal static readonly ISet<char> lowerAlpha = new HashSet<char>()
    {
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
        'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
    };

    internal static readonly ISet<char> upperAlpha = new HashSet<char>()
    {
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
        'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
    };

    internal static readonly ISet<char> numbers = new HashSet<char>()
    {
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
    };

    internal static readonly ISet<char> special = new HashSet<char>()
    {
        '.', '-', '_',
    };

    internal static readonly ISet<char> tdChars = lowerAlpha
                                                        .Concat(numbers)
                                                        .Concat(special)
                                                        .ToHashSet();
    internal static readonly ISet<char> pathChars = lowerAlpha
                                                        .Concat(upperAlpha)
                                                        .Concat(numbers)
                                                        .Concat(special)
                                                        .ToHashSet();
}