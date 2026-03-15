using FluentAssertions;
using Spiffe.Id;

namespace Spiffe.Tests.Id;

public class TestSpiffePath
{
    [Fact]
    public void TestJoinPathSegments()
    {
        void AssertBad(string expectedErr, params string[] segments)
        {
            ArgumentException e = Assert.Throws<ArgumentException>(() => SpiffePath.JoinPathSegments(segments));
            Assert.Contains(expectedErr, e.Message);
        }

        void AssertOk(string expectedPath, params string[] segments)
        {
            string path = SpiffePath.JoinPathSegments(segments);
            Assert.Equal(expectedPath, path);
        }

        Action f = () => SpiffePath.JoinPathSegments(null);
        f.Should().Throw<ArgumentNullException>().WithParameterName("segments");
        AssertBad("Path cannot contain empty segments", string.Empty);
        AssertBad("Path cannot contain dot segments", ".");
        AssertBad("Path cannot contain dot segments", "..");
        AssertBad("Path segment characters are limited to letters, numbers, dots, dashes, and underscores", "/");
        AssertOk("/a", "a");
        AssertOk("/a/b", "a", "b");
    }

    [Fact]
    public void TestValidatePathSegment()
    {
        void AssertFail(string expectedErr, string input)
        {
            ArgumentException e = Assert.Throws<ArgumentException>(() => SpiffePath.ValidatePathSegment(input));
            Assert.Contains(expectedErr, e.Message);
        }

        AssertFail("Path cannot contain empty segments", string.Empty);
        AssertFail("Path cannot contain dot segments", ".");
        AssertFail("Path cannot contain dot segments", "..");
        AssertFail("Path segment characters are limited to letters, numbers, dots, dashes, and underscores", "/");

        Exception e = Record.Exception(() => SpiffePath.ValidatePathSegment("a"));
        Assert.Null(e);
    }

    [Fact]
    public void TestValidatePath()
    {
        void AssertFail(string expectedErr, string path)
        {
            ArgumentException e = Assert.Throws<ArgumentException>(() => SpiffePath.ValidatePath(path));
            Assert.Contains(expectedErr, e.Message);
        }

        // Empty and null paths are valid
        Exception? ex = Record.Exception(() => SpiffePath.ValidatePath(string.Empty));
        ex.Should().BeNull();

        ex = Record.Exception(() => SpiffePath.ValidatePath(null));
        ex.Should().BeNull();

        // Valid paths
        ex = Record.Exception(() => SpiffePath.ValidatePath("/foo"));
        ex.Should().BeNull();

        ex = Record.Exception(() => SpiffePath.ValidatePath("/foo/bar"));
        ex.Should().BeNull();

        ex = Record.Exception(() => SpiffePath.ValidatePath("/.path"));
        ex.Should().BeNull();

        ex = Record.Exception(() => SpiffePath.ValidatePath("/..."));
        ex.Should().BeNull();

        // Missing leading slash
        AssertFail("Path must have a leading slash", "foo");
        AssertFail("Path must have a leading slash", "foo/bar");

        // Trailing slash
        AssertFail("Path cannot have a trailing slash", "/foo/");
        AssertFail("Path cannot have a trailing slash", "/");

        // Empty segments
        AssertFail("Path cannot contain empty segments", "//");
        AssertFail("Path cannot contain empty segments", "//foo");
        AssertFail("Path cannot contain empty segments", "/foo//bar");

        // Dot segments
        AssertFail("Path cannot contain dot segments", "/.");
        AssertFail("Path cannot contain dot segments", "/..");
        AssertFail("Path cannot contain dot segments", "/./foo");
        AssertFail("Path cannot contain dot segments", "/../foo");
        AssertFail("Path cannot contain dot segments", "/foo/.");
        AssertFail("Path cannot contain dot segments", "/foo/..");

        // Bad path chars
        AssertFail("Path segment characters are limited to letters, numbers, dots, dashes, and underscores", "/foo$bar");
        AssertFail("Path segment characters are limited to letters, numbers, dots, dashes, and underscores", "/foo%20bar");
    }
}
