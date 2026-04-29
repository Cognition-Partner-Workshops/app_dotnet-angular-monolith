using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryService.Api.Controllers;
using InventoryService.Api.Data;
using FluentAssertions;
using Xunit;

namespace InventoryService.Api.Tests;

public class InventoryControllerTests
{
    private (InventoryController Controller, InventoryDbContext Context) CreateController()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new InventoryDbContext(options);
        SeedData.Initialize(context);
        var service = new Services.InventoryService(context);
        var controller = new InventoryController(service);
        return (controller, context);
    }

    [Fact]
    public async Task GetAll_Returns200WithItems()
    {
        var (controller, context) = CreateController();
        using var _ = context;

        var result = await controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetByProduct_Returns200_WhenExists()
    {
        var (controller, context) = CreateController();
        using var _ = context;

        var result = await controller.GetByProduct(1);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByProduct_Returns404_WhenNotFound()
    {
        var (controller, context) = CreateController();
        using var _ = context;

        var result = await controller.GetByProduct(9999);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Reserve_Returns200_WhenSufficientStock()
    {
        var (controller, context) = CreateController();
        using var _ = context;

        var result = await controller.Reserve(1, new ReserveRequest(5));

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Reserve_Returns400_WhenInsufficientStock()
    {
        var (controller, context) = CreateController();
        using var _ = context;

        var result = await controller.Reserve(1, new ReserveRequest(99999));

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Release_Returns200_OnSuccess()
    {
        var (controller, context) = CreateController();
        using var _ = context;

        var result = await controller.Release(1, new ReleaseRequest(5));

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Release_Returns400_WhenProductNotFound()
    {
        var (controller, context) = CreateController();
        using var _ = context;

        var result = await controller.Release(9999, new ReleaseRequest(5));

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
