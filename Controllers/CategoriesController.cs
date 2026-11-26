using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using UniEventsAPI.Data;
using UniEventsAPI.Models;
using UniEventsAPI.Services;

namespace UniEventsAPI.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ApiContext _context;

    public CategoriesController(ApiContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<EventCategory>>> GetAll(CancellationToken ct)
    {
        return Ok(await _context.Categories.OrderBy(category => category.Name).ToListAsync(ct));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<EventCategory>> Create(CategoryRequest request, CancellationToken ct)
    {
        var category = new EventCategory { Name = request.Name.Trim() };
        _context.Categories.Add(category);
        await SaveCategory(ct);
        return StatusCode(StatusCodes.Status201Created, category);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CategoryRequest request, CancellationToken ct)
    {
        var category = await _context.Categories.FindAsync([id], ct);
        if (category is null)
            return NotFound();

        category.Name = request.Name.Trim();
        await SaveCategory(ct);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var category = await _context.Categories.FindAsync([id], ct);
        if (category is null)
            return NotFound();

        _context.Categories.Remove(category);
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        {
            throw new ApiError(409, "This category is used by one or more events.");
        }
        return NoContent();
    }

    private async Task SaveCategory(CancellationToken ct)
    {
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ApiError(409, "A category with this name already exists.");
        }
    }
}
