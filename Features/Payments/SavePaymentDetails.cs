using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.OutStandingPartyAmount.GetOutStandingAmount;
using static Coil.Api.Features.Payments.SavePaymentDetails;
using static Coil.Api.Features.RawMaterialOperations.GetRawMaterialPurchasesDetails;

namespace Coil.Api.Features.Payments
{
    public static class SavePaymentDetails
    {
        public record SavePaymentCommand(
            int PlantId,
            int PartyId,
            decimal Amount,
            DateTime PaymentDate
        ) : IRequest<Result<Payment>>;

        public sealed class SavePaymentValidator : AbstractValidator<SavePaymentCommand>
        {
            public SavePaymentValidator()
            {
                RuleFor(x => x.PlantId)
                    .GreaterThan(0)
                    .WithMessage("Plant ID must be greater than zero.");

                RuleFor(x => x.PartyId)
                    .GreaterThan(0)
                    .WithMessage("Party ID must be greater than zero.");

                RuleFor(x => x.Amount)
                    .GreaterThan(0)
                    .WithMessage("Amount must be greater than zero.");

                RuleFor(x => x.PaymentDate)
                    .LessThanOrEqualTo(DateTime.UtcNow)
                    .WithMessage("Payment date cannot be in the future.");
            }
        }

        internal sealed class SavePaymentCommandHandler(CoilApplicationDbContext _dbContext, IRequestHandler<GetRawMaterialPurchasesQuery, Result<List<RawMaterialPurchase>>> handler, IRequestHandler<GetOutStandingPurchaseAmountQuery, Result<List<GetOutStandingPurchaseAmountResponse>>> outStandingAmountHandler) : IRequestHandler<SavePaymentCommand, Result<Payment>>
        {
            public async Task<Result<Payment>> Handle(SavePaymentCommand request, CancellationToken cancellationToken)
            {
                var result = await handler.Handle(new GetRawMaterialPurchasesQuery(request.PlantId), cancellationToken);
                var purchaseHasMade = result.Value.Where(x => x.PartyId == request.PartyId).ToList();
                if (!purchaseHasMade.Any())
                {
                    return Result.Failure<Payment>(new Error(
                       "SavePaymentCommand.PlantNotFound",
                       $"Plant with ID {request.PlantId} for party {request.PartyId} purchase has not been made."));
                }
                // Validate Plant existence
                var plantExists = await _dbContext.Plants.AnyAsync(p => p.PlantId == request.PlantId, cancellationToken);
                if (!plantExists)
                {
                    return Result.Failure<Payment>(new Error(
                        "SavePaymentCommand.PlantNotFound",
                        $"Plant with ID {request.PlantId} does not exist."));
                }

                // Validate Party existence
                var partyExists =  _dbContext.Parties.Where(p => p.PartyId == request.PartyId).ToList();
                if (!partyExists.Any())
                {
                    return Result.Failure<Payment>(new Error(
                        "SavePaymentCommand.PartyNotFound",
                        $"Party with ID {request.PartyId} does not exist."));
                }
                //validate partner should not pay more than due amount

                var resultAmount = await outStandingAmountHandler.Handle(new GetOutStandingPurchaseAmountQuery(request.PlantId), cancellationToken);
                var outStandingAmountOfCurrentParty = resultAmount.Value?.FirstOrDefault(x => x.PartyName == partyExists.FirstOrDefault()?.PartyName)?.Amount;

                if(outStandingAmountOfCurrentParty < request.Amount)
                {
                    return Result.Failure<Payment>(new Error(
                       "SavePaymentCommand.PartyNotFound",
                       $"Partner should not pay more than due amount. due  amount is {outStandingAmountOfCurrentParty}"));
                }
                // Create a new Payment entity
                var newPayment = new Payment
                {
                    PlantId = request.PlantId,
                    PartyId = request.PartyId,
                    Amount = request.Amount,
                    PaymentDate = request.PaymentDate
                };

                // Add the new payment
                _dbContext.Payments.Add(newPayment);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return Result.Success(newPayment);
            }
        }
    }

    public class SavePaymentDetailsEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/payments", async (
                SavePaymentCommand command,
                IRequestHandler<SavePaymentCommand, Result<Payment>> handler,
                IValidator<SavePaymentCommand> validator,
                CancellationToken cancellationToken) =>
            {
                var validationResult = await validator.ValidateAsync(command, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Invalid Request",
                        Detail = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)),
                        Instance = "/payments"
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
                        Instance = "/payments"
                    };
                    return Results.Problem(problemDetails);
                }

                return Results.Created($"/payments/{result.Value.PaymentId}", result.Value);
            })
            .WithName("SavePaymentDetails")
            .WithTags("CoilApi")
            .RequireAuthorization("coil.api")
            .Produces(StatusCodes.Status201Created, typeof(Payment))
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();
        }
    }
}
