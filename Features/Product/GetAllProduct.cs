using Carter;
using Coil.Api.Database;
using Microsoft.EntityFrameworkCore;

namespace Coil.Api.Features.Product
{
    public class GetAllProductEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/products", async (CoilApplicationDbContext dbContext,HttpContext context, int pageNumber = 1, int pageSize = 10) =>
            {
                if (pageNumber < 1 || pageSize < 1)
                {
                    return Results.BadRequest("Page number and page size must be greater than 0.");
                }

                var totalProducts = await dbContext.Products.CountAsync();
                var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);

                var products = await dbContext.Products.Include(x=>x.ProductImages)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                var request = context.Request;
                var host = $"{request.Scheme}://{request.Host}";

                foreach (var product in products)
                {
                    foreach (var image in product.ProductImages)
                    {
                        image.Uri = $"{host}/{image.Uri.TrimStart('/')}";
                    }
                }

                var response = new
                {
                    TotalCount = totalProducts,
                    PageSize = pageSize,
                    CurrentPage = pageNumber,
                    TotalPages = totalPages,
                    Products = products
                };

                return Results.Ok(response);
            }).WithTags("CoilApi")
                .WithName("GetAllProduct")
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .WithOpenApi();
        }
    }
}
