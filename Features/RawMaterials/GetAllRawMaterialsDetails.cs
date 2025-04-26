using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.RawMaterials.GetAllRawMaterialsDetails;

namespace Coil.Api.Features.RawMaterials
{
    public static class GetAllRawMaterialsDetails
    {
        public record AllRawMaterialsDetailsQuery() : IRequest<Result<List<RawMaterial>>>;

        internal sealed class GetAllRawMaterialsDetailsHandler(CoilApplicationDbContext _dbContext) : IRequestHandler<AllRawMaterialsDetailsQuery, Result<List<RawMaterial>>>
        {
            public async Task<Result<List<RawMaterial>>> Handle(AllRawMaterialsDetailsQuery request, CancellationToken cancellationToken)
            {
                var rawMaterials = await _dbContext.RawMaterials.ToListAsync(cancellationToken);

                return Result.Success(rawMaterials);
            }
        }
    }

    public class GetAllRawMaterialsDetailsEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/rawmaterials", async (IRequestHandler<AllRawMaterialsDetailsQuery, Result<List<RawMaterial>>> requestHandler, CancellationToken cancellationToken) =>
            {
                var result = await requestHandler.Handle(new AllRawMaterialsDetailsQuery(), cancellationToken);
                return Results.Ok(result.Value);
            })
            .WithName("GetRawMaterialsDetails")
            .WithTags("CoilApi")
            .RequireAuthorization("coil.api")
            .Produces(StatusCodes.Status200OK, typeof(List<RawMaterial>))
            .WithOpenApi();
        }
    }
}
