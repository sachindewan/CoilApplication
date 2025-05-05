using Carter;
using Coil.Api.Database;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.Cost.GetAverageCost;

namespace Coil.Api.Features.Cost
{
    public static class GetAverageCost
    {
        public record GetAverageCostQuery(DateTime? StartDate, DateTime? EndDate, int? PlantId) : IRequest<Result<List<GetAverageCostResponse>>>;
        public record GetAverageCostResponse(string RawMaterialName, decimal AverageCost);

        internal class GetAverageCostHandler(CoilApplicationDbContext coilApplicationDbContext) : IRequestHandler<GetAverageCostQuery, Result<List<GetAverageCostResponse>>>
        {
            public async Task<Result<List<GetAverageCostResponse>>> Handle(GetAverageCostQuery request, CancellationToken cancellationToken)
            {
                var rawMaterialPurchasesQuery = coilApplicationDbContext.RawMaterialPurchases
                    .Include(x => x.RawMaterial)
                    .Where(x => x.PurchaseDate >= request.StartDate && x.PurchaseDate <= request.EndDate);

                var expensesQuery = coilApplicationDbContext.Expenses
                    .Where(x => x.ExpenseDate >= request.StartDate && x.ExpenseDate <= request.EndDate);

                var salesQuery = coilApplicationDbContext.Sales
                    .Where(x => x.SaleDate >= request.StartDate && x.SaleDate <= request.EndDate);

                // Apply PlantId filter if provided
                if (request.PlantId.HasValue)
                {
                    rawMaterialPurchasesQuery = rawMaterialPurchasesQuery.Where(x => x.PlantId == request.PlantId.Value);
                    expensesQuery = expensesQuery.Where(x => x.PlantId == request.PlantId.Value);
                    salesQuery = salesQuery.Where(x => x.PlantId == request.PlantId.Value);
                }

                var rawMaterialPurchases = await rawMaterialPurchasesQuery.ToListAsync(cancellationToken);
                var expenses = await expensesQuery.ToListAsync(cancellationToken);
                var sales = await salesQuery.ToListAsync(cancellationToken);

                var totalExpenses = expenses.Sum(e => e.TotalBillAmount);
                var totalSales = (decimal)sales.Sum(s => s.Weight);

                if (totalSales == 0)
                {
                    return Result.Success(new List<GetAverageCostResponse> { new(string.Empty, 0) });
                }

                var groupedCosts = rawMaterialPurchases
                    .GroupBy(p => p.RawMaterial.RawMaterialName)
                    .Select(group =>
                    {
                        var materialCost = group.Sum(p => p.TotalBillAmount);

                        var averageCost = (materialCost + totalExpenses) / totalSales;

                        return new GetAverageCostResponse(
                            RawMaterialName: group.Key,
                            AverageCost: Math.Round(averageCost, 2)
                        );
                    });

                return Result.Success(groupedCosts.ToList());
            }
        }
    }

    public class GetAverageCostEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/cost/average", async (
                [FromQuery] DateTime? startDate,
                [FromQuery] DateTime? endDate,
                [FromQuery] int? plantId,
                IRequestHandler<GetAverageCostQuery, Result<List<GetAverageCostResponse>>> handler,
                CancellationToken cancellationToken) =>
            {
                var normalizedStartDate = DateTime.SpecifyKind(startDate ?? DateTime.UtcNow.AddDays(-15), DateTimeKind.Utc);
                var normalizedEndDate = DateTime.SpecifyKind(endDate ?? DateTime.UtcNow, DateTimeKind.Utc);
                var query = new GetAverageCostQuery(normalizedStartDate, normalizedEndDate, plantId);
                var result = await handler.Handle(query, cancellationToken);

                if (result.IsFailure)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Failed to retrieve average cost",
                        Detail = result.Error.Message,
                        Instance = $"/cost/average"
                    };
                    return Results.Problem(problemDetails);
                }

                return Results.Ok(result.Value);
            })
            .WithName("GetAverageCost")
            .WithTags("CoilApi")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();
        }
    }
}