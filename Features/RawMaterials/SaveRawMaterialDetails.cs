using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.RawMaterials.SaveRawMaterialDetails;

namespace Coil.Api.Features.RawMaterials
{
    public static class SaveRawMaterialDetails
    {
        public record SaveRawMaterialCommand(string RawMaterialName) : IRequest<Result<RawMaterial>>;

        internal sealed class SaveRawMaterialCommandHandler(CoilApplicationDbContext _dbContext) : IRequestHandler<SaveRawMaterialCommand, Result<RawMaterial>>
        {
            public async Task<Result<RawMaterial>> Handle(SaveRawMaterialCommand request, CancellationToken cancellationToken)
            {
                // Check if a RawMaterial with the same name already exists
                var rawMaterialExists = await _dbContext.RawMaterials.AnyAsync(rm => rm.RawMaterialName.Trim().Equals(request.RawMaterialName, StringComparison.CurrentCultureIgnoreCase), cancellationToken);
                if (rawMaterialExists)
                {
                    return Result.Failure<RawMaterial>(new Error(
                        "SaveRawMaterialCommand.DuplicateRawMaterial",
                        $"A raw material with the name '{request.RawMaterialName}' already exists."));
                }

                // Create a new RawMaterial entity
                var newRawMaterial = new RawMaterial
                {
                    RawMaterialName = request.RawMaterialName.Trim()
                };

                // Add and save the RawMaterial
                _dbContext.RawMaterials.Add(newRawMaterial);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return Result.Success(newRawMaterial);
            }
        }
    }

    public class SaveRawMaterialDetailsEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/rawmaterial", async (SaveRawMaterialCommand command, IRequestHandler<SaveRawMaterialCommand, Result<RawMaterial>> handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(command, cancellationToken);

                if (result.IsFailure)
                {
                    return Results.BadRequest(result.Error.Message);
                }

                return Results.Created($"/rawmaterial/{result.Value.RawMaterialId}", result.Value);
            })
            .WithName("SaveRawMaterialDetails")
            .WithTags("CoilApi")
            .RequireAuthorization("coil.api")
            .Produces(StatusCodes.Status201Created, typeof(RawMaterial))
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();
        }
    }
}
