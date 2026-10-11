using HomeClass.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeClass.Infrastructure.Seed;

public static class DbInitializer
{
    public static async Task InicializarAsync(IServiceProvider services, SeedOptions options)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();

        // Si ya generaste migraciones (dotnet ef migrations add Inicial) se aplican;
        // si todavía no, la base se crea directamente desde el modelo para poder arrancar de inmediato.
        if (db.Database.GetMigrations().Any())
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();

        await DbSeeder.SeedAsync(sp, options);
    }
}
