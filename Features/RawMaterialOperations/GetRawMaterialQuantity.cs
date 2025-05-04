using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.RawMaterialOperations.GetRawMaterialQuantity;

namespace Coil.Api.Features.RawMaterialOperations
{
    public static class GetRawMaterialQuantity
    {
        public record RawMaterialQuantityQuery(int? RawMaterialId, int PlantId) : IRequest<Result<List<RawMaterialQuantity>>>;

        internal sealed class GetRawMaterialQuantityHandler(CoilApplicationDbContext _dbContext) : IRequestHandler<RawMaterialQuantityQuery, Result<List<RawMaterialQuantity>>>
        {
            public async Task<Result<List<RawMaterialQuantity>>> Handle(RawMaterialQuantityQuery request, CancellationToken cancellationToken)
            {
                List<RawMaterialQuantity> rawMaterialQuantities = new List<RawMaterialQuantity>();
                var query = _dbContext.RawMaterialQuantities
                    .Include(rmq => rmq.RawMaterial)
                    .Include(rmq => rmq.Plant)
                    .Where(rmq => rmq.PlantId == request.PlantId);

                if (request.RawMaterialId == null)
                {
                    rawMaterialQuantities = await query.ToListAsync(cancellationToken);
                }
                else
                {
                    var rawMaterialQuantity = await query
                        .FirstOrDefaultAsync(rmq => rmq.RawMaterialId == request.RawMaterialId, cancellationToken);
                    rawMaterialQuantities.Add(rawMaterialQuantity);
                }
                return Result.Success<List<RawMaterialQuantity>>(rawMaterialQuantities);
            }
        }
    }

    public class GetRawMaterialQuantityEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/rawmaterialquantity", async ([FromQuery] int? rawMaterialId, [FromQuery] int plantId, IRequestHandler<RawMaterialQuantityQuery, Result<List<RawMaterialQuantity>>> handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new RawMaterialQuantityQuery(rawMaterialId, plantId), cancellationToken);
                return Results.Ok(result.Value);
            })
            .WithName("GetRawMaterialQuantity")
            .WithTags("CoilApi")
            .RequireAuthorization("coil.api")
            .Produces(StatusCodes.Status200OK, typeof(object))
            .Produces(StatusCodes.Status404NotFound)
            .WithOpenApi();
        }
    }
}
