using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.Sales.GetSalesDetails;

namespace Coil.Api.Features.Sales
{
    public static class GetSalesDetails
    {
        public record GetSalesDetailsQuery(DateTime StartDate, DateTime EndDate, int? PlantId) : IRequest<Result<List<Sale>>>;

        internal sealed class GetSalesDetailsQueryHandler(CoilApplicationDbContext _dbContext) : IRequestHandler<GetSalesDetailsQuery, Result<List<Sale>>>
        {
            public async Task<Result<List<Sale>>> Handle(GetSalesDetailsQuery request, CancellationToken cancellationToken)
            {
                var query = _dbContext.Sales.AsQueryable();
                query = query.Where(s => s.SaleDate >= request.StartDate && s.SaleDate <= request.EndDate);

                if (request.PlantId.HasValue)
                {
                    query = query.Where(s => s.PlantId == request.PlantId.Value);
                }

                var sales = await query.ToListAsync(cancellationToken);

                return Result.Success(sales);
            }
        }
    }
    public class GetSalesDetailsEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/sales", async (
                [FromQuery] DateTime startDate,
                [FromQuery] DateTime endDate,
                [FromQuery] int? plantId,
                IRequestHandler<GetSalesDetailsQuery, Result<List<Sale>>> handler,
                CancellationToken cancellationToken) =>
            {
                var normalizedStartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
                var normalizedEndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

                var query = new GetSalesDetailsQuery(normalizedStartDate, normalizedEndDate, plantId);
                var result = await handler.Handle(query, cancellationToken);

                return Results.Ok(result.Value);
            })
            .WithName("GetSalesDetails")
            .WithTags("CoilApi")
            .RequireAuthorization("coil.api")
            .Produces(StatusCodes.Status200OK, typeof(List<Sale>))
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();
        }
    }
}
