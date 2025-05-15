using Carter;
using Coil.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace Coil.Api.Features.Authentication
{
    public class AuthEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/login", async (
                        SignInManager<ApplicationUser> signInManager,
                        UserManager<ApplicationUser> userManager,
                        IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
                        [FromBody] LoginRequest login) =>
            {
                var user = await userManager.FindByEmailAsync(login.Email);
                var result = await signInManager.PasswordSignInAsync(login.Email, login.Password, false, lockoutOnFailure: true);
                if (!result.Succeeded)
                {
                    return Results.Problem(
                        title: "Login Failed",
                        detail: "Invalid username or password.",
                        statusCode: StatusCodes.Status400BadRequest
                    );
                }

                var newPrincipal = await signInManager.CreateUserPrincipalAsync(user);
                return TypedResults.SignIn(newPrincipal, authenticationScheme: IdentityConstants.BearerScheme);
            })
            .WithName("Login")
            .WithTags("CoilApi")
            .Produces(StatusCodes.Status200OK, typeof(string))
            .Produces(StatusCodes.Status401Unauthorized)
            .WithOpenApi();
        }
    }
}
