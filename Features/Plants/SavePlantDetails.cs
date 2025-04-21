using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.Plants.SavePlantDetails;

namespace Coil.Api.Features.Plants
{
    public static class SavePlantDetails
    {
        public record SavePlantCommand(string PlantName, string Location) : IRequest<Result<Plant>>;

        public sealed class CreatePlantValidator : AbstractValidator<SavePlantCommand>
        {
            public CreatePlantValidator()
            {
                RuleFor(x => x.PlantName)
                    .Must(name => !string.IsNullOrWhiteSpace(name))
                    .WithMessage("Plant name is required.")
                    .MaximumLength(1000)
                    .WithMessage("Plant name exceeds 1000 characters")
                    .Must(name => name.Any(char.IsLetterOrDigit))
                    .WithMessage("Plant name must contain at least one letter or number.")
                    .Must(name => char.IsLetter(name.Trim()[0]))
                    .WithMessage("Plant name must start with an alphabet.");
                RuleFor(x => x.Location)
                    .Must(name => !string.IsNullOrWhiteSpace(name))
                    .WithMessage("Location is required.")
                    .MaximumLength(1000)
                    .WithMessage("Location exceeds 1000 characters")
                    .Must(name => name.Any(char.IsLetterOrDigit))
                    .WithMessage("Location must contain at least one letter or number.")
                    .Must(name => char.IsLetter(name.Trim()[0]))
                    .WithMessage("Location must start with an alphabet.");
            }
        }

        internal sealed class SavePlantCommandHandler(CoilApplicationDbContext _dbContext) : IRequestHandler<SavePlantCommand, Result<Plant>>
        {
            public async Task<Result<Plant>> Handle(SavePlantCommand request, CancellationToken cancellationToken)
            {
                // Check if a Plant with the same name and location already exists
                var plantExists = await _dbContext.Plants.AnyAsync(p => p.PlantName.Trim().ToLower() == request.PlantName.Trim().ToLower() && p.Location.Trim().ToLower() == request.Location.Trim().ToLower(), cancellationToken);
                if (plantExists)
                {
                    return Result.Failure<Plant>(new Error(
                        "SavePlantCommand.DuplicatePlant",
                        $"A plant with the name '{request.PlantName}' and location '{request.Location}' already exists."));
                }

                // Create a new Plant entity
                var newPlant = new Plant
                {
                    PlantName = request.PlantName.Trim(),
                    Location = request.Location.Trim(),
                    Parties = []
                };

                // Add and save the Plant
                _dbContext.Plants.Add(newPlant);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return Result.Success(newPlant);
            }
        }
    }

    public class SavePlantsDetailsEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/plant", async (SavePlantCommand command, IRequestHandler<SavePlantCommand, Result<Plant>> handler, IValidator<SavePlantCommand> validator, CancellationToken cancellationToken) =>
            {
                var validationResult = await validator.ValidateAsync(command, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Invalid Request",
                        Detail = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)),
                        Instance = "/plant"
                    };
                    return Results.Problem(problemDetails);
                }

                var result = await handler.Handle(command, cancellationToken);

                if (result.IsFailure)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Invalid Request",
                        Detail = result.Error.Message,
                        Instance = "/plant"
                    };
                    return Results.Problem(problemDetails);
                }

                return Results.Created($"/plant/{result.Value.PlantId}", result.Value);
            })
            .WithName("SavePlantDetails")
            .WithTags("CoilApi")
            .RequireAuthorization("coil.api")
            .Produces(StatusCodes.Status201Created, typeof(Plant))
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();
        }
    }
}
