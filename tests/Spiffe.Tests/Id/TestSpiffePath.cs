﻿using Spiffe.Id;

namespace Tests.Spiffe.Id;

public class TestSpiffePath
{
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

        AssertBad("Path cannot contain empty segments", string.Empty);
        AssertBad("Path cannot contain dot segments", ".");
        AssertBad("Path cannot contain dot segments", "..");
        AssertBad("Path segment characters are limited to letters, numbers, dots, dashes, and underscores", "/");
        AssertOk("/a", "a");
        AssertOk("/a/b", "a", "b");
    }

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
}
