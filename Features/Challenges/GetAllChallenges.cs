using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.Challenges.GetAllChallenges;

namespace Coil.Api.Features.Challenges
{
    public static class GetAllChallenges
    {
        public record AllChallengesQuery() : IRequest<Result<List<Challenge>>>;

        internal sealed class GetAllChallengesHandler(CoilApplicationDbContext _dbContext) : IRequestHandler<AllChallengesQuery, Result<List<Challenge>>>
        {
            public async Task<Result<List<Challenge>>> Handle(AllChallengesQuery request, CancellationToken cancellationToken)
            {
                var challenges = await _dbContext.Challenges.ToListAsync(cancellationToken);
                if (challenges is null || challenges.Count == 0)
                {
                    return Result.Failure<List<Challenge>>(new Error(
                        "GetAllChallenges.NotFound",
                        "No challenges were found in the database."));
                }
                return Result.Success(challenges);
            }
        }
    }

    public class GetAllChallengesEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/challenges", async (IRequestHandler<AllChallengesQuery, Result<List<Challenge>>> handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new AllChallengesQuery(), cancellationToken);
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
                return Results.Ok(result.Value);
            })
            .WithName("GetAllChallenges")
            .WithTags("CoilApi")
            .RequireAuthorization("coil.api")
            .Produces(StatusCodes.Status200OK, typeof(List<Challenge>))
            .WithOpenApi();
        }
    }
}
