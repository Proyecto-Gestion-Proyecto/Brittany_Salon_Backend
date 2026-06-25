using Brittany_Salon_Backend.Application.DTOs.Category;
using Brittany_Salon_Backend.Application.Exceptions;
using Brittany_Salon_Backend.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Brittany_Salon_Backend.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // GET: api/categories?onlyActive=true|false
        [HttpGet]
        public async Task<ActionResult<List<CategoryReadDto>>> GetAll([FromQuery] bool onlyActive = true)
        {
            var result = await _categoryService.GetAllAsync(onlyActive);
            return Ok(result);
        }

        // GET: api/categories/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<CategoryReadDto>> GetById(int id)
        {
            var result = await _categoryService.GetByIdAsync(id);

            if (result is null)
                return NotFound(new { message = "Categoría no encontrada." });

            return Ok(result);
        }

        // POST: api/categories
        [HttpPost]
        public async Task<ActionResult<CategoryReadDto>> Create([FromBody] CategoryCreateDto dto)
        {
            try
            {
                var created = await _categoryService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.CategoryId }, created);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new
                {
                    message = "Errores de validación",
                    errors = ex.Errors
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        // PUT: api/categories/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<CategoryReadDto>> Update(int id, [FromBody] CategoryUpdateDto dto)
        {
            try
            {
                var updated = await _categoryService.UpdateAsync(id, dto);

                if (updated is null)
                    return NotFound(new { message = "Categoría no encontrada." });

                return Ok(updated);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new
                {
                    message = "Errores de validación",
                    errors = ex.Errors
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        // PATCH: api/categories/{id}/deactivate
        [HttpPatch("{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {
            try
            {
                var ok = await _categoryService.DeactivateAsync(id);

                if (!ok)
                    return NotFound(new { message = "Categoría no encontrada." });

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        // PATCH: api/categories/{id}/reactivate
        [HttpPatch("{id:int}/reactivate")]
        public async Task<IActionResult> Reactivate(int id)
        {
            var ok = await _categoryService.ReactivateAsync(id);

            if (!ok)
                return NotFound(new { message = "Categoría no encontrada." });

            return NoContent();
        }
    }
}
