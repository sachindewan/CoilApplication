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
        public record AllChallengesStateQuery(int? PlantId, DateTime? StartDate, DateTime? EndDate) : IRequest<Result<List<ChallengesState>>>;

        internal sealed class GetAllChallengesStateHandler(CoilApplicationDbContext _dbContext) : IRequestHandler<AllChallengesStateQuery, Result<List<ChallengesState>>>
        {
            public async Task<Result<List<ChallengesState>>> Handle(AllChallengesStateQuery request, CancellationToken cancellationToken)
            {
                var query = _dbContext.ChallengesStates
                    .Include(cs => cs.Plant)
                    .Include(cs => cs.Challenge)
                    .AsQueryable();

                // Fetch open state challenges
                if (request.PlantId.HasValue && !request.StartDate.HasValue && !request.EndDate.HasValue)
                {
                    query = query.Where(cs => cs.PlantId == request.PlantId && cs.State == true);
                }

                // Fetch closed state challenges
                else if (request.PlantId.HasValue && request.StartDate.HasValue && request.EndDate.HasValue)
                {
                    query = query.Where(cs => cs.PlantId == request.PlantId &&
                                              cs.State == false &&
                                              cs.ChallengeStartDateTime >= request.StartDate &&
                                              cs.ChallengeStartDateTime <= request.EndDate);
                }

                var challengesStates = await query.ToListAsync(cancellationToken);

                return Result.Success(challengesStates);
            }
        }
    }

    public class GetAllChallengesStateEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/challengesstate", async (
                [FromQuery] int? plantId,
                [FromQuery] DateTime? startDate,
                [FromQuery] DateTime? endDate,
                IRequestHandler<AllChallengesStateQuery, Result<List<ChallengesState>>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new AllChallengesStateQuery(plantId, startDate, endDate), cancellationToken);
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
