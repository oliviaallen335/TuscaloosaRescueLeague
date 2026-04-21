namespace AdoptionAgency.Api.Models;

public record DashboardStatsDto(
    int Intake,
    int InFoster,
    int Adoptable,
    int Adopted,
    int Hold,
    int MedicalHold,
    int OpenFosterPlacements);
