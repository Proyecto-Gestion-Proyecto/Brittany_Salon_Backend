using Brittany_Salon_Backend.Application.DTOs.Payment;
using Brittany_Salon_Backend.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Brittany_Salon_Backend.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Obtiene todos los pagos
        /// </summary>
        /// <returns>Lista de pagos</returns>
        /// <response code="200">Lista de pagos</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<PaymentReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PaymentReadDto>>> GetAll()
        {
            var payments = await _paymentService.GetAllAsync();
            return Ok(payments);
        }

        /// <summary>
        /// Obtiene todos los pagos incluyendo inactivos
        /// </summary>
        /// <returns>Lista de todos los pagos</returns>
        /// <response code="200">Lista de pagos</response>
        [HttpGet("all")]
        [ProducesResponseType(typeof(List<PaymentReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PaymentReadDto>>> GetAllWithInactive()
        {
            var payments = await _paymentService.GetAllWithInactiveAsync();
            return Ok(payments);
        }

        /// <summary>
        /// Obtiene un pago por su ID
        /// </summary>
        /// <param name="id">ID del pago</param>
        /// <returns>Pago encontrado</returns>
        /// <response code="200">Pago encontrado</response>
        /// <response code="404">Pago no encontrado</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(PaymentReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PaymentReadDto>> GetById(int id)
        {
            var payment = await _paymentService.GetByIdAsync(id);

            if (payment == null)
                return NotFound(new { message = "Pago no encontrado." });

            return Ok(payment);
        }

        /// <summary>
        /// Obtiene pagos por ID de cita
        /// </summary>
        /// <param name="appointmentId">ID de la cita</param>
        /// <returns>Lista de pagos para la cita</returns>
        /// <response code="200">Lista de pagos</response>
        [HttpGet("by-appointment/{appointmentId:int}")]
        [ProducesResponseType(typeof(List<PaymentReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PaymentReadDto>>> GetByAppointmentId(int appointmentId)
        {
            var payments = await _paymentService.GetByAppointmentIdAsync(appointmentId);
            return Ok(payments);
        }

        /// <summary>
        /// Obtiene pagos por ID de cliente
        /// </summary>
        /// <param name="clientId">ID del cliente</param>
        /// <returns>Lista de pagos para el cliente</returns>
        /// <response code="200">Lista de pagos</response>
        [HttpGet("by-client/{clientId:int}")]
        [ProducesResponseType(typeof(List<PaymentReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PaymentReadDto>>> GetByClientId(int clientId)
        {
            var payments = await _paymentService.GetByClientIdAsync(clientId);
            return Ok(payments);
        }

        /// <summary>
        /// Obtiene pagos por rango de fechas
        /// </summary>
        /// <param name="startDate">Fecha de inicio</param>
        /// <param name="endDate">Fecha de fin</param>
        /// <returns>Lista de pagos en el rango</returns>
        /// <response code="200">Lista de pagos</response>
        [HttpGet("by-date")]
        [ProducesResponseType(typeof(List<PaymentReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PaymentReadDto>>> GetByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var payments = await _paymentService.GetByDateRangeAsync(startDate, endDate);
            return Ok(payments);
        }

        /// <summary>
        /// Obtiene pagos por estado
        /// </summary>
        /// <param name="active">Estado activo (true) o inactivo (false)</param>
        /// <returns>Lista de pagos con el estado</returns>
        /// <response code="200">Lista de pagos</response>
        [HttpGet("by-status")]
        [ProducesResponseType(typeof(List<PaymentReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PaymentReadDto>>> GetByStatus([FromQuery] bool active)
        {
            var payments = await _paymentService.GetByStatusAsync(active);
            return Ok(payments);
        }

        /// <summary>
        /// Registra un nuevo pago
        /// </summary>
        /// <param name="dto">Datos del pago a registrar</param>
        /// <returns>Pago creado</returns>
        /// <response code="201">Pago creado exitosamente</response>
        /// <response code="400">Errores de validaci�n</response>
        [HttpPost]
        [ProducesResponseType(typeof(PaymentReadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PaymentReadDto>> Create([FromBody] PaymentCreateDto dto)
        {
            try
            {
                var created = await _paymentService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.PaymentId }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Registra un nuevo pago y reduce el saldo pendiente del cliente
        /// </summary>
        /// <param name="dto">Datos del pago a registrar</param>
        /// <returns>Pago creado</returns>
        /// <response code="201">Pago creado exitosamente</response>
        /// <response code="400">Errores de validaci�n</response>
        [HttpPost("create-and-reduce-balance")]
        [ProducesResponseType(typeof(PaymentReadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PaymentReadDto>> CreateAndReduceBalance([FromBody] PaymentCreateDto dto)
        {
            try
            {
                var created = await _paymentService.CreateAndReduceBalanceAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.PaymentId }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Actualiza un pago existente
        /// </summary>
        /// <param name="id">ID del pago a actualizar</param>
        /// <param name="dto">Datos a actualizar</param>
        /// <returns>NoContent si se actualiz� correctamente</returns>
        /// <response code="204">Pago actualizado exitosamente</response>
        /// <response code="404">Pago no encontrado</response>
        /// <response code="400">Errores de validaci�n</response>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] PaymentUpdateDto dto)
        {
            try
            {
                var updated = await _paymentService.UpdateAsync(id, dto);
                if (!updated)
                    return NotFound(new { message = "Pago no encontrado." });

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Desactiva un pago (eliminaci�n l�gica)
        /// </summary>
        /// <param name="id">ID del pago a desactivar</param>
        /// <returns>NoContent si se desactiv� correctamente</returns>
        /// <response code="204">Pago desactivado exitosamente</response>
        /// <response code="404">Pago no encontrado</response>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _paymentService.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = "Pago no encontrado." });

            return NoContent();
        }

        /// <summary>
        /// Obtiene todos los pagos ordenados por fecha descendente
        /// </summary>
        /// <returns>Lista de pagos ordenados por fecha</returns>
        /// <response code="200">Lista de pagos</response>
        [HttpGet("ordered-by-date")]
        [ProducesResponseType(typeof(List<PaymentReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PaymentReadDto>>> GetOrderedByDate()
        {
            var payments = await _paymentService.GetAllWithInactiveAsync();
            return Ok(payments);
        }
    }
}