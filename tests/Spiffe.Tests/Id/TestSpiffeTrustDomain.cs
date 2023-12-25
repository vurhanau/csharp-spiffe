using Spiffe.Id;
using static Tests.Spiffe.Id.TestConstants;

namespace Tests.Spiffe.Id;

public class TestSpiffeTrustDomain
{
    [Fact]
    public void TestFromString()
    {
        void AssertOk(string input, SpiffeTrustDomain expected)
        {
            SpiffeTrustDomain actual = SpiffeTrustDomain.FromString(input);
            Assert.Equal(expected, actual);
        }

        void AssertFail(string input, string expectedErr)
        {
            ArgumentException e = Assert.Throws<ArgumentException>(() => SpiffeTrustDomain.FromString(input));
            Assert.Contains(expectedErr, e.Message);
        }

        AssertFail(string.Empty, "Trust domain is missing");
        AssertOk("spiffe://trustdomain", Td);
        AssertOk("spiffe://trustdomain/path", Td);

        AssertFail("spiffe:/trustdomain/path", "Scheme is missing or invalid");
        AssertFail("spiffe://", "Trust domain is missing");
        AssertFail("spiffe:///path", "Trust domain is missing");
        AssertFail("spiffe://trustdomain/", "Path cannot have a trailing slash");
        AssertFail("spiffe://trustdomain/path/", "Path cannot have a trailing slash");
        AssertFail("spiffe://%F0%9F%A4%AF/path", "Trust domain characters are limited to lowercase letters, numbers, dots, dashes, and underscores");
        AssertFail("spiffe://trustdomain/%F0%9F%A4%AF", "Path segment characters are limited to letters, numbers, dots, dashes, and underscores");

        for (int i = 0; i < 256; i++)
        {
            char c = (char)i;
            if (TdChars.Contains(c))
            {
                SpiffeTrustDomain expected = SpiffeTrustDomain.FromString($"trustdomain{c}");
                AssertOk($"trustdomain{c}", expected);
                AssertOk($"spiffe://trustdomain{c}", expected);
            }
            else
            {
                AssertFail($"trustdomain{c}", "Trust domain characters are limited to lowercase letters, numbers, dots, dashes, and underscores");
            }
        }
    }

    [Fact]
    public void TestFromUri()
    {
        void AssertOk(string s)
        {
            Uri uri = new(s);
            SpiffeTrustDomain td = SpiffeTrustDomain.FromUri(uri);
            Assert.Equal(SpiffeTrustDomain.FromString(uri.Host), td);
        }

        void AssertFail(Uri uri, string expectedErr)
        {
            ArgumentException e = Assert.Throws<ArgumentException>(() => SpiffeTrustDomain.FromUri(uri));
            Assert.Contains(expectedErr, e.Message);
        }

        AssertOk("spiffe://trustdomain");
        AssertOk("spiffe://trustdomain/path");

        AssertFail(new Uri("spiffe://trustdomain/path$"), "Path segment characters are limited to letters, numbers, dots, dashes, and underscores");
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
