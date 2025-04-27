using Carter;
using Coil.Api.Database;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Coil.Api.Features.Identity
{
    public class GetRoleEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {


            app.MapGet("/get/user-information", (HttpContext context, CoilApplicationDbContext dbContext , CoilIdentityDbContext identityDbContext) =>
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

                if (!roleClaims.Any(x => x.Value == "Partner"))
                {
                    return Results.Ok(new
                    {
                        Claims = roleClaims,
                    });
                }

                var user = identityDbContext.Users.FirstOrDefault(x => x.UserName == context.User.Identity.Name);

                var assignedPlant = dbContext.Plants.FirstOrDefault(x => x.PlantId == user.PlantId);

                return Results.Ok(new
                {
                    Claims = roleClaims,
                    AssignedPlant = assignedPlant
                });
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
