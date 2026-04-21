using AdoptionAgency.Api.Extensions;
using AdoptionAgency.Api.Models;
using AdoptionAgency.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdoptionAgency.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnimalsController : ControllerBase
{
    private readonly AnimalsService _animals;
    private readonly IntakesService _intakes;
    private readonly NameSuggestionService _nameSuggest;
    private readonly IWebHostEnvironment _env;

    public AnimalsController(AnimalsService animals, IntakesService intakes, NameSuggestionService nameSuggest, IWebHostEnvironment env)
    {
        _animals = animals;
        _intakes = intakes;
        _nameSuggest = nameSuggest;
        _env = env;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AnimalListItemDto>>> List(
        [FromQuery] Species? species,
        [FromQuery] AnimalStatus? status,
        [FromQuery] string? search)
    {
        var catalogOnly = !User.IsEmployee();
        return Ok(await _animals.ListAsync(species, status, search, catalogOnly));
    }

    [AllowAnonymous]
    [HttpGet("{publicId}")]
    public async Task<ActionResult<AnimalDetailDto>> Get(string publicId)
    {
        var a = await _animals.GetByPublicIdAsync(publicId);
        return a == null ? NotFound() : Ok(a);
    }

    [Authorize(Roles = "Employee")]
    [HttpPost("suggest-name")]
    public async Task<ActionResult<NameSuggestionResponseDto>> SuggestName([FromBody] NameSuggestionRequestDto dto)
    {
        var name = await _nameSuggest.SuggestOneAsync(dto.Species, dto.Sex, dto.Color, dto.BreedPrimary, dto.ExcludeAnimalPublicId);
        if (name == null)
            return Ok(new NameSuggestionResponseDto(null, "Could not generate a name. Check DeepSeek:ApiKey or try again."));
        return Ok(new NameSuggestionResponseDto(name, null));
    }

    [Authorize(Roles = "Employee")]
    [HttpPost]
    public async Task<ActionResult<AnimalDetailDto>> Create([FromBody] AnimalCreateDto dto)
    {
        var created = await _animals.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { publicId = created.PublicId }, created);
    }

    [Authorize(Roles = "Employee")]
    [HttpPut("{publicId}")]
    public async Task<ActionResult<AnimalDetailDto>> Update(string publicId, [FromBody] AnimalUpdateDto dto)
    {
        var updated = await _animals.UpdateAsync(publicId, dto);
        return updated == null ? NotFound() : Ok(updated);
    }

    [Authorize(Roles = "Employee")]
    [HttpDelete("{publicId}")]
    public async Task<IActionResult> Delete(string publicId)
    {
        var (ok, error) = await _animals.DeleteAsync(publicId);
        if (!ok)
            return error == null ? NotFound() : Conflict(new { error });
        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("{publicId}/intakes")]
    public async Task<ActionResult<IReadOnlyList<IntakeListDto>>> ListIntakes(string publicId)
    {
        if (await _animals.GetByPublicIdAsync(publicId) == null)
            return NotFound();
        return Ok(await _intakes.ListByAnimalPublicIdAsync(publicId));
    }

    [Authorize(Roles = "Employee")]
    [HttpPost("{publicId}/intakes")]
    public async Task<ActionResult<IntakeListDto>> AddIntake(string publicId, [FromBody] IntakeCreateDto dto)
    {
        var (row, error) = await _intakes.AddAsync(publicId, dto);
        if (error != null)
            return NotFound(new { error });
        return Ok(row);
    }

    [Authorize(Roles = "Employee")]
    [HttpPost("{publicId}/photos")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<AnimalPhotoDto>> UploadPhoto(string publicId, IFormFile file, [FromQuery] bool primary = false)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file" });

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || ext.Length > 10)
            ext = ".jpg";

        var uploads = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "animals");
        Directory.CreateDirectory(uploads);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploads, fileName);
        await using (var stream = System.IO.File.Create(fullPath))
            await file.CopyToAsync(stream);

        var relative = $"/uploads/animals/{fileName}";
        var dto = await _animals.AddPhotoAsync(publicId, relative, primary);
        return dto == null ? NotFound() : Ok(dto);
    }
}
