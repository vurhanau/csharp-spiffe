using System.Data.Common;

namespace Spiffe.Tests;

public class TestSpiffeId
{
    private static readonly SpiffeTrustDomain td = SpiffeTrustDomain.FromString("trustdomain");

    private static readonly ISet<char> lowerAlpha = new HashSet<char>()
    {
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
        'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
    };

    private static readonly ISet<char> upperAlpha = new HashSet<char>()
    {
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
        'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
    };

    private static readonly ISet<char> numbers = new HashSet<char>()
    {
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
    };

    private static readonly ISet<char> special = new HashSet<char>()
    {
        '.', '-', '_',
    };

    private static readonly ISet<char> tdChars = lowerAlpha
                                                        .Concat(numbers)
                                                        .Concat(special)
                                                        .ToHashSet();
    private static readonly ISet<char> pathChars = lowerAlpha
                                                        .Concat(upperAlpha)
                                                        .Concat(numbers)
                                                        .Concat(special)
                                                        .ToHashSet();

    [Fact]
    public void TestFromString()
    {
        Assert.Throws<ArgumentException>(() => SpiffeId.FromString(null));
        Assert.Throws<ArgumentException>(() => SpiffeId.FromString(string.Empty));
        
        AssertOk("spiffe://trustdomain", td, string.Empty);
        
        // Go all the way through 255, which ensures we reject UTF-8 appropriately
        for (int i = 0; i < 256; i++)
        {
            char c = (char) i;
            if (c == '/')
            {
                // Don't test / since it is the delimeter between path segments
                continue;
            }

            if (tdChars.Contains(c))
            {
                // Allow good trustdomain char
                AssertOk($"spiffe://trustdomain{c}/path", SpiffeTrustDomain.FromString($"trustdomain{c}"), "/path");
            }
            else
            {
                // Reject bad trustdomain char
				AssertFail($"spiffe://trustdomain{c}/path", "trust domain characters are limited to lowercase letters, numbers, dots, dashes, and underscores");
            }

            if (pathChars.Contains(c))
            {
                // Allow good path char
                AssertOk($"spiffe://trustdomain/path{c}", td, $"/path{c}");
            }
            else
            {
                // Reject bad path char
                AssertFail($"spiffe://trustdomain/path{c}", "path segment characters are limited to letters, numbers, dots, dashes, and underscores");
            }
        }

        // Reject bad scheme
        AssertFail("s", "scheme is missing or invalid");
        AssertFail("spiffe:/", "scheme is missing or invalid");
        AssertFail("Spiffe://", "scheme is missing or invalid");

        // Reject empty segments
        AssertFail("spiffe://trustdomain/", "path cannot have a trailing slash");
		AssertFail("spiffe://trustdomain//", "path cannot contain empty segments");
		AssertFail("spiffe://trustdomain//path", "path cannot contain empty segments");
		AssertFail("spiffe://trustdomain/path/", "path cannot have a trailing slash");

        // Reject dot segments
        AssertFail("spiffe://trustdomain/.", "path cannot contain dot segments");
		AssertFail("spiffe://trustdomain/./path", "path cannot contain dot segments");
		AssertFail("spiffe://trustdomain/path/./other", "path cannot contain dot segments");
		AssertFail("spiffe://trustdomain/path/..", "path cannot contain dot segments");
		AssertFail("spiffe://trustdomain/..", "path cannot contain dot segments");
		AssertFail("spiffe://trustdomain/../path", "path cannot contain dot segments");
		AssertFail("spiffe://trustdomain/path/../other", "path cannot contain dot segments");
		// The following are ok since the the segments, while containing dots
		// are not all dots (or are more than two dots)
		AssertOk("spiffe://trustdomain/.path", td, "/.path");
		AssertOk("spiffe://trustdomain/..path", td, "/..path");
		AssertOk("spiffe://trustdomain/...", td, "/...");

        // Reject percent encoding
        // percent-encoded unicode
		AssertFail("spiffe://%F0%9F%A4%AF/path", "trust domain characters are limited to lowercase letters, numbers, dots, dashes, and underscores");
		AssertFail("spiffe://trustdomain/%F0%9F%A4%AF", "path segment characters are limited to letters, numbers, dots, dashes, and underscores");
		// percent-encoded ascii
		AssertFail("spiffe://%62%61%64/path", "trust domain characters are limited to lowercase letters, numbers, dots, dashes, and underscores");
		AssertFail("spiffe://trustdomain/%62%61%64", "path segment characters are limited to letters, numbers, dots, dashes, and underscores");
    }

    private static void AssertOk(string idString, SpiffeTrustDomain expectedTd, string expectedPath)
    {
        SpiffeId id = SpiffeId.FromString(idString);
        AssertIdEqual(id, expectedTd, expectedPath);
        id = SpiffeId.FromStringf("%s", id);
        AssertIdEqual(id, expectedTd, expectedPath);
    }

    private static void AssertFail(string idString, string err)
    {
        var e = Assert.Throws<ArgumentException>(() => SpiffeId.FromString(idString));
        Assert.Contains(err, e.Message);
        e = Assert.Throws<ArgumentException>(() => SpiffeId.FromStringf("%s", idString));
        Assert.Contains(err, e.Message);
    }

    private static void AssertIdEqual(SpiffeId id, SpiffeTrustDomain expectedTd, string expectedPath)
    {
        Assert.Equal(expectedTd, id.TrustDomain);
        Assert.Equal(expectedPath, id.Path);
        Assert.Equal(expectedTd.String + expectedPath, id.String);
        Assert.Equal(id.ToUri().ToString(), id.String);
    }
}
