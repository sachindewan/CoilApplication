using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.Wastages.GetAvailableQuantity;
using static Coil.Api.Features.Wastages.SaveWastageDetails;

namespace Coil.Api.Features.Wastages
{
    public static class SaveWastageDetails
    {
        public record SaveWastageCommand(int PlantId, int RawMaterialId, double WastagePercentage, string WastageReason) : IRequest<Result<Wastage>>;

        public sealed class SaveWastageValidator : AbstractValidator<SaveWastageCommand>
        {
            public SaveWastageValidator()
            {
                RuleFor(x => x.PlantId)
                    .GreaterThan(0)
                    .WithMessage("Plant ID is required and must be greater than 0.");

                RuleFor(x => x.RawMaterialId)
                    .GreaterThan(0)
                    .WithMessage("Raw Material ID is required and must be greater than 0.");

                RuleFor(x => x.WastagePercentage)
                    .InclusiveBetween(0, 100)
                    .WithMessage("Wastage percentage must be between 0 and 100.");

                RuleFor(x => x.WastageReason)
                    .NotEmpty()
                    .WithMessage("Wastage reason is required.")
                    .MaximumLength(500)
                    .WithMessage("Wastage reason must not exceed 500 characters.");
            }
        }

        internal sealed class SaveWastageCommandHandler(
            CoilApplicationDbContext dbContext,
            IRequestHandler<GetAvailableQuantityQuery, Result<double>> getAvailableQuantityHandler) : IRequestHandler<SaveWastageCommand, Result<Wastage>>
        {
            private readonly CoilApplicationDbContext _dbContext = dbContext;
            private readonly IRequestHandler<GetAvailableQuantityQuery, Result<double>> _getAvailableQuantityHandler = getAvailableQuantityHandler;

            public async Task<Result<Wastage>> Handle(SaveWastageCommand request, CancellationToken cancellationToken)
            {
                // Start a database transaction
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    // Validate Plant existence
                    var plantExists = await _dbContext.Plants.AnyAsync(p => p.PlantId == request.PlantId, cancellationToken);
                    if (!plantExists)
                    {
                        return Result.Failure<Wastage>(new Error(
                            "SaveWastageCommand.InvalidPlant",
                            $"Plant with ID '{request.PlantId}' does not exist."));
                    }

                    // Validate RawMaterial existence
                    var rawMaterialExists = await _dbContext.RawMaterials.AnyAsync(rm => rm.RawMaterialId == request.RawMaterialId, cancellationToken);
                    if (!rawMaterialExists)
                    {
                        return Result.Failure<Wastage>(new Error(
                            "SaveWastageCommand.InvalidRawMaterial",
                            $"Raw material with ID '{request.RawMaterialId}' does not exist."));
                    }

                    // Fetch the available quantity using the GetAvailableQuantityQueryHandler
                    var availableQuantityResult = await _getAvailableQuantityHandler.Handle(
                        new GetAvailableQuantityQuery(request.PlantId, request.RawMaterialId),
                        cancellationToken);

                    if (availableQuantityResult.IsFailure)
                    {
                        return Result.Failure<Wastage>(availableQuantityResult.Error);
                    }

                    var availableQuantity = availableQuantityResult.Value;

                    if (availableQuantity <= 0)
                    {
                        return Result.Failure<Wastage>(new Error(
                            "SaveWastageCommand.NoAvailableQuantity",
                            "Available quantity is 0. Cannot process wastage."));
                    }

                    // Calculate the wastage amount based on the WastagePercentage
                    var wastageAmount = availableQuantity * request.WastagePercentage / 100;

                    // Ensure the wastage amount does not exceed the available quantity
                    if (wastageAmount > availableQuantity)
                    {
                        return Result.Failure<Wastage>(new Error(
                            "SaveWastageCommand.InvalidWastagePercentage",
                            "Wastage amount exceeds the available quantity."));
                    }

                    // Update the AvailableQuantity in RawMaterialQuantity
                    var rawMaterialQuantity = await _dbContext.RawMaterialQuantities
                        .FirstOrDefaultAsync(rmq => rmq.RawMaterialId == request.RawMaterialId && rmq.PlantId == request.PlantId, cancellationToken);

                    if (rawMaterialQuantity == null)
                    {
                        return Result.Failure<Wastage>(new Error(
                            "SaveWastageCommand.RawMaterialQuantityNotFound",
                            $"Raw material quantity for RawMaterialId {request.RawMaterialId} and PlantId {request.PlantId} was not found."));
                    }

                    rawMaterialQuantity.AvailableQuantity -= (decimal)wastageAmount;

                    // Save the updated RawMaterialQuantity
                    _dbContext.RawMaterialQuantities.Update(rawMaterialQuantity);

                    // Create a new Wastage entity
                    var newWastage = new Wastage
                    {
                        PlantId = request.PlantId,
                        RawMaterialId = request.RawMaterialId,
                        WastagePercentage = request.WastagePercentage,
                        WastageReason = request.WastageReason.Trim()
                    };

                    // Add and save the Wastage
                    _dbContext.Wastages.Add(newWastage);

                    // Commit the transaction
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return Result.Success(newWastage);
                }
                catch (Exception ex)
                {
                    // Rollback the transaction in case of an error
                    await transaction.RollbackAsync(cancellationToken);
                    return Result.Failure<Wastage>(new Error(
                        "SaveWastageCommand.TransactionFailed",
                        $"An error occurred while saving wastage details: {ex.Message}"));
                }
            }
        }
    }

    public class SaveWastageDetailsEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/savewastage", async (SaveWastageCommand command, IRequestHandler<SaveWastageCommand, Result<Wastage>> handler, IValidator<SaveWastageCommand> validator, CancellationToken cancellationToken) =>
            {
                var validationResult = await validator.ValidateAsync(command, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Invalid Request",
                        Detail = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)),
                        Instance = "/savewastage"
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
                        Instance = "/savewastage"
                    };
                    return Results.Problem(problemDetails);
                }

                return Results.Created($"/savewastage/{result.Value.WastageId}", result.Value);
            })
            .WithName("SaveWastageDetails")
            .WithTags("CoilApi")
            .RequireAuthorization("coil.api")
            .Produces(StatusCodes.Status201Created, typeof(Wastage))
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();
        }
    }
}
