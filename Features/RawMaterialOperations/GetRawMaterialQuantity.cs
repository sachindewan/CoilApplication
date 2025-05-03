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
        public record RawMaterialQuantityQuery(int? RawMaterialId, int PlantId) : IRequest<Result<object>>;

        internal sealed class GetRawMaterialQuantityHandler(CoilApplicationDbContext _dbContext) : IRequestHandler<RawMaterialQuantityQuery, Result<object>>
        {
            public async Task<Result<object>> Handle(RawMaterialQuantityQuery request, CancellationToken cancellationToken)
            {
                var query = _dbContext.RawMaterialQuantities
                    .Include(rmq => rmq.RawMaterial)
                    .Include(rmq => rmq.Plant)
                    .Where(rmq => rmq.PlantId == request.PlantId);

                if (request.RawMaterialId == null)
                {
                    var rawMaterialQuantities = await query.ToListAsync(cancellationToken);

                    if (rawMaterialQuantities.Count == 0)
                    {
                        return Result.Failure<object>(new Error(
                            "GetRawMaterialQuantity.NotFound",
                            $"No raw material quantities found for PlantId {request.PlantId}."));
                    }

                    return Result.Success<object>(rawMaterialQuantities);
                }
                else
                {
                    var rawMaterialQuantity = await query
                        .FirstOrDefaultAsync(rmq => rmq.RawMaterialId == request.RawMaterialId, cancellationToken);

                    if (rawMaterialQuantity == null)
                    {
                        return Result.Failure<object>(new Error(
                            "GetRawMaterialQuantity.NotFound",
                            $"Raw material quantity for RawMaterialId {request.RawMaterialId} & PlantId {request.PlantId} was not found."));
                    }

                    return Result.Success<object>(rawMaterialQuantity);
                }
            }
        }
    }

    public class GetRawMaterialQuantityEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/rawmaterialquantity", async ([FromQuery] int? rawMaterialId, [FromQuery] int plantId, IRequestHandler<RawMaterialQuantityQuery, Result<object>> handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new RawMaterialQuantityQuery(rawMaterialId, plantId), cancellationToken);
                if (result.IsFailure)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Invalid Request",
                        Detail = result.Error.Message,
                        Instance = $"/rawmaterialquantity/{rawMaterialId}/{plantId}"
                    };

                    return Results.Problem(problemDetails);
                }
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
