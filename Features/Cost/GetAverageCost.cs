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
        public record GetAverageCostQuery(DateTime? startDate, DateTime? endDate) : IRequest<Result<GetAverageCostResponse>>;
        public record GetAverageCostResponse(string RawMaterialName, decimal AverageCost);

        internal class GetAverageCostHandler(CoilApplicationDbContext coilApplicationDbContext) : IRequestHandler<GetAverageCostQuery, Result<GetAverageCostResponse>>
        {
            public async Task<Result<GetAverageCostResponse>> Handle(GetAverageCostQuery request, CancellationToken cancellationToken)
            {
                var rawMaterialPurchases = await coilApplicationDbContext.RawMaterialPurchases.Include(x=>x.RawMaterial)
                    .Where(x => x.PurchaseDate >= request.startDate && x.PurchaseDate <= request.endDate)
                    .ToListAsync(cancellationToken);

                var expenses = await coilApplicationDbContext.Expenses
                    .Where(x => x.ExpenseDate >= request.startDate && x.ExpenseDate <= request.endDate)
                    .ToListAsync(cancellationToken);

                var sales = await coilApplicationDbContext.Sales
                    .Where(x => x.SaleDate >= request.startDate && x.SaleDate <= request.endDate)
                    .ToListAsync(cancellationToken);

                var totalExpenses = expenses.Sum(e => e.TotalBillAmount);
                var totalSales = (decimal)sales.Sum(s => s.Weight);

                if (totalSales == 0)
                {
                    return Result<GetAverageCostResponse>.Success(new GetAverageCostResponse(string.Empty,0));
                }

                var groupedCosts = rawMaterialPurchases
                    .GroupBy(p => p.RawMaterial.RawMaterialName)
                    .Select(group =>
                    {
                        var materialCost = group.Sum(p => p.Weight);
                        var wastageCost = group.Sum(p => p.Weight); // Replace with actual property name if different

                        var averageCost = (materialCost + wastageCost + totalExpenses) / totalSales;

                        return new GetAverageCostResponse(
                            RawMaterialName: group.Key,
                            AverageCost: Math.Round(averageCost, 2)
                        );
                    });

                return Result.Success(groupedCosts.First());
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
                    IRequestHandler<GetAverageCostQuery, Result<GetAverageCostResponse>> handler,
                    CancellationToken cancellationToken) =>
                {
                    var normalizedStartDate = DateTime.SpecifyKind(startDate ?? DateTime.UtcNow.AddDays(-15), DateTimeKind.Utc);
                    var normalizedEndDate = DateTime.SpecifyKind(endDate ?? DateTime.UtcNow, DateTimeKind.Utc);
                    var query = new GetAverageCostQuery(normalizedStartDate, normalizedEndDate);
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