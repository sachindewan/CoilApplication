using Carter;
using Coil.Api.Database;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.Wastages.GetAvailableQuantity;

namespace Coil.Api.Features.Wastages
{
    public static class GetAvailableQuantity
    {
        public record GetAvailableQuantityQuery(int PlantId, int RawMaterialId) : IRequest<Result<double>>;

        internal sealed class GetAvailableQuantityQueryHandler(CoilApplicationDbContext _dbContext) : IRequestHandler<GetAvailableQuantityQuery, Result<double>>
        {
            public async Task<Result<double>> Handle(GetAvailableQuantityQuery request, CancellationToken cancellationToken)
            {
                // Validate Plant existence
                var plantExists = await _dbContext.Plants.AnyAsync(p => p.PlantId == request.PlantId, cancellationToken);
                if (!plantExists)
                {
                    return Result.Failure<double>(new Error(
                        "GetAvailableQuantityQuery.PlantNotFound",
                        $"Plant with ID {request.PlantId} does not exist."));
                }

                // Validate RawMaterial existence
                var rawMaterialExists = await _dbContext.RawMaterials.AnyAsync(rm => rm.RawMaterialId == request.RawMaterialId, cancellationToken);
                if (!rawMaterialExists)
                {
                    return Result.Failure<double>(new Error(
                        "GetAvailableQuantityQuery.RawMaterialNotFound",
                        $"Raw Material with ID {request.RawMaterialId} does not exist."));
                }

                // Fetch the last wastage date for the given PlantId and RawMaterialId
                var lastWastageDate = await _dbContext.Wastages
                    .Where(w => w.PlantId == request.PlantId && w.RawMaterialId == request.RawMaterialId)
                    .OrderByDescending(w => w.CreatedOn)
                    .Select(w => (DateTime?)w.CreatedOn)
                    .FirstOrDefaultAsync(cancellationToken);

                // Fetch the total quantity of raw material purchased after the last wastage date
                var availableQuantity = await _dbContext.RawMaterialPurchases
                    .Where(rmp => rmp.RawMaterialId == request.RawMaterialId &&
                                  rmp.PlantId == request.PlantId &&
                                  (lastWastageDate == null || rmp.PurchaseDate > lastWastageDate))
                    .SumAsync(rmp => (double?)rmp.Weight, cancellationToken) ?? 0;

                return Result.Success(availableQuantity);
            }
        }
    }

    public class GetAvailableQuantityEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/wastage/availablequantity/{plantId:int}/{rawMaterialId:int}", async (
                int plantId, int rawMaterialId,
                IRequestHandler<GetAvailableQuantityQuery, Result<double>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new GetAvailableQuantityQuery(plantId, rawMaterialId), cancellationToken);

                if (result.IsFailure)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Invalid Request",
                        Detail = result.Error.Message,
                        Instance = $"/wastage/availablequantity/{plantId}/{rawMaterialId}"
                    };
                    return Results.Problem(problemDetails);
                }

                return Results.Ok(result.Value);
            })
            .WithName("GetAvailableQuantity")
            .WithTags("CoilApi")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();
        }
    }
}
