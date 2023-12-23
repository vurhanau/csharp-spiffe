namespace Spiffe.Tests;

using static TestConstants;

public class TestSpiffeId
{
    [Fact]
    public void TestFromString()
    {
        Assert.Throws<ArgumentException>(() => SpiffeId.FromString(null));
        Assert.Throws<ArgumentException>(() => SpiffeId.FromString(string.Empty));

        void assertOk(string idString, SpiffeTrustDomain expectedTd, string expectedPath)
        {
            SpiffeId id = SpiffeId.FromString(idString);
            AssertIdEqual(id, expectedTd, expectedPath);
        }

        void assertFail(string idString, string err)
        {
            var e = Assert.Throws<ArgumentException>(() => SpiffeId.FromString(idString));
            Assert.Contains(err, e.Message);
        }

        assertOk("spiffe://trustdomain", td, string.Empty);

        // Go all the way through 255, which ensures we reject UTF-8 appropriately
        for (int i = 0; i < 256; i++)
        {
            char c = (char)i;
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
        static void assertOk(string s)
        {
            SpiffeId expected = SpiffeId.FromString(s);
            SpiffeId actual = SpiffeId.FromUri(new Uri(s));
            Assert.Equal(expected, actual);
        }
        assertOk("spiffe://trustdomain");
        assertOk("spiffe://trustdomain/path");

        var e = Assert.Throws<ArgumentException>(() => SpiffeId.FromUri(new Uri("spiffe://trustdomain/path$")));
        Assert.Contains("Path segment characters are limited to letters, numbers, dots, dashes, and underscores", e.Message);
    }

    [Fact]
    public void TestFromSegments()
    {
        void assertOk(string[] segments, string expectedPath)
        {
            SpiffeId id = SpiffeId.FromSegments(td, segments);
            AssertIdEqual(id, td, expectedPath);
        }
        void assertFail(string[] segments, string expectedErr)
        {
            var e = Assert.Throws<ArgumentException>(() => SpiffeId.FromSegments(td, segments));
            Assert.Contains(expectedErr, e.Message);
        }
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

    [Fact]
    public void TestMemberOf()
    {
        SpiffeId spiffeId = SpiffeId.FromSegments(td, "path", "element");
        Assert.True(spiffeId.MemberOf(td));

        spiffeId = SpiffeId.FromSegments(td);
        Assert.True(spiffeId.MemberOf(td));

        SpiffeTrustDomain td2 = SpiffeTrustDomain.FromString("domain2.test");
        spiffeId = SpiffeId.FromSegments(td2, "path", "element");
        Assert.False(spiffeId.MemberOf(td));
    }

    [Fact]
    public void TestId()
    {
        SpiffeId spiffeId = SpiffeId.FromString("spiffe://trustdomain");
        Assert.Equal("spiffe://trustdomain", spiffeId.Id);

        spiffeId = SpiffeId.FromString("spiffe://trustdomain/path");
        Assert.Equal("spiffe://trustdomain/path", spiffeId.Id);
    }

    [Fact]
    public void TestUri()
    {
        static Uri asUri(string td, string path) => new UriBuilder()
        {
            Scheme = "spiffe",
            Host = td,
            Path = path,
        }.Uri;

        SpiffeId spiffeId = SpiffeId.FromSegments(td, "path", "element");
        Assert.Equal(asUri("trustdomain", "/path/element"), spiffeId.ToUri());

        spiffeId = SpiffeId.FromSegments(td);
        Assert.Equal(asUri("trustdomain", string.Empty), spiffeId.ToUri());
    }

    [Fact]
    public void TestReplacePath()
    {
        void assertOk(string startsWith, string replaceWith, string expectedPath)
        {
            SpiffeId spiffeId = SpiffeId.FromPath(td, startsWith).ReplacePath(replaceWith);
            AssertIdEqual(spiffeId, td, expectedPath);
        }

        void assertFail(string startsWith, string replaceWith, string expectedErr)
        {
            var e = Assert.Throws<ArgumentException>(() => SpiffeId.FromPath(td, startsWith).ReplacePath(replaceWith));
            Assert.Contains(expectedErr, e.Message);
        }

        assertOk("", "/foo", "/foo");
        assertOk("/path", "/foo", "/foo");

        assertFail("", "foo", "Path must have a leading slash");
        assertFail("/path", "/", "Path cannot have a trailing slash");
        assertFail("/path", "foo", "Path must have a leading slash");
    }

    [Fact]
    public void TestReplaceSegments()
    {
        void assertOk(string startsWith, string[] replaceWith, string expectedPath)
        {
            SpiffeId spiffeId = SpiffeId.FromPath(td, startsWith).ReplaceSegments(replaceWith);
            AssertIdEqual(spiffeId, td, expectedPath);
        }

        void assertFail(string startsWith, string[] replaceWith, string expectedErr)
        {
            var e = Assert.Throws<ArgumentException>(() => SpiffeId.FromPath(td, startsWith).ReplaceSegments(replaceWith));
            Assert.Contains(expectedErr, e.Message);
        }
        
        assertOk("", ["foo"], "/foo");
        assertOk("/path", ["foo"], "/foo");

        assertFail("", [""], "Path cannot contain empty segments");
       	assertFail("", ["/foo"], "Path segment characters are limited to letters, numbers, dots, dashes, and underscores");
    }

    [Fact]
    public void TestAppendPath()
    {
        void assertOk(string startsWith, string replaceWith, string expectedPath)
        {
            SpiffeId spiffeId = SpiffeId.FromPath(td, startsWith).AppendPath(replaceWith);
            AssertIdEqual(spiffeId, td, expectedPath);
        }

        void assertFail(string startsWith, string replaceWith, string expectedErr)
        {
            var e = Assert.Throws<ArgumentException>(() => SpiffeId.FromPath(td, startsWith).AppendPath(replaceWith));
            Assert.Contains(expectedErr, e.Message);
        }

        assertOk("", "/foo", "/foo");
    	assertOk("/path", "/foo", "/path/foo");

        assertFail("", "foo", "Path must have a leading slash");
        assertFail("/path", "/", "Path cannot have a trailing slash");
        assertFail("/path", "foo", "Path must have a leading slash");
    }

    [Fact]
    public void TestAppendSegments()
    {
        void assertOk(string startsWith, string[] replaceWith, string expectedPath)
        {
            SpiffeId spiffeId = SpiffeId.FromPath(td, startsWith).AppendSegments(replaceWith);
            AssertIdEqual(spiffeId, td, expectedPath);
        }

        void assertFail(string startsWith, string[] replaceWith, string expectedErr)
        {
            var e = Assert.Throws<ArgumentException>(() => SpiffeId.FromPath(td, startsWith).AppendSegments(replaceWith));
            Assert.Contains(expectedErr, e.Message);
        }

        assertOk("", ["foo"], "/foo");
        assertOk("/path", ["foo"], "/path/foo");

        assertFail("", [""], "Path cannot contain empty segments");
        assertFail("", ["/foo"], "Path segment characters are limited to letters, numbers, dots, dashes, and underscores");
    }

    private static void AssertIdEqual(SpiffeId spiffeId, SpiffeTrustDomain expectedTd, string expectedPath)
    {
        Assert.Equal(expectedTd, spiffeId.TrustDomain);
        Assert.Equal(expectedPath, spiffeId.Path);
        Assert.Equal(expectedTd.SpiffeId.Id + expectedPath, spiffeId.Id);
        // Root uri has trailing '/': spiffe://example.org/
        // Uri with has no trailing '/': spiffe://example.org/abc
        Assert.Equal(spiffeId.ToUri().ToString().TrimEnd('/'), spiffeId.Id);
    }
}
