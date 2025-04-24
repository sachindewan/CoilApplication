using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.Expenses.SaveExpenseDetails;

namespace Coil.Api.Features.Expenses
{
    public static class SaveExpenseDetails
    {
        public record SaveExpenseCommand(
            int PlantId,
            string ExpenseType,
            string? BillNumber,
            decimal BillValue,
            int GST,
            decimal TotalBillAmount,
            DateTime ExpenseDate,
            int PartyId
        ) : IRequest<Result<Expense>>;

        public sealed class CreateExpenseValidator : AbstractValidator<SaveExpenseCommand>
        {
            public CreateExpenseValidator()
            {
                RuleFor(x => x.PlantId)
                    .GreaterThan(0)
                    .WithMessage("Plant ID must be greater than zero.");

                RuleFor(x => x.ExpenseType)
                    .NotEmpty()
                    .WithMessage("Expense Type is required.")
                    .MaximumLength(100)
                    .WithMessage("Expense Type exceeds 100 characters.");

                RuleFor(x => x.BillValue)
                    .GreaterThan(0)
                    .WithMessage("Bill value must be greater than zero.");

                RuleFor(x => x.GST)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("GST must be zero or greater.");

                RuleFor(x => x.TotalBillAmount)
                    .GreaterThan(0)
                    .WithMessage("Total bill amount must be greater than zero.");

                RuleFor(x => x.ExpenseDate)
                    .LessThanOrEqualTo(DateTime.UtcNow)
                    .WithMessage("Expense date cannot be in the future.");

                RuleFor(x => x.PartyId)
                    .GreaterThan(0)
                    .WithMessage("Party ID must be greater than zero.");
            }
        }

        internal sealed class SaveExpenseCommandHandler(CoilApplicationDbContext _dbContext) : IRequestHandler<SaveExpenseCommand, Result<Expense>>
        {
            public async Task<Result<Expense>> Handle(SaveExpenseCommand request, CancellationToken cancellationToken)
            {
                // Validate Plant existence
                var plantExists = await _dbContext.Plants.AnyAsync(p => p.PlantId == request.PlantId, cancellationToken);
                if (!plantExists)
                {
                    return Result.Failure<Expense>(new Error(
                        "SaveExpenseCommand.PlantNotFound",
                        $"Plant with ID {request.PlantId} does not exist."));
                }

                // Validate Party existence
                var partyExists = await _dbContext.Parties.AnyAsync(p => p.PartyId == request.PartyId, cancellationToken);
                if (!partyExists)
                {
                    return Result.Failure<Expense>(new Error(
                        "SaveExpenseCommand.PartyNotFound",
                        $"Party with ID {request.PartyId} does not exist."));
                }

                // Create a new Expense entity
                var newExpense = new Expense
                {
                    PlantId = request.PlantId,
                    ExpenseType = request.ExpenseType.Trim(),
                    BillNumber = request.BillNumber?.Trim(),
                    BillValue = request.BillValue,
                    GST = request.GST,
                    TotalBillAmount = request.TotalBillAmount,
                    ExpenseDate = request.ExpenseDate,
                    PartyId = request.PartyId
                };

                // Add the new expense
                _dbContext.Expenses.Add(newExpense);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return Result.Success(newExpense);
            }
        }
    }

    public class SaveExpenseDetailsEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/expenses", async (SaveExpenseCommand command, IRequestHandler<SaveExpenseCommand, Result<Expense>> handler, IValidator<SaveExpenseCommand> validator, CancellationToken cancellationToken) =>
            {
                var validationResult = await validator.ValidateAsync(command, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Invalid Request",
                        Detail = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)),
                        Instance = "/expenses"
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
                        Instance = "/expenses"
                    };
                    return Results.Problem(problemDetails);
                }

                return Results.Created($"/expenses/{result.Value.ExpenseId}", result.Value);
            })
            .WithName("SaveExpenseDetails")
            .WithTags("CoilApi")
            .RequireAuthorization("coil.api")
            .Produces(StatusCodes.Status201Created, typeof(Expense))
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();
        }
    }
}
