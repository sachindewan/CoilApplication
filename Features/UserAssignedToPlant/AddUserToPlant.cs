using System.ComponentModel.DataAnnotations;
using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.Identity.SetRoleToUser;
using static Coil.Api.Features.UserAssignedToPlant.AddUserToPlant;

namespace Coil.Api.Features.UserAssignedToPlant
{
    public static class AddUserToPlant
    {
        public record AddUserToPlantCommand(int PlantId, string Email) : IRequest<Result<AddUserToPlantCommandResponse>>;

        public sealed class AddUserToPlantCommandValidator : AbstractValidator<AddUserToPlantCommand>
        {
            public AddUserToPlantCommandValidator()
            {
                RuleFor(query => query.Email)
                    .NotEmpty().WithMessage("Email is required.")
                    .EmailAddress().WithMessage("Invalid email format.");

                RuleFor(query => query.PlantId)
                    .GreaterThan(0).WithMessage("plantid must be greater than 0");
            }
        }

        public record AddUserToPlantCommandResponse(bool IsSuccess);

        internal class AddUserToPlantHandler(UserManager<ApplicationUser> userManager,CoilApplicationDbContext coilApplicationDbContext, CoilIdentityDbContext dbContext, IValidator<AddUserToPlantCommand> validator) : IRequestHandler<AddUserToPlantCommand, Result<AddUserToPlantCommandResponse>>
        {
            public async Task<Result<AddUserToPlantCommandResponse>> Handle(AddUserToPlantCommand request, CancellationToken cancellationToken)
            {
                var validationResult = await validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    return Result.Failure<AddUserToPlantCommandResponse>(new Error(
                        "AddUserToPlantCommand.Invalid",
                        string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))));
                }

                var user = await userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return Result.Failure<AddUserToPlantCommandResponse>(new Error(
                        "AddUserToPlantCommand.Invalid",
                        "User not found."));
                }
                // Check if the Plant exists
                var plantExists = await coilApplicationDbContext.Plants.AnyAsync(p => p.PlantId == request.PlantId, cancellationToken);
                if (!plantExists)
                {
                    return Result.Failure<AddUserToPlantCommandResponse>(new Error(
                        "SavePartyCommand.PlantNotFound",
                        $"Plant with ID {request.PlantId} does not exist."));
                }
                user.PlantId = request.PlantId;
                dbContext.Update(user);
               var result = await dbContext.SaveChangesAsync(cancellationToken);
                if (result>0)
                {
                    return Result.Success(new AddUserToPlantCommandResponse(true));
                }

                return Result.Failure<AddUserToPlantCommandResponse>(new Error(
                    "SetUserRoleQuery.Invalid",
                    "Failed to assign role to user."));
            }
        }
    }

    public sealed class AddUserToPlantEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/assigned/user/plant", async (AddUserToPlantCommand request, IRequestHandler<AddUserToPlantCommand, Result<AddUserToPlantCommandResponse>> handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(request, cancellationToken);
                if (result.IsFailure)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Invalid Request",
                        Detail = result.Error.Message,
                        Instance = "/assigned/user/plant"
                    };

                    return Results.Problem(problemDetails);
                }
                return Results.Ok(result.Value.IsSuccess);
            }).WithTags("Operational")
             .WithName("AddUserToPlantEndpoint")
             .Produces(StatusCodes.Status200OK)
             .Produces(StatusCodes.Status400BadRequest, typeof(ProblemDetails))
             .WithOpenApi();
        }
    }
}
