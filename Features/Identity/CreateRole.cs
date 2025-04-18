using Carter;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Security.Claims;
using static Coil.Api.Features.Identity.CreateRole;
using static Coil.Api.Features.Identity.SetRoleToUser;

namespace Coil.Api.Features.Identity
{
    public static class CreateRole
    {
        public sealed record CreateRoleQuery(string Role) : IRequest<Result<CreateRoleResponse>>;
        public sealed record CreateRoleResponse(bool IsSucess);
        public sealed class SetUserRoleQueryValidator : AbstractValidator<CreateRoleQuery>
        {
            public SetUserRoleQueryValidator()
            {
                RuleFor(query => query.Role)
                    .NotEmpty().WithMessage("Role is required.")
                    .MaximumLength(200).WithMessage("Invalid length role should not exceed max 200 charecter.");
            }
        }
        internal sealed class CreateRoleHandler(RoleManager<IdentityRole> roleManager, IValidator<CreateRoleQuery> validator) : IRequestHandler<CreateRoleQuery, Result<CreateRoleResponse>>
        {
            public async Task<Result<CreateRoleResponse>> Handle(CreateRoleQuery request, CancellationToken cancellationToken)
            {
                var validationResult = validator.Validate(request);
                if (!validationResult.IsValid)
                {
                    return Result.Failure<CreateRoleResponse>(new Error(
                        "CreateRoleQuery.Invalid",
                        string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))));
                }
                var role = new IdentityRole() { Name = request.Role };

                var result = await roleManager.CreateAsync(role);

                if (result.Succeeded)
                {
                    return Result.Success(new CreateRoleResponse(true));
                }

                return Result.Failure<CreateRoleResponse>(new Error(
                    "CreateRoleQuery.Invalid",
                    "Failed to assign role to user."));
            }
        }
    }

    public sealed class CreateRoleEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/create/role", async (CreateRoleQuery request, IRequestHandler<CreateRoleQuery, Result<CreateRoleResponse>> handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(request, cancellationToken);
                if (result.IsFailure)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Invalid Request",
                        Detail = result.Error.Message,
                        Instance = "/create/role"
                    };

                    return Results.Problem(problemDetails);
                }
                return Results.Ok(result.Value.IsSucess);
            }).WithTags("Operational")
              .WithName("CreateRoleEndpoint")
              .Produces(StatusCodes.Status200OK)
              .Produces(StatusCodes.Status400BadRequest, typeof(ProblemDetails))
              .WithOpenApi();
        }
    }
}
