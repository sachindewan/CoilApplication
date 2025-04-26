using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.ChallengeOperations.SaveChallengeStateDetails;

namespace Coil.Api.Features.ChallengeOperations
{
    public static class SaveChallengeStateDetails
    {
        public record SaveChallengeStateCommand(
            int PlantId,
            int ChallengeId,
            DateTime ChallengeStartDateTime
        ) : IRequest<Result<ChallengesState>>;

        public sealed class CreateChallengesStateValidator : AbstractValidator<SaveChallengeStateCommand>
        {
            public CreateChallengesStateValidator()
            {
                RuleFor(x => x.PlantId)
                    .GreaterThan(0)
                    .WithMessage("Plant ID is required and must be greater than zero.");

                RuleFor(x => x.ChallengeId)
                    .GreaterThan(0)
                    .WithMessage("Challenge ID is required and must be greater than zero.");

                RuleFor(x => x.ChallengeStartDateTime)
                    .NotEmpty()
                    .WithMessage("Challenge Start DateTime is required.")
                    .LessThanOrEqualTo(DateTime.UtcNow)
                    .WithMessage("Challenge start date cannot be in the future.");
            }
        }

        internal sealed class SaveChallengesStateCommandHandler(CoilApplicationDbContext _dbContext) : IRequestHandler<SaveChallengeStateCommand, Result<ChallengesState>>
        {
            public async Task<Result<ChallengesState>> Handle(SaveChallengeStateCommand request, CancellationToken cancellationToken)
            {
                // Validate Plant existence
                var plantExists = await _dbContext.Plants.AnyAsync(p => p.PlantId == request.PlantId, cancellationToken);
                if (!plantExists)
                {
                    return Result.Failure<ChallengesState>(new Error(
                        "SaveChallengeStateCommand.PlantNotFound",
                        $"Plant with ID {request.PlantId} does not exist."));
                }

                // Validate Challenge existence
                var challengeExists = await _dbContext.Challenges.AnyAsync(c => c.ChallengeId == request.ChallengeId, cancellationToken);
                if (!challengeExists)
                {
                    return Result.Failure<ChallengesState>(new Error(
                        "SaveChallengeStateCommand.ChallengeNotFound",
                        $"Challenge with ID {request.ChallengeId} does not exist."));
                }

                // Validate duplicate entry
                var duplicateExists = await _dbContext.ChallengesStates.AnyAsync(cs =>
                    cs.PlantId == request.PlantId &&
                    cs.ChallengeId == request.ChallengeId &&
                    cs.State == true, cancellationToken);

                if (duplicateExists)
                {
                    return Result.Failure<ChallengesState>(new Error(
                        "SaveChallengeStateCommand.DuplicateEntry",
                        $"The selected challenge for Plant ID {request.PlantId} already exists with 'OPEN' state."));
                }

                // Create a new ChallengesState entity
                var newChallengesState = new ChallengesState
                {
                    PlantId = request.PlantId,
                    ChallengeId = request.ChallengeId,
                    ChallengeStartDateTime = request.ChallengeStartDateTime
                };

                // Add the new challenge state
                _dbContext.ChallengesStates.Add(newChallengesState);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return Result.Success(newChallengesState);
            }
        }
    }

    public class SaveChallengesStateDetailsEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/challengesstate", async (SaveChallengeStateCommand command, IRequestHandler<SaveChallengeStateCommand, Result<ChallengesState>> handler, IValidator<SaveChallengeStateCommand> validator, CancellationToken cancellationToken) =>
            {
                var validationResult = await validator.ValidateAsync(command, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Invalid Request",
                        Detail = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)),
                        Instance = "/challengesstate"
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
                        Instance = "/challengesstate"
                    };
                    return Results.Problem(problemDetails);
                }

                return Results.Created($"/challengesstate/{result.Value.ChallengesStateId}", result.Value);
            })
            .WithName("SaveChallengeStateDetails")
            .WithTags("CoilApi")
            .RequireAuthorization("coil.api")
            .Produces(StatusCodes.Status201Created, typeof(ChallengesState))
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();
        }
    }
}
