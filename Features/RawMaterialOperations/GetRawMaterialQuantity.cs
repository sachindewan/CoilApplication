using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Coil.Api.Features.RawMaterials
{
    public static class GetRawMaterialQuantity
    {
        public record RawMaterialQuantityQuery(int RawMaterialId) : IRequest<Result<RawMaterialQuantity>>;

        internal sealed class GetRawMaterialQuantityHandler(CoilApplicationDbContext _dbContext) : IRequestHandler<RawMaterialQuantityQuery, Result<RawMaterialQuantity>>
        {
            public async Task<Result<RawMaterialQuantity>> Handle(RawMaterialQuantityQuery request, CancellationToken cancellationToken)
            {
                // Fetch the RawMaterialQuantity based on RawMaterialId
                var rawMaterialQuantity = await _dbContext.RawMaterialQuantities
                    .Include(rmq => rmq.RawMaterial) // Include related RawMaterial data
                    .FirstOrDefaultAsync(rmq => rmq.RawMaterialId == request.RawMaterialId, cancellationToken);

                if (rawMaterialQuantity == null)
                {
                    return Result.Failure<RawMaterialQuantity>(new Error(
                        "GetRawMaterialQuantity.NotFound",
                        $"Raw material quantity for RawMaterialId {request.RawMaterialId} was not found."));
                }

                return Result.Success<RawMaterialQuantity>(rawMaterialQuantity);
            }
        }
    }

    public class GetRawMaterialQuantityEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/rawmaterialquantity/{rawMaterialId:int}", async (int rawMaterialId, IRequestHandler<GetRawMaterialQuantity.RawMaterialQuantityQuery, Result<RawMaterialQuantity>> handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new GetRawMaterialQuantity.RawMaterialQuantityQuery(rawMaterialId), cancellationToken);
                return Results.Ok(result.Value);
            })
            .WithName("GetRawMaterialQuantity")
            .WithTags("CoilApi")
            .RequireAuthorization("coil.api")
            .Produces(StatusCodes.Status200OK, typeof(RawMaterialQuantity))
            .Produces(StatusCodes.Status404NotFound)
            .WithOpenApi();
        }
    }
}
