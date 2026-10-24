using ReleaseNotes.Web.ContentModel;

namespace ReleaseNotes.Web.Tests;

public sealed class ReleaseSavingHandlerTests
{
    [Theory]
    [InlineData("[\"Minor\"]", "Minor")]
    [InlineData("Major", "Major")]
    [InlineData("[]", null)]
    [InlineData(null, null)]
    public void Dropdown_values_are_read_from_their_storage_format(string? stored, string? expected)
    {
        Assert.Equal(expected, ReleaseSavingHandler.ReadDropdownValue(stored));
    }
}
