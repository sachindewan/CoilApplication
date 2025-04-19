using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.Parties.SavePartyDetails;

namespace Coil.Api.Features.Parties
{
    public static class SavePartyDetails
    {
        public record SavePartyCommand(string PartyName, int PlantId) : IRequest<Result<Party>>;

        internal sealed class SavePartyCommandHandler(CoilApplicationDbContext _dbContext) : IRequestHandler<SavePartyCommand, Result<Party>>
        {
            public async Task<Result<Party>> Handle(SavePartyCommand request, CancellationToken cancellationToken)
            {
                // Check if the Plant exists
                var plantExists = await _dbContext.Plants.AnyAsync(p => p.PlantId == request.PlantId, cancellationToken);
                if (!plantExists)
                {
                    return Result.Failure<Party>(new Error(
                        "SavePartyCommand.PlantNotFound",
                        $"Plant with ID {request.PlantId} does not exist."));
                }

                // Check if a Party with the same PartyName and PlantId already exists
                var partyExists = await _dbContext.Parties.AnyAsync(p => p.PartyName.Trim().ToLower() == request.PartyName.Trim().ToLower() && p.PlantId == request.PlantId, cancellationToken);
                if (partyExists)
                {
                    return Result.Failure<Party>(new Error(
                        "SavePartyCommand.DuplicateParty",
                        $"A party with the name '{request.PartyName}' already exists for Plant ID {request.PlantId}."));
                }

                // Create a new Party entity
                var newParty = new Party
                {
                    PartyName = request.PartyName.Trim(),
                    PlantId = request.PlantId
                };

                // Add and save the Party
                _dbContext.Parties.Add(newParty);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return Result.Success(newParty);
            }
        }
    }

    public class SavePartiesDetailsEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/party", async (SavePartyCommand command, IRequestHandler<SavePartyCommand, Result<Party>> handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(command, cancellationToken);

                if (result.IsFailure)
                {
                    return Results.BadRequest(result.Error.Message);
                }

                return Results.Created($"/party/{result.Value.PartyId}", result.Value);
            })
            .WithName("SavePartyDetails")
            .WithTags("CoilApi")
            .RequireAuthorization("coil.api")
            .Produces(StatusCodes.Status201Created, typeof(Party))
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();
        }
    }
}
