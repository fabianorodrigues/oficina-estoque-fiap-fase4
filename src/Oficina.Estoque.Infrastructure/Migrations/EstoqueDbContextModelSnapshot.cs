using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Oficina.Estoque.Infrastructure.Persistencia;

#nullable disable

namespace Oficina.Estoque.Infrastructure.Migrations;

[DbContext(typeof(EstoqueDbContext))]
partial class EstoqueDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.7")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

        new EstoqueDbContext(new DbContextOptionsBuilder<EstoqueDbContext>().Options)
            .GetType();
    }
}
