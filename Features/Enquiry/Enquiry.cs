using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Numerics;
using static Coil.Api.Features.Enquiry.Enquiry;
using static Coil.Api.Features.RawMaterials.SaveRawMaterialDetails;

namespace Coil.Api.Features.Enquiry
{
    public static class Enquiry
    {
        public record EnquiryCommand(Int64 mobileNumber, string name, string rowMatrial, string place, double quantity) : IRequest<Result<EnquiryResponse>>;
        public record EnquiryResponse(bool IsSuccess);
        public class EnquiryValidator : AbstractValidator<EnquiryCommand>
        {
            public EnquiryValidator()
            {
                RuleFor(x => x.mobileNumber)
                    .GreaterThan(0)
                    .WithMessage("Mobile number must be greater than 0.")
                    .Must(BeAValidMobile)
                    .WithMessage("Invalid mobile number. It must be a 10-digit number starting with 6-9.");

                RuleFor(x => x.rowMatrial)
                    .NotEmpty()
                    .WithMessage("Raw material is required.");

                RuleFor(x => x.place)
                    .NotEmpty()
                    .WithMessage("Place is required.");

                RuleFor(x => x.quantity)
                    .GreaterThan(0)
                    .WithMessage("Quantity must be greater than 0.");
            }

            private bool BeAValidMobile(Int64 mobile)
            {
                var mobileStr = mobile.ToString();
                return System.Text.RegularExpressions.Regex.IsMatch(mobileStr, @"^[6-9]\d{9}$");
            }
        }

        internal class EnquiryHandler(CoilApplicationDbContext coilApplicationDbContext, IValidator<EnquiryCommand> validator) : IRequestHandler<EnquiryCommand, Result<EnquiryResponse>>
        {
            public async Task<Result<EnquiryResponse>> Handle(EnquiryCommand request, CancellationToken cancellationToken)
            {
                var valiDationResult = validator.Validate(request);
                if (!valiDationResult.IsValid) {
                    return Result.Failure<EnquiryResponse>(new Error("EnquiryCommand.Invalid", "request is invalid"));
                }

                var entity = new Coil.Api.Entities.Enquiry { MobileNumber = request.mobileNumber, Name = request.name, Place = request.place, Quantity = request.quantity, RowMatrial = request.rowMatrial };
                await coilApplicationDbContext.AddAsync(entity);
                await coilApplicationDbContext.SaveChangesAsync();

                return Result.Success( new EnquiryResponse(true));
            }
        }
    }

    public class EnquiryEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/enquiry", async (EnquiryCommand command, IRequestHandler<EnquiryCommand, Result<EnquiryResponse>> handler, CancellationToken cancellationToken) =>
            {
                

                var result = await handler.Handle(command, cancellationToken);

                if (result.IsFailure)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Invalid Request",
                        Detail = result.Error.Message,
                        Instance = "/enquiry"
                    };
                    return Results.Problem(problemDetails);
                }

                return Results.Created();
            })
            .WithName("Enquiry")
            .WithTags("CoilApi")
            .RequireAuthorization("coil.api")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();
        }
    }
}
