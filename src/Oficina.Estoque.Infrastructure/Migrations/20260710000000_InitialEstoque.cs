using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Oficina.Estoque.Infrastructure.Persistencia;

#nullable disable

namespace Oficina.Estoque.Infrastructure.Migrations;

[DbContext(typeof(EstoqueDbContext))]
[Migration("20260710000000_InitialEstoque")]
public partial class InitialEstoque : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Insumos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                PrecoUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Insumos", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Pecas",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                PrecoUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Pecas", x => x.Id));

        migrationBuilder.CreateTable(
            name: "EstoqueInsumos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                InsumoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Quantidade = table.Column<int>(type: "int", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_EstoqueInsumos", x => x.Id));

        migrationBuilder.CreateTable(
            name: "EstoquePecas",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PecaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Quantidade = table.Column<int>(type: "int", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_EstoquePecas", x => x.Id));

        migrationBuilder.CreateTable(
            name: "MovimentacoesEstoque",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TipoMaterial = table.Column<int>(type: "int", nullable: false),
                MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Tipo = table.Column<int>(type: "int", nullable: false),
                Quantidade = table.Column<int>(type: "int", nullable: false),
                SaldoResultante = table.Column<int>(type: "int", nullable: false),
                Data = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ReferenciaOperacao = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_MovimentacoesEstoque", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ReservasEstoque",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ChaveOperacao = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                DataCriacao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                DataLiberacao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_ReservasEstoque", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ItensReservaEstoque",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TipoMaterial = table.Column<int>(type: "int", nullable: false),
                MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Quantidade = table.Column<int>(type: "int", nullable: false),
                ReservaEstoqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ItensReservaEstoque", x => x.Id);
                table.ForeignKey(
                    name: "FK_ItensReservaEstoque_ReservasEstoque_ReservaEstoqueId",
                    column: x => x.ReservaEstoqueId,
                    principalTable: "ReservasEstoque",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_EstoqueInsumos_InsumoId", "EstoqueInsumos", "InsumoId", unique: true);
        migrationBuilder.CreateIndex("IX_EstoquePecas_PecaId", "EstoquePecas", "PecaId", unique: true);
        migrationBuilder.CreateIndex("IX_ItensReservaEstoque_ReservaEstoqueId_TipoMaterial_MaterialId", "ItensReservaEstoque", new[] { "ReservaEstoqueId", "TipoMaterial", "MaterialId" }, unique: true);
        migrationBuilder.CreateIndex("IX_MovimentacoesEstoque_ReferenciaOperacao", "MovimentacoesEstoque", "ReferenciaOperacao");
        migrationBuilder.CreateIndex("IX_MovimentacoesEstoque_TipoMaterial_MaterialId_Data", "MovimentacoesEstoque", new[] { "TipoMaterial", "MaterialId", "Data" });
        migrationBuilder.CreateIndex("IX_ReservasEstoque_ChaveOperacao", "ReservasEstoque", "ChaveOperacao", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("EstoqueInsumos");
        migrationBuilder.DropTable("EstoquePecas");
        migrationBuilder.DropTable("Insumos");
        migrationBuilder.DropTable("ItensReservaEstoque");
        migrationBuilder.DropTable("MovimentacoesEstoque");
        migrationBuilder.DropTable("Pecas");
        migrationBuilder.DropTable("ReservasEstoque");
    }
}
