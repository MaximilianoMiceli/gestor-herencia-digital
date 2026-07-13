using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Herencia.Data;

/// <summary>
/// Fabrica de DbContext en tiempo de diseno: permite a "dotnet ef migrations add" construir un
/// AppDbContext con una cadena de conexion minima. La cadena real se define en la capa Api.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite("Data Source=herencia_digital.db");

        return new AppDbContext(optionsBuilder.Options);
    }
}
