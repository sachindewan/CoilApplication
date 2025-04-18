﻿using Carter;
using Coil.Api.Database;
using Coil.Api.Features.Wheather;
using Coil.Api.Shared;
using Coil.Api.Shared.Exception.Handler;
using Coil.Api.Shared.MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using static Coil.Api.Features.Wheather.GetWheatherForCast;
using System.Reflection;
using static Coil.Api.Features.Identity.SetRoleToUser;
using static Coil.Api.Features.Identity.CreateRole;

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
            services.AddScoped<IRequestHandler<SetUserRoleQuery, Result<SetUserRoleResponse>>, SetRoleToUserHandler>();
            services.AddScoped<IRequestHandler<CreateRoleQuery, Result<CreateRoleResponse>>, CreateRoleHandler>();

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
