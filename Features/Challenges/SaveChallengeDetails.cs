using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.Challenges.SaveChallengeDetails;

namespace Coil.Api.Features.Challenges
{
    public static class SaveChallengeDetails
    {
        public record SaveChallengeCommand(string ChallengeName) : IRequest<Result<Challenge>>;

        public sealed class CreateChallengeValidator : AbstractValidator<SaveChallengeCommand>
        {
            public CreateChallengeValidator()
            {
                RuleFor(x => x.ChallengeName)
                    .NotEmpty()
                    .WithMessage("Challenge Name is required.")
                    .MaximumLength(1000)
                    .WithMessage("Challenge Name exceeds 1000 characters.");
            }
        }

        internal sealed class SaveChallengeCommandHandler(CoilApplicationDbContext _dbContext) : IRequestHandler<SaveChallengeCommand, Result<Challenge>>
        {
            public async Task<Result<Challenge>> Handle(SaveChallengeCommand request, CancellationToken cancellationToken)
            {
                // Check if a challenge with the same name already exists
                var challengeExists = await _dbContext.Challenges
                    .AnyAsync(c => c.ChallengeName.Trim().ToLower() == request.ChallengeName.Trim().ToLower(), cancellationToken);

                if (challengeExists)
                {
                    return Result.Failure<Challenge>(new Error(
                        "SaveChallengeCommand.DuplicateChallengeName",
                        $"A challenge with the name '{request.ChallengeName}' already exists."));
                }

                // Create a new Challenge entity
                var newChallenge = new Challenge
                {
                    ChallengeName = request.ChallengeName.Trim()
                };

                // Add the new challenge to the database
                _dbContext.Challenges.Add(newChallenge);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return Result.Success(newChallenge);
            }
        }
    }

    public class SaveChallengeDetailsEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/challenges", async (SaveChallengeCommand command, IRequestHandler<SaveChallengeCommand, Result<Challenge>> handler, IValidator<SaveChallengeCommand> validator, CancellationToken cancellationToken) =>
            {
                var validationResult = await validator.ValidateAsync(command, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Invalid Request",
                        Detail = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)),
                        Instance = "/challenges"
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
                        Instance = "/challenges"
                    };
                    return Results.Problem(problemDetails);
                }

                return Results.Created($"/challenges/{result.Value.ChallengeId}", result.Value);
            })
            .WithName("SaveChallengeDetails")
            .WithTags("CoilApi")
            .RequireAuthorization("coil.api")
            .Produces(StatusCodes.Status201Created, typeof(Challenge))
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();
        }
    }
}
