﻿// <auto-generated />
using CHEF.Components.Commands.Ignore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace CHEF.Migrations.Ignore
{
    [DbContext(typeof(IgnoreContext))]
    partial class IgnoreContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("CHEF.Components.Commands.Ignore.Ignore", b =>
                {
                    b.Property<decimal>("DiscordId")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("discord_id")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("DiscordId")
                        .HasName("pk_user_ids");

                    b.ToTable("user_ids");
                });
#pragma warning restore 612, 618
        }
    }
}
