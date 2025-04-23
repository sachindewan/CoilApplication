using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.RawMaterials.SaveRawMaterialDetails;

namespace Coil.Api.Features.RawMaterials
{
    public static class SaveRawMaterialDetails
    {
        public record SaveRawMaterialCommand(string RawMaterialName) : IRequest<Result<RawMaterial>>;

        public sealed class CreateRawMaterialValidator : AbstractValidator<SaveRawMaterialCommand>
        {
            public CreateRawMaterialValidator()
            {
                RuleFor(x => x.RawMaterialName)
                    .Must(name => !string.IsNullOrWhiteSpace(name))
                    .WithMessage("Raw material name is required.")
                    .Must(name => name.Any(char.IsLetterOrDigit))
                    .WithMessage("Raw material name must contain at least one letter or number.")
                    .Must(name => char.IsLetter(name.Trim()[0]))
                    .WithMessage("Raw material name must start with an alphabet.");
            }
        }

        internal sealed class SaveRawMaterialCommandHandler(CoilApplicationDbContext _dbContext) : IRequestHandler<SaveRawMaterialCommand, Result<RawMaterial>>
        {
            public async Task<Result<RawMaterial>> Handle(SaveRawMaterialCommand request, CancellationToken cancellationToken)
            {
                // Start a database transaction
                using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    // Check if a RawMaterial with the same name already exists
                    var rawMaterialExists = await _dbContext.RawMaterials.AnyAsync(rm => rm.RawMaterialName.Trim().ToLower() == request.RawMaterialName.Trim().ToLower(), cancellationToken);
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

                    // Add an entry in the RawMaterialQuantity table with AvailableQuantity = 0
                    var rawMaterialQuantity = new RawMaterialQuantity
                    {
                        RawMaterialId = newRawMaterial.RawMaterialId,
                        AvailableQuantity = 0
                    };

                    _dbContext.RawMaterialQuantities.Add(rawMaterialQuantity);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    // Commit the transaction
                    await transaction.CommitAsync(cancellationToken);

                    return Result.Success(newRawMaterial);
                }
                catch (Exception ex)
                {
                    // Rollback the transaction in case of an error
                    await transaction.RollbackAsync(cancellationToken);
                    return Result.Failure<RawMaterial>(new Error(
                        "SaveRawMaterialCommand.TransactionFailed",
                        $"An error occurred while processing the request: {ex.Message}"));
                }
            }
        }
    }

    public class SaveRawMaterialDetailsEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/rawmaterial", async (SaveRawMaterialCommand command, IRequestHandler<SaveRawMaterialCommand, Result<RawMaterial>> handler, IValidator<SaveRawMaterialCommand> validator, CancellationToken cancellationToken) =>
            {
                var validationResult = await validator.ValidateAsync(command, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Invalid Request",
                        Detail = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)),
                        Instance = "/rawmaterial"
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
                        Instance = "/rawmaterial"
                    };
                    return Results.Problem(problemDetails);
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
