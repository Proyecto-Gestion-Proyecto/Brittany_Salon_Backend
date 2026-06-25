using Brittany_Salon_Backend.Application.DTOs.Product;
using Brittany_Salon_Backend.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Brittany_Salon_Backend.Application.Exceptions;

namespace Brittany_Salon_Backend.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        public ProductsController(IProductService productService)
        {
            _productService = productService;
        } 


        // POST: api/products
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ProductReadDto>> Create([FromForm] ProductCreateDto dto)
        {
            try
            {
                var created = await _productService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.ProductId }, created);
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




        // GET: api/products/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductReadDto>> GetById(int id)
        {
            var result = await _productService.GetByIdAsync(id);

            if (result is null)
                return NotFound("Producto no encontrado.");

            return Ok(result);
        }

       
        // GET: api/products
        [HttpGet]
        public async Task<ActionResult<List<ProductReadDto>>> GetAll([FromQuery] bool? onlyActive)
        {
            var result = await _productService.GetAllAsync(onlyActive);
            return Ok(result);
        }

        // GET: api/products/search
        [HttpGet("search")]
        public async Task<ActionResult<List<ProductReadDto>>> SearchByName(
            [FromQuery] string name,
            [FromQuery] bool? onlyActive)
        {
            var result = await _productService.SearchByNameAsync(name, onlyActive);
            return Ok(result);
        }


        // PUT: api/products/{id}
        [HttpPut("{id:int}")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ProductReadDto>> Update(int id, [FromForm] ProductUpdateDto dto)
        {
            try
            {
                var updated = await _productService.UpdateAsync(id, dto);

                if (updated is null)
                    return NotFound(new { message = "Producto no encontrado." });

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


        // PATCH: api/products/{id}/deactivate
        [HttpPatch("{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var ok = await _productService.DeactivateAsync(id);

            if (!ok)
                return NotFound("Producto no encontrado.");

            return NoContent();
        }

        // PATCH: api/products/{id}/reactivate
        [HttpPatch("{id:int}/reactivate")]
        public async Task<IActionResult> Reactivate(int id)
        {
            var ok = await _productService.ReactivateAsync(id);

            if (!ok)
                return NotFound("Producto no encontrado.");

            return NoContent();
        }
    }
}
