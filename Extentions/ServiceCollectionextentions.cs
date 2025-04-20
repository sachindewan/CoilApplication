using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Shared;
using Coil.Api.Shared.Exception.Handler;
using Coil.Api.Shared.MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static Coil.Api.Features.Parties.GetAllPartiesDetails;
using static Coil.Api.Features.Parties.SavePartyDetails;
using static Coil.Api.Features.Plants.GetAllPlantDetails;
using static Coil.Api.Features.Plants.SavePlantDetails;
using static Coil.Api.Features.RawMaterials.GetAllRawMaterialsDetails;
using static Coil.Api.Features.RawMaterials.SaveRawMaterialDetails;
using FluentValidation;
using static Coil.Api.Features.Wheather.GetWheatherForCast;
using System.Reflection;
using static Coil.Api.Features.Identity.SetRoleToUser;
using static Coil.Api.Features.Identity.CreateRole;
using Coil.Api.Features.Product;
using static Coil.Api.Features.Product.CreateProduct;
using Microsoft.OpenApi.Models;

namespace Coil.Api.Extentions
{
    public static class ServiceCollectionextentions
    {
        public static IServiceCollection RegisterApplicationServices(this IServiceCollection services)
        {
            var currentAssembly = Assembly.GetExecutingAssembly();
            services.AddProblemDetails();
            services.AddAuthentication().AddBearerToken(IdentityConstants.BearerScheme);
            services.AddAuthorizationBuilder()
                .AddPolicy("coil.api", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddAuthenticationSchemes(IdentityConstants.BearerScheme);
                });
            services.AddEndpointsApiExplorer();
            AddSwaggerGenDocSecurityRequirements(services);
            services.AddCarter();
            // Register your handlers
            services.AddTransient<IRequestHandler<WheatherForCastQuery, Result<List<WeatherForecastResponse>>>, GetWheatherForCastHandler>();
            services.AddTransient<IRequestHandler<AllPlantDetailsQuery, Result<List<Entities.Plant>>>, GetAllPlantDetailsHandler>();
            services.AddTransient<IRequestHandler<AllPartiesDetailsQuery, Result<List<Party>>>, GetAllPartiesDetailsHandler>();
            services.AddTransient<IRequestHandler<AllRawMaterialsDetailsQuery, Result<List<RawMaterial>>>, GetAllRawMaterialsDetailsHandler>();
            services.AddTransient<IRequestHandler<SavePartyCommand, Result<Party>>, SavePartyCommandHandler>();
            services.AddTransient<IRequestHandler<SavePlantCommand, Result<Plant>>, SavePlantCommandHandler>();
            services.AddTransient<IRequestHandler<SaveRawMaterialCommand, Result<RawMaterial>>, SaveRawMaterialCommandHandler>();

            services.AddScoped<IRequestHandler<SetUserRoleQuery, Result<SetUserRoleResponse>>, SetRoleToUserHandler>();
            services.AddScoped<IRequestHandler<CreateRoleQuery, Result<CreateRoleResponse>>, CreateRoleHandler>();
            services.AddScoped<IRequestHandler<CreateProductQuery, Result<CreateProductResponse>>, CreateProductHandler>();

            services.AddExceptionHandler<GlobalExceptionMiddleware>();
            services.AddValidatorsFromAssembly(currentAssembly);
            return services;
        }
        public static IServiceCollection RegisterPersistenceServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<CoilApplicationDbContext>(o =>
                o.UseNpgsql(configuration.GetConnectionString("CoilDatabaseConnectionString")));

            services.AddDbContext<CoilIdentityDbContext>(o =>
               o.UseNpgsql(configuration.GetConnectionString("CoilDatabaseConnectionString")));
            services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<CoilIdentityDbContext>().AddApiEndpoints();
            return services;
        }

        private static void AddSwaggerGenDocSecurityRequirements(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "My API", Version = "v1" });

                // Add security definition for Bearer token
                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Please enter a valid token",
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "bearer"
                });

                // Add security requirement to use the Bearer token
                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });
        }
    }
}
