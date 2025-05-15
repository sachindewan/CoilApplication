using Microsoft.EntityFrameworkCore;

namespace Coil.Api.Shared.Extentions
{
    public static class DataBaseExtention
    {
        public static IApplicationBuilder UseMigration<TContext>(this IApplicationBuilder app) where TContext : DbContext
        {
            MigrateDatabaseAsync<TContext>(app.ApplicationServices).GetAwaiter().GetResult();
            return app;
        }

        private static async Task MigrateDatabaseAsync<TContext>(IServiceProvider serviceProvider) where TContext : DbContext
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TContext>();
            if (context.Database.GetPendingMigrations().Any())
            {
                await context.Database.MigrateAsync();
            }
        }
    }
}
