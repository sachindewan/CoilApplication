using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.Parties.GetAllPartiesDetails;

namespace Coil.Api.Features.Parties
{
    public static class GetAllPartiesDetails
    {
        public record AllPartiesDetailsQuery() : IRequest<Result<List<Party>>>;

        internal sealed class GetAllPartiesDetailsHandler(CoilApplicationDbContext _dbContext) : IRequestHandler<AllPartiesDetailsQuery, Result<List<Party>>>
        {
            public async Task<Result<List<Party>>> Handle(AllPartiesDetailsQuery request, CancellationToken cancellationToken)
            {
                var parties = await _dbContext.Parties
                    .ToListAsync(cancellationToken);

                if (parties is null || parties.Count == 0)
                {
                    return Result.Failure<List<Party>>(new Error(
                        "GetAllPartiesDetails.NotFound",
                        "No parties were found in the database."));
                }

                return Result.Success(parties);
            }
        }
    }

    public class GetAllPartiesDetailsEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/parties", async (IRequestHandler<AllPartiesDetailsQuery, Result<List<Party>>> requestHandler, CancellationToken cancellationToken) =>
            {
                var result = await requestHandler.Handle(new AllPartiesDetailsQuery(), cancellationToken);

                if (result.IsFailure)
                {
                    return Results.NotFound(result.Error.Message);
                }

                return Results.Ok(result.Value);
            })
            .WithName("GetPartiesDetails")
            .WithTags("CoilApi")
            .RequireAuthorization("coil.api")
            .Produces(StatusCodes.Status200OK, typeof(List<Party>))
            .WithOpenApi();
        }
    }
}
