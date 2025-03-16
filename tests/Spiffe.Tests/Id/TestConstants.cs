using Spiffe.Id;

namespace Spiffe.Tests.Id;

internal static class TestConstants
{
    internal static readonly TrustDomain Td = TrustDomain.FromString("trustdomain");

    internal static readonly ISet<char> LowerAlpha = new HashSet<char>
    {
        'a',
        'b',
        'c',
        'd',
        'e',
        'f',
        'g',
        'h',
        'i',
        'j',
        'k',
        'l',
        'm',
        'n',
        'o',
        'p',
        'q',
        'r',
        's',
        't',
        'u',
        'v',
        'w',
        'x',
        'y',
        'z'
    };

    internal static readonly ISet<char> UpperAlpha = new HashSet<char>
    {
        'A',
        'B',
        'C',
        'D',
        'E',
        'F',
        'G',
        'H',
        'I',
        'J',
        'K',
        'L',
        'M',
        'N',
        'O',
        'P',
        'Q',
        'R',
        'S',
        'T',
        'U',
        'V',
        'W',
        'X',
        'Y',
        'Z'
    };

    internal static readonly ISet<char> Numbers = new HashSet<char>
    {
        '0',
        '1',
        '2',
        '3',
        '4',
        '5',
        '6',
        '7',
        '8',
        '9'
    };

    internal static readonly ISet<char> Special = new HashSet<char> { '.', '-', '_' };

    internal static readonly ISet<char> TdChars = LowerAlpha
        .Concat(Numbers)
        .Concat(Special)
        .ToHashSet();

    internal static readonly ISet<char> PathChars = LowerAlpha
        .Concat(UpperAlpha)
        .Concat(Numbers)
        .Concat(Special)
        .ToHashSet();
}
