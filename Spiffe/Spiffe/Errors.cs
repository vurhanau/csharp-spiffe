namespace Spiffe;

internal static class Errors
{
    internal const string BadTrustDomainChar = "Trust domain characters are limited to lowercase letters, numbers, dots, dashes, and underscores";

    internal const string BadPathSegmentChar = "Path segment characters are limited to letters, numbers, dots, dashes, and underscores";

    internal const string DotSegment = "Path cannot contain dot segments";

    internal const string NoLeadingSlash = "Path must have a leading slash";

    internal const string Empty = "Cannot be empty";

    internal const string EmptySegment = "Path cannot contain empty segments";

    internal const string MissingTrustDomain = "Trust domain is missing";

    internal const string TrailingSlash = "Path cannot have a trailing slash";

    internal const string WrongScheme = "Scheme is missing or invalid";
}