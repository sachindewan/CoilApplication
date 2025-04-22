using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.Parties.SavePartyDetails;

namespace Coil.Api.Features.Parties
{
    public static class SavePartyDetails
    {
        public record SavePartyCommand(string PartyName, int PlantId) : IRequest<Result<Party>>;

        public sealed class CreatePartyValidator : AbstractValidator<SavePartyCommand>
        {
            public CreatePartyValidator()
            {
                RuleFor(x => x.PartyName)
                    .Must(name => !string.IsNullOrWhiteSpace(name))
                    .WithMessage("Party name is required.")
                    .MaximumLength(1000)
                    .WithMessage("Party name exceeds 1000 characters")
                    .Must(name => name.Any(char.IsLetterOrDigit))
                    .WithMessage("Party name must contain at least one letter or number.")
                    .Must(name => char.IsLetter(name.Trim()[0]))
                    .WithMessage("Party name must start with an alphabet.");
                RuleFor(x => x.PlantId)
                    .GreaterThan(0)
                    .WithMessage("Plant ID must be greater than zero.");
            }
        }

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
            app.MapPost("/party", async (SavePartyCommand command, IRequestHandler<SavePartyCommand, Result<Party>> handler, IValidator<SavePartyCommand> validator, CancellationToken cancellationToken) =>
            {
                var validationResult = await validator.ValidateAsync(command, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Invalid Request",
                        Detail = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)),
                        Instance = "/party"
                    };
                    return Results.Problem(problemDetails);
                }

                var result = await handler.Handle(command, cancellationToken);

                if (result.IsFailure)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Invalid Request",
                        Detail = result.Error.Message,
                        Instance = "/party"
                    };
                    return Results.Problem(problemDetails);
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
