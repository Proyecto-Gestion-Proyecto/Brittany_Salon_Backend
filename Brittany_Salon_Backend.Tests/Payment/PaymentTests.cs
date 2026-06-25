using Brittany_Salon_Backend.Application.DTOs.Payment;
using Brittany_Salon_Backend.Application.Services;
using Brittany_Salon_Backend.Domain.Entities;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Brittany_Salon_Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using PaymentEntity = Brittany_Salon_Backend.Domain.Entities.Payment;

namespace Brittany_Salon_Backend.Tests.Payment
{
    public class PaymentTests
    {
        private static AppDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private static (PaymentService svc, Mock<IDevLogger> log, AppDbContext db) Build()
        {
            var db = CreateDb();
            var log = new Mock<IDevLogger>();
            var svc = new PaymentService(db, log.Object);
            return (svc, log, db);
        }

        #region CREATE TESTS


        [Fact]
        public async Task CreateAsync_ValidDto_CreatesPayment()
        {
            var (svc, _, db) = Build();
            var client = new Clients {
                Name = "Britany Villalobos",
                Email = "britanyv@gmail.com",
                Phone = "85123456",
                IsActive = true,
                PendingBalance = 100000m
            };
            db.Clients.Add(client);
            await db.SaveChangesAsync();
            var appointment = new Appointment
            {
                ClientId = client.ClientId,
                AppointmentDate = DateTime.Now,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddHours(1),
                AppointmentStatus = "Completada con saldo pendiente",
                TotalCost = 100000m,
                IsActive = true
            };
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();
            var dto = new PaymentCreateDto
            {
                AppointmentId = appointment.AppointmentId,
                Amount = 50000m,
                PaymentMethod = "Efectivo",
                PaymentStatus = "Completado",
                IsActive = true
            };
            var result = await svc.CreateAsync(dto);
            Assert.NotNull(result);
            Assert.Equal(50000m, result.Amount);
            Assert.Equal("Efectivo", result.PaymentMethod);
            Assert.Equal("Completado", result.PaymentStatus);
            Assert.True(result.IsActive);
        }

        #endregion

        #region READ TESTS

        [Fact]
        public async Task GetAllAsync_ReturnsOnlyActivePayments()
        {
            var (svc, _, db) = Build();
            var client = new Clients {
                Name = "Test Client",
                Email = "test@test.com",
                Phone = "12345678",
                IsActive = true
            };
            db.Clients.Add(client);
            await db.SaveChangesAsync();
            var appointment = new Appointment
            {
                ClientId = client.ClientId,
                AppointmentDate = DateTime.Now,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddHours(1),
                AppointmentStatus = "Finalizada",
                TotalCost = 100000m,
                IsActive = true
            };
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();
            var payment1 = new PaymentEntity { AppointmentId = appointment.AppointmentId, Amount = 50000m, PaymentDate = DateTime.Now, PaymentMethod = "Efectivo", PaymentStatus = "Completado", IsActive = true };
            var payment2 = new PaymentEntity { AppointmentId = appointment.AppointmentId, Amount = 50000m, PaymentDate = DateTime.Now, PaymentMethod = "SINPE", PaymentStatus = "Completado", IsActive = false };
            db.Payments.AddRange(payment1, payment2);
            await db.SaveChangesAsync();
            var result = await svc.GetAllAsync();
            Assert.Single(result);
            Assert.Equal(50000m, result[0].Amount);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingPayment_ReturnsPayment()
        {
            var (svc, _, db) = Build();
            var client = new Clients { Name = "Test", Email = "test@test.com", Phone = "123", IsActive = true };
            db.Clients.Add(client);
            await db.SaveChangesAsync();
            var appointment = new Appointment
            {
                ClientId = client.ClientId,
                AppointmentDate = DateTime.Now,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddHours(1),
                AppointmentStatus = "Finalizada",
                TotalCost = 100000m,
                IsActive = true
            };
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();
            var payment = new PaymentEntity { AppointmentId = appointment.AppointmentId, Amount = 100000m, PaymentDate = DateTime.Now, PaymentMethod = "Efectivo", PaymentStatus = "Completado", IsActive = true };
            db.Payments.Add(payment);
            await db.SaveChangesAsync();
            var result = await svc.GetByIdAsync(payment.PaymentId);
            Assert.NotNull(result);
            Assert.Equal(100000m, result.Amount);
            Assert.Equal("Efectivo", result.PaymentMethod);
        }

        [Fact]
        public async Task GetByClientIdAsync_ReturnsPaymentsForClient()
        {
            var (svc, _, db) = Build();
            var client = new Clients { Name = "Test Client", Email = "test@test.com", Phone = "123", IsActive = true };
            db.Clients.Add(client);
            await db.SaveChangesAsync();
            var appointment1 = new Appointment
            {
                ClientId = client.ClientId,
                AppointmentDate = DateTime.Now,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddHours(1),
                AppointmentStatus = "Finalizada",
                TotalCost = 100000m,
                IsActive = true
            };
            var appointment2 = new Appointment
            {
                ClientId = client.ClientId,
                AppointmentDate = DateTime.Now,
                StartTime = DateTime.Now.AddHours(2),
                EndTime = DateTime.Now.AddHours(3),
                AppointmentStatus = "Finalizada",
                TotalCost = 50000m,
                IsActive = true
            };
            db.Appointments.AddRange(appointment1, appointment2);
            await db.SaveChangesAsync();
            var payment1 = new PaymentEntity { AppointmentId = appointment1.AppointmentId, Amount = 100000m, PaymentDate = DateTime.Now, PaymentMethod = "Efectivo", PaymentStatus = "Completado", IsActive = true };
            var payment2 = new PaymentEntity { AppointmentId = appointment2.AppointmentId, Amount = 50000m, PaymentDate = DateTime.Now, PaymentMethod = "SINPE", PaymentStatus = "Completado", IsActive = true };
            db.Payments.AddRange(payment1, payment2);
            await db.SaveChangesAsync();
            var result = await svc.GetByClientIdAsync(client.ClientId);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetByDateRangeAsync_ReturnPaymentsInRange()
        {
            var (svc, _, db) = Build();
            var client = new Clients { Name = "Test", Email = "test@test.com", Phone = "123", IsActive = true };
            db.Clients.Add(client);
            await db.SaveChangesAsync();

            var appointment = new Appointment
            {
                ClientId = client.ClientId,
                AppointmentDate = DateTime.Now,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddHours(1),
                AppointmentStatus = "Finalizada",
                TotalCost = 100000m,
                IsActive = true
            };
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();

            var today = DateTime.Now.Date;
            var payment1 = new PaymentEntity { AppointmentId = appointment.AppointmentId, Amount = 50000m, PaymentDate = today, PaymentMethod = "Efectivo", PaymentStatus = "Completado", IsActive = true };
            var payment2 = new PaymentEntity { AppointmentId = appointment.AppointmentId, Amount = 30000m, PaymentDate = today.AddDays(-5), PaymentMethod = "SINPE", PaymentStatus = "Completado", IsActive = true };

            db.Payments.AddRange(payment1, payment2);
            await db.SaveChangesAsync();
            var result = await svc.GetByDateRangeAsync(today.AddDays(-2), today);
            Assert.Single(result); 
            Assert.Equal(50000m, result[0].Amount);
        }

        [Fact]
        public async Task GetByStatusAsync_ReturnsPaymentsByStatus()
        {
            var (svc, _, db) = Build();
            var client = new Clients { Name = "Test", Email = "test@test.com", Phone = "123", IsActive = true };
            db.Clients.Add(client);
            await db.SaveChangesAsync();
            var appointment = new Appointment
            {
                ClientId = client.ClientId,
                AppointmentDate = DateTime.Now,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddHours(1),
                AppointmentStatus = "Finalizada",
                TotalCost = 100000m,
                IsActive = true
            };
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();
            var payment1 = new PaymentEntity { AppointmentId = appointment.AppointmentId, Amount = 50000m, PaymentDate = DateTime.Now, PaymentMethod = "Efectivo", PaymentStatus = "Completado", IsActive = true };
            var payment2 = new PaymentEntity { AppointmentId = appointment.AppointmentId, Amount = 30000m, PaymentDate = DateTime.Now, PaymentMethod = "SINPE", PaymentStatus = "Completado", IsActive = false };
            db.Payments.AddRange(payment1, payment2);
            await db.SaveChangesAsync();
            var activeResult = await svc.GetByStatusAsync(true);
            var inactiveResult = await svc.GetByStatusAsync(false);
            Assert.Single(activeResult);
            Assert.Single(inactiveResult);
        }

        #endregion

        #region UPDATE TESTS

        [Fact]
        public async Task UpdateAsync_ValidData_UpdatesPayment()
        {
            var (svc, _, db) = Build();
            var client = new Clients { Name = "Test", Email = "test@test.com", Phone = "123", IsActive = true };
            db.Clients.Add(client);
            await db.SaveChangesAsync();
            var appointment = new Appointment
            {
                ClientId = client.ClientId,
                AppointmentDate = DateTime.Now,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddHours(1),
                AppointmentStatus = "Finalizada",
                TotalCost = 100000m,
                IsActive = true
            };
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();
            var payment = new PaymentEntity { AppointmentId = appointment.AppointmentId, Amount = 50000m, PaymentDate = DateTime.Now, PaymentMethod = "Efectivo", PaymentStatus = "Completado", IsActive = true };
            db.Payments.Add(payment);
            await db.SaveChangesAsync();
            var updateDto = new PaymentUpdateDto
            {
                Amount = 60000m,
                PaymentMethod = "SINPE",
                PaymentStatus = "Completado"
            };
            var result = await svc.UpdateAsync(payment.PaymentId, updateDto);
            Assert.True(result);
            var updated = await db.Payments.FirstOrDefaultAsync(p => p.PaymentId == payment.PaymentId);
            Assert.Equal(60000m, updated.Amount);
            Assert.Equal("SINPE", updated.PaymentMethod);
        }

      

        #endregion

        #region DELETE TESTS

        [Fact]
        public async Task DeleteAsync_ExistingPayment_DeactivatesPayment()
        {
            var (svc, _, db) = Build();
            var client = new Clients { Name = "Test", Email = "test@test.com", Phone = "123", IsActive = true };
            db.Clients.Add(client);
            await db.SaveChangesAsync();
            var appointment = new Appointment
            {
                ClientId = client.ClientId,
                AppointmentDate = DateTime.Now,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddHours(1),
                AppointmentStatus = "Finalizada",
                TotalCost = 100000m,
                IsActive = true
            };
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();
            var payment = new PaymentEntity { AppointmentId = appointment.AppointmentId, Amount = 100000m, PaymentDate = DateTime.Now, PaymentMethod = "Efectivo", PaymentStatus = "Completado", IsActive = true };
            db.Payments.Add(payment);
            await db.SaveChangesAsync();
            var result = await svc.DeleteAsync(payment.PaymentId);
            Assert.True(result);
            var deactivated = await db.Payments.FirstOrDefaultAsync(p => p.PaymentId == payment.PaymentId);
            Assert.False(deactivated.IsActive);
        }

      

        #endregion
    }
}




