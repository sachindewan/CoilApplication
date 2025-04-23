using Carter;
using Coil.Api.Extentions;
using Microsoft.AspNetCore.Identity;

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

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("CorsPolicy");
app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseStaticFiles();
app.MapGroup("api/auth").WithTags("Identity.Apis").MapIdentityApi<IdentityUser>();
app.MapCarter();
app.Run();

