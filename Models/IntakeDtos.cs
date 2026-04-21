namespace AdoptionAgency.Api.Models;

public record IntakeListDto(int Id, string? Source, string? Notes, DateTime IntakeDate, DateTime CreatedAt);

public record IntakeCreateDto(string? Source, string? Notes, DateTime? IntakeDate);
