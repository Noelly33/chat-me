using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Consumer.Messaging.Worker.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "conversaciones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    es_grupal = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nombre = table.Column<string>(type: "text", nullable: true),
                    creado_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversaciones", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tipos_mensaje",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "text", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_mensaje", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "participantes_conversacion",
                columns: table => new
                {
                    conversacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unido_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    ultimo_leido_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_participantes_conversacion", x => new { x.conversacion_id, x.usuario_id });
                    table.ForeignKey(
                        name: "FK_participantes_conversacion_conversaciones_conversacion_id",
                        column: x => x.conversacion_id,
                        principalTable: "conversaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mensajes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    emisor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_mensaje_id = table.Column<int>(type: "integer", nullable: false),
                    contenido = table.Column<string>(type: "text", nullable: true),
                    creado_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mensajes", x => x.id);
                    table.ForeignKey(
                        name: "FK_mensajes_conversaciones_conversacion_id",
                        column: x => x.conversacion_id,
                        principalTable: "conversaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_mensajes_tipos_mensaje_tipo_mensaje_id",
                        column: x => x.tipo_mensaje_id,
                        principalTable: "tipos_mensaje",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mensajes_conversacion_id",
                table: "mensajes",
                column: "conversacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_mensajes_tipo_mensaje_id",
                table: "mensajes",
                column: "tipo_mensaje_id");

            migrationBuilder.CreateIndex(
                name: "IX_tipos_mensaje_codigo",
                table: "tipos_mensaje",
                column: "codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mensajes");

            migrationBuilder.DropTable(
                name: "participantes_conversacion");

            migrationBuilder.DropTable(
                name: "tipos_mensaje");

            migrationBuilder.DropTable(
                name: "conversaciones");
        }
    }
}
