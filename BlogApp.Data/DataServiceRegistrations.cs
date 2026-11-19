using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BlogApp.Data;

public static class DataServiceRegistrations
{
    public static IServiceCollection AddDataServices(this IServiceCollection services, IConfiguration configuration)
    {
        // SQL Server by default; "Sqlite" (Database:Provider) runs the public demo, with the database in a file that
        // starts again from the demo content on every restart.
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var useSqlite = string.Equals(configuration["Database:Provider"], "Sqlite", StringComparison.OrdinalIgnoreCase);
        services.AddDbContext<AppDbContext>(options =>
        {
            if (useSqlite)
                options.UseSqlite(connectionString);
            else
                options.UseSqlServer(connectionString);
        });

        return services;
    }
}
