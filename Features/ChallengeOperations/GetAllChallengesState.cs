using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.ChallengeOperations.GetAllChallengesState;

namespace Coil.Api.Features.ChallengeOperations
{
    public static class GetAllChallengesState
    {
        public record AllChallengesStateQuery() : IRequest<Result<List<ChallengesState>>>;

        internal sealed class GetAllChallengesStateHandler(CoilApplicationDbContext _dbContext) : IRequestHandler<AllChallengesStateQuery, Result<List<ChallengesState>>>
        {
            public async Task<Result<List<ChallengesState>>> Handle(AllChallengesStateQuery request, CancellationToken cancellationToken)
            {
                var challengesStates = await _dbContext.ChallengesStates
                    .Include(cs => cs.Plant)
                    .Include(cs => cs.Challenge)
                    .ToListAsync(cancellationToken);

                return Result.Success(challengesStates);
            }
        }
    }

    public class GetAllChallengesStateEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/challengesstate", async (IRequestHandler<AllChallengesStateQuery, Result<List<ChallengesState>>> handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new AllChallengesStateQuery(), cancellationToken);
                return Results.Ok(result.Value);
            })
            .WithName("GetAllChallengesState")
            .WithTags("CoilApi")
            .RequireAuthorization("coil.api")
            .Produces(StatusCodes.Status200OK, typeof(List<ChallengesState>))
            .WithOpenApi();
        }
    }
}
