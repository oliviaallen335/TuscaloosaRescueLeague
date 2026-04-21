using AdoptionAgency.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AdoptionAgency.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AdoptionDbContext db, IConfiguration config)
    {
        var email = config["Seed:AdminEmail"] ?? "employee@test.com";
        var password = config["Seed:AdminPassword"] ?? "Test123!";

        var hasAdmin = await db.Users.AnyAsync(u => u.Email == email);
        if (!hasAdmin)
        {
            db.Users.Add(new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = UserRole.Employee,
                CanViewPaymentDetails = true
            });
        }

        if (!await db.OutreachEvents.AnyAsync())
        {
            var baseMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(1);
            var events = new[]
            {
                new OutreachEvent
                {
                    Title = "Spring Adoption Fair",
                    Description = "Meet adoptable cats and dogs, talk with staff, and start applications onsite.",
                    Type = OutreachType.Event,
                    StartDate = baseMonth.AddDays(9).AddHours(10),
                    EndDate = baseMonth.AddDays(9).AddHours(15),
                    Location = "Riverwalk Park, Tuscaloosa",
                    IsPublished = true
                },
                new OutreachEvent
                {
                    Title = "Foster Family Open House",
                    Description = "Learn how fostering works, what supplies we provide, and how to sign up.",
                    Type = OutreachType.Event,
                    StartDate = baseMonth.AddMonths(2).AddDays(6).AddHours(18),
                    EndDate = baseMonth.AddMonths(2).AddDays(6).AddHours(20),
                    Location = "Tuscaloosa Public Library - Community Room",
                    IsPublished = true
                },
                new OutreachEvent
                {
                    Title = "Summer Kitten & Puppy Drive",
                    Description = "Donation campaign for formula, litter, and starter-care kits during peak intake season.",
                    Type = OutreachType.Campaign,
                    StartDate = baseMonth.AddMonths(4).AddDays(1),
                    EndDate = baseMonth.AddMonths(4).AddDays(30),
                    Location = "Online + Dropoff at Main Shelter",
                    IsPublished = true
                },
                new OutreachEvent
                {
                    Title = "Back-to-School Pet Wellness Day",
                    Description = "Free microchip scans, low-cost vaccines, and adoption counseling sessions.",
                    Type = OutreachType.Event,
                    StartDate = baseMonth.AddMonths(6).AddDays(12).AddHours(9),
                    EndDate = baseMonth.AddMonths(6).AddDays(12).AddHours(13),
                    Location = "Downtown Community Center",
                    IsPublished = true
                },
                new OutreachEvent
                {
                    Title = "Fall Rescue Run 5K",
                    Description = "Community fundraiser run supporting medical treatment and transport for rescue animals.",
                    Type = OutreachType.Event,
                    StartDate = baseMonth.AddMonths(8).AddDays(4).AddHours(8),
                    EndDate = baseMonth.AddMonths(8).AddDays(4).AddHours(11),
                    Location = "University Trails, Tuscaloosa",
                    IsPublished = true
                },
                new OutreachEvent
                {
                    Title = "Holiday Home for the Holidays Campaign",
                    Description = "December campaign highlighting long-stay pets with sponsored adoption fees.",
                    Type = OutreachType.Campaign,
                    StartDate = baseMonth.AddMonths(10).AddDays(1),
                    EndDate = baseMonth.AddMonths(10).AddDays(31),
                    Location = "Shelter + Social Media",
                    IsPublished = true
                }
            };

            db.OutreachEvents.AddRange(events);
        }

        await db.SaveChangesAsync();
    }

    /// <summary>Clears animals and applicant/customer data. Keeps employee user(s).</summary>
    public static async Task ClearAnimalsAndApplicantsAsync(AdoptionDbContext db)
    {
        db.Payments.RemoveRange(await db.Payments.ToListAsync());
        db.Placements.RemoveRange(await db.Placements.ToListAsync());
        db.Applications.RemoveRange(await db.Applications.ToListAsync());
        db.Intakes.RemoveRange(await db.Intakes.ToListAsync());
        db.AnimalPhotos.RemoveRange(await db.AnimalPhotos.ToListAsync());
        db.Animals.RemoveRange(await db.Animals.ToListAsync());
        db.OutreachEvents.RemoveRange(await db.OutreachEvents.ToListAsync());

        var applicants = await db.Applicants.Include(a => a.User).ToListAsync();
        var applicantUsers = applicants.Select(a => a.User).ToList();
        db.Applicants.RemoveRange(applicants);
        db.Users.RemoveRange(applicantUsers);

        await db.SaveChangesAsync();
    }
}
