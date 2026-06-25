using Brittany_Salon_Backend.Application.DTOs.Report;
using Brittany_Salon_Backend.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Brittany_Salon_Backend.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        { 
            _reportService = reportService;
        }

        /// Obtiene los top 5 clientes más fieles (con más citas completadas)
        [HttpGet("top-5-loyal-clients")]
        [ProducesResponseType(typeof(List<TopClientDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<TopClientDto>>> GetTop5LoyalClients()
        {
            try
            {
                var topClients = await _reportService.GetTop5LoyalClientsAsync();
                return Ok(topClients);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error al obtener los clientes más fieles.",
                    detail = ex.Message
                });
            }
        }

        /// Obtiene los productos más vendidos ordenados por cantidad vendida 
        [HttpGet("top-selling-products")]
        [ProducesResponseType(typeof(List<TopProductDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<TopProductDto>>> GetTopSellingProducts()
        {
            try
            {
                var topProducts = await _reportService.GetTopSellingProductsAsync();
                return Ok(topProducts);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error al obtener los productos más vendidos.",
                    detail = ex.Message
                });
            }
        }

        /// Obtiene todos los pagos del día actual o de una fecha específica
        [HttpGet("daily-payments")]
        [ProducesResponseType(typeof(List<DailyPaymentDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DailyPaymentDto>>> GetDailyPayments([FromQuery] DateTime? date = null)
        {
            try
            {
                var payments = await _reportService.GetDailyPaymentsAsync(date);
                return Ok(payments);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error al obtener los pagos del día.",
                    detail = ex.Message
                });
            }
        }

        /// Obtiene el total de pagos del día
        [HttpGet("daily-total")]
        [ProducesResponseType(typeof(DailyTotalDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DailyTotalDto>> GetDailyTotal([FromQuery] DateTime? date = null)
        {
            try
            {
                var dailyTotal = await _reportService.GetDailyTotalAsync(date);

                if (dailyTotal == null)
                    return NotFound(new { message = "No hay pagos registrados para esa fecha." });

                return Ok(dailyTotal);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error al obtener el total de pagos del día.",
                    detail = ex.Message
                });
            }
        }

        /// Obtiene los ingresos mensuales por año
        [HttpGet("monthly-revenue")]
        [ProducesResponseType(typeof(List<MonthlyRevenueDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<MonthlyRevenueDto>>> GetMonthlyRevenue([FromQuery] int? year = null)
        {
            try
            {
                var monthlyRevenue = await _reportService.GetMonthlyRevenueAsync(year);
                return Ok(monthlyRevenue);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error al obtener los ingresos mensuales.",
                    detail = ex.Message
                });
            }
        }

        /// Obtiene resumen general del negocio 
        [HttpGet("business-summary")]
        [ProducesResponseType(typeof(BusinessSummaryDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<BusinessSummaryDto>> GetBusinessSummary()
        {
            try
            {
                var summary = await _reportService.GetBusinessSummaryAsync();
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error al obtener el resumen del negocio.",
                    detail = ex.Message
                });
            }
        }

        /// Obtiene los servicios top filtrados por citas completadas o ingresos
        [HttpGet("top-services")]
        [ProducesResponseType(typeof(List<TopServiceDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<TopServiceDto>>> GetTopServices(
            [FromQuery] int top = 5, 
            [FromQuery] string orderBy = "appointments", 
            [FromQuery] int? year = null)
        {
            try
            {
                if (top <= 0 || top > 50)
                    return BadRequest(new { message = "El parámetro 'top' debe estar entre 1 y 50." });

                if (orderBy != "appointments" && orderBy != "revenue")
                    return BadRequest(new { message = "El parámetro 'orderBy' debe ser 'appointments' o 'revenue'." });

                if (year.HasValue && (year.Value < 2000 || year.Value > DateTime.Now.Year + 1))
                    return BadRequest(new { message = "El año debe estar entre 2000 y el próximo año." });

                var topServices = await _reportService.GetTopServicesAsync(top, orderBy, year);
                return Ok(topServices);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error al obtener los servicios top.",
                    detail = ex.Message
                });
            }
        }
    }
}
