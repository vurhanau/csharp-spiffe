using Xunit.Sdk;

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
        
        Action<string, SpiffeTrustDomain, string> assertOk = (idString, expectedTd, expectedPath) =>
        {
            SpiffeId id = SpiffeId.FromString(idString);
            AssertIdEqual(id, expectedTd, expectedPath);
            id = SpiffeId.FromStringf("{0}", idString);
            AssertIdEqual(id, expectedTd, expectedPath);
        };

        Action<string, string> assertFail = (idString, err) =>
        {
            var e = Assert.Throws<ArgumentException>(() => SpiffeId.FromString(idString));
            Assert.Contains(err, e.Message);
            e = Assert.Throws<ArgumentException>(() => SpiffeId.FromStringf("{0}", idString));
            Assert.Contains(err, e.Message);
        };

        assertOk("spiffe://trustdomain", td, string.Empty);
        
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
                assertOk($"spiffe://trustdomain{c}/path", SpiffeTrustDomain.FromString($"trustdomain{c}"), "/path");
            }
            else
            {
                // Reject bad trustdomain char
				assertFail($"spiffe://trustdomain{c}/path", "Trust domain characters are limited to lowercase letters, numbers, dots, dashes, and underscores");
            }

            if (pathChars.Contains(c))
            {
                // Allow good path char
                assertOk($"spiffe://trustdomain/path{c}", td, $"/path{c}");
            }
            else
            {
                // Reject bad path char
                assertFail($"spiffe://trustdomain/path{c}", "Path segment characters are limited to letters, numbers, dots, dashes, and underscores");
            }
        }

        // Reject bad scheme
        assertFail("s", "Scheme is missing or invalid");
        assertFail("spiffe:/", "Scheme is missing or invalid");
        assertFail("Spiffe://", "Scheme is missing or invalid");

        // Reject empty segments
        assertFail("spiffe://trustdomain/", "Path cannot have a trailing slash");
		assertFail("spiffe://trustdomain//", "Path cannot contain empty segments");
		assertFail("spiffe://trustdomain//path", "Path cannot contain empty segments");
		assertFail("spiffe://trustdomain/path/", "Path cannot have a trailing slash");

        // Reject dot segments
        assertFail("spiffe://trustdomain/.", "Path cannot contain dot segments");
		assertFail("spiffe://trustdomain/./path", "Path cannot contain dot segments");
		assertFail("spiffe://trustdomain/path/./other", "Path cannot contain dot segments");
		assertFail("spiffe://trustdomain/path/..", "Path cannot contain dot segments");
		assertFail("spiffe://trustdomain/..", "Path cannot contain dot segments");
		assertFail("spiffe://trustdomain/../path", "Path cannot contain dot segments");
		assertFail("spiffe://trustdomain/path/../other", "Path cannot contain dot segments");
		// The following are ok since the the segments, while containing dots
		// are not all dots (or are more than two dots)
		assertOk("spiffe://trustdomain/.path", td, "/.path");
		assertOk("spiffe://trustdomain/..path", td, "/..path");
		assertOk("spiffe://trustdomain/...", td, "/...");

        // Reject percent encoding
        // percent-encoded unicode
		assertFail("spiffe://%F0%9F%A4%AF/path", "Trust domain characters are limited to lowercase letters, numbers, dots, dashes, and underscores");
		assertFail("spiffe://trustdomain/%F0%9F%A4%AF", "Path segment characters are limited to letters, numbers, dots, dashes, and underscores");
		// percent-encoded ascii
		assertFail("spiffe://%62%61%64/path", "Trust domain characters are limited to lowercase letters, numbers, dots, dashes, and underscores");
		assertFail("spiffe://trustdomain/%62%61%64", "Path segment characters are limited to letters, numbers, dots, dashes, and underscores");
    }

    [Fact]
    public void TestFromUri()
    {
        Action<string> assertOk = s =>
        {
            SpiffeId expected = SpiffeId.FromString(s);
            SpiffeId actual = SpiffeId.FromUri(new Uri(s));  
            Assert.Equal(expected, actual);
        };
        assertOk("spiffe://trustdomain");
        assertOk("spiffe://trustdomain/path");

        var e = Assert.Throws<ArgumentException>(() => SpiffeId.FromUri(new Uri("spiffe://trustdomain/path$")));
        Assert.Contains("Path segment characters are limited to letters, numbers, dots, dashes, and underscores", e.Message);
    }

    [Fact]
    public void TestFromSegments()
    {
        Action<string[], string> assertOk = (segments, expectedPath) =>
        {
            SpiffeId id = SpiffeId.FromSegments(td, segments);
            AssertIdEqual(id, td, expectedPath);
        };
        Action<string[], string> assertFail = (segments, expectedErr) =>
        {
            var e = Assert.Throws<ArgumentException>(() => SpiffeId.FromSegments(td, segments));
            Assert.Contains(expectedErr, e.Message);
        };
        Assert.Throws<ArgumentNullException>(() => SpiffeId.FromSegments(null, "foo"));
        Assert.Throws<ArgumentNullException>(() => SpiffeId.FromSegments(td, null));
        assertOk([], string.Empty);
        assertOk(["foo"], "/foo");
        assertOk(["foo", "bar"], "/foo/bar");

        assertFail([string.Empty], "Path cannot contain empty segments");
        assertFail(["/"], "Path segment characters are limited to letters, numbers, dots, dashes, and underscores");
        assertFail(["/foo"], "Path segment characters are limited to letters, numbers, dots, dashes, and underscores");
        assertFail(["$"], "Path segment characters are limited to letters, numbers, dots, dashes, and underscores");
    }

    private static void AssertIdEqual(SpiffeId id, SpiffeTrustDomain expectedTd, string expectedPath)
    {
        Assert.Equal(expectedTd, id.TrustDomain);
        Assert.Equal(expectedPath, id.Path);
        Assert.Equal(expectedTd.IdString + expectedPath, id.String);
        // Root uri has trailing '/': spiffe://example.org/
        // Uri with has no trailing '/': spiffe://example.org/abc
        Assert.Equal(id.ToUri().ToString().TrimEnd('/'), id.String);
    }
}
