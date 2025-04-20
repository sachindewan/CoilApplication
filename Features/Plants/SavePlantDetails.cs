using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.Plants.SavePlantDetails;

namespace Coil.Api.Features.Plants
{
    public static class SavePlantDetails
    {
        public record SavePlantCommand(string PlantName, string Location) : IRequest<Result<Plant>>;

        internal sealed class SavePlantCommandHandler(CoilApplicationDbContext _dbContext) : IRequestHandler<SavePlantCommand, Result<Plant>>
        {
            public async Task<Result<Plant>> Handle(SavePlantCommand request, CancellationToken cancellationToken)
            {
                // Check if a Plant with the same name and location already exists
                var plantExists = await _dbContext.Plants.AnyAsync(p => p.PlantName.Trim().Equals(request.PlantName.Trim(), StringComparison.CurrentCultureIgnoreCase) && p.Location.Trim().Equals(request.Location.Trim(), StringComparison.CurrentCultureIgnoreCase), cancellationToken);
                if (plantExists)
                {
                    return Result.Failure<Plant>(new Error(
                        "SavePlantCommand.DuplicatePlant",
                        $"A plant with the name '{request.PlantName}' and location '{request.Location}' already exists."));
                }

                // Create a new Plant entity
                var newPlant = new Plant
                {
                    PlantName = request.PlantName.Trim(),
                    Location = request.Location.Trim(),
                    Parties = []
                };

                // Add and save the Plant
                _dbContext.Plants.Add(newPlant);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return Result.Success(newPlant);
            }
        }
    }

    public class SavePlantsDetailsEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/plant", async (SavePlantCommand command, IRequestHandler<SavePlantCommand, Result<Plant>> handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(command, cancellationToken);

                if (result.IsFailure)
                {
                    return Results.BadRequest(result.Error.Message);
                }

                return Results.Created($"/plant/{result.Value.PlantId}", result.Value);
            })
            .WithName("SavePlantDetails")
            .WithTags("CoilApi")
            .RequireAuthorization("coil.api")
            .Produces(StatusCodes.Status201Created, typeof(Plant))
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();
        }
    }
}
