using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.RawMaterialOperations.SaveRawMaterialPurchaseDetails;

namespace Coil.Api.Features.RawMaterialOperations
{
    public static class SaveRawMaterialPurchaseDetails
    {
        public record SaveRawMaterialPurchaseCommand(
            int PlantId,
            string BillNumber,
            decimal Weight,
            decimal Rate,
            decimal BillValue,
            int GST,
            decimal TotalBillAmount,
            DateTime PurchaseDate,
            int RawMaterialId,
            int PartyId
        ) : IRequest<Result<RawMaterialPurchase>>;

        public sealed class CreateRawMaterialPurchaseValidator : AbstractValidator<SaveRawMaterialPurchaseCommand>
        {
            public CreateRawMaterialPurchaseValidator()
            {
                RuleFor(x => x.PlantId)
                    .GreaterThan(0)
                    .WithMessage("Plant ID must be greater than zero.");

                RuleFor(x => x.BillNumber)
                    .NotEmpty()
                    .WithMessage("Bill number is required.")
                    .MaximumLength(100)
                    .WithMessage("Bill number exceeds 100 characters.");

                RuleFor(x => x.Weight)
                    .GreaterThan(0)
                    .WithMessage("Weight must be greater than zero.");

                RuleFor(x => x.Rate)
                    .GreaterThan(0)
                    .WithMessage("Rate must be greater than zero.");

                RuleFor(x => x.BillValue)
                    .GreaterThan(0)
                    .WithMessage("Bill value must be greater than zero.");

                RuleFor(x => x.GST)
                    .GreaterThan(0)
                    .WithMessage("GST must be greater than zero.");

                RuleFor(x => x.TotalBillAmount)
                    .GreaterThan(0)
                    .WithMessage("Total bill amount must be greater than zero.");

                RuleFor(x => x.PurchaseDate)
                    .LessThanOrEqualTo(DateTime.UtcNow)
                    .WithMessage("Purchase date cannot be in the future.");

                RuleFor(x => x.RawMaterialId)
                    .GreaterThan(0)
                    .WithMessage("Raw Material ID must be greater than zero.");

                RuleFor(x => x.PartyId)
                    .GreaterThan(0)
                    .WithMessage("Party ID must be greater than zero.");
            }
        }

        internal sealed class SaveRawMaterialPurchaseCommandHandler(CoilApplicationDbContext _dbContext) : IRequestHandler<SaveRawMaterialPurchaseCommand, Result<RawMaterialPurchase>>
        {
            public async Task<Result<RawMaterialPurchase>> Handle(SaveRawMaterialPurchaseCommand request, CancellationToken cancellationToken)
            {
                // Start a database transaction
                using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    // Validate Plant existence
                    var plantExists = await _dbContext.Plants.AnyAsync(p => p.PlantId == request.PlantId, cancellationToken);
                    if (!plantExists)
                    {
                        return Result.Failure<RawMaterialPurchase>(new Error(
                            "SaveRawMaterialPurchaseCommand.PlantNotFound",
                            $"Plant with ID {request.PlantId} does not exist."));
                    }

                    // Validate RawMaterial existence
                    var rawMaterialExists = await _dbContext.RawMaterials.AnyAsync(rm => rm.RawMaterialId == request.RawMaterialId, cancellationToken);
                    if (!rawMaterialExists)
                    {
                        return Result.Failure<RawMaterialPurchase>(new Error(
                            "SaveRawMaterialPurchaseCommand.RawMaterialNotFound",
                            $"Raw Material with ID {request.RawMaterialId} does not exist."));
                    }

                    // Validate Party existence
                    var partyExists = await _dbContext.Parties.AnyAsync(p => p.PartyId == request.PartyId, cancellationToken);
                    if (!partyExists)
                    {
                        return Result.Failure<RawMaterialPurchase>(new Error(
                            "SaveRawMaterialPurchaseCommand.PartyNotFound",
                            $"Party with ID {request.PartyId} does not exist."));
                    }

                    // Check for duplicate BillNumber for the same Plant & the same Raw material
                    var billExists = await _dbContext.RawMaterialPurchases.AnyAsync(rmp => rmp.BillNumber.Trim().ToLower() == request.BillNumber.Trim().ToLower() && rmp.PlantId == request.PlantId && rmp.RawMaterialId == request.RawMaterialId, cancellationToken);
                    if (billExists)
                    {
                        return Result.Failure<RawMaterialPurchase>(new Error(
                            "SaveRawMaterialPurchaseCommand.DuplicateBill",
                            $"A purchase with the bill number '{request.BillNumber}' already exists for Plant ID {request.PlantId}."));
                    }

                    // Create a new RawMaterialPurchase entity
                    var newPurchase = new RawMaterialPurchase
                    {
                        PlantId = request.PlantId,
                        BillNumber = request.BillNumber.Trim(),
                        Weight = request.Weight,
                        Rate = request.Rate,
                        BillValue = request.BillValue,
                        GST = request.GST,
                        TotalBillAmount = request.TotalBillAmount,
                        PurchaseDate = request.PurchaseDate,
                        RawMaterialId = request.RawMaterialId,
                        PartyId = request.PartyId
                    };

                    // Add the new purchase
                    _dbContext.RawMaterialPurchases.Add(newPurchase);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    // Create/Update RawMaterialQuantity
                    var rawMaterialQuantity = await _dbContext.RawMaterialQuantities
                        .FirstOrDefaultAsync(rmq => rmq.RawMaterialId == request.RawMaterialId && rmq.PlantId == request.PlantId, cancellationToken);

                    if (rawMaterialQuantity == null)
                    {
                        rawMaterialQuantity = new RawMaterialQuantity
                        {
                            RawMaterialId = request.RawMaterialId,
                            PlantId = request.PlantId,
                            AvailableQuantity = request.Weight
                        };
                        _dbContext.RawMaterialQuantities.Add(rawMaterialQuantity);
                    }
                    else
                    {
                        rawMaterialQuantity.AvailableQuantity += request.Weight;
                        _dbContext.RawMaterialQuantities.Update(rawMaterialQuantity);
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);

                    // Commit the transaction
                    await transaction.CommitAsync(cancellationToken);

                    return Result.Success(newPurchase);
                }
                catch (Exception ex)
                {
                    // Rollback the transaction in case of an error
                    await transaction.RollbackAsync(cancellationToken);
                    return Result.Failure<RawMaterialPurchase>(new Error(
                        "SaveRawMaterialPurchaseCommand.TransactionFailed",
                        $"An error occurred while processing the request: {ex.Message}"));
                }
            }
        }
    }

    public class SaveRawMaterialPurchaseDetailsEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/rawmaterialpurchase", async (SaveRawMaterialPurchaseCommand command, IRequestHandler<SaveRawMaterialPurchaseCommand, Result<RawMaterialPurchase>> handler, IValidator<SaveRawMaterialPurchaseCommand> validator, CancellationToken cancellationToken) =>
            {
                var validationResult = await validator.ValidateAsync(command, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Invalid Request",
                        Detail = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)),
                        Instance = "/rawmaterialpurchase"
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
                        Instance = "/rawmaterialpurchase"
                    };
                    return Results.Problem(problemDetails);
                }

                return Results.Created($"/rawmaterialpurchase/{result.Value.PurchaseId}", result.Value);
            })
            .WithName("SaveRawMaterialPurchaseDetails")
            .WithTags("CoilApi")
            .RequireAuthorization("coil.api")
            .Produces(StatusCodes.Status201Created, typeof(RawMaterialPurchase))
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();
        }
    }
}
