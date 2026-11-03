namespace Pavena.Application.Social.DTOs;

public record FeedResponseDto(
    IReadOnlyList<PostDto> Items,
    string? NextCursor,
    bool HasMore
);

public record FeedQueryParameters(
    int Limit = 20,
    string? Cursor = null
);
