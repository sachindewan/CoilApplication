using Carter;
using Coil.Api.Contracts;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using static Coil.Api.Features.Product.CreateProduct;

namespace Coil.Api.Features.Product
{
    public class CreateProduct
    {
        public sealed record CreateProductQuery(Guid Id, string Name, string Specification, decimal Price, List<string> ImageList) : IRequest<Result<CreateProductResponse>>;
        public sealed record CreateProductResponse(bool IsSuccess);

        public sealed class CreateProductValidator : AbstractValidator<CreateProductQuery>
        {
            public CreateProductValidator()
            {
                RuleFor(x => x.Name).NotEmpty().WithMessage("Product name is required.");
                RuleFor(x => x.Price).NotEmpty().WithMessage("Product price is required.");
                RuleFor(x => x.ImageList)
                        .NotEmpty()
                        .WithMessage("Product image is required.")
                        .Must(images => images.Count <= 5)
                        .WithMessage("You can upload a maximum of 5 images.");
                RuleFor(x => x.Specification).NotEmpty().WithMessage("Product specification is required.");
            }
        }

        internal class CreateProductHandler(CoilApplicationDbContext dbContext, IValidator<CreateProductQuery> validator) : IRequestHandler<CreateProductQuery, Result<CreateProductResponse>>
        {
            public async Task<Result<CreateProductResponse>> Handle(CreateProductQuery request, CancellationToken cancellationToken)
            {


                var validationResult = validator.Validate(request);
                if (!validationResult.IsValid)
                {
                    return Result.Failure<CreateProductResponse>(new Error(
                        "CreateProductQuery.Invalid",
                        string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))));
                }
                // Map the request to the Product entity
                var product = new Entities.Product
                {
                    Id = request.Id,
                    Name = request.Name,
                    Specification = request.Specification,
                    Price = request.Price,
                    ProductImages = request.ImageList.Select(uri => new ProductImage
                    {
                        Id = Guid.NewGuid(),
                        Uri = uri,
                        ProductId = request.Id
                    }).ToList()
                };

                // Add the product to the context
                await dbContext.Products.AddAsync(product, cancellationToken);

                // Save changes to the database
                await dbContext.SaveChangesAsync(cancellationToken);

                return Result.Success(new CreateProductResponse(true));
            }
        }
    }
    public sealed class CreateProductEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/product", async (
                    [FromForm] ProductRequest productInput,
                    IRequestHandler<CreateProductQuery, Result<CreateProductResponse>> handler,
                    HttpContext context,
                    IWebHostEnvironment env,
                    CancellationToken cancellationToken) =>
                {
                    var form = await context.Request.ReadFormAsync();
                    var files = form.Files;

                    if (files.Count > 5)
                    {
                        return Results.BadRequest("You can upload a maximum of 5 images.");
                    }

                    var productId = Guid.NewGuid();
                    var productFolder = Path.Combine(env.WebRootPath, "ProductUploads", productInput.Name);

                    if (!Directory.Exists(productFolder))
                    {
                        Directory.CreateDirectory(productFolder);
                    }
                    else
                    {
                        var problemDetails = new ProblemDetails
                        {
                            Status = StatusCodes.Status400BadRequest,
                            Title = "Invalid Request",
                            Detail = $"product {productInput.Name} already exist please try adding another product",
                            Instance = "/products"
                        };

                        return Results.Problem(problemDetails);
                    }

                    var imageUrls = new List<string>();

                    foreach (var file in files)
                    {
                        if (file.Length > 0)
                        {
                            var sanitizedFileName = Path.GetFileName(file.FileName); // Prevents path traversal
                            var filePath = Path.Combine(productFolder, sanitizedFileName);

                            if (System.IO.File.Exists(filePath))
                            {
                                return Results.BadRequest($"A file with the name '{sanitizedFileName}' already exists for this product.");
                            }

                            using var stream = new FileStream(filePath, FileMode.CreateNew); // Fail if file exists
                            await file.CopyToAsync(stream);

                            var relativeUrl = $"/ProductUploads/{productInput.Name}/{sanitizedFileName}";
                            imageUrls.Add(relativeUrl);
                        }
                    }

                    var product = new CreateProductQuery(productId, productInput.Name, productInput.Specification, productInput.Price, imageUrls);
                    var result = await handler.Handle(product, cancellationToken);

                    if (result.IsFailure)
                    {
                        var problemDetails = new ProblemDetails
                        {
                            Status = StatusCodes.Status400BadRequest,
                            Title = "Invalid Request",
                            Instance = "/products"
                        };

                        var errors = new List<string>
                                {
                                     $"{result.Error.Message}"
                                };
                        problemDetails.Extensions.Add("Errors", errors);
                        DeleteDirectory(productFolder);
                        return Results.Problem(problemDetails);
                    }

                    return Results.Ok(product);
                })
                .WithTags("CoilApi")
                .WithName("CreateProduct")
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .WithOpenApi()
                .DisableAntiforgery();
        }

        private static void DeleteDirectory(string path)
        {
            Directory.Delete(path, true);
        }
    }
}

