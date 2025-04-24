using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.Plants.GetAllPlantDetails;

namespace Coil.Api.Features.Plants
{
    public static class GetAllPlantDetails
    {
        public record AllPlantDetailsQuery() : IRequest<Result<List<Plant>>>;
            
        internal sealed class GetAllPlantDetailsHandler(CoilApplicationDbContext _dbContext) : IRequestHandler<AllPlantDetailsQuery, Result<List<Plant>>>
        {
            public async Task<Result<List<Plant>>> Handle(AllPlantDetailsQuery request, CancellationToken cancellationToken)
            {
                var plants = await _dbContext.Plants.Include(p => p.Parties).ToListAsync(cancellationToken);

                if (plants is null || plants.Count == 0)
                {
                    return Result.Failure<List<Plant>>(new Error(
                        "GetAllPlantDetails.NotFound",
                        "No plants were found in the database."));
                }

                return Result.Success(plants);
            }
        }
    }

    public class GetAllPlantDetailsEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/plants", async (IRequestHandler<AllPlantDetailsQuery, Result<List<Plant>>> requestHandler, CancellationToken cancellationToken) =>
            {
                var result = await requestHandler.Handle(new AllPlantDetailsQuery(), cancellationToken);

                return Results.Ok(result.Value);
            })
            .WithName("GetPlantDetails")
            .WithTags("CoilApi")
            .RequireAuthorization("coil.api")
            .Produces(StatusCodes.Status200OK, typeof(List<Plant>))
            .WithOpenApi();
        }
    }
}
