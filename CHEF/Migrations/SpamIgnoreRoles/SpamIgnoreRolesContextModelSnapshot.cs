﻿// <auto-generated />
using CHEF.Components.Watcher.Spam;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace CHEF.Migrations.SpamIgnoreRoles
{
    [DbContext(typeof(SpamFilterContext))]
    partial class SpamIgnoreRolesContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("CHEF.Components.Watcher.Spam.SpamFilterConfig", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int>("ActionOnSpam")
                        .HasColumnName("action_on_spam")
                        .HasColumnType("integer");

                    b.Property<bool>("InlcudeMessageContentInLog")
                        .HasColumnName("inlcude_message_content_in_log")
                        .HasColumnType("boolean");

                    b.Property<int>("MessageSecondsGap")
                        .HasColumnName("message_seconds_gap")
                        .HasColumnType("integer");

                    b.Property<int>("MessagesForAction")
                        .HasColumnName("messages_for_action")
                        .HasColumnType("integer");

                    b.Property<decimal>("MuteRoleId")
                        .HasColumnName("mute_role_id")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id")
                        .HasName("pk_spam_filter_configs");

                    b.ToTable("spam_filter_configs");
                });

            modelBuilder.Entity("CHEF.Components.Watcher.Spam.SpamIgnoreRole", b =>
                {
                    b.Property<decimal>("DiscordId")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("discord_id")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("DiscordId")
                        .HasName("pk_spam_ignore_roles");

                    b.ToTable("spam_ignore_roles");
                });
#pragma warning restore 612, 618
        }
    }
}
