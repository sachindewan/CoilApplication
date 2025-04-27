using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Features.UserAssignedToPlant;
using Coil.Api.Shared;
using Coil.Api.Shared.Exception.Handler;
using Coil.Api.Shared.MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using static Coil.Api.Features.ChallengeOperations.GetAllChallengesState;
using static Coil.Api.Features.ChallengeOperations.SaveChallengeStateDetails;
using static Coil.Api.Features.ChallengeOperations.UpdateChallengeState;
using static Coil.Api.Features.Challenges.GetAllChallenges;
using static Coil.Api.Features.Challenges.SaveChallengeDetails;
using static Coil.Api.Features.Expenses.SaveExpenseDetails;
using static Coil.Api.Features.Identity.CreateRole;
using static Coil.Api.Features.Identity.SetRoleToUser;
using static Coil.Api.Features.OutStandingPartyAmount.GetOutStandingAmount;
using static Coil.Api.Features.Parties.GetAllPartiesDetails;
using static Coil.Api.Features.Parties.SavePartyDetails;
using static Coil.Api.Features.Payments.SavePaymentDetails;
using static Coil.Api.Features.Plants.GetAllPlantDetails;
using static Coil.Api.Features.Plants.SavePlantDetails;
using static Coil.Api.Features.Product.CreateProduct;
using static Coil.Api.Features.RawMaterialOperations.GetRawMaterialQuantity;
using static Coil.Api.Features.RawMaterialOperations.SaveRawMaterialPurchaseDetails;
using static Coil.Api.Features.RawMaterials.GetAllRawMaterialsDetails;
using static Coil.Api.Features.RawMaterials.SaveRawMaterialDetails;
using static Coil.Api.Features.UserAssignedToPlant.AddUserToPlant;
using static Coil.Api.Features.Wheather.GetWheatherForCast;

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
            services.AddScoped<IRequestHandler<WheatherForCastQuery, Result<List<WeatherForecastResponse>>>, GetWheatherForCastHandler>();
            services.AddScoped<IRequestHandler<AllPlantDetailsQuery, Result<List<Entities.Plant>>>, GetAllPlantDetailsHandler>();
            services.AddScoped<IRequestHandler<AllPartiesDetailsQuery, Result<List<Party>>>, GetAllPartiesDetailsHandler>();
            services.AddScoped<IRequestHandler<AllRawMaterialsDetailsQuery, Result<List<RawMaterial>>>, GetAllRawMaterialsDetailsHandler>();
            services.AddScoped<IRequestHandler<SavePartyCommand, Result<Party>>, SavePartyCommandHandler>();
            services.AddScoped<IRequestHandler<SavePlantCommand, Result<Plant>>, SavePlantCommandHandler>();
            services.AddScoped<IRequestHandler<SaveRawMaterialCommand, Result<RawMaterial>>, SaveRawMaterialCommandHandler>();
            services.AddScoped<IRequestHandler<SaveRawMaterialPurchaseCommand, Result<RawMaterialPurchase>>, SaveRawMaterialPurchaseCommandHandler>();
            services.AddScoped<IRequestHandler<RawMaterialQuantityQuery, Result<RawMaterialQuantity>>, GetRawMaterialQuantityHandler>();
            services.AddScoped<IRequestHandler<SaveExpenseCommand, Result<Expense>>, SaveExpenseCommandHandler>();
            services.AddScoped<IRequestHandler<SavePaymentCommand, Result<Payment>>, SavePaymentCommandHandler>();
            services.AddScoped<IRequestHandler<AllChallengesQuery, Result<List<Challenge>>>, GetAllChallengesHandler>();
            services.AddScoped<IRequestHandler<SaveChallengeCommand, Result<Challenge>>, SaveChallengeCommandHandler>();
            services.AddScoped<IRequestHandler<AllChallengesStateQuery, Result<List<ChallengesState>>>, GetAllChallengesStateHandler>();
            services.AddScoped<IRequestHandler<SaveChallengeStateCommand, Result<ChallengesState>>, SaveChallengesStateCommandHandler>();
            services.AddScoped<IRequestHandler<UpdateChallengeStateCommand, Result<ChallengesState>>, UpdateChallengeStateCommandHandler>();
            services.AddScoped<IRequestHandler<AddUserToPlantCommand, Result<AddUserToPlantCommandResponse>>, AddUserToPlantHandler>();

            services.AddScoped<IRequestHandler<SetUserRoleQuery, Result<SetUserRoleResponse>>, SetRoleToUserHandler>();
            services.AddScoped<IRequestHandler<CreateRoleQuery, Result<CreateRoleResponse>>, CreateRoleHandler>();
            services.AddScoped<IRequestHandler<CreateProductQuery, Result<CreateProductResponse>>, CreateProductHandler>();
            services.AddScoped<IRequestHandler<GetOutStandingPurchaseAmountQuery, Result<GetOutStandingPurchaseAmountResponse>>, GetOutStandingPurchaseAmountHandler>();
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
            services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<CoilIdentityDbContext>().AddApiEndpoints();
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
