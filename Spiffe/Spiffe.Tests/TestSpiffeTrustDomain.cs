namespace Spiffe.Tests;

using static TestConstants;

public class TestSpiffeTrustDomain
{
    [Fact]
    public void TestFromString()
    {
        void assertOk(string input, SpiffeTrustDomain expected)
        {
            SpiffeTrustDomain actual = SpiffeTrustDomain.FromString(input);
            Assert.Equal(expected, actual);
        }

        void assertFail(string input, string expectedErr)
        {
            var e = Assert.Throws<ArgumentException>(() => SpiffeTrustDomain.FromString(input));
            Assert.Contains(expectedErr, e.Message);
        }

        assertFail("", "Trust domain is missing");
        assertOk("spiffe://trustdomain", td);
        assertOk("spiffe://trustdomain/path", td);

        assertFail("spiffe:/trustdomain/path", "Scheme is missing or invalid");
		assertFail("spiffe://", "Trust domain is missing");
		assertFail("spiffe:///path", "Trust domain is missing");
		assertFail("spiffe://trustdomain/", "Path cannot have a trailing slash");
		assertFail("spiffe://trustdomain/path/", "Path cannot have a trailing slash");
		assertFail("spiffe://%F0%9F%A4%AF/path", "Trust domain characters are limited to lowercase letters, numbers, dots, dashes, and underscores");
		assertFail("spiffe://trustdomain/%F0%9F%A4%AF", "Path segment characters are limited to letters, numbers, dots, dashes, and underscores");

        for (int i = 0; i < 256; i++)
        {
            char c = (char) i;
            if (tdChars.Contains(c))
            {
                SpiffeTrustDomain expected = SpiffeTrustDomain.FromString($"trustdomain{c}");
                assertOk($"trustdomain{c}", expected);
                assertOk($"spiffe://trustdomain{c}", expected);
            }
            else
            {
				assertFail($"trustdomain{c}", "Trust domain characters are limited to lowercase letters, numbers, dots, dashes, and underscores");
            }
        }
    }

    [Fact]
    public void TestFromUri()
    {
        void assertOk(string s)
        {
            Uri uri = new(s);
            SpiffeTrustDomain td = SpiffeTrustDomain.FromUri(uri);
            Assert.Equal(SpiffeTrustDomain.FromString(uri.Host), td);
        }
        void assertFail(Uri uri, string expectedErr)
        {
            var e = Assert.Throws<ArgumentException>(() => SpiffeTrustDomain.FromUri(uri));
            Assert.Contains(expectedErr, e.Message);
        }

        assertOk("spiffe://trustdomain");
        assertOk("spiffe://trustdomain/path");

        assertFail(new Uri("spiffe://trustdomain/path$"), "Path segment characters are limited to letters, numbers, dots, dashes, and underscores");
    }

    [Fact]
    public void TestId()
    {
        string expected = "spiffe://trustdomain";
        string[] arr = ["trustdomain", "spiffe://trustdomain", "spiffe://trustdomain/path"];
        foreach (string s in arr)
        {
            var td = SpiffeTrustDomain.FromString(s);
            Assert.Equal(expected, td.SpiffeId.Id);
        }
    }
}