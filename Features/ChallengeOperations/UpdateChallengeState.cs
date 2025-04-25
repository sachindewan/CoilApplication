using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.ChallengeOperations.UpdateChallengeState;

namespace Coil.Api.Features.ChallengeOperations
{
    public static class UpdateChallengeState
    {
        public record UpdateChallengeStateCommand(int ChallengesStateId) : IRequest<Result<ChallengesState>>;

        public sealed class UpdateChallengeStateValidator : AbstractValidator<UpdateChallengeStateCommand>
        {
            public UpdateChallengeStateValidator()
            {
                RuleFor(x => x.ChallengesStateId)
                    .GreaterThan(0)
                    .WithMessage("ChallengesStateId must be greater than zero.");
            }
        }

        internal sealed class UpdateChallengeStateCommandHandler(CoilApplicationDbContext _dbContext) : IRequestHandler<UpdateChallengeStateCommand, Result<ChallengesState>>
        {
            private static readonly string ChallengeStateClosed = "CLOSED";

            public async Task<Result<ChallengesState>> Handle(UpdateChallengeStateCommand request, CancellationToken cancellationToken)
            {
                // Fetch the ChallengesState entity
                var challengesState = await _dbContext.ChallengesStates
                    .Include(cs => cs.Plant)
                    .Include(cs => cs.Challenge)
                    .FirstOrDefaultAsync(cs => cs.ChallengesStateId == request.ChallengesStateId, cancellationToken);

                if (challengesState == null)
                {
                    return Result.Failure<ChallengesState>(new Error(
                        "UpdateChallengeState.NotFound",
                        $"ChallengesState with ID {request.ChallengesStateId} does not exist."));
                }

                // Check if the ChallengeState is already CLOSED
                if (challengesState.ChallengeState == ChallengeStateClosed)
                {
                    return Result.Failure<ChallengesState>(new Error(
                        "UpdateChallengeState.AlreadyClosed",
                        $"ChallengesState with ID {request.ChallengesStateId} is already closed."));
                }

                // Update the ChallengeState property
                challengesState.ChallengeState = ChallengeStateClosed;

                // Save changes to the database
                await _dbContext.SaveChangesAsync(cancellationToken);

                return Result.Success(challengesState);
            }
        }
    }
    public class UpdateChallengeStateEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPut("/updatechallengestate/{id}/closed", async (
                int id,
                IRequestHandler<UpdateChallengeStateCommand, Result<ChallengesState>> handler,
                IValidator<UpdateChallengeStateCommand> validator,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateChallengeStateCommand(id);

                var validationResult = await validator.ValidateAsync(command, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Invalid Request",
                        Detail = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)),
                        Instance = $"/updatechallengestate/{id}/closed"
                    };
                    return Results.Problem(problemDetails);
                }

                var result = await handler.Handle(command, cancellationToken);

                if (result.IsFailure)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status404NotFound,
                        Title = "Invalid Request",
                        Detail = result.Error.Message,
                        Instance = $"/updatechallengestate/{id}/closed"
                    };
                    return Results.Problem(problemDetails);
                }

                return Results.Ok(result.Value);
            })
            .WithName("UpdateChallengeState")
            .WithTags("CoilApi")
            .RequireAuthorization("coil.api")
            .Produces(StatusCodes.Status201Created, typeof(string))
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();
        }
    }
}
