using Carter;
using Coil.Api.Database;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.OutStandingPartyAmount.GetOutStandingAmount;

namespace Coil.Api.Features.OutStandingPartyAmount
{
    public static class GetOutStandingAmount
    {
        public record GetOutStandingPurchaseAmountQuery(int PlantId) : IRequest<Result<List<GetOutStandingPurchaseAmountResponse>>>;
        public record GetOutStandingPurchaseAmountResponse(string PartyName, decimal Amount);

        public class GetOutStandingPurchaseAmountQueryValidator : AbstractValidator<GetOutStandingPurchaseAmountQuery>
        {
            public GetOutStandingPurchaseAmountQueryValidator()
            {
                RuleFor(x => x.PlantId).GreaterThan(0).WithMessage("PlantId Id should be greater than 0");
            }
        }
        internal sealed class GetOutStandingPurchaseAmountHandler(CoilApplicationDbContext dbContext, IValidator<GetOutStandingPurchaseAmountQuery> validator) : IRequestHandler<GetOutStandingPurchaseAmountQuery, Result<List<GetOutStandingPurchaseAmountResponse>>>
        {
            public async Task<Result<List<GetOutStandingPurchaseAmountResponse>>> Handle(GetOutStandingPurchaseAmountQuery request, CancellationToken cancellationToken)
            {
                var validationResult = validator.Validate(request);
                if (validationResult is { IsValid: false })
                {
                    return Result.Failure<List<GetOutStandingPurchaseAmountResponse>>(new Error(
                        "GetOutStandingAmount.InvalidRequest",
                        $"Plant ID {request.PlantId} is invalid"));
                }

                var purchases = await dbContext.RawMaterialPurchases
                    .Where(x => x.PlantId == request.PlantId)
                    .GroupBy(x => new { x.PartyId, x.Party.PartyName })
                    .Select(g => new
                    {
                        g.Key.PartyId,
                        g.Key.PartyName,
                        TotalAmount = g.Sum(x => x.TotalBillAmount)
                    })
                    .ToListAsync(cancellationToken);

                var payments = await dbContext.Payments
                    .Where(x => x.PlantId == request.PlantId)
                    .GroupBy(x => x.PartyId)
                    .Select(g => new
                    {
                        PartyId = g.Key,
                        TotalPaid = g.Sum(x => x.Amount)
                    })
                    .ToListAsync(cancellationToken);

                var paymentsDict = payments.ToDictionary(x => x.PartyId, x => x.TotalPaid);

                var outstandingAmounts = purchases.Select(purchase =>
                {
                    paymentsDict.TryGetValue(purchase.PartyId, out var paidAmount);
                    var outstanding = purchase.TotalAmount - paidAmount;
                    return new GetOutStandingPurchaseAmountResponse(purchase.PartyName, outstanding);
                }).ToList();

                return Result.Success(outstandingAmounts);
            }
        }
    }


    public sealed class GetOutStandingAmountEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {

            app.MapGet("/outstanding-party-amount/{plantId:int}", async (int plantId, IRequestHandler<GetOutStandingPurchaseAmountQuery, Result<List<GetOutStandingPurchaseAmountResponse>>> requestHandler, CancellationToken cancellationToken) =>
            {
                var result = await requestHandler.Handle(new GetOutStandingPurchaseAmountQuery(plantId), cancellationToken);
                if (result.IsFailure)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Extensions = { ["ErrorCode"] = result.Error.Code },
                        Detail = result.Error.Message,
                        Title = "Invalid Request",
                        Instance = "/OutSatandingPartyAmount"
                    };
                    return Results.Problem(problemDetails);
                }
                return Results.Ok(result.Value);
            })
          .WithName("GetOutStanding")
          .WithTags("CoilApi")
          .RequireAuthorization("coil.api")
          .Produces(StatusCodes.Status200OK, typeof(List<GetOutStandingPurchaseAmountResponse>))
          .Produces(StatusCodes.Status404NotFound)
          .WithOpenApi();
        }
    }
}
