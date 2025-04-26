using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Features.RawMaterials;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.OutStandingPartyAmount.GetOutStandingAmount;
using static Coil.Api.Features.Product.CreateProduct;

namespace Coil.Api.Features.OutStandingPartyAmount
{
    public static class GetOutStandingAmount
    {
        public record GetOutStandingPurchaseAmountQuery(int PlantId) : IRequest<Result<GetOutStandingPurchaseAmountResponse>>;
        public record GetOutStandingPurchaseAmountResponse(string ParyName , decimal Amount);

        public class GetOutStandingPurchaseAmountQueryValidator : AbstractValidator<GetOutStandingPurchaseAmountQuery>
        {
            public GetOutStandingPurchaseAmountQueryValidator()
            {
                RuleFor(x => x.PlantId).GreaterThan(0).WithMessage("party Id should be greater than 0");
            }
        }
        internal sealed class GetOutStandingPurchaseAmountHandler(CoilApplicationDbContext dbContext, IValidator<GetOutStandingPurchaseAmountQuery> validator) : IRequestHandler<GetOutStandingPurchaseAmountQuery, Result<GetOutStandingPurchaseAmountResponse>>
        {
            public async Task<Result<GetOutStandingPurchaseAmountResponse>> Handle(GetOutStandingPurchaseAmountQuery request, CancellationToken cancellationToken)
            {
                var validationResul = validator.Validate(request);
                if (validationResul != null && !validationResul.IsValid) {
                    return Result.Failure<GetOutStandingPurchaseAmountResponse>(new Error("GetOutStandingAmount.InvalidRequest", $"Plant ID {request.PlantId} is invalid"));
                }

                var purchaseDetails = await dbContext.RawMaterialPurchases.Include(x=>x.Party).Where(x=>x.PlantId==request.PlantId).ToListAsync();

                var totalDueAmount= purchaseDetails.Sum(x => x.TotalBillAmount);

                // Need to get the payment done till now and then minus from totalAmountDue
                //var totalPaymentMade = await dbContext.pay

                return Result.Success(new GetOutStandingPurchaseAmountResponse(purchaseDetails.FirstOrDefault()?.Party?.PartyName, totalDueAmount));
            }
        }
    }


    public sealed class GetOutStandingAmountEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {

            app.MapGet("/outstanding-party-amount/{plantId:int}", async (int  plantId, IRequestHandler<GetOutStandingPurchaseAmountQuery, Result<GetOutStandingPurchaseAmountResponse>> requestHandler, CancellationToken cancellationToken) =>
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
          .Produces(StatusCodes.Status200OK, typeof(GetOutStandingPurchaseAmountResponse))
          .Produces(StatusCodes.Status404NotFound)
          .WithOpenApi();
        }
    }
}
