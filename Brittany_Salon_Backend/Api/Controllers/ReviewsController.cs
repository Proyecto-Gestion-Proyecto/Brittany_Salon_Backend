using Brittany_Salon_Backend.Application.DTOs.Review;
using Brittany_Salon_Backend.Application.Exceptions;
using Brittany_Salon_Backend.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Brittany_Salon_Backend.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }
      
      //Obtener todas las reseñas
        [HttpGet]
        [ProducesResponseType(typeof(List<ReviewReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ReviewReadDto>>> GetAll()
        {
            var reviews = await _reviewService.GetAllAsync();
            return Ok(reviews);
        }

        //Obtener reseñas por ID de cliente
        [HttpGet("by-client/{clientId:int}")]
        [ProducesResponseType(typeof(List<ReviewReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<ReviewReadDto>>> GetByClientId(int clientId)
        {
            var reviews = await _reviewService.GetByClientIdAsync(clientId);
            if (reviews == null || reviews.Count == 0)
                return NotFound($"No se encontraron reseñas para el cliente con ID {clientId}.");

            return Ok(reviews);
        }

        //Obtener reseña específica de un cliente
        [HttpGet("by-client/{clientId:int}/review/{reviewId:int}")]
        [ProducesResponseType(typeof(ReviewReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ReviewReadDto>> GetByClientAndReviewId(int clientId, int reviewId)
        {
            var review = await _reviewService.GetByClientAndReviewIdAsync(clientId, reviewId);
            if (review == null)
                return NotFound(new { message = $"Reseña con ID {reviewId} no encontrada para el cliente con ID {clientId}." });

            return Ok(review);
        }

     //Crear una reseña
        [HttpPost]
        [ProducesResponseType(typeof(ReviewReadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ReviewReadDto>> Create([FromBody] ReviewCreateDto reviewCreateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdReview = await _reviewService.CreateAsync(reviewCreateDto);
                return CreatedAtAction(nameof(GetAll), new { id = createdReview.ReviewId }, createdReview);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
        }

        //Eliminar respuesta de una reseña (Empleado elimina su respuesta)
        [HttpDelete("{reviewId:int}/response")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteResponse(int reviewId)
        {
            try
            {
                var result = await _reviewService.DeleteResponseAsync(reviewId);
                if (!result)
                    return NotFound(new { message = "Reseña no encontrada o sin respuesta." });

                return Ok(new { message = "Respuesta eliminada exitosamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        //Agregar respuesta a una reseña
        [HttpPut("{reviewId:int}/response")]
        [ProducesResponseType(typeof(ReviewReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ReviewReadDto>> AddResponse(int reviewId, [FromBody] ReviewResponseDto responseDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var updated = await _reviewService.AddResponseAsync(reviewId, responseDto);
                if (updated == null)
                    return NotFound(new { message = "Reseña no encontrada." });

                return Ok(updated);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        //Actualizar una reseña (Cliente actualiza su calificación y comentario)
        [HttpPut("{reviewId:int}/client-update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateClient(int reviewId, [FromQuery] int clientId, [FromBody] ReviewClientUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _reviewService.UpdateAsync(reviewId, clientId, new ReviewUpdateDto { Comment = updateDto.Comment, Rating = updateDto.Rating });
                if (!result)
                    return NotFound(new { message = "Reseña no encontrada." });

                return Ok(new { message = "Reseña actualizada exitosamente." });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        //Actualizar respuesta de una reseña (Empleado actualiza  respuesta)
        [HttpPut("{reviewId:int}/employee-update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEmployee(int reviewId, [FromBody] ReviewEmployeeUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Los empleados pueden actualizar la respuesta sin validación de clientId
                // Se obtiene el clientId de la reseña actual
                var review = await _reviewService.GetByIdAsync(reviewId);
                if (review == null)
                    return NotFound(new { message = "Reseña no encontrada." });

                var result = await _reviewService.UpdateAsync(reviewId, review.ClientId, new ReviewUpdateDto { Response = updateDto.Response });
                if (!result)
                    return NotFound(new { message = "Reseña no encontrada." });

                return Ok(new { message = "Respuesta actualizada exitosamente." });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        //Eliminar una reseña
        [HttpDelete("{reviewId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Delete(int reviewId, [FromQuery] int clientId)
        {
            try
            {
                var result = await _reviewService.DeleteAsync(reviewId, clientId);
                if (!result)
                    return NotFound(new { message = "Reseña no encontrada." });

                return Ok(new { message = "Reseña eliminada exitosamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
