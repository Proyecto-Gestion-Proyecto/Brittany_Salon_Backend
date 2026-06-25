using Brittany_Salon_Backend.Api.Controllers;
using Brittany_Salon_Backend.Application.DTOs.Report;
using Brittany_Salon_Backend.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

public class ReportTests
{
    private static ReportsController CreateController(Mock<IReportService> reportService)
    {
        return new ReportsController(reportService.Object);
    }

    [Fact]
    public async Task GetTop5LoyalClients_ReturnsOkWithListOfTopClients()
    {
        var reportServiceMock = new Mock<IReportService>();
        var topClients = new List<TopClientDto>
        {
            new TopClientDto
            {
                ClientId = 1,
                Name = "Juan Pérez",
                Email = "juan@example.com",
                Phone = "123456789",
                CompletedAppointments = 15
            },
            new TopClientDto
            {
                ClientId = 2,
                Name = "María García",
                Email = "maria@example.com",
                Phone = "987654321",
                CompletedAppointments = 12
            }
        };

        reportServiceMock
            .Setup(x => x.GetTop5LoyalClientsAsync())
            .ReturnsAsync(topClients);

        var controller = CreateController(reportServiceMock);

        var result = await controller.GetTop5LoyalClients();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
        
        var returnedClients = Assert.IsType<List<TopClientDto>>(okResult.Value);
        Assert.Equal(2, returnedClients.Count);
        Assert.Equal("Juan Pérez", returnedClients[0].Name);
    }

    [Fact]
    public async Task GetTopSellingProducts_ReturnsOkWithListOfTopProducts()
    {
        var reportServiceMock = new Mock<IReportService>();
        var topProducts = new List<TopProductDto>
        {
            new TopProductDto
            {
                ProductId = 1,
                ProductName = "Champú Premium",
                Price = 25000m,
                TotalSold = 50,
                TotalRevenue = 1250000m
            }
        };

        reportServiceMock
            .Setup(x => x.GetTopSellingProductsAsync())
            .ReturnsAsync(topProducts);

        var controller = CreateController(reportServiceMock);

        var result = await controller.GetTopSellingProducts();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedProducts = Assert.IsType<List<TopProductDto>>(okResult.Value);
        Assert.Single(returnedProducts);
        Assert.Equal("Champú Premium", returnedProducts[0].ProductName);
    }

    [Fact]
    public async Task GetDailyPayments_ReturnsOkWithDailyPayments()
    {
        var reportServiceMock = new Mock<IReportService>();
        var dailyPayments = new List<DailyPaymentDto>
        {
            new DailyPaymentDto
            {
                PaymentId = 1,
                AppointmentId = 1,
                ClientId = 1,
                ClientName = "Juan Pérez",
                Amount = 50000m,
                PaymentDate = DateTime.Now,
                PaymentMethod = "Efectivo",
                PaymentStatus = "Completado"
            }
        };

        reportServiceMock
            .Setup(x => x.GetDailyPaymentsAsync(null))
            .ReturnsAsync(dailyPayments);

        var controller = CreateController(reportServiceMock);

        var result = await controller.GetDailyPayments();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedPayments = Assert.IsType<List<DailyPaymentDto>>(okResult.Value);
        Assert.Single(returnedPayments);
        Assert.Equal(50000m, returnedPayments[0].Amount);
    }

    [Fact]
    public async Task GetDailyTotal_ReturnsOkWithDailyTotal()
    {
        var reportServiceMock = new Mock<IReportService>();
        var today = DateTime.Now.Date;
        var dailyTotal = new DailyTotalDto
        {
            Date = today,
            TotalAmount = 125000m,
            TotalPayments = 2
        };

        reportServiceMock
            .Setup(x => x.GetDailyTotalAsync(null))
            .ReturnsAsync(dailyTotal);

        var controller = CreateController(reportServiceMock);

        var result = await controller.GetDailyTotal();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTotal = Assert.IsType<DailyTotalDto>(okResult.Value);
        Assert.Equal(125000m, returnedTotal.TotalAmount);
        Assert.Equal(2, returnedTotal.TotalPayments);
    }

    [Fact]
    public async Task GetMonthlyRevenue_ReturnsOkWithMonthlyRevenueData()
    {
        var reportServiceMock = new Mock<IReportService>();
        var monthlyRevenue = new List<MonthlyRevenueDto>
        {
            new MonthlyRevenueDto
            {
                Year = 2026,
                Month = 1,
                MonthName = "Enero",
                TotalPayments = 10,
                TotalAmount = 500000m,
                AveragePayment = 50000m
            }
        };

        reportServiceMock
            .Setup(x => x.GetMonthlyRevenueAsync(null))
            .ReturnsAsync(monthlyRevenue);

        var controller = CreateController(reportServiceMock);

        var result = await controller.GetMonthlyRevenue();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRevenue = Assert.IsType<List<MonthlyRevenueDto>>(okResult.Value);
        Assert.Single(returnedRevenue);
        Assert.Equal("Enero", returnedRevenue[0].MonthName);
        Assert.Equal(500000m, returnedRevenue[0].TotalAmount);
    }

    [Fact]
    public async Task GetBusinessSummary_ReturnsOkWithBusinessSummary()
    {
        var reportServiceMock = new Mock<IReportService>();
        var businessSummary = new BusinessSummaryDto
        {
            TotalRevenue = 1000000m,
            CompletedAppointments = 50,
            ActiveClients = 20,
            ActiveServices = 15
        };

        reportServiceMock
            .Setup(x => x.GetBusinessSummaryAsync())
            .ReturnsAsync(businessSummary);

        var controller = CreateController(reportServiceMock);

        var result = await controller.GetBusinessSummary();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSummary = Assert.IsType<BusinessSummaryDto>(okResult.Value);
        Assert.Equal(1000000m, returnedSummary.TotalRevenue);
        Assert.Equal(50, returnedSummary.CompletedAppointments);
        Assert.Equal(20, returnedSummary.ActiveClients);
        Assert.Equal(15, returnedSummary.ActiveServices);
    }

    [Fact]
    public async Task GetTopServices_ReturnsOkWithTopServices()
    {
        var reportServiceMock = new Mock<IReportService>();
        var topServices = new List<TopServiceDto>
        {
            new TopServiceDto
            {
                ServiceId = 1,
                ServiceName = "Corte de cabello",
                CompletedAppointments = 30,
                TotalRevenue = 300000m,
                TotalMinutes = 900
            }
        };

        reportServiceMock
            .Setup(x => x.GetTopServicesAsync(5, "appointments", null))
            .ReturnsAsync(topServices);

        var controller = CreateController(reportServiceMock);

        var result = await controller.GetTopServices();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedServices = Assert.IsType<List<TopServiceDto>>(okResult.Value);
        Assert.Single(returnedServices);
        Assert.Equal("Corte de cabello", returnedServices[0].ServiceName);
        Assert.Equal(30, returnedServices[0].CompletedAppointments);
    }
}
