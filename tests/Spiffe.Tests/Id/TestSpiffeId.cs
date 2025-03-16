using FluentAssertions;
using Spiffe.Id;
using static Spiffe.Tests.Id.TestConstants;

namespace Spiffe.Tests.Id;

public class TestSpiffeId
{
    [Fact]
    public void TestFromString()
    {
        Assert.Throws<ArgumentException>(() => SpiffeId.FromString(string.Empty));

        void AssertOk(string idString, TrustDomain expectedTd, string expectedPath)
        {
            SpiffeId id = SpiffeId.FromString(idString);
            AssertIdEqual(id, expectedTd, expectedPath);
        }

        void AssertFail(string idString, string err)
        {
            ArgumentException e = Assert.Throws<ArgumentException>(() => SpiffeId.FromString(idString));
            Assert.Contains(err, e.Message);
        }

        AssertOk("spiffe://trustdomain", Td, string.Empty);

        // Go all the way through 255, which ensures we reject UTF-8 appropriately
        for (int i = 0; i < 256; i++)
        {
            char c = (char)i;
            if (c == '/')
            {
                // Don't test / since it is the delimeter between path segments
                continue;
            }

            if (TdChars.Contains(c))
            {
                // Allow good trustdomain char
                AssertOk($"spiffe://trustdomain{c}/path", TrustDomain.FromString($"trustdomain{c}"), "/path");
            }
            else
            {
                // Reject bad trustdomain char
                AssertFail($"spiffe://trustdomain{c}/path",
                    "Trust domain characters are limited to lowercase letters, numbers, dots, dashes, and underscores");
            }

            if (PathChars.Contains(c))
            {
                // Allow good path char
                AssertOk($"spiffe://trustdomain/path{c}", Td, $"/path{c}");
            }
            else
            {
                // Reject bad path char
                AssertFail($"spiffe://trustdomain/path{c}",
                    "Path segment characters are limited to letters, numbers, dots, dashes, and underscores");
            }
        }

        // Reject bad scheme
        AssertFail("s", "Scheme is missing or invalid");
        AssertFail("spiffe:/", "Scheme is missing or invalid");
        AssertFail("Spiffe://", "Scheme is missing or invalid");

        // Reject empty segments
        AssertFail("spiffe://trustdomain/", "Path cannot have a trailing slash");
        AssertFail("spiffe://trustdomain//", "Path cannot contain empty segments");
        AssertFail("spiffe://trustdomain//path", "Path cannot contain empty segments");
        AssertFail("spiffe://trustdomain/path/", "Path cannot have a trailing slash");

        // Reject dot segments
        AssertFail("spiffe://trustdomain/.", "Path cannot contain dot segments");
        AssertFail("spiffe://trustdomain/./path", "Path cannot contain dot segments");
        AssertFail("spiffe://trustdomain/path/./other", "Path cannot contain dot segments");
        AssertFail("spiffe://trustdomain/path/..", "Path cannot contain dot segments");
        AssertFail("spiffe://trustdomain/..", "Path cannot contain dot segments");
        AssertFail("spiffe://trustdomain/../path", "Path cannot contain dot segments");
        AssertFail("spiffe://trustdomain/path/../other", "Path cannot contain dot segments");

        // The following are ok since the the segments, while containing dots
        // are not all dots (or are more than two dots)
        AssertOk("spiffe://trustdomain/.path", Td, "/.path");
        AssertOk("spiffe://trustdomain/..path", Td, "/..path");
        AssertOk("spiffe://trustdomain/...", Td, "/...");

        // Reject percent encoding
        // percent-encoded unicode
        AssertFail("spiffe://%F0%9F%A4%AF/path",
            "Trust domain characters are limited to lowercase letters, numbers, dots, dashes, and underscores");
        AssertFail("spiffe://trustdomain/%F0%9F%A4%AF",
            "Path segment characters are limited to letters, numbers, dots, dashes, and underscores");

        // percent-encoded ascii
        AssertFail("spiffe://%62%61%64/path",
            "Trust domain characters are limited to lowercase letters, numbers, dots, dashes, and underscores");
        AssertFail("spiffe://trustdomain/%62%61%64",
            "Path segment characters are limited to letters, numbers, dots, dashes, and underscores");
    }

    [Fact]
    public void TestFromUri()
    {
        static void AssertOk(string s)
        {
            SpiffeId expected = SpiffeId.FromString(s);
            SpiffeId actual = SpiffeId.FromUri(new Uri(s));
            Assert.Equal(expected, actual);
        }

        AssertOk("spiffe://trustdomain");
        AssertOk("spiffe://trustdomain/path");

        ArgumentException e =
            Assert.Throws<ArgumentException>(() => SpiffeId.FromUri(new Uri("spiffe://trustdomain/path$")));
        Assert.Contains("Path segment characters are limited to letters, numbers, dots, dashes, and underscores",
            e.Message);
    }

    [Fact]
    public void TestFromSegments()
    {
        void AssertOk(string[] segments, string expectedPath)
        {
            SpiffeId id = SpiffeId.FromSegments(Td, segments);
            AssertIdEqual(id, Td, expectedPath);
        }

        void AssertFail(string[] segments, string expectedErr)
        {
            ArgumentException e = Assert.Throws<ArgumentException>(() => SpiffeId.FromSegments(Td, segments));
            Assert.Contains(expectedErr, e.Message);
        }

        AssertOk([], string.Empty);
        AssertOk(["foo"], "/foo");
        AssertOk(["foo", "bar"], "/foo/bar");

        AssertFail([string.Empty], "Path cannot contain empty segments");
        AssertFail(["/"], "Path segment characters are limited to letters, numbers, dots, dashes, and underscores");
        AssertFail(["/foo"], "Path segment characters are limited to letters, numbers, dots, dashes, and underscores");
        AssertFail(["$"], "Path segment characters are limited to letters, numbers, dots, dashes, and underscores");
    }

    [Fact]
    public void TestMemberOf()
    {
        SpiffeId spiffeId = SpiffeId.FromSegments(Td, "path", "element");
        Assert.True(spiffeId.MemberOf(Td));

        spiffeId = SpiffeId.FromSegments(Td);
        Assert.True(spiffeId.MemberOf(Td));

        TrustDomain td2 = TrustDomain.FromString("domain2.test");
        spiffeId = SpiffeId.FromSegments(td2, "path", "element");
        Assert.False(spiffeId.MemberOf(Td));
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
        static Uri AsUri(string td, string path)
        {
            return new UriBuilder { Scheme = "spiffe", Host = td, Path = path }.Uri;
        }

        SpiffeId spiffeId = SpiffeId.FromSegments(Td, "path", "element");
        Assert.Equal(AsUri("trustdomain", "/path/element"), spiffeId.ToUri());

        spiffeId = SpiffeId.FromSegments(Td);
        Assert.Equal(AsUri("trustdomain", string.Empty), spiffeId.ToUri());
    }

    [Fact]
    public void TestReplacePath()
    {
        void AssertOk(string startsWith, string replaceWith, string expectedPath)
        {
            SpiffeId spiffeId = SpiffeId.FromPath(Td, startsWith).ReplacePath(replaceWith);
            AssertIdEqual(spiffeId, Td, expectedPath);
        }

        void AssertFail(string startsWith, string replaceWith, string expectedErr)
        {
            ArgumentException e =
                Assert.Throws<ArgumentException>(() => SpiffeId.FromPath(Td, startsWith).ReplacePath(replaceWith));
            Assert.Contains(expectedErr, e.Message);
        }

        AssertOk(string.Empty, "/foo", "/foo");
        AssertOk("/path", "/foo", "/foo");

        AssertFail(string.Empty, "foo", "Path must have a leading slash");
        AssertFail("/path", "/", "Path cannot have a trailing slash");
        AssertFail("/path", "foo", "Path must have a leading slash");
    }

    [Fact]
    public void TestReplaceSegments()
    {
        void AssertOk(string startsWith, string[] replaceWith, string expectedPath)
        {
            SpiffeId spiffeId = SpiffeId.FromPath(Td, startsWith).ReplaceSegments(replaceWith);
            AssertIdEqual(spiffeId, Td, expectedPath);
        }

        void AssertFail(string startsWith, string[] replaceWith, string expectedErr)
        {
            ArgumentException e =
                Assert.Throws<ArgumentException>(() => SpiffeId.FromPath(Td, startsWith).ReplaceSegments(replaceWith));
            Assert.Contains(expectedErr, e.Message);
        }

        AssertOk(string.Empty, ["foo"], "/foo");
        AssertOk("/path", ["foo"], "/foo");

        AssertFail(string.Empty, [string.Empty], "Path cannot contain empty segments");
        AssertFail(string.Empty, ["/foo"],
            "Path segment characters are limited to letters, numbers, dots, dashes, and underscores");
    }

    [Fact]
    public void TestAppendPath()
    {
        void AssertOk(string startsWith, string replaceWith, string expectedPath)
        {
            SpiffeId spiffeId = SpiffeId.FromPath(Td, startsWith).AppendPath(replaceWith);
            AssertIdEqual(spiffeId, Td, expectedPath);
        }

        void AssertFail(string startsWith, string replaceWith, string expectedErr)
        {
            ArgumentException e =
                Assert.Throws<ArgumentException>(() => SpiffeId.FromPath(Td, startsWith).AppendPath(replaceWith));
            Assert.Contains(expectedErr, e.Message);
        }

        AssertOk(string.Empty, "/foo", "/foo");
        AssertOk("/path", "/foo", "/path/foo");

        AssertFail(string.Empty, "foo", "Path must have a leading slash");
        AssertFail("/path", "/", "Path cannot have a trailing slash");
        AssertFail("/path", "foo", "Path must have a leading slash");
    }

    [Fact]
    public void TestAppendSegments()
    {
        void AssertOk(string startsWith, string[] replaceWith, string expectedPath)
        {
            SpiffeId spiffeId = SpiffeId.FromPath(Td, startsWith).AppendSegments(replaceWith);
            AssertIdEqual(spiffeId, Td, expectedPath);
        }

        void AssertFail(string startsWith, string[] replaceWith, string expectedErr)
        {
            ArgumentException e =
                Assert.Throws<ArgumentException>(() => SpiffeId.FromPath(Td, startsWith).AppendSegments(replaceWith));
            Assert.Contains(expectedErr, e.Message);
        }

        AssertOk(string.Empty, ["foo"], "/foo");
        AssertOk("/path", ["foo"], "/path/foo");

        AssertFail(string.Empty, [string.Empty], "Path cannot contain empty segments");
        AssertFail(string.Empty, ["/foo"],
            "Path segment characters are limited to letters, numbers, dots, dashes, and underscores");
    }

    [Fact]
    public void TestToString()
    {
        string str = "spiffe://example.org/myworkload";
        SpiffeId id = SpiffeId.FromString(str);
        id.ToString().Should().Be(str);
    }

    [Fact]
    public void TestEquals()
    {
        SpiffeId id1 = SpiffeId.FromString("spiffe://example.org/myworkload1");
        SpiffeId id2 = SpiffeId.FromString("spiffe://example.org/myworkload2");
        id1.Equals(id1).Should().BeTrue();
        id1.Equals(SpiffeId.FromString(id1.Id)).Should().BeTrue();
        id1.Equals(SpiffeId.FromString("spiffe://example.org/MYWORKLOAD1")).Should().BeFalse();
        id1.Equals(id2).Should().BeFalse();
        id1.Equals(null).Should().BeFalse();
        id1.Equals(new object()).Should().BeFalse();
    }

    [Fact]
    public void TestFromSegmentsFails()
    {
        Action f = () => SpiffeId.FromSegments(null);
        f.Should().Throw<ArgumentNullException>();
        f = () => SpiffeId.FromSegments(null, "foo");
        f.Should().Throw<ArgumentNullException>();
        f = () => SpiffeId.FromSegments(TrustDomain.FromString("spiffe://example.org"), null);
        f.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TestFromUriFails()
    {
        Action f = () => SpiffeId.FromUri(null);
        f.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TestMakeIdFails()
    {
        Action f = () => SpiffeId.MakeId(null, "/abc");
        f.Should().Throw<ArgumentNullException>();
        f = () => SpiffeId.MakeId(new TrustDomain(string.Empty), "/abc");
        f.Should().Throw<ArgumentException>();
        f = () => SpiffeId.MakeId(new TrustDomain(null), "/abc");
        f.Should().Throw<ArgumentException>();
        f = () => SpiffeId.MakeId(new TrustDomain("spiffe://example.org"), null);
        f.Should().Throw<ArgumentNullException>();
    }

    private static void AssertIdEqual(SpiffeId spiffeId, TrustDomain expectedTd, string expectedPath)
    {
        Assert.Equal(expectedTd, spiffeId.TrustDomain);
        Assert.Equal(expectedPath, spiffeId.Path);
        Assert.Equal(expectedTd.SpiffeId.Id + expectedPath, spiffeId.Id);

        // Root uri has trailing '/': spiffe://example.org/
        // Uri with has no trailing '/': spiffe://example.org/abc
        Assert.Equal(spiffeId.ToUri().ToString().TrimEnd('/'), spiffeId.Id);
    }
}
