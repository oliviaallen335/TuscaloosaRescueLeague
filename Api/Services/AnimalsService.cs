using AdoptionAgency.Api.Data;
using AdoptionAgency.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace AdoptionAgency.Api.Services;

public class AnimalsService
{
    private readonly AdoptionDbContext _db;
    private readonly PublicIdGenerator _publicIds;
    private readonly IWebHostEnvironment _env;

    public AnimalsService(AdoptionDbContext db, PublicIdGenerator publicIds, IWebHostEnvironment env)
    {
        _db = db;
        _publicIds = publicIds;
        _env = env;
    }

    /// <param name="catalogOnly">When true, only return animals available for adoption (Adoptable, InFoster). Hides Adopted, Hold, etc.</param>
    public async Task<IReadOnlyList<AnimalListItemDto>> ListAsync(
        Species? species,
        AnimalStatus? status,
        string? search,
        bool catalogOnly = false,
        CancellationToken ct = default)
    {
        var q = _db.Animals
            .Include(a => a.Photos)
            .AsQueryable();

        if (catalogOnly)
            q = q.Where(a => a.Status == AnimalStatus.Adoptable || a.Status == AnimalStatus.InFoster);

        if (species.HasValue)
            q = q.Where(a => a.Species == species.Value);
        if (status.HasValue)
            q = q.Where(a => a.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(a =>
                a.Name.Contains(s) ||
                a.PublicId.Contains(s) ||
                (a.BreedPrimary != null && a.BreedPrimary.Contains(s)) ||
                (a.BreedSecondary != null && a.BreedSecondary.Contains(s)));
        }

        var list = await q.OrderByDescending(a => a.IntakeDate).ToListAsync(ct);
        return list.Select(ToListItem).ToList();
    }

    public async Task<AnimalDetailDto?> GetByPublicIdAsync(string publicId, CancellationToken ct = default)
    {
        var a = await _db.Animals
            .Include(x => x.Photos)
            .Include(x => x.BehaviorProfile)
            .FirstOrDefaultAsync(x => x.PublicId == publicId, ct);
        return a == null ? null : ToDetail(a);
    }

    public async Task<AnimalDetailDto> CreateAsync(AnimalCreateDto dto, CancellationToken ct = default)
    {
        var publicId = await _publicIds.NextAsync(dto.Species);
        var intakeDate = dto.IntakeDate ?? DateTime.UtcNow;

        var animal = new Animal
        {
            PublicId = publicId,
            Species = dto.Species,
            Name = dto.Name,
            Sex = dto.Sex,
            Size = dto.Size,
            Color = dto.Color,
            EstimatedAgeMonths = dto.EstimatedAgeMonths,
            BreedPrimary = dto.BreedPrimary,
            BreedSecondary = dto.BreedSecondary,
            Status = dto.Status,
            IntakeDate = intakeDate,
            Description = dto.Description
        };

        if (dto.Behavior != null)
        {
            animal.BehaviorProfile = new BehaviorProfile
            {
                GoodWithKids = dto.Behavior.GoodWithKids,
                GoodWithCats = dto.Behavior.GoodWithCats,
                GoodWithDogs = dto.Behavior.GoodWithDogs,
                HouseTrained = dto.Behavior.HouseTrained,
                EnergyLevel = dto.Behavior.EnergyLevel,
                Notes = dto.Behavior.Notes
            };
        }

        _db.Animals.Add(animal);
        await _db.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(dto.IntakeSource) || !string.IsNullOrWhiteSpace(dto.IntakeNotes))
        {
            _db.Intakes.Add(new Intake
            {
                AnimalId = animal.Id,
                Source = dto.IntakeSource,
                Notes = dto.IntakeNotes,
                IntakeDate = intakeDate
            });
            await _db.SaveChangesAsync(ct);
        }

        return (await GetByPublicIdAsync(animal.PublicId, ct))!;
    }

    public async Task<AnimalDetailDto?> UpdateAsync(string publicId, AnimalUpdateDto dto, CancellationToken ct = default)
    {
        var animal = await _db.Animals
            .Include(a => a.BehaviorProfile)
            .FirstOrDefaultAsync(a => a.PublicId == publicId, ct);
        if (animal == null)
            return null;

        var speciesChanged = animal.Species != dto.Species;
        if (speciesChanged)
        {
            animal.PublicId = await _publicIds.NextAsync(dto.Species, animal.IntakeDate);
        }

        animal.Name = dto.Name;
        animal.Species = dto.Species;
        animal.Sex = dto.Sex;
        animal.Size = dto.Size;
        animal.Color = dto.Color;
        animal.EstimatedAgeMonths = dto.EstimatedAgeMonths;
        animal.BreedPrimary = dto.BreedPrimary;
        animal.BreedSecondary = dto.BreedSecondary;
        animal.Status = dto.Status;
        animal.IntakeDate = dto.IntakeDate;
        animal.Description = dto.Description;
        animal.UpdatedAt = DateTime.UtcNow;

        if (dto.Behavior != null)
        {
            if (animal.BehaviorProfile == null)
            {
                animal.BehaviorProfile = new BehaviorProfile { AnimalId = animal.Id };
                _db.BehaviorProfiles.Add(animal.BehaviorProfile);
            }

            animal.BehaviorProfile.GoodWithKids = dto.Behavior.GoodWithKids;
            animal.BehaviorProfile.GoodWithCats = dto.Behavior.GoodWithCats;
            animal.BehaviorProfile.GoodWithDogs = dto.Behavior.GoodWithDogs;
            animal.BehaviorProfile.HouseTrained = dto.Behavior.HouseTrained;
            animal.BehaviorProfile.EnergyLevel = dto.Behavior.EnergyLevel;
            animal.BehaviorProfile.Notes = dto.Behavior.Notes;
        }

        await _db.SaveChangesAsync(ct);
        return await GetByPublicIdAsync(animal.PublicId, ct);
    }

    public async Task<(bool Ok, string? Error)> DeleteAsync(string publicId, CancellationToken ct = default)
    {
        var animal = await _db.Animals
            .Include(a => a.Photos)
            .FirstOrDefaultAsync(a => a.PublicId == publicId, ct);
        if (animal == null)
            return (false, null);

        var hasPlacements = await _db.Placements.AnyAsync(p => p.AnimalId == animal.Id, ct);
        var hasApplications = await _db.Applications.AnyAsync(a => a.AnimalId == animal.Id, ct);
        if (hasPlacements || hasApplications)
            return (false, "Cannot delete: animal has placements or applications. Remove those first.");

        _db.Animals.Remove(animal);
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<AnimalPhotoDto?> AddPhotoAsync(string publicId, string relativePath, bool setPrimary, CancellationToken ct = default)
    {
        var animal = await _db.Animals.Include(a => a.Photos).FirstOrDefaultAsync(a => a.PublicId == publicId, ct);
        if (animal == null)
            return null;

        var existing = animal.Photos.ToList();
        var hadAnyPhoto = existing.Count > 0;

        List<string>? oldPathsToDelete = null;
        if (setPrimary && hadAnyPhoto)
        {
            oldPathsToDelete = existing.Select(p => p.FilePath).ToList();
            _db.AnimalPhotos.RemoveRange(existing);
        }
        else if (setPrimary)
        {
            foreach (var p in existing)
                p.IsPrimary = false;
        }

        var displayOrder = setPrimary && hadAnyPhoto ? 0 : existing.Count;
        var isPrimary = setPrimary || !hadAnyPhoto;
        var photo = new AnimalPhoto
        {
            AnimalId = animal.Id,
            FilePath = relativePath,
            IsPrimary = isPrimary,
            DisplayOrder = displayOrder
        };
        _db.AnimalPhotos.Add(photo);
        await _db.SaveChangesAsync(ct);

        if (oldPathsToDelete != null)
        {
            foreach (var rel in oldPathsToDelete)
                TryDeleteStoredAnimalPhoto(rel);
        }

        return new AnimalPhotoDto(photo.FilePath, photo.IsPrimary, photo.DisplayOrder);
    }

    /// <summary>Deletes a file under wwwroot/uploads/animals if the path is safe.</summary>
    private void TryDeleteStoredAnimalPhoto(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath) || !relativePath.StartsWith("/uploads/animals/", StringComparison.Ordinal))
            return;

        var root = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var segments = relativePath.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var full = Path.GetFullPath(Path.Combine(new[] { root }.Concat(segments).ToArray()));
        var safeRoot = Path.GetFullPath(root);
        if (!full.StartsWith(safeRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(full))
            return;

        try
        {
            File.Delete(full);
        }
        catch
        {
            // Orphan file is acceptable vs failing after DB commit
        }
    }

    private static AnimalListItemDto ToListItem(Animal a)
    {
        var primary = a.Photos.OrderBy(p => p.DisplayOrder).FirstOrDefault(p => p.IsPrimary)
            ?? a.Photos.OrderBy(p => p.DisplayOrder).FirstOrDefault();
        return new AnimalListItemDto(
            a.PublicId,
            a.Name,
            a.Species,
            a.Sex,
            a.Status,
            a.BreedPrimary,
            a.EstimatedAgeMonths,
            primary?.FilePath);
    }

    private static AnimalDetailDto ToDetail(Animal a)
    {
        var photos = a.Photos.OrderBy(p => p.DisplayOrder)
            .Select(p => new AnimalPhotoDto(p.FilePath, p.IsPrimary, p.DisplayOrder))
            .ToList();

        BehaviorProfileDto? b = null;
        if (a.BehaviorProfile != null)
        {
            var bp = a.BehaviorProfile;
            b = new BehaviorProfileDto(
                bp.GoodWithKids,
                bp.GoodWithCats,
                bp.GoodWithDogs,
                bp.HouseTrained,
                bp.EnergyLevel,
                bp.Notes);
        }

        return new AnimalDetailDto(
            a.PublicId,
            a.Name,
            a.Species,
            a.Sex,
            a.Size,
            a.Color,
            a.EstimatedAgeMonths,
            a.BreedPrimary,
            a.BreedSecondary,
            a.Status,
            a.IntakeDate,
            a.Description,
            photos,
            b);
    }
}
