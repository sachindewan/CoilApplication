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

                return Result.Success(rawMaterialQuantity);
            }
        }
    }

    public class GetRawMaterialQuantityEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/rawmaterialquantity/{rawMaterialId:int}", async (int rawMaterialId, IRequestHandler<RawMaterialQuantityQuery, Result<RawMaterialQuantity>> handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new RawMaterialQuantityQuery(rawMaterialId), cancellationToken);
                if (result.IsFailure)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Invalid Request",
                        Detail = result.Error.Message,
                        Instance = $"/rawmaterialquantity/{rawMaterialId}"
                    };

                    return Results.Problem(problemDetails);
                }
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
