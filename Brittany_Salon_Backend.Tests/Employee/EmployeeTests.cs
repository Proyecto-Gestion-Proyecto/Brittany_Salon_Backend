using Brittany_Salon_Backend.Application.DTOs.Employee;
using Brittany_Salon_Backend.Application.Exceptions;
using Brittany_Salon_Backend.Application.Services;
using Brittany_Salon_Backend.Application.Validators;
using Brittany_Salon_Backend.Domain.Entities;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Brittany_Salon_Backend.Infrastructure.Persistence;
using Brittany_Salon_Backend.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Text;
using Xunit;

public class EmployeeTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static (EmployeeService svc, Mock<IImageService> img, Mock<IDevLogger> log, AppDbContext db) Build()
    {
        var db = CreateDb();
        var img = new Mock<IImageService>();
        var log = new Mock<IDevLogger>();

        var svc = new EmployeeService(db, img.Object, log.Object);
        return (svc, img, log, db);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesEmployee_AndNormalizesNameEmail()
    {
        var (svc, _, _, db) = Build();

        var dto = new EmployeeCreateDto
        {
            Name = "Isaac Chevez",              
            Phone = "88887777",               
            Email = "isaac.chevez@test.com",    
            Password = "Password123!",       
            Specialty = "Colorista",
            IsActive = true
        };

        var created = await svc.CreateAsync(dto);

        Assert.True(created.Id > 0);
        Assert.Equal("Isaac Chevez", created.Name);
        Assert.Equal("isaac.chevez@test.com", created.Email);
        Assert.Equal("88887777", created.Phone);
        Assert.Equal("Colorista", created.Specialty);

        // Confirmar persistencia
        var entity = await db.Employees.FirstOrDefaultAsync(e => e.Id == created.Id);
        Assert.NotNull(entity);
    }

    [Fact]
    public async Task CreateAsync_WhenPhoneIsInvalid_ThrowsValidationException()
    {
        var (svc, _, _, _) = Build();

        var dto = new EmployeeCreateDto
        {
            Name = "Isaac Chevez",
            Phone = "123",
            Email = "isaac.chevez@test.com",
            Password = "Password123!",
            Specialty = "Colorista",
            IsActive = true
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => svc.CreateAsync(dto));
        Assert.NotEmpty(ex.Errors);
    }
    [Fact]
    public async Task GetByIdAsync_WhenEmployeeExists_ReturnsEmployeeReadDto()
    {
        var (svc, _, _, db) = Build();

        // Arrange: crear y guardar un empleado en la BD InMemory
        var emp = new Employee
        {
            Name = "Ana Mora",
            Phone = "11112222",
            Email = "ana@test.com",
            Password = "Password123!",
            Specialty = "Colorista",
            IsActive = true
        };

        db.Employees.Add(emp);
        await db.SaveChangesAsync();

        // Act
        var result = await svc.GetByIdAsync(emp.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(emp.Id, result!.Id);
        Assert.Equal("Ana Mora", result.Name);
        Assert.Equal("11112222", result.Phone);
        Assert.Equal("ana@test.com", result.Email);
        Assert.Equal("Colorista", result.Specialty);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEmployeeDoesNotExist_ReturnsNull()
    {
        var (svc, _, _, _) = Build();

        // Act
        var result = await svc.GetByIdAsync(99999);

        // Assert
        Assert.Null(result);
    }
    [Fact]
    public async Task UpdateAsync_ValidDto_UpdatesEmployeeSuccessfully()
    {
        // Arrange
        var (svc, _, _, db) = Build();

        var employee = new Employee
        {
            Name = "Ana Mora",
            Phone = "11112222",
            Email = "ana@test.com",
            Password = "Password123!",
            Specialty = "Colorista",
            IsActive = true
        };

        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        var dto = new EmployeeUpdateDto
        {
            Name = "Isaac Chevez",
            Phone = "88887777",
            Specialty = "Estilista",
            IsActive = true
        };

        // Act
        var result = await svc.UpdateAsync(employee.Id, dto);

        // Assert
        Assert.True(result);

        var updated = await db.Employees.FirstOrDefaultAsync(e => e.Id == employee.Id);
        Assert.NotNull(updated);
        Assert.Equal("Isaac Chevez", updated!.Name);
        Assert.Equal("88887777", updated.Phone);
        Assert.Equal("Estilista", updated.Specialty);
        Assert.True(updated.IsActive);
    }

    [Fact]
    public async Task DeletePermanentlyAsync_WhenEmployeeExists_DeletesEmployee()
    {
        // Arrange
        var (svc, img, _, db) = Build();

        var employee = new Employee
        {
            Name = "Ana Mora",
            Phone = "11112222",
            Email = "ana@test.com",
            Password = "Password123!",
            Specialty = "Colorista",
            IsActive = true,
            Image = null
        };

        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        // Act
        var result = await svc.DeletePermanentlyAsync(employee.Id);

        // Assert
        Assert.True(result);

        var deleted = await db.Employees.FirstOrDefaultAsync(e => e.Id == employee.Id);
        Assert.Null(deleted);

        // (Opcional) Verificar que NO intentó borrar imagen
        img.Verify(x => x.DeleteImage(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeactivateAsync_WhenEmployeeExists_SetsIsActiveFalse()
    {
        // Arrange
        var (svc, _, _, db) = Build();

        var employee = new Employee
        {
            Name = "Ana Mora",
            Phone = "11112222",
            Email = "ana@test.com",
            Password = "Password123!",
            Specialty = "Colorista",
            IsActive = true
        };

        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        // Act
        var result = await svc.DeactivateAsync(employee.Id);

        // Assert
        Assert.True(result);

        var updated = await db.Employees.FirstOrDefaultAsync(e => e.Id == employee.Id);
        Assert.NotNull(updated);
        Assert.False(updated!.IsActive);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmployeesOrderedByName()
    {
        // Arrange
        var (svc, _, _, db) = Build();

        db.Employees.AddRange(
            new Employee { Name = "Zoe", Phone = "11112222", Email = "zoe@test.com", Password = "Password123!", IsActive = true, Specialty = "Colorista" },
            new Employee { Name = "Ana", Phone = "33334444", Email = "ana@test.com", Password = "Password123!", IsActive = true, Specialty = "Estilista" },
            new Employee { Name = "Luis", Phone = "55556666", Email = "luis@test.com", Password = "Password123!", IsActive = false, Specialty = "Cortes" }
        );
        await db.SaveChangesAsync();

        // Act
        var result = await svc.GetAllAsync();

        // Assert
        Assert.Equal(3, result.Count);

        // Verifica orden por Name (Ana, Luis, Zoe)
        Assert.Equal("Ana", result[0].Name);
        Assert.Equal("Luis", result[1].Name);
        Assert.Equal("Zoe", result[2].Name);
    }




}
