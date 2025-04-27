using Carter;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using static Coil.Api.Features.Identity.SetRoleToUser;

namespace Coil.Api.Features.Identity
{
    public static class SetRoleToUser
    {
        public sealed record SetUserRoleQuery(string Email, string Role) : IRequest<Result<SetUserRoleResponse>>;

        public sealed record SetUserRoleResponse(bool IsSuccess);

        public sealed class SetUserRoleQueryValidator : AbstractValidator<SetUserRoleQuery>
        {
            public SetUserRoleQueryValidator()
            {
                RuleFor(query => query.Email)
                    .NotEmpty().WithMessage("Email is required.")
                    .EmailAddress().WithMessage("Invalid email format.");

                RuleFor(query => query.Role)
                    .NotEmpty().WithMessage("Role is required.");
            }
        }

        internal sealed class SetRoleToUserHandler : IRequestHandler<SetUserRoleQuery, Result<SetUserRoleResponse>>
        {
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly RoleManager<IdentityRole> _roleManager;
            private readonly IValidator<SetUserRoleQuery> _validator;

            public SetRoleToUserHandler(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IValidator<SetUserRoleQuery> validator)
            {
                _userManager = userManager;
                _roleManager = roleManager;
                _validator = validator;
            }

            public async Task<Result<SetUserRoleResponse>> Handle(SetUserRoleQuery request, CancellationToken cancellationToken)
            {
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    return Result.Failure<SetUserRoleResponse>(new Error(
                        "SetUserRoleQuery.Invalid",
                        string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))));
                }

                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return Result.Failure<SetUserRoleResponse>(new Error(
                        "SetUserRoleQuery.Invalid",
                        "User not found."));
                }

                var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
                if (roleResult.Succeeded)
                {
                    return Result.Success(new SetUserRoleResponse(true));
                }

                return Result.Failure<SetUserRoleResponse>(new Error(
                    "SetUserRoleQuery.Invalid",
                    "Failed to assign role to user."));
            }
        }
    }
    public class GrantRoleEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/assigned/user/role", async (SetUserRoleQuery request, IRequestHandler<SetUserRoleQuery, Result<SetUserRoleResponse>> handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(request, cancellationToken);
                if (result.IsFailure)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Invalid Request",
                        Detail = result.Error.Message,
                        Instance = "/assigned/user/role"
                    };

                    return Results.Problem(problemDetails);
                }
              return Results.Ok(result.Value.IsSuccess);
            }).WithTags("Operational")
              .WithName("GrantRoleEndpoint")
              .Produces(StatusCodes.Status200OK)
              .Produces(StatusCodes.Status400BadRequest,typeof(ProblemDetails))
              .WithOpenApi();

        }
    }
}
