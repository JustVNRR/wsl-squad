using FluentAssertions;

using wsl_squad.Core.Infrastructure.Json;

namespace wsl_squad.Tests.Core.Json;

public class JsoncSanitizerTests
{
    [Fact]
    public void Removes_line_and_block_comments()
    {
        const string json = """
            {
              // line comment
              "name": "value", /* block comment */
              "other": 1
            }
            """;

        string sanitized = JsoncSanitizer.Sanitize(json);

        sanitized.Should().NotContain("comment");
        sanitized.Should().Contain("\"name\": \"value\"");
    }

    [Fact]
    public void Removes_trailing_commas()
    {
        const string json = """{ "items": [1, 2, 3,], "name": "x", }""";

        JsoncSanitizer.Sanitize(json).Should().Be("""{ "items": [1, 2, 3], "name": "x" }""");
    }

    [Fact]
    public void Preserves_comment_like_text_inside_strings()
    {
        const string json = """{ "path": "https://example.com//file", "text": "/* keep */" }""";

        JsoncSanitizer.Sanitize(json).Should().Be(json);
    }

    [Fact]
    public void Handles_escaped_quotes_inside_strings()
    {
        const string json = """{ "text": "a \"quoted\" // value" }""";

        JsoncSanitizer.Sanitize(json).Should().Be(json);
    }
}
