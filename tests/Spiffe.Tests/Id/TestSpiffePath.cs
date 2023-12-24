using Spiffe.Id;

namespace Tests.Spiffe.Id;

public class TestSpiffePath
{
    public void TestJoinPathSegments()
    {
        void assertBad(string expectedErr, params string[] segments)
        {
            var e = Assert.Throws<ArgumentException>(() => SpiffePath.JoinPathSegments(segments));
            Assert.Contains(expectedErr, e.Message);
        }
        void assertOk(string expectedPath, params string[] segments)
        {
            string path = SpiffePath.JoinPathSegments(segments);
            Assert.Equal(expectedPath, path);
        }

        assertBad("Path cannot contain empty segments", "");
        assertBad("Path cannot contain dot segments", ".");
        assertBad("Path cannot contain dot segments", "..");
        assertBad("Path segment characters are limited to letters, numbers, dots, dashes, and underscores", "/");
        assertOk("/a", "a");
        assertOk("/a/b", "a", "b");
    }

    public void TestValidatePathSegment()
    {
        void assertFail(string expectedErr, string input)
        {
            var e = Assert.Throws<ArgumentException>(() => SpiffePath.ValidatePathSegment(input));
            Assert.Contains(expectedErr, e.Message);
        }

        assertFail("Path cannot contain empty segments", "");
        assertFail("Path cannot contain dot segments", ".");
        assertFail("Path cannot contain dot segments", "..");
        assertFail("Path segment characters are limited to letters, numbers, dots, dashes, and underscores", "/");

        var e = Record.Exception(() => SpiffePath.ValidatePathSegment("a"));
        Assert.Null(e);
    }
}
