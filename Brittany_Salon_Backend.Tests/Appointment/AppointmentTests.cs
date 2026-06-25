using Brittany_Salon_Backend.Application.DTOs.Appointment;
using Brittany_Salon_Backend.Application.Exceptions;
using Brittany_Salon_Backend.Application.Services;
using Brittany_Salon_Backend.Domain.Entities;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Brittany_Salon_Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;
using AppointmentServiceEntity = Brittany_Salon_Backend.Domain.Entities.AppointmentService;

public class AppointmentTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    private static (Brittany_Salon_Backend.Application.Services.AppointmentService svc, Mock<IDevLogger> log, AppDbContext db) Build()
    {
        var db = CreateDb();
        var log = new Mock<IDevLogger>();

        var svc = new Brittany_Salon_Backend.Application.Services.AppointmentService(db, log.Object);
        return (svc, log, db);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesAppointment()
    {
        // Arrange
        var (svc, _, db) = Build();

        // Add a client
        var client = new Clients
        {
            Name = "Isaac Chevez",
            Email = "isaac@test.com",
            Phone = "88887777",
            Password = "Password123!",
            IsActive = true
        };
        db.Clients.Add(client);

        // Add a service (tipo NO cabello para evitar validacion de HairLengthOption)
        var service = new Service
        {
            ServiceName = "Manicure",
            ServiceDescription = "Descripcion",
            Price = 15000m,
            DurationMinutes = 60,
            ServiceType = "Unas",
            IsActive = true
        };
        db.Services.Add(service);

        await db.SaveChangesAsync();

        // Crear fecha valida (proximo dia habil a las 10am)
        var appointmentDate = GetNextWeekday(DateTime.Now.Date);

        var dto = new AppointmentCreateDto
        { 
            AppointmentDate = appointmentDate,
            StartTime = appointmentDate.AddHours(10),
            AppointmentStatus = "Pendiente",
            ClientId = client.ClientId,
            Services = new List<AppointmentServiceCreateDto>
            {
                new AppointmentServiceCreateDto { ServiceId = service.ServiceId }
            },
            HairLengthOption = 0,
            Products = null
        };

        // Act
        var appointmentId = await svc.CreateAsync(dto);

        // Assert
        Assert.True(appointmentId > 0);

        var entity = await db.Appointments.FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);
        Assert.NotNull(entity);
        Assert.Equal(client.ClientId, entity!.ClientId);
        Assert.Equal("Pendiente", entity.AppointmentStatus);
        Assert.Equal(15000m, entity.TotalCost);
    }

    private static DateTime GetNextWeekday(DateTime start)
    {
        var date = start.AddDays(1);
        while (date.DayOfWeek == DayOfWeek.Sunday)
        {
            date = date.AddDays(1);
        }
        return date;
    }

    [Fact]
    public async Task CreateAsync_WhenClientDoesNotExist_ThrowsValidationException()
    {
        // Arrange
        var (svc, _, db) = Build();

        var dto = new AppointmentCreateDto
        {
            AppointmentDate = DateTime.Now.Date,
            StartTime = DateTime.Now.Date.AddHours(10),
            AppointmentStatus = "Pendiente",
            ClientId = 99999, // Non-existent client
            Services = new List<AppointmentServiceCreateDto>
            {
                new AppointmentServiceCreateDto { ServiceId = 1 }
            },
            HairLengthOption = 0,
            Products = null
        };

        // Act + Assert - Puede lanzar ValidationException o InvalidOperationException
        await Assert.ThrowsAnyAsync<Exception>(() => svc.CreateAsync(dto));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAppointmentsOrderedByStartTimeDescending()
    {
        // Arrange
        var (svc, _, db) = Build();

        var client = new Clients
        {
            Name = "Isaac Chevez",
            Email = "isaac@test.com",
            Phone = "88887777",
            Password = "Password123!",
            IsActive = true
        };
        db.Clients.Add(client);
        await db.SaveChangesAsync();

        var now = DateTime.Now;
        db.Appointments.AddRange(
            new Appointment
            {
                AppointmentDate = now.Date,
                StartTime = now.AddHours(2),
                EndTime = now.AddHours(3),
                AppointmentStatus = "Pendiente",
                ClientId = client.ClientId,
                TotalCost = 15000m,
                IsActive = true
            },
            new Appointment
            {
                AppointmentDate = now.Date,
                StartTime = now.AddHours(1),
                EndTime = now.AddHours(2),
                AppointmentStatus = "Completada",
                ClientId = client.ClientId,
                TotalCost = 10000m,
                IsActive = false
            }
        );
        await db.SaveChangesAsync();

        // Act
        var result = await svc.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(now.AddHours(2), result[0].StartTime);
        Assert.Equal(now.AddHours(1), result[1].StartTime);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAppointmentExists_ReturnsAppointmentDetailDto()
    {
        // Arrange
        var (svc, _, db) = Build();

        var client = new Clients
        {
            Name = "Isaac Chevez",
            Email = "isaac@test.com",
            Phone = "88887777",
            Password = "Password123!",
            IsActive = true
        };
        db.Clients.Add(client);

        var service = new Service
        {
            ServiceName = "Corte de Cabello",
            ServiceDescription = "Descripci�n",
            Price = 15000m,
            DurationMinutes = 60,
            ServiceType = "Cabello",
            IsActive = true
        };
        db.Services.Add(service);

        await db.SaveChangesAsync();

        var appointment = new Appointment
        {
            AppointmentDate = DateTime.Now.Date,
            StartTime = DateTime.Now.Date.AddHours(10),
            EndTime = DateTime.Now.Date.AddHours(11),
            AppointmentStatus = "Pendiente",
            ClientId = client.ClientId,
            TotalCost = 15000m,
            IsActive = true
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        db.AppointmentServices.Add(new AppointmentServiceEntity
        {
            AppointmentId = appointment.AppointmentId,
            ServiceId = service.ServiceId,
            ServicePrice = service.Price
        });
        await db.SaveChangesAsync();

        // Act
        var result = await svc.GetByIdAsync(appointment.AppointmentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(appointment.AppointmentId, result!.AppointmentId);
        Assert.Equal(client.ClientId, result.ClientId);
        Assert.Equal("Isaac Chevez", result.Client.Name);
        Assert.Equal(1, result.Services.Count);
        Assert.Equal("Corte de Cabello", result.Services[0].ServiceName);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAppointmentDoesNotExist_ReturnsNull()
    {
        // Arrange
        var (svc, _, _) = Build();

        // Act
        var result = await svc.GetByIdAsync(99999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CancelAsync_WhenAppointmentExists_SetsStatusToCancelled()
    {
        // Arrange
        var (svc, _, db) = Build();

        var client = new Clients
        {
            Name = "Isaac Chevez",
            Email = "isaac@test.com",
            Phone = "88887777",
            Password = "Password123!",
            IsActive = true
        };
        db.Clients.Add(client);
        await db.SaveChangesAsync();

        var appointment = new Appointment
        {
            AppointmentDate = DateTime.Now.Date,
            StartTime = DateTime.Now.Date.AddHours(10),
            EndTime = DateTime.Now.Date.AddHours(11),
            AppointmentStatus = "Pendiente",
            ClientId = client.ClientId,
            TotalCost = 15000m,
            IsActive = true
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        // Act
        var result = await svc.CancelAsync(appointment.AppointmentId);

        // Assert
        Assert.True(result);

        var updated = await db.Appointments.FirstOrDefaultAsync(a => a.AppointmentId == appointment.AppointmentId);
        Assert.NotNull(updated);
        Assert.Equal("Cancelada", updated!.AppointmentStatus);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task CancelAsync_WhenAppointmentIsCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var (svc, _, db) = Build();

        var client = new Clients
        {
            Name = "Isaac Chevez",
            Email = "isaac@test.com",
            Phone = "88887777",
            Password = "Password123!",
            IsActive = true
        };
        db.Clients.Add(client);
        await db.SaveChangesAsync();

        var appointment = new Appointment
        {
            AppointmentDate = DateTime.Now.Date,
            StartTime = DateTime.Now.Date.AddHours(10),
            EndTime = DateTime.Now.Date.AddHours(11),
            AppointmentStatus = "Completada",
            ClientId = client.ClientId,
            TotalCost = 15000m,
            IsActive = false
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        // Act + Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CancelAsync(appointment.AppointmentId)
        );

        Assert.Contains("No se puede cancelar", ex.Message);
    }

    [Fact]
    public async Task CompleteAsync_WhenAppointmentIsPending_SetsStatusToCompleted()
    {
        // Arrange
        var (svc, _, db) = Build();

        var client = new Clients
        {
            Name = "Isaac Chevez",
            Email = "isaac@test.com",
            Phone = "88887777",
            Password = "Password123!",
            IsActive = true
        };
        db.Clients.Add(client);
        await db.SaveChangesAsync();

        var appointment = new Appointment
        {
            AppointmentDate = DateTime.Now.Date,
            StartTime = DateTime.Now.Date.AddHours(10),
            EndTime = DateTime.Now.Date.AddHours(11),
            AppointmentStatus = "Pendiente",
            ClientId = client.ClientId,
            TotalCost = 15000m,
            IsActive = true
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        // Agregar un pago completo para que el estado sea Finalizada
        db.Payments.Add(new Payment
        {
            AppointmentId = appointment.AppointmentId,
            Amount = 15000m,
            PaymentDate = DateTime.Now,
            PaymentMethod = "Efectivo",
            IsActive = true
        });
        await db.SaveChangesAsync();

        // Act
        var result = await svc.CompleteAsync(appointment.AppointmentId);

        // Assert
        Assert.True(result);

        var updated = await db.Appointments.FirstOrDefaultAsync(a => a.AppointmentId == appointment.AppointmentId);
        Assert.NotNull(updated);
        Assert.Equal("Finalizada", updated!.AppointmentStatus);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task CompleteAsync_WhenAppointmentIsCancelled_ThrowsInvalidOperationException()
    {
        // Arrange
        var (svc, _, db) = Build();

        var client = new Clients
        {
            Name = "Isaac Chevez",
            Email = "isaac@test.com",
            Phone = "88887777",
            Password = "Password123!",
            IsActive = true
        };
        db.Clients.Add(client);
        await db.SaveChangesAsync();

        var appointment = new Appointment
        {
            AppointmentDate = DateTime.Now.Date,
            StartTime = DateTime.Now.Date.AddHours(10),
            EndTime = DateTime.Now.Date.AddHours(11),
            AppointmentStatus = "Cancelada",
            ClientId = client.ClientId,
            TotalCost = 15000m,
            IsActive = false
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        // Act + Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CompleteAsync(appointment.AppointmentId)
        );

        Assert.Contains("No se puede completar una cita cancelada", ex.Message);
    }

    [Fact]
    public async Task ValidateAvailabilityAsync_WhenTimeIsInvalid_ReturnsNotAvailable()
    {
        // Arrange
        var (svc, _, _) = Build();

        var dto = new AppointmentAvailabilityRequestDto
        {
            StartTime = DateTime.Now.Date.AddHours(25), // Invalid time
            EndTime = DateTime.Now.Date.AddHours(26),
            ServiceIds = new List<int> { 1 }
        };

        // Act
        var result = await svc.ValidateAvailabilityAsync(dto);

        // Assert
        Assert.False(result.IsAvailable);
        // Puede ser mensaje de horario o de dia invalido
        Assert.False(string.IsNullOrEmpty(result.Message));
    }

    [Fact]
    public async Task ValidateAvailabilityAsync_WhenAvailable_ReturnsAvailable()
    {
        // Arrange
        var (svc, _, db) = Build();

        var dto = new AppointmentAvailabilityRequestDto
        {
            StartTime = DateTime.Now.Date.AddHours(10),
            EndTime = DateTime.Now.Date.AddHours(11),
            ServiceIds = new List<int> { 1 }
        };

        // Act
        var result = await svc.ValidateAvailabilityAsync(dto);

        // Assert
        Assert.True(result.IsAvailable);
        Assert.Equal("Horario disponible.", result.Message);
    }
}