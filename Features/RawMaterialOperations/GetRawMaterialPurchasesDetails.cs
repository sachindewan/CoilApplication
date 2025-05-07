using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.RawMaterialOperations.GetRawMaterialPurchasesDetails;

namespace Coil.Api.Features.RawMaterialOperations
{
    public static class GetRawMaterialPurchasesDetails
    {
        public record GetRawMaterialPurchasesQuery(int? PlantId) : IRequest<Result<List<RawMaterialPurchase>>>;

        internal sealed class GetRawMaterialPurchasesQueryHandler(CoilApplicationDbContext _dbContext) : IRequestHandler<GetRawMaterialPurchasesQuery, Result<List<RawMaterialPurchase>>>
        {
            public async Task<Result<List<RawMaterialPurchase>>> Handle(GetRawMaterialPurchasesQuery request, CancellationToken cancellationToken)
            {
                var query = _dbContext.RawMaterialPurchases
                    .Include(rmp => rmp.RawMaterial)
                    .Include(rmp => rmp.Party)
                    .AsQueryable();

                // Apply PlantId filter if provided
                if (request.PlantId.HasValue)
                {
                    query = query.Where(rmp => rmp.PlantId == request.PlantId.Value);
                }

                var rawMaterialPurchases = await query.ToListAsync(cancellationToken);

                return Result.Success(rawMaterialPurchases);
            }
        }
    }

    public class GetRawMaterialPurchasesDetailsEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/rawmaterialpurchases", async (
                [FromQuery] int? plantId,
                IRequestHandler<GetRawMaterialPurchasesQuery, Result<List<RawMaterialPurchase>>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new GetRawMaterialPurchasesQuery(plantId), cancellationToken);
                return Results.Ok(result.Value);
            })
            .WithName("GetRawMaterialPurchasesDetails")
            .WithTags("CoilApi")
            .RequireAuthorization("coil.api")
            .Produces(StatusCodes.Status200OK, typeof(List<RawMaterialPurchase>))
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();
        }
    }
}
