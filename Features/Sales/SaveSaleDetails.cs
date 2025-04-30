using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using static Coil.Api.Features.Sales.SaveSaleDetails;

namespace Coil.Api.Features.Sales
{
    public static class SaveSaleDetails
    {
        public record SaveSaleCommand(
            int PlantId,
            double Weight,
            DateTime SaleDate,
            string RawMaterialsJson // JSON string containing raw material IDs and sale percentages
        ) : IRequest<Result<Sale>>;

        public sealed class CreateSaleValidator : AbstractValidator<SaveSaleCommand>
        {
            public CreateSaleValidator()
            {
                RuleFor(x => x.PlantId)
                    .GreaterThan(0)
                    .WithMessage("Plant ID must be greater than zero.");

                RuleFor(x => x.Weight)
                    .GreaterThan(0)
                    .WithMessage("Weight must be greater than zero.");

                RuleFor(x => x.SaleDate)
                    .LessThanOrEqualTo(DateTime.UtcNow)
                    .WithMessage("Sale date cannot be in the future.");

                RuleFor(x => x.RawMaterialsJson)
                    .NotEmpty()
                    .WithMessage("RawMaterialsJson is required.")
                    .Must(BeValidJson)
                    .WithMessage("RawMaterialsJson must be a valid JSON string.");
            }

            private bool BeValidJson(string rawMaterialsJson)
            {
                try
                {
                    JsonDocument.Parse(rawMaterialsJson);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        internal sealed class SaveSaleCommandHandler(CoilApplicationDbContext _dbContext) : IRequestHandler<SaveSaleCommand, Result<Sale>>
        {
            public async Task<Result<Sale>> Handle(SaveSaleCommand request, CancellationToken cancellationToken)
            {
                // Start a database transaction
                using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    // Validate Plant existence
                    var plantExists = await _dbContext.Plants.AnyAsync(p => p.PlantId == request.PlantId, cancellationToken);
                    if (!plantExists)
                    {
                        return Result.Failure<Sale>(new Error(
                            "SaveSaleCommand.PlantNotFound",
                            $"Plant with ID {request.PlantId} does not exist."));
                    }

                    // Validate RawMaterialsJson structure
                    try
                    {
                        var rawMaterials = JsonDocument.Parse(request.RawMaterialsJson).RootElement.EnumerateArray();
                        double totalSalePercentage = 0;

                        foreach (var rawMaterial in rawMaterials)
                        {
                            if (!rawMaterial.TryGetProperty("RawMaterialId", out var rawMaterialId) ||
                                !rawMaterial.TryGetProperty("SalePercentage", out var salePercentage))
                            {
                                return Result.Failure<Sale>(new Error(
                                    "SaveSaleCommand.InvalidRawMaterialsJson",
                                    "RawMaterialsJson must contain RawMaterialId and SalePercentage for each item."));
                            }

                            var rawMaterialIdValue = rawMaterialId.GetInt32();
                            var salePercent = salePercentage.GetDouble();

                            // Validate RawMaterialId existence
                            var rawMaterialExists = await _dbContext.RawMaterials.AnyAsync(
                                rm => rm.RawMaterialId == rawMaterialIdValue, cancellationToken);

                            if (!rawMaterialExists)
                            {
                                return Result.Failure<Sale>(new Error(
                                    "SaveSaleCommand.RawMaterialNotFound",
                                    $"RawMaterialId {rawMaterialIdValue} does not exist."));
                            }

                            // Validate RawMaterialQuantity existence
                            var rawMaterialQuantity = await _dbContext.RawMaterialQuantities
                                .FirstOrDefaultAsync(rmq => rmq.RawMaterialId == rawMaterialIdValue && rmq.PlantId == request.PlantId, cancellationToken);

                            if (rawMaterialQuantity == null)
                            {
                                return Result.Failure<Sale>(new Error(
                                    "SaveSaleCommand.RawMaterialQuantityNotFound",
                                    $"Raw material quantity for RawMaterialId {rawMaterialIdValue} and PlantId {request.PlantId} was not found."));
                            }

                            if (rawMaterialQuantity.AvailableQuantity <= 0)
                            {
                                return Result.Failure<Sale>(new Error(
                                    "SaveSaleCommand.NoAvailableQuantity",
                                    $"Available quantity is 0 for RawMaterialId {rawMaterialIdValue} and PlantId {request.PlantId}. Cannot process sale."));
                            }

                            // Calculate the value of the sale percentage
                            var salePercentageValue = rawMaterialQuantity.AvailableQuantity * (decimal)(salePercent / 100);

                            // Subtract the sale percentage value from the available quantity
                            rawMaterialQuantity.AvailableQuantity -= salePercentageValue;

                            // Update the RawMaterialQuantity in the database
                            _dbContext.RawMaterialQuantities.Update(rawMaterialQuantity);

                            // Accumulate SalePercentage
                            totalSalePercentage += salePercent;
                        }

                        // Validate that the total SalePercentage equals 100%
                        if (Math.Abs(totalSalePercentage - 100.0) > 0.01) // Allowing a small tolerance for floating-point precision
                        {
                            return Result.Failure<Sale>(new Error(
                                "SaveSaleCommand.InvalidSalePercentage",
                                "The sum of SalePercentage must equal 100%."));
                        }
                    }
                    catch
                    {
                        return Result.Failure<Sale>(new Error(
                            "SaveSaleCommand.InvalidRawMaterialsJson",
                            "RawMaterialsJson is not a valid JSON array."));
                    }

                    // Create a new Sale entity
                    var newSale = new Sale
                    {
                        PlantId = request.PlantId,
                        Weight = request.Weight,
                        SaleDate = request.SaleDate,
                        RawMaterialsJson = request.RawMaterialsJson
                    };

                    // Add the new sale
                    _dbContext.Sales.Add(newSale);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    // Commit the transaction
                    await transaction.CommitAsync(cancellationToken);

                    return Result.Success(newSale);
                }
                catch (Exception ex)
                {
                    // Rollback the transaction in case of an error
                    await transaction.RollbackAsync(cancellationToken);
                    return Result.Failure<Sale>(new Error(
                        "SaveSaleCommand.TransactionFailed",
                        $"An error occurred while processing the transaction: {ex.Message}"));
                }
            }
        }
    }

    public class SaveSaleDetailsEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/sales", async (SaveSaleCommand command, IRequestHandler<SaveSaleCommand, Result<Sale>> handler, IValidator<SaveSaleCommand> validator, CancellationToken cancellationToken) =>
            {
                var validationResult = await validator.ValidateAsync(command, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Invalid Request",
                        Detail = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)),
                        Instance = "/sales"
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
                        Instance = "/sales"
                    };
                    return Results.Problem(problemDetails);
                }

                return Results.Created($"/sales/{result.Value.SaleId}", result.Value);
            })
            .WithName("SaveSaleDetails")
            .WithTags("CoilApi")
            .RequireAuthorization("coil.api")
            .Produces(StatusCodes.Status201Created, typeof(Sale))
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();
        }
    }
}
