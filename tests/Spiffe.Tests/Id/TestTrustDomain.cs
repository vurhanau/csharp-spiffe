using FluentAssertions;
using Spiffe.Id;
using static Spiffe.Tests.Id.TestConstants;

namespace Spiffe.Tests.Id;

public class TestTrustDomain
{
    [Fact]
    public void TestFromString()
    {
        void AssertOk(string input, TrustDomain expected)
        {
            TrustDomain actual = TrustDomain.FromString(input);
            Assert.Equal(expected, actual);
        }

        void AssertFail(string input, string expectedErr)
        {
            ArgumentException e = Assert.Throws<ArgumentException>(() => TrustDomain.FromString(input));
            Assert.Contains(expectedErr, e.Message);
        }

        AssertFail(null, "Trust domain is missing");
        AssertFail(string.Empty, "Trust domain is missing");
        AssertOk("spiffe://trustdomain", Td);
        AssertOk("spiffe://trustdomain/path", Td);

        AssertFail("spiffe:/trustdomain/path", "Scheme is missing or invalid");
        AssertFail("spiffe://", "Trust domain is missing");
        AssertFail("spiffe:///path", "Trust domain is missing");
        AssertFail("spiffe://trustdomain/", "Path cannot have a trailing slash");
        AssertFail("spiffe://trustdomain/path/", "Path cannot have a trailing slash");
        AssertFail("spiffe://%F0%9F%A4%AF/path",
            "Trust domain characters are limited to lowercase letters, numbers, dots, dashes, and underscores");
        AssertFail("spiffe://trustdomain/%F0%9F%A4%AF",
            "Path segment characters are limited to letters, numbers, dots, dashes, and underscores");

        for (int i = 0; i < 256; i++)
        {
            char c = (char)i;
            if (TdChars.Contains(c))
            {
                TrustDomain expected = TrustDomain.FromString($"trustdomain{c}");
                AssertOk($"trustdomain{c}", expected);
                AssertOk($"spiffe://trustdomain{c}", expected);
            }
            else
            {
                AssertFail($"trustdomain{c}",
                    "Trust domain characters are limited to lowercase letters, numbers, dots, dashes, and underscores");
            }
        }
    }

    [Fact]
    public void TestFromUri()
    {
        void AssertOk(string s)
        {
            Uri uri = new(s);
            TrustDomain td = TrustDomain.FromUri(uri);
            Assert.Equal(TrustDomain.FromString(uri.Host), td);
        }

        void AssertFail(Uri uri, string expectedErr)
        {
            ArgumentException e = Assert.Throws<ArgumentException>(() => TrustDomain.FromUri(uri));
            Assert.Contains(expectedErr, e.Message);
        }

        AssertOk("spiffe://trustdomain");
        AssertOk("spiffe://trustdomain/path");

        AssertFail(new Uri("spiffe://trustdomain/path$"),
            "Path segment characters are limited to letters, numbers, dots, dashes, and underscores");

        Action f = () => TrustDomain.FromUri(null);
        f.Should().Throw<ArgumentNullException>().WithParameterName("uri");
    }

    [Fact]
    public void TestId()
    {
        string expected = "spiffe://trustdomain";
        string[] arr = ["trustdomain", "spiffe://trustdomain", "spiffe://trustdomain/path"];
        foreach (string s in arr)
        {
            TrustDomain td = TrustDomain.FromString(s);
            Assert.Equal(expected, td.SpiffeId.Id);
        }
    }

    [Fact]
    public void TestEquals()
    {
        TrustDomain td1 = TrustDomain.FromString("spiffe://example1.org");
        TrustDomain td2 = TrustDomain.FromString("spiffe://example1.org");
        TrustDomain td3 = TrustDomain.FromString("spiffe://example2.org");
        td1.Equals(td1).Should().BeTrue();
        td1.Equals(td2).Should().BeTrue();
        td1.Equals(td3).Should().BeFalse();
        td1.Equals(new object()).Should().BeFalse();
    }
}
