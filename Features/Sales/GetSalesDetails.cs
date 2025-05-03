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
        public record GetSalesDetailsQuery(DateTime StartDate, DateTime EndDate) : IRequest<Result<List<Sale>>>;

        internal sealed class GetSalesDetailsQueryHandler(CoilApplicationDbContext _dbContext) : IRequestHandler<GetSalesDetailsQuery, Result<List<Sale>>>
        {
            public async Task<Result<List<Sale>>> Handle(GetSalesDetailsQuery request, CancellationToken cancellationToken)
            {
                var sales = await _dbContext.Sales
                    .Where(s => s.SaleDate >= request.StartDate && s.SaleDate <= request.EndDate)
                    .ToListAsync(cancellationToken);

                return Result.Success(sales);
            }
        }
    }
    public class GetSalesDetailsEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/sales", async ([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, IRequestHandler<GetSalesDetailsQuery, Result<List<Sale>>> handler, CancellationToken cancellationToken) =>
            {
                var normalizedStartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
                var normalizedEndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

                var query = new GetSalesDetailsQuery(normalizedStartDate, normalizedEndDate);
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
