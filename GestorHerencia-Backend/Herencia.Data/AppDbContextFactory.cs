using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Herencia.Data;

// Fabrica de DbContext en tiempo de diseno. Herencia.Data es una libreria de clases
// (no un proyecto ejecutable), por lo que "dotnet ef migrations add" no tiene forma
// de construir un AppDbContext por si solo. Esta clase le da al tooling de EF Core
// una cadena de conexion minima para poder generar migraciones DESDE la propia
// capa Data, sin depender de que la capa Api ya tenga su Program.cs configurado.
// La cadena de conexion real (la que usa la aplicacion en ejecucion) se define en
// la capa Api, no aqui.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite("Data Source=herencia_digital.db");

        return new AppDbContext(optionsBuilder.Options);
    }
}
