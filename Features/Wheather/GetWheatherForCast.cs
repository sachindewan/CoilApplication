using Carter;
using Microsoft.AspNetCore.Identity.Data;
using Coil.Api.Shared.MediatR;
using static Coil.Api.Features.Wheather.GetWheatherForCast;

namespace Coil.Api.Features.Wheather
{

    public static class GetWheatherForCast
    {
        public record WheatherForCastQuery() : IRequest<List<WeatherForecastResponse>>;
        internal record WeatherForecastResponse(DateOnly Date, int TemperatureC, string? Summary)
        {
            public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
        }
        internal sealed class GetItinerariesHandler : IRequestHandler<WheatherForCastQuery, List<WeatherForecastResponse>>
        {

            public Task<List<WeatherForecastResponse>> Handle(WheatherForCastQuery request, CancellationToken cancellationToken)
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
                    .ToArray();
                return Task.FromResult(forecast.ToList());
            }
        }
    }
    public class GetWheatherForeCastEndpoint : ICarterModule
    {

        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/weatherforecast", (IRequestHandler<WheatherForCastQuery, List<WeatherForecastResponse>> requestHandler, CancellationToken token) =>
            {
                var result = requestHandler.Handle(new WheatherForCastQuery(), token);
                return Results.Ok(result);
            })
            .WithName("GetWeatherForecast")
            .Produces(StatusCodes.Status200OK,typeof(WeatherForecastResponse))
            .WithOpenApi();
        }
    }
}
