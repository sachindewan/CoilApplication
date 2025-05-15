using Carter;
using Coil.Api.Database;
using Coil.Api.Entities;
using Coil.Api.Extentions;
using Coil.Api.Shared.Extentions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.RegisterApplicationServices();
builder.Services.RegisterPersistenceServices(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{

}
app.UseMigration<CoilApplicationDbContext>();
app.UseMigration<CoilIdentityDbContext>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("CorsPolicy");
app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseStaticFiles();
app.MapGroup("api/auth").WithTags("Identity.Apis").MapIdentityApi<ApplicationUser>();
app.MapCarter();
app.Run();

