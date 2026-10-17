using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RodajeIA.Web.Datos.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Series",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Series", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Episodios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SerieId = table.Column<int>(type: "INTEGER", nullable: false),
                    Numero = table.Column<int>(type: "INTEGER", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", nullable: false),
                    Guion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Episodios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Episodios_Series_SerieId",
                        column: x => x.SerieId,
                        principalTable: "Series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hojas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SerieId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    NombreClave = table.Column<string>(type: "TEXT", nullable: false),
                    Edad = table.Column<string>(type: "TEXT", nullable: false),
                    DescripcionFisica = table.Column<string>(type: "TEXT", nullable: false),
                    Peinado = table.Column<string>(type: "TEXT", nullable: false),
                    Vestuario = table.Column<string>(type: "TEXT", nullable: false),
                    HeridasMarcas = table.Column<string>(type: "TEXT", nullable: true),
                    Personalidad = table.Column<string>(type: "TEXT", nullable: false),
                    Rol = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hojas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hojas_Series_SerieId",
                        column: x => x.SerieId,
                        principalTable: "Series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Escenas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EpisodioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Numero = table.Column<int>(type: "INTEGER", nullable: false),
                    IntExt = table.Column<string>(type: "TEXT", nullable: false),
                    Lugar = table.Column<string>(type: "TEXT", nullable: false),
                    MomentoDia = table.Column<string>(type: "TEXT", nullable: false),
                    Locacion = table.Column<string>(type: "TEXT", nullable: false),
                    Iluminacion = table.Column<string>(type: "TEXT", nullable: false),
                    PuestaEnEscena = table.Column<string>(type: "TEXT", nullable: false),
                    Audio = table.Column<string>(type: "TEXT", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", nullable: false),
                    MotivoError = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Escenas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Escenas_Episodios_EpisodioId",
                        column: x => x.EpisodioId,
                        principalTable: "Episodios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Clips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EscenaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Numero = table.Column<int>(type: "INTEGER", nullable: false),
                    Accion = table.Column<string>(type: "TEXT", nullable: false),
                    TextoEnPantalla = table.Column<string>(type: "TEXT", nullable: true),
                    TextoEnPantallaOriginal = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clips_Escenas_EscenaId",
                        column: x => x.EscenaId,
                        principalTable: "Escenas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonajesEnEscena",
                columns: table => new
                {
                    EscenaId = table.Column<int>(type: "INTEGER", nullable: false),
                    HojaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonajesEnEscena", x => new { x.EscenaId, x.HojaId });
                    table.ForeignKey(
                        name: "FK_PersonajesEnEscena_Escenas_EscenaId",
                        column: x => x.EscenaId,
                        principalTable: "Escenas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonajesEnEscena_Hojas_HojaId",
                        column: x => x.HojaId,
                        principalTable: "Hojas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Variantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EscenaId = table.Column<int>(type: "INTEGER", nullable: false),
                    HojaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Edad = table.Column<string>(type: "TEXT", nullable: true),
                    Peinado = table.Column<string>(type: "TEXT", nullable: true),
                    Vestuario = table.Column<string>(type: "TEXT", nullable: true),
                    ReemplazaHeridas = table.Column<bool>(type: "INTEGER", nullable: false),
                    HeridasMarcas = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Variantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Variantes_Escenas_EscenaId",
                        column: x => x.EscenaId,
                        principalTable: "Escenas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Variantes_Hojas_HojaId",
                        column: x => x.HojaId,
                        principalTable: "Hojas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bloques",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClipId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bloques", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bloques_Clips_ClipId",
                        column: x => x.ClipId,
                        principalTable: "Clips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LineasDialogo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClipId = table.Column<int>(type: "INTEGER", nullable: false),
                    Posicion = table.Column<int>(type: "INTEGER", nullable: false),
                    Personaje = table.Column<string>(type: "TEXT", nullable: false),
                    Acotacion = table.Column<string>(type: "TEXT", nullable: true),
                    Texto = table.Column<string>(type: "TEXT", nullable: false),
                    Original = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineasDialogo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineasDialogo_Clips_ClipId",
                        column: x => x.ClipId,
                        principalTable: "Clips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonajesEnClip",
                columns: table => new
                {
                    ClipId = table.Column<int>(type: "INTEGER", nullable: false),
                    HojaId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonajesEnClip", x => new { x.ClipId, x.HojaId });
                    table.ForeignKey(
                        name: "FK_PersonajesEnClip_Clips_ClipId",
                        column: x => x.ClipId,
                        principalTable: "Clips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonajesEnClip_Hojas_HojaId",
                        column: x => x.HojaId,
                        principalTable: "Hojas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tomas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BloqueId = table.Column<int>(type: "INTEGER", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    Plano = table.Column<string>(type: "TEXT", nullable: false),
                    Optica = table.Column<string>(type: "TEXT", nullable: false),
                    Iluminacion = table.Column<string>(type: "TEXT", nullable: false),
                    Angulo = table.Column<string>(type: "TEXT", nullable: true),
                    Movimiento = table.Column<string>(type: "TEXT", nullable: true),
                    Accion = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tomas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tomas_Bloques_BloqueId",
                        column: x => x.BloqueId,
                        principalTable: "Bloques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TomasDialogo",
                columns: table => new
                {
                    TomaId = table.Column<int>(type: "INTEGER", nullable: false),
                    LineaDialogoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TomasDialogo", x => new { x.TomaId, x.LineaDialogoId });
                    table.ForeignKey(
                        name: "FK_TomasDialogo_LineasDialogo_LineaDialogoId",
                        column: x => x.LineaDialogoId,
                        principalTable: "LineasDialogo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TomasDialogo_Tomas_TomaId",
                        column: x => x.TomaId,
                        principalTable: "Tomas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bloques_ClipId",
                table: "Bloques",
                column: "ClipId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clips_EscenaId_Numero",
                table: "Clips",
                columns: new[] { "EscenaId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Episodios_SerieId_Numero",
                table: "Episodios",
                columns: new[] { "SerieId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Escenas_EpisodioId_Numero",
                table: "Escenas",
                columns: new[] { "EpisodioId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Hojas_SerieId_NombreClave",
                table: "Hojas",
                columns: new[] { "SerieId", "NombreClave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LineasDialogo_ClipId_Posicion",
                table: "LineasDialogo",
                columns: new[] { "ClipId", "Posicion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonajesEnClip_HojaId",
                table: "PersonajesEnClip",
                column: "HojaId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonajesEnEscena_HojaId",
                table: "PersonajesEnEscena",
                column: "HojaId");

            migrationBuilder.CreateIndex(
                name: "IX_Tomas_BloqueId_Orden",
                table: "Tomas",
                columns: new[] { "BloqueId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TomasDialogo_LineaDialogoId",
                table: "TomasDialogo",
                column: "LineaDialogoId");

            migrationBuilder.CreateIndex(
                name: "IX_Variantes_EscenaId_HojaId",
                table: "Variantes",
                columns: new[] { "EscenaId", "HojaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Variantes_HojaId",
                table: "Variantes",
                column: "HojaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonajesEnClip");

            migrationBuilder.DropTable(
                name: "PersonajesEnEscena");

            migrationBuilder.DropTable(
                name: "TomasDialogo");

            migrationBuilder.DropTable(
                name: "Variantes");

            migrationBuilder.DropTable(
                name: "LineasDialogo");

            migrationBuilder.DropTable(
                name: "Tomas");

            migrationBuilder.DropTable(
                name: "Hojas");

            migrationBuilder.DropTable(
                name: "Bloques");

            migrationBuilder.DropTable(
                name: "Clips");

            migrationBuilder.DropTable(
                name: "Escenas");

            migrationBuilder.DropTable(
                name: "Episodios");

            migrationBuilder.DropTable(
                name: "Series");
        }
    }
}
