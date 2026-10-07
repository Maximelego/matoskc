namespace MatosKC.Application.Auth.CreateSession;

public sealed record CreateAuthenticationSessionDto(
    int? AgencyCode,
    string? Email,
    string Password
);
