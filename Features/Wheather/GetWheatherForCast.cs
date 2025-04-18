using Carter;
using Coil.Api.Shared;
using Coil.Api.Shared.MediatR;
using static Coil.Api.Features.Wheather.GetWheatherForCast;

namespace Coil.Api.Features.Wheather
{

    public static class GetWheatherForCast
    {
        public record WheatherForCastQuery() : IRequest<Result<List<WeatherForecastResponse>>>;
        internal record WeatherForecastResponse(DateOnly Date, int TemperatureC, string? Summary)
        {
            public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
        }
        internal sealed class GetWheatherForCastHandler : IRequestHandler<WheatherForCastQuery, Result<List<WeatherForecastResponse>>>
        {

            public Task<Result<List<WeatherForecastResponse>>> Handle(WheatherForCastQuery request, CancellationToken cancellationToken)
            {
                var summaries = new[]
                    {
                                    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
                                };
                var forecast = Enumerable.Range(1, 5).Select(index =>
                    new WeatherForecastResponse
                    (
                        DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        Random.Shared.Next(-20, 55),
                        summaries[Random.Shared.Next(summaries.Length)]
                    ))
                    .ToList();
                if (forecast is null)
                {
                    return Task.FromResult(Result.Failure<List<WeatherForecastResponse>>(new Error(
                        "GetWheatherForCast.Null",
                        "The article with the specified ID was not found")));
                }
                Result<List<WeatherForecastResponse>> result = forecast;

                return Task.FromResult(result);
            }
        }
    }
    public class GetWheatherForeCastEndpoint : ICarterModule
    {

        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/weatherforecast", (IRequestHandler<WheatherForCastQuery, Result<List<WeatherForecastResponse>>> requestHandler, CancellationToken cancellationToken) =>
            {
                var result = requestHandler.Handle(new WheatherForCastQuery(), cancellationToken).Result;
                if (result.IsFailure)
                {
                    return Results.NotFound(result.Error);
                }
                return Results.Ok(result.Value);
            }).WithTags("wheather")
            .WithName("GetWeatherForecast")
            .Produces(StatusCodes.Status200OK, typeof(WeatherForecastResponse));
        }
    }
}
