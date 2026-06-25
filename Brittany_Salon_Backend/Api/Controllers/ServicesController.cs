using Brittany_Salon_Backend.Application.DTOs.Service;
using Brittany_Salon_Backend.Application.Services.Interfaces;
using Brittany_Salon_Backend.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Brittany_Salon_Backend.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServicesController : ControllerBase
    {
        private readonly IServiceService _serviceService;

        public ServicesController(IServiceService serviceService)
        {
            _serviceService = serviceService;
        }

        [HttpGet]
        public async Task<ActionResult<List<ServiceReadDto>>> GetAll([FromQuery] bool onlyActive = false)
        {
            var result = await _serviceService.GetAllAsync(onlyActive);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ServiceReadDto>> GetById(int id)
        {
            var result = await _serviceService.GetByIdAsync(id);
            if (result is null) return NotFound("Servicio no encontrado.");

            return Ok(result);
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10_000_000)]
        public async Task<ActionResult<ServiceReadDto>> Create([FromForm] ServiceCreateDto dto)
        {
            try
            {
                var created = await _serviceService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.ServiceId }, created);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpPut("{id:int}")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> Update(int id, [FromForm] ServiceUpdateDto dto)
        {
            try
            {
                var updated = await _serviceService.UpdateAsync(id, dto);
                if (!updated) return NotFound("Servicio no encontrado.");

                return NoContent();
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {
            try 
            {
                var ok = await _serviceService.DeactivateAsync(id);
                if (!ok) return NotFound("Servicio no encontrado.");
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
           
        }
        [HttpGet("search")]
        public async Task<ActionResult<List<ServiceReadDto>>> SearchByName(
        [FromQuery] string name,
        [FromQuery] bool onlyActive = false)
        {
            var result = await _serviceService.SearchByNameAsync(name, onlyActive);
            return Ok(result);
        }

        [HttpPatch("{id:int}/reactivate")]
        public async Task<IActionResult> Reactivate(int id)
        {
            var ok = await _serviceService.ReactivateAsync(id);
            if (!ok) return NotFound("Servicio no encontrado.");

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePermanently(int id)
        {
            try 
            {
                var ok = await _serviceService.DeletePermanentlyAsync(id);
                if (!ok) return NotFound("Servicio no encontrado.");
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }
        [HttpGet("featured")]
        public async Task<ActionResult<List<FeaturedServiceReadDto>>> GetFeatured(
        [FromQuery] int top = 5,
        [FromQuery] bool onlyActive = true)
        {
            var result = await _serviceService.GetFeaturedAsync(top, onlyActive);
            return Ok(result);
        }



    }
}
