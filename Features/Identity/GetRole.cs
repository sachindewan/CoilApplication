using Carter;
using Coil.Api.Database;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Coil.Api.Features.Identity
{
    public class GetRoleEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/get/user-information", async (HttpContext context, CoilApplicationDbContext dbContext, CoilIdentityDbContext identityDbContext) =>
            {
                if (!context.User.Identity?.IsAuthenticated ?? false)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Unauthorized",
                        Detail = "User is not authenticated.",
                        Instance = context.Request.GetDisplayUrl()
                    });
                }

                var roleClaims = context.User.FindAll(ClaimTypes.Role)
                    .Select(claim => new { claim.Type, claim.Value })
                    .ToList();

                if (roleClaims.Count == 0)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "No Role Claims",
                        Detail = "No role claims found for the user."
                    });
                }

                // Normalize roles once
                var hasPartnerRole = roleClaims.Any(rc => rc.Value.Equals("partner", StringComparison.OrdinalIgnoreCase));

                if (!hasPartnerRole)
                {
                    var plants = await dbContext.Plants.ToListAsync();

                    return Results.Ok(new
                    {
                        Claims = roleClaims,
                        AssignedPlant = plants
                    });
                }

                var userName = context.User.Identity?.Name;
                if (string.IsNullOrEmpty(userName))
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Invalid User",
                        Detail = "Authenticated user has no username."
                    });
                }

                var user = await identityDbContext.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserName == userName);

                if (user == null)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "User Not Found",
                        Detail = "User could not be found in identity database."
                    });
                }

                var assignedPlant = await dbContext.Plants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PlantId == user.PlantId);

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
