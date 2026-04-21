namespace AdoptionAgency.Api.Models;

public enum Species
{
    Dog,
    Cat
}

public enum AnimalStatus
{
    Intake,
    InFoster,
    Adoptable,
    Adopted,
    Hold,
    MedicalHold
}

/// <summary>Tri-state for "good with X" — supports Unknown when we don't know yet.</summary>
public enum GoodWith
{
    Unknown,
    Yes,
    No
}

public enum PlacementType
{
    Foster,
    Adoption
}

public enum Sex
{
    Unknown,
    Male,
    Female
}

public enum EnergyLevel
{
    Unknown,
    Low,
    Medium,
    High
}

public enum ApplicationStatus
{
    Pending,
    Approved,
    Denied,
    Withdrawn
}

public enum UserRole
{
    Employee,
    Applicant
}

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded
}

public enum OutreachType
{
    Event,
    Campaign
}
