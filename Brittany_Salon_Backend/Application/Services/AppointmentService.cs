using Brittany_Salon_Backend.Application.DTOs.Appointment;
using Brittany_Salon_Backend.Application.Exceptions;
using Brittany_Salon_Backend.Application.Services.Interfaces;
using Brittany_Salon_Backend.Application.Validators;
using Brittany_Salon_Backend.Domain.Constants;
using Brittany_Salon_Backend.Domain.Entities;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Brittany_Salon_Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using AppointmentServiceEntity = Brittany_Salon_Backend.Domain.Entities.AppointmentService;

namespace Brittany_Salon_Backend.Application.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly AppDbContext _db;
        private readonly IDevLogger _logger;

        public AppointmentService(AppDbContext db, IDevLogger logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<int> CreateAsync(AppointmentCreateDto dto)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            var validationErrors = AppointmentValidator.ValidateCreate(dto, _logger);
            if (validationErrors.Count > 0)
                throw new ValidationException(validationErrors);

            var serviceIds = dto.Services
                .Select(s => s.ServiceId)
                .Distinct()
                .ToList();

            var services = await _db.Services
                .Where(s => serviceIds.Contains(s.ServiceId))
                .ToListAsync();

            if (services.Count != serviceIds.Count)
                throw new InvalidOperationException("Uno o más servicios no existen.");

            var hairServiceCount = services.Count(s =>
                !string.IsNullOrWhiteSpace(s.ServiceType) &&
                s.ServiceType.Trim().ToLower() == "cabello"
            );

            if (hairServiceCount > 0 && (!dto.HairLengthOption.HasValue || dto.HairLengthOption.Value <= 0))
                throw new InvalidOperationException("Debe seleccionar el largo del cabello para servicios de tipo 'cabello'.");

            var appointment = new Appointment
            {
                AppointmentDate = dto.AppointmentDate,
                StartTime = dto.StartTime,
                EndTime = dto.StartTime,
                AppointmentStatus = string.IsNullOrWhiteSpace(dto.AppointmentStatus) 
                    ? AppointmentStatuses.Pending 
                    : dto.AppointmentStatus,
                ClientId = dto.ClientId,
                IsActive = true,
                HairLengthOption = hairServiceCount > 0 ? dto.HairLengthOption : null
            };

            _db.Appointments.Add(appointment);
            await _db.SaveChangesAsync();

            decimal totalCost = 0m;

            foreach (var service in services)
            {
                _db.AppointmentServices.Add(new AppointmentServiceEntity
                {
                    AppointmentId = appointment.AppointmentId,
                    ServiceId = service.ServiceId,
                    ServicePrice = service.Price
                });

                totalCost += service.Price;
            }

            var baseDurationMinutes = services.Sum(s => s.DurationMinutes);

            var hairOption = hairServiceCount > 0 ? dto.HairLengthOption!.Value : 0;

            decimal hairCostPerService = hairOption switch
            {
                0 => 0m,
                1 => 5000m,
                2 => 10000m,
                3 => 15000m,
                4 => 20000m,
                5 => 25000m,
                6 => 30000m,
                7 => 35000m,
                8 => 40000m,
                9 => 45000m,
                _ => throw new InvalidOperationException("Opción de largo de pelo inválida.")
            };

            var hairExtraMinutesPerService = hairOption == 0 ? 0 : hairOption * 10;

            totalCost += hairCostPerService * hairServiceCount;

            var totalDurationMinutes = baseDurationMinutes + (hairExtraMinutesPerService * hairServiceCount);
            appointment.EndTime = appointment.StartTime.AddMinutes(totalDurationMinutes);

            var availability = await ValidateAvailabilityAsync(new AppointmentAvailabilityRequestDto
            {
                StartTime = appointment.StartTime,
                EndTime = appointment.EndTime,
                ServiceIds = serviceIds
            });

            if (!availability.IsAvailable)
                throw new InvalidOperationException(availability.Message);

            if (dto.Products != null && dto.Products.Count > 0)
            {
                var normalizedProducts = dto.Products
                    .Where(p => p.ProductId > 0)
                    .GroupBy(p => p.ProductId)
                    .Select(g => new
                    {
                        ProductId = g.Key,
                        Quantity = g.Sum(x => x.Quantity <= 0 ? 1 : x.Quantity)
                    })
                    .ToList();

                var productIds = normalizedProducts.Select(x => x.ProductId).ToList();

                var products = await _db.Products
                    .Where(p => productIds.Contains(p.ProductId))
                    .ToDictionaryAsync(p => p.ProductId, p => p);

                if (products.Count != productIds.Count)
                    throw new InvalidOperationException("Uno o más productos no existen.");

                
                var inventories = await _db.Inventory
                    .Where(i => productIds.Contains(i.ProductId) && i.IsActive)
                    .ToDictionaryAsync(i => i.ProductId, i => i);

                
                if (inventories.Count != productIds.Count)
                    throw new InvalidOperationException("Uno o más productos no tienen inventario activo.");

                
                foreach (var item in normalizedProducts)
                {
                    var inv = inventories[item.ProductId];

                    if (item.Quantity > inv.Quantity)
                        throw new InvalidOperationException(
                            $"Stock insuficiente para \"{inv.Product.ProductName}\". Disponible: {inv.Quantity}, solicitado: {item.Quantity}"
                        );
                }


                foreach (var item in normalizedProducts)
                {
                    var product = products[item.ProductId];

                    _db.AppointmentProducts.Add(new AppointmentProduct
                    {
                        AppointmentId = appointment.AppointmentId,
                        ProductId = product.ProductId,
                        Quantity = item.Quantity
                    });

                    totalCost += product.Price * item.Quantity;
                }
            }

            appointment.TotalCost = totalCost;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return appointment.AppointmentId;
        }


        public async Task<List<AppointmentReadDto>> GetAllAsync()
        {
            return await _db.Appointments
                .AsNoTracking()
                .Include(a => a.Client)
                .OrderByDescending(a => a.StartTime)
                .Select(a => new AppointmentReadDto
                {
                    AppointmentId = a.AppointmentId,
                    AppointmentDate = a.AppointmentDate,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    AppointmentStatus = a.AppointmentStatus,
                    TotalCost = a.TotalCost,
                    IsActive = a.IsActive,
                    ClientId = a.ClientId,
                    ClientName = a.Client.Name
                })
                .ToListAsync();
        }

        public async Task<List<AppointmentReadDto>> GetByDateAsync(DateTime date)
        {
            var onlyDate = date.Date;

            return await _db.Appointments
                .AsNoTracking()
                .Include(a => a.Client)
                .Where(a => a.AppointmentDate == onlyDate)
                .OrderBy(a => a.StartTime)
                .Select(a => new AppointmentReadDto
                {
                    AppointmentId = a.AppointmentId,
                    AppointmentDate = a.AppointmentDate,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    AppointmentStatus = a.AppointmentStatus,
                    TotalCost = a.TotalCost,
                    IsActive = a.IsActive,
                    ClientId = a.ClientId,
                    ClientName = a.Client.Name,
                    HairLengthOption = a.HairLengthOption
                })
                .ToListAsync();
        }

        public async Task<List<AppointmentReadDto>> GetByStatusAsync(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return new List<AppointmentReadDto>();

            var normalized = status.Trim().ToLower();

            return await _db.Appointments
                .AsNoTracking()
                .Include(a => a.Client)
                .Where(a => a.AppointmentStatus != null && a.AppointmentStatus.Trim().ToLower() == normalized)
                .OrderByDescending(a => a.StartTime)
                .Select(a => new AppointmentReadDto
                {
                    AppointmentId = a.AppointmentId,
                    AppointmentDate = a.AppointmentDate,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    AppointmentStatus = a.AppointmentStatus,
                    TotalCost = a.TotalCost,
                    IsActive = a.IsActive,
                    ClientId = a.ClientId,
                    ClientName = a.Client.Name,
                    HairLengthOption = a.HairLengthOption
                })
                .ToListAsync();
        }

        public async Task<List<AppointmentReadDto>> GetByClientIdAsync(int clientId)
        {
            if (clientId <= 0)
                throw new InvalidOperationException("ClientId inválido.");

            return await _db.Appointments
                .AsNoTracking()
                .Include(a => a.Client)
                .Where(a => a.ClientId == clientId)
                .OrderByDescending(a => a.StartTime)
                .Select(a => new AppointmentReadDto
                {
                    AppointmentId = a.AppointmentId,
                    AppointmentDate = a.AppointmentDate,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    AppointmentStatus = a.AppointmentStatus,
                    TotalCost = a.TotalCost,
                    IsActive = a.IsActive,
                    ClientId = a.ClientId,
                    ClientName = a.Client.Name,
                    HairLengthOption = a.HairLengthOption
                })
                .ToListAsync();
        }

        public async Task<bool> UpdatePendingAsync(int appointmentId, AppointmentUpdateDto dto)
        {
            if (appointmentId <= 0)
                throw new InvalidOperationException("AppointmentId inválido.");

            var validationErrors = AppointmentValidator.ValidateUpdate(dto, _logger);
            if (validationErrors.Count > 0)
                throw new ValidationException(validationErrors);

            await using var tx = await _db.Database.BeginTransactionAsync();

            var appointment = await _db.Appointments
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment is null) return false;

            if (!AppointmentStatuses.CanBeEdited(appointment.AppointmentStatus))
                throw new InvalidOperationException("Solo se puede editar una cita con estado Pendiente o Confirmada.");

            var serviceIds = dto.Services
                .Select(s => s.ServiceId)
                .Distinct()
                .ToList();

            if (serviceIds.Count == 0)
                throw new InvalidOperationException("Debe seleccionar al menos un servicio.");

            var services = await _db.Services
                .Where(s => serviceIds.Contains(s.ServiceId))
                .ToListAsync();

            if (services.Count != serviceIds.Count)
                throw new InvalidOperationException("Uno o más servicios no existen.");

            var hairServiceCount = services.Count(s =>
                !string.IsNullOrWhiteSpace(s.ServiceType) &&
                s.ServiceType.Trim().ToLower() == "cabello"
            );

            if (hairServiceCount > 0 && (!dto.HairLengthOption.HasValue || dto.HairLengthOption.Value <= 0))
                throw new InvalidOperationException("Debe seleccionar el largo del cabello para servicios de tipo 'cabello'.");

            decimal totalCost = services.Sum(s => s.Price);
            var baseDurationMinutes = services.Sum(s => s.DurationMinutes);

            var hairOption = hairServiceCount > 0 ? dto.HairLengthOption!.Value : 0;

            decimal hairCostPerService = hairOption switch
            {
                0 => 0m,
                1 => 5000m,
                2 => 10000m,
                3 => 15000m,
                4 => 20000m,
                5 => 25000m,
                6 => 30000m,
                7 => 35000m,
                8 => 40000m,
                9 => 45000m,
                _ => throw new InvalidOperationException("Opción de largo de pelo inválida.")
            };

            var hairExtraMinutesPerService = hairOption == 0 ? 0 : hairOption * 10;

            totalCost += hairCostPerService * hairServiceCount;

            var totalDurationMinutes = baseDurationMinutes + (hairExtraMinutesPerService * hairServiceCount);

            var normalizedProducts = new List<(int ProductId, int Quantity)>();
            Dictionary<int, Product> productDict = new();

            if (dto.Products != null && dto.Products.Count > 0)
            {
                normalizedProducts = dto.Products
                    .Where(p => p.ProductId > 0)
                    .GroupBy(p => p.ProductId)
                    .Select(g => (
                        ProductId: g.Key,
                        Quantity: g.Sum(x => x.Quantity <= 0 ? 1 : x.Quantity)
                    ))
                    .ToList();

                var productIds = normalizedProducts.Select(x => x.ProductId).ToList();

                productDict = await _db.Products
                    .Where(p => productIds.Contains(p.ProductId))
                    .ToDictionaryAsync(p => p.ProductId, p => p);

                if (productDict.Count != productIds.Count)
                    throw new InvalidOperationException("Uno o más productos no existen.");

                var inventories = await _db.Inventory
                    .Where(i => productIds.Contains(i.ProductId) && i.IsActive)
                    .ToDictionaryAsync(i => i.ProductId, i => i);

                if (inventories.Count != productIds.Count)
                    throw new InvalidOperationException("Uno o más productos no tienen inventario activo.");

                foreach (var item in normalizedProducts)
                {
                    var inv = inventories[item.ProductId];
                    var productName = productDict[item.ProductId].ProductName;

                    if (item.Quantity > inv.Quantity)
                        throw new InvalidOperationException(                       
                            $"Stock insuficiente para \"{productName}\". Disponible: {inv.Quantity}, solicitado: {item.Quantity}"
                        );
                }


                foreach (var item in normalizedProducts)
                {
                    var product = productDict[item.ProductId];
                    totalCost += product.Price * item.Quantity;
                }
            }

            var newStart = dto.StartTime;
            var newEnd = dto.StartTime.AddMinutes(totalDurationMinutes);

            var availability = await ValidateAvailabilityAsync(new AppointmentAvailabilityRequestDto
            {
                StartTime = newStart,
                EndTime = newEnd,
                ServiceIds = serviceIds,
                ExcludeAppointmentId = appointmentId
            });

            if (!availability.IsAvailable)
                throw new InvalidOperationException(availability.Message);

            appointment.StartTime = newStart;
            appointment.EndTime = newEnd;
            appointment.AppointmentDate = newStart.Date;
            appointment.TotalCost = totalCost;
            appointment.HairLengthOption = hairServiceCount > 0 ? dto.HairLengthOption : null;

            if (string.Equals(appointment.AppointmentStatus, AppointmentStatuses.Confirmed, StringComparison.OrdinalIgnoreCase))
                appointment.AppointmentStatus = AppointmentStatuses.Pending;

            var existingApServices = await _db.AppointmentServices
                .Where(x => x.AppointmentId == appointmentId)
                .ToListAsync();

            if (existingApServices.Count > 0)
                _db.AppointmentServices.RemoveRange(existingApServices);

            foreach (var s in services)
            {
                _db.AppointmentServices.Add(new AppointmentServiceEntity
                {
                    AppointmentId = appointmentId,
                    ServiceId = s.ServiceId,
                    ServicePrice = s.Price
                });
            }

            var existingApProducts = await _db.AppointmentProducts
                .Where(x => x.AppointmentId == appointmentId)
                .ToListAsync();

            if (existingApProducts.Count > 0)
                _db.AppointmentProducts.RemoveRange(existingApProducts);

            if (normalizedProducts.Count > 0)
            {
                foreach (var item in normalizedProducts)
                {
                    _db.AppointmentProducts.Add(new AppointmentProduct
                    {
                        AppointmentId = appointmentId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity
                    });
                }
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return true;
        }

        public async Task<AppointmentDetailDto?> GetByIdAsync(int id)
        {
            if (id <= 0) return null;

            var appointment = await _db.Appointments
                .AsNoTracking()
                .Include(a => a.Client)
                .Include(a => a.AppointmentServices)
                    .ThenInclude(x => x.Service)
                .Include(a => a.AppointmentProducts)
                    .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);

            if (appointment is null) return null;

            return new AppointmentDetailDto
            {
                AppointmentId = appointment.AppointmentId,
                AppointmentDate = appointment.AppointmentDate,
                StartTime = appointment.StartTime,
                EndTime = appointment.EndTime,
                AppointmentStatus = appointment.AppointmentStatus,
                TotalCost = appointment.TotalCost,
                IsActive = appointment.IsActive,
                ClientId = appointment.ClientId,
                HairLengthOption = appointment.HairLengthOption,

                Client = new ClientMiniDto
                {
                    ClientId = appointment.Client.ClientId,
                    Name = appointment.Client.Name,
                    Email = appointment.Client.Email,
                    Phone = appointment.Client.Phone
                },

                Services = appointment.AppointmentServices
                    .Select(s => new AppointmentServiceDetailDto
                    {
                        ServiceId = s.ServiceId,
                        ServiceName = s.Service.ServiceName,
                        ServicePrice = s.ServicePrice ?? 0m
                    })
                    .ToList(),

                Products = appointment.AppointmentProducts
                    .Select(p => new AppointmentProductDetailDto
                    {
                        ProductId = p.ProductId,
                        ProductName = p.Product.ProductName,
                        Price = p.Product.Price,
                        Quantity = p.Quantity
                    })
                    .ToList()
            };
        }

        public async Task<bool> CancelAsync(int appointmentId)
        {
            if (appointmentId <= 0)
                throw new InvalidOperationException("AppointmentId inválido.");

            var appointment = await _db.Appointments
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment is null) return false;

            if (string.Equals(appointment.AppointmentStatus, AppointmentStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
                return true;

            if (AppointmentStatuses.IsFinalState(appointment.AppointmentStatus))
                throw new InvalidOperationException("No se puede cancelar una cita finalizada.");

            if (!AppointmentStatuses.CanBeCancelled(appointment.AppointmentStatus))
                throw new InvalidOperationException("No se puede cancelar una cita en este estado.");

            appointment.AppointmentStatus = AppointmentStatuses.Cancelled;
            appointment.IsActive = false;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CompleteAsync(int appointmentId, IInventoryService? inventoryService = null)
        {
            if (appointmentId <= 0)
                throw new InvalidOperationException("AppointmentId inválido.");

            var appointment = await _db.Appointments
                .Include(a => a.AppointmentProducts)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment is null) return false;

            if (string.Equals(appointment.AppointmentStatus, AppointmentStatuses.Finalized, StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(appointment.AppointmentStatus, AppointmentStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("No se puede completar una cita cancelada.");

            var previousStatus = appointment.AppointmentStatus;

            _logger.LogInfo("CompleteAsync: Estado anterior: {PreviousStatus}, Productos: {ProductCount}, InventoryService: {HasService}",
                previousStatus, appointment.AppointmentProducts.Count, inventoryService != null);

            var totalPaid = await _db.Payments
                .Where(p => p.AppointmentId == appointmentId && p.IsActive)
                .SumAsync(p => p.Amount);

            var pendingBalance = (appointment.TotalCost ?? 0) - totalPaid;

            var newStatus = pendingBalance > 0
                ? AppointmentStatuses.CompletedPendingPayment
                : AppointmentStatuses.Finalized;

            _logger.LogInfo("CompleteAsync: Nuevo estado: {NewStatus}, PendingBalance: {Balance}", newStatus, pendingBalance);

            // Descontar del inventario
            var shouldDiscount = ShouldDiscountInventory(previousStatus, newStatus);
            _logger.LogInfo("CompleteAsync: ShouldDiscount: {ShouldDiscount}", shouldDiscount);

            if (shouldDiscount && appointment.AppointmentProducts.Count > 0 && inventoryService != null)
            {
                _logger.LogInfo("Descuento de inventario iniciado para cita {AppointmentId}", appointmentId);

                var productsToDiscount = appointment.AppointmentProducts
                    .ToDictionary(ap => ap.ProductId, ap => ap.Quantity);

                _logger.LogInfo("Productos a descontar: {Products}", string.Join(", ", productsToDiscount.Select(p => $"ProductId:{p.Key}, Qty:{p.Value}")));

                var discountSuccess = await inventoryService.DiscountMultipleAsync(productsToDiscount);

                if (discountSuccess)
                {
                    _logger.LogInfo("Inventario descontado exitosamente para cita {AppointmentId}", appointmentId);
                }
                else
                {
                    _logger.LogWarning("Fallo al descontar inventario para cita {AppointmentId}", appointmentId);
                }
            }
            else
            {
                _logger.LogInfo("Descuento NO aplicado. Razones - ShouldDiscount:{SD}, HasProducts:{HP}, HasService:{HS}",
                    shouldDiscount, appointment.AppointmentProducts.Count > 0, inventoryService != null);
            }

            appointment.AppointmentStatus = newStatus;
            appointment.IsActive = false;

            await _db.SaveChangesAsync();
            return true;
        }

        private static bool ShouldDiscountInventory(string? previousStatus, string? newStatus)
        {
            if (string.IsNullOrWhiteSpace(previousStatus) || string.IsNullOrWhiteSpace(newStatus))
                return false;

            var prevNormalized = previousStatus.Trim().ToLower();
            var newNormalized = newStatus.Trim().ToLower();

            var previousIsDiscountable = prevNormalized == AppointmentStatuses.Pending.ToLower() ||
                                        prevNormalized == AppointmentStatuses.Confirmed.ToLower();

            var newIsCompletionState = newNormalized == AppointmentStatuses.CompletedPendingPayment.ToLower() ||
                                       newNormalized == AppointmentStatuses.Finalized.ToLower();

            return previousIsDiscountable && newIsCompletionState;
        }

        public async Task<AppointmentAvailabilityResponseDto> ValidateAvailabilityAsync(AppointmentAvailabilityRequestDto dto)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto));

            if (dto.StartTime == default || dto.EndTime == default)
                return new AppointmentAvailabilityResponseDto { IsAvailable = false, Message = "Fecha/hora inválidas." };

            if (dto.EndTime <= dto.StartTime)
                return new AppointmentAvailabilityResponseDto { IsAvailable = false, Message = "La hora de fin debe ser mayor a la hora de inicio." };

            if (dto.ServiceIds is null || dto.ServiceIds.Count == 0)
                return new AppointmentAvailabilityResponseDto { IsAvailable = false, Message = "Debe enviar al menos un servicio." };

            if (!IsWithinBusinessHours(dto.StartTime, dto.EndTime, out var hoursMsg))
                return new AppointmentAvailabilityResponseDto { IsAvailable = false, Message = hoursMsg };

            var serviceIds = dto.ServiceIds.Distinct().ToList();

            var conflict = await HasServiceConflictAsync(dto.StartTime, dto.EndTime, serviceIds, dto.ExcludeAppointmentId);
            if (conflict)
                return new AppointmentAvailabilityResponseDto { IsAvailable = false, Message = "El horario choca con otra cita que incluye uno o más de los mismos servicios." };

            return new AppointmentAvailabilityResponseDto { IsAvailable = true, Message = "Horario disponible." };
        }

        private bool IsWithinBusinessHours(DateTime start, DateTime end, out string message)
        {
            message = string.Empty;

            if (start.Date != end.Date)
            {
                message = "La cita debe iniciar y finalizar el mismo día.";
                return false;
            }

            var day = start.DayOfWeek;

            if (day == DayOfWeek.Sunday)
            {
                message = "Domingo: cerrado.";
                return false;
            }

            var open = new TimeSpan(9, 0, 0);
            var close = day == DayOfWeek.Saturday
                ? new TimeSpan(18, 0, 0)
                : new TimeSpan(20, 0, 0);

            var startTod = start.TimeOfDay;
            var endTod = end.TimeOfDay;

            if (startTod < open || endTod > close)
            {
                message = day == DayOfWeek.Saturday
                    ? "Sábado: horario permitido de 9:00 AM a 6:00 PM."
                    : "Lunes a Viernes: horario permitido de 9:00 AM a 8:00 PM.";
                return false;
            }

            return true;
        }

        private async Task<bool> HasServiceConflictAsync(
            DateTime start,
            DateTime end,
            List<int> serviceIds,
            int? excludeAppointmentId = null)
        {
            var cancelledStatus = AppointmentStatuses.Cancelled.ToLower();

            var query = _db.Appointments
                .AsNoTracking()
                .Where(a => a.StartTime < end && a.EndTime > start)
                .Where(a => a.AppointmentStatus == null || 
                            a.AppointmentStatus.ToLower() != cancelledStatus);

            if (excludeAppointmentId.HasValue)
                query = query.Where(a => a.AppointmentId != excludeAppointmentId.Value);

            return await query
                .Join(_db.AppointmentServices.AsNoTracking(),
                      a => a.AppointmentId,
                      aps => aps.AppointmentId,
                      (a, aps) => new { a, aps })
                .AnyAsync(x => serviceIds.Contains(x.aps.ServiceId));
        }

        public async Task<decimal> GetPendingBalanceAsync(int appointmentId)
        {
            if (appointmentId <= 0)
                throw new InvalidOperationException("AppointmentId inválido.");

            var appointment = await _db.Appointments
                .AsNoTracking()
                .Include(a => a.Payments)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment is null)
                throw new InvalidOperationException("Cita no encontrada.");

            var totalPaid = appointment.Payments
                .Where(p => p.IsActive)
                .Sum(p => p.Amount);

            return (appointment.TotalCost ?? 0) - totalPaid;
        }

        public async Task<decimal> GetPendingBalanceClientAsync(int appointmentId)
        {
            if (appointmentId <= 0)
                throw new InvalidOperationException("AppointmentId inválido.");

            var appointment = await _db.Appointments
                .AsNoTracking()
                .Include(a => a.Client)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment is null)
                throw new InvalidOperationException("Cita no encontrada.");

            return appointment.Client.PendingBalance;
        }

        public async Task<bool> ChangeStatusAsync(int appointmentId, string newStatus, IInventoryService? inventoryService = null)
        {
            if (appointmentId <= 0)
                throw new InvalidOperationException("AppointmentId inválido.");

            if (string.IsNullOrWhiteSpace(newStatus))
                throw new InvalidOperationException("El estado no puede estar vacío.");

            if (!AppointmentStatuses.IsValid(newStatus))
                throw new InvalidOperationException($"Estado inválido: {newStatus}");

            var appointment = await _db.Appointments
                .Include(a => a.AppointmentProducts)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment is null)
                return false;

            var currentStatus = appointment.AppointmentStatus ?? string.Empty;

            if (AppointmentStatuses.IsFinalState(currentStatus) && 
                !string.Equals(currentStatus, AppointmentStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("No se puede cambiar el estado de una cita en estado final.");

            if (string.Equals(currentStatus, newStatus, StringComparison.OrdinalIgnoreCase))
                return true; 

            if (string.Equals(currentStatus, AppointmentStatuses.Pending, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(newStatus, AppointmentStatuses.Confirmed, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(newStatus, AppointmentStatuses.CompletedPendingPayment, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(newStatus, AppointmentStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("No se puede cambiar a ese estado desde Pendiente.");
            }
            else if (string.Equals(currentStatus, AppointmentStatuses.Confirmed, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(newStatus, AppointmentStatuses.CompletedPendingPayment, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(newStatus, AppointmentStatuses.Cancelled, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(newStatus, AppointmentStatuses.Finalized, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("No se puede cambiar a ese estado desde Confirmada.");
            }
            else if (string.Equals(currentStatus, AppointmentStatuses.CompletedPendingPayment, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(newStatus, AppointmentStatuses.Finalized, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("No se puede cambiar a ese estado desde CompletedPendingPayment.");
            }
            else if (string.Equals(currentStatus, AppointmentStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("No se puede cambiar el estado de una cita cancelada.");
            }

            
            var shouldDiscount = ShouldDiscountInventoryOnStatusChange(currentStatus, newStatus);

            if (shouldDiscount && appointment.AppointmentProducts.Count > 0 && inventoryService != null)
            {
                _logger.LogInfo("Descuento de inventario iniciado en ChangeStatus para cita {AppointmentId}", appointmentId);

                var productsToDiscount = appointment.AppointmentProducts
                    .ToDictionary(ap => ap.ProductId, ap => ap.Quantity);

                _logger.LogInfo("Productos a descontar: {Products}", string.Join(", ", productsToDiscount.Select(p => $"ProductId:{p.Key}, Qty:{p.Value}")));

                var discountSuccess = await inventoryService.DiscountMultipleAsync(productsToDiscount);

                if (discountSuccess)
                {
                    _logger.LogInfo("Inventario descontado exitosamente en ChangeStatus para cita {AppointmentId}", appointmentId);
                }
                else
                {
                    _logger.LogWarning("Fallo al descontar inventario en ChangeStatus para cita {AppointmentId}", appointmentId);
                }
            }

            appointment.AppointmentStatus = newStatus;

            if (AppointmentStatuses.IsFinalState(newStatus))
                appointment.IsActive = false;

            await _db.SaveChangesAsync();
            return true;
        }


        private static bool ShouldDiscountInventoryOnStatusChange(string? currentStatus, string? newStatus)
        {
            if (string.IsNullOrWhiteSpace(currentStatus) || string.IsNullOrWhiteSpace(newStatus))
                return false;

            var currNormalized = currentStatus.Trim().ToLower();
            var newNormalized = newStatus.Trim().ToLower();

            var isConfirmedToFinal = currNormalized == AppointmentStatuses.Confirmed.ToLower() &&
                                     newNormalized == AppointmentStatuses.Finalized.ToLower();

            var isConfirmedToCompleted = currNormalized == AppointmentStatuses.Confirmed.ToLower() &&
                                        newNormalized == AppointmentStatuses.CompletedPendingPayment.ToLower();

            var isPendingToCompleted = currNormalized == AppointmentStatuses.Pending.ToLower() &&
                                      newNormalized == AppointmentStatuses.CompletedPendingPayment.ToLower();

            var isPendingToFinal = currNormalized == AppointmentStatuses.Pending.ToLower() &&
                                  newNormalized == AppointmentStatuses.Finalized.ToLower();

            return isConfirmedToFinal || isConfirmedToCompleted || isPendingToCompleted || isPendingToFinal;
        }
    }
}
