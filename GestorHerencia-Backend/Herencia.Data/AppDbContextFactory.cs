using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Herencia.Data;

/// <summary>
/// Fabrica de DbContext en tiempo de diseno: Herencia.Data es una libreria de
/// clases, no un ejecutable, asi que "dotnet ef migrations add" no puede construir
/// un AppDbContext por si solo. Le da al tooling una cadena de conexion minima para
/// generar migraciones sin depender de que la capa Api ya este configurada. La
/// cadena de conexion real de la aplicacion se define en la capa Api, no aqui.
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
