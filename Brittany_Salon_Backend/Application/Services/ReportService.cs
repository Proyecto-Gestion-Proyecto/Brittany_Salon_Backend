using Brittany_Salon_Backend.Application.DTOs.Report;
using Brittany_Salon_Backend.Application.Services.Interfaces;
using Brittany_Salon_Backend.Domain.Constants;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Brittany_Salon_Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Brittany_Salon_Backend.Application.Services
{
    public class ReportService : IReportService
    {
        private readonly AppDbContext _db;
        private readonly IDevLogger _logger;

        public ReportService(AppDbContext db, IDevLogger logger)
        {
            _db = db;
            _logger = logger;
        }

        ///Top 5 clientes 
        public async Task<List<TopClientDto>> GetTop5LoyalClientsAsync()
        {
            _logger.LogInfo("Obteniendo top 5 clientes más fieles");

            var topClients = await _db.Appointments
                .AsNoTracking()
                .Where(a => a.AppointmentStatus == AppointmentStatuses.Finalized ||
                           a.AppointmentStatus == AppointmentStatuses.CompletedPendingPayment)
                .GroupBy(a => new
                {
                    a.ClientId,
                    a.Client.Name,
                    a.Client.Email,
                    a.Client.Phone
                })
                .Select(g => new TopClientDto
                {
                    ClientId = g.Key.ClientId,
                    Name = g.Key.Name,
                    Email = g.Key.Email,
                    Phone = g.Key.Phone,
                    CompletedAppointments = g.Count()
                })
                .OrderByDescending(c => c.CompletedAppointments)
                .Take(5)
                .ToListAsync();

            _logger.LogInfo("Se encontraron {Count} clientes en el top 5", topClients.Count);
            return topClients;
        }

        /// Productos ordenados del mas venidido al menos
        public async Task<List<TopProductDto>> GetTopSellingProductsAsync()
        {
            _logger.LogInfo("Obteniendo productos más vendidos");

            var topProducts = await _db.AppointmentProducts
                .AsNoTracking()
                .Include(ap => ap.Product)
                .Where(ap => ap.Appointment.AppointmentStatus == AppointmentStatuses.Finalized ||
                            ap.Appointment.AppointmentStatus == AppointmentStatuses.CompletedPendingPayment)
                .GroupBy(ap => new
                {
                    ap.ProductId,
                    ap.Product.ProductName,
                    ap.Product.Price
                })
                .Select(g => new TopProductDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    Price = g.Key.Price,
                    TotalSold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.Quantity) * g.Key.Price
                })
                .OrderByDescending(p => p.TotalSold)
                .ToListAsync();

            _logger.LogInfo("Se encontraron {Count} productos vendidos", topProducts.Count);
            return topProducts;
        }

        /// Pagos diarios
        public async Task<List<DailyPaymentDto>> GetDailyPaymentsAsync(DateTime? date = null)
        {
            var targetDate = (date ?? DateTime.Now).Date;
            _logger.LogInfo("Obteniendo pagos del día: {Date}", targetDate);

            var payments = await _db.Payments
                .AsNoTracking()
                .Include(p => p.Appointment)
                .ThenInclude(a => a.Client)
                .Where(p => p.PaymentDate.Date == targetDate && p.IsActive)
                .Select(p => new DailyPaymentDto
                {
                    PaymentId = p.PaymentId,
                    AppointmentId = p.AppointmentId,
                    ClientId = p.Appointment.ClientId,
                    ClientName = p.Appointment.Client.Name,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    PaymentMethod = p.PaymentMethod ?? "N/A",
                    PaymentStatus = p.PaymentStatus ?? "N/A"
                })
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            _logger.LogInfo("Se encontraron {Count} pagos para la fecha {Date}", payments.Count, targetDate);
            return payments;
        }

        ///Total Pagos diarios
        public async Task<DailyTotalDto?> GetDailyTotalAsync(DateTime? date = null)
        {
            var targetDate = (date ?? DateTime.Now).Date;
            _logger.LogInfo("Obteniendo total de pagos del día: {Date}", targetDate);

            var dailyTotal = await _db.Payments
                .AsNoTracking() 
                .Where(p => p.PaymentDate.Date == targetDate && p.IsActive)
                .GroupBy(p => p.PaymentDate.Date)
                .Select(g => new DailyTotalDto
                {
                    Date = g.Key,
                    TotalPayments = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .FirstOrDefaultAsync();

            if (dailyTotal != null)
            {
                _logger.LogInfo("Total de pagos para {Date}: {Count} pagos, ${Total}", 
                    targetDate, dailyTotal.TotalPayments, dailyTotal.TotalAmount);
            }
            else
            {
                _logger.LogInfo("No hay pagos para la fecha {Date}", targetDate);
            }

            return dailyTotal;
        }

        /// Ingresos mensuales en un año 
        public async Task<List<MonthlyRevenueDto>> GetMonthlyRevenueAsync(int? year = null)
        {
            var targetYear = year ?? DateTime.Now.Year;
            _logger.LogInfo("Obteniendo ingresos mensuales para el año: {Year}", targetYear);

            var monthlyRevenue = await _db.Payments
                .AsNoTracking()
                .Where(p => p.PaymentDate.Year == targetYear && p.IsActive)
                .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                .Select(g => new MonthlyRevenueDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthName = GetMonthName(g.Key.Month),
                    TotalPayments = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount),
                    AveragePayment = g.Average(p => p.Amount)
                })
                .OrderBy(m => m.Month)
                .ToListAsync();

            _logger.LogInfo("Se encontraron ingresos para {Count} meses del año {Year}", monthlyRevenue.Count, targetYear);
            return monthlyRevenue;
        }

        private static string GetMonthName(int month)
        {
            return month switch
            {
                1 => "Enero",
                2 => "Febrero", 
                3 => "Marzo",
                4 => "Abril",
                5 => "Mayo",
                6 => "Junio",
                7 => "Julio",
                8 => "Agosto",
                9 => "Septiembre",
                10 => "Octubre",
                11 => "Noviembre",
                12 => "Diciembre",
                _ => "Desconocido"
            };
        }

        
        public async Task<BusinessSummaryDto> GetBusinessSummaryAsync()
        {
            _logger.LogInfo("Obteniendo resumen general del negocio");

            var totalRevenue = await _db.Payments
                .AsNoTracking()
                .Where(p => p.IsActive)
                .SumAsync(p => p.Amount);

            var completedAppointments = await _db.Appointments
                .AsNoTracking()
                .CountAsync(a => a.AppointmentStatus == AppointmentStatuses.Finalized ||
                                a.AppointmentStatus == AppointmentStatuses.CompletedPendingPayment);

          
            var activeClients = await _db.Clients
                .AsNoTracking()
                .CountAsync(c => c.IsActive);

            var activeServices = await _db.Services
                .AsNoTracking()
                .CountAsync(s => s.IsActive);

            var summary = new BusinessSummaryDto
            {
                TotalRevenue = totalRevenue,
                CompletedAppointments = completedAppointments,
                ActiveClients = activeClients,
                ActiveServices = activeServices
            };

            _logger.LogInfo("Resumen del negocio - Ingresos: ${Revenue}, Citas completadas: {Appointments}, Clientes activos: {Clients}, Servicios activos: {Services}",
                summary.TotalRevenue, summary.CompletedAppointments, summary.ActiveClients, summary.ActiveServices);

            return summary;
        }


        public async Task<List<TopServiceDto>> GetTopServicesAsync(int top = 5, string orderBy = "appointments", int? year = null)
        {
            _logger.LogInfo("Obteniendo top {Top} servicios ordenados por {OrderBy}, año: {Year}", top, orderBy, year?.ToString() ?? "todos");

            try
            {
                var appointmentsQuery = _db.Appointments
                    .AsNoTracking()
                    .Where(a => a.AppointmentStatus == AppointmentStatuses.Finalized ||
                                a.AppointmentStatus == AppointmentStatuses.CompletedPendingPayment);

                if (year.HasValue)
                {
                    appointmentsQuery = appointmentsQuery.Where(a => a.AppointmentDate.Year == year.Value);
                }

                var serviceData = await appointmentsQuery
                    .SelectMany(a => a.AppointmentServices)
                    .GroupBy(aps => new
                    {
                        aps.ServiceId,
                        aps.Service.ServiceName
                    })
                    .Select(g => new TopServiceDto
                    {
                        ServiceId = g.Key.ServiceId,
                        ServiceName = g.Key.ServiceName,
                        CompletedAppointments = g.Count(),
                        TotalRevenue = g.Sum(x => x.ServicePrice ?? 0),
                        TotalMinutes = g.Sum(x => x.Service.DurationMinutes) 
                    })
                    .ToListAsync();

                var orderedResult = orderBy.ToLower() switch
                {
                    "revenue" => serviceData.OrderByDescending(s => s.TotalRevenue),
                    "appointments" => serviceData.OrderByDescending(s => s.CompletedAppointments),
                    _ => serviceData.OrderByDescending(s => s.CompletedAppointments)
                };

                var finalResult = orderedResult.Take(top).ToList();

                _logger.LogInfo("Se encontraron {Count} servicios en el top {Top}", finalResult.Count, top);
                return finalResult;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error al obtener top servicios: {Error}", ex.Message);
                throw;
            }
        }
    }
}
