using System.ComponentModel.DataAnnotations;

namespace AdoptionAgency.Api.Models;

public record ApplicantProfileDto(
    int Id,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? PostalCode,
    bool? HasKids,
    bool? HasCats,
    bool? HasDogs,
    bool? HasYard,
    string? HousingType,
    string? ExperienceLevel);

public class ApplicantProfileUpdateDto
{
    [MaxLength(100)] public string? FirstName { get; set; }
    [MaxLength(100)] public string? LastName { get; set; }
    [MaxLength(256)] public string? Email { get; set; }
    [MaxLength(20)] public string? Phone { get; set; }
    [MaxLength(200)] public string? AddressLine1 { get; set; }
    [MaxLength(200)] public string? AddressLine2 { get; set; }
    [MaxLength(50)] public string? City { get; set; }
    [MaxLength(20)] public string? State { get; set; }
    [MaxLength(20)] public string? PostalCode { get; set; }
    public bool? HasKids { get; set; }
    public bool? HasCats { get; set; }
    public bool? HasDogs { get; set; }
    public bool? HasYard { get; set; }
    [MaxLength(50)] public string? HousingType { get; set; }
    [MaxLength(50)] public string? ExperienceLevel { get; set; }
}
