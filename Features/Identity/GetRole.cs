using Carter;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Coil.Api.Features.Identity
{
    public class GetRoleEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {


            app.MapGet("/get/role", (HttpContext context) =>
            {
                if (!context.User.Identity.IsAuthenticated)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Unauthorized",
                        Detail = "User is not authenticated.",
                        Instance = context.Request.GetDisplayUrl()
                    });
                }

                var roleClaims = context.User.Claims
                    .Where(claim => claim.Type == ClaimTypes.Role)
                    .Select(claim => new { claim.Type, claim.Value })
                    .ToList();

                if (!roleClaims.Any())
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "No Role Claims",
                        Detail = "No role claims found for the user."
                    });
                }

                return Results.Ok(roleClaims);
            })
            .WithTags("CoilApi")
            .WithName(nameof(GetRoleEndpoint))
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(ProblemDetails))
            .WithOpenApi()
            .RequireAuthorization("coil.api");

        }
    }
}
