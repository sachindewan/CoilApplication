﻿// <auto-generated />
using Coil.Api.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Coil.Api.Migrations
{
    [DbContext(typeof(CoilApplicationDbContext))]
    partial class CoilApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Coil.Api.Entities.Party", b =>
                {
                    b.Property<int>("PartyId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("PartyId"));

                    b.Property<string>("PartyName")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.Property<int>("PlantId")
                        .HasColumnType("integer");

                    b.HasKey("PartyId");

                    b.HasIndex("PlantId");

                    b.ToTable("Parties");
                });

            modelBuilder.Entity("Coil.Api.Entities.Plant", b =>
                {
                    b.Property<int>("PlantId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("PlantId"));

                    b.Property<string>("Location")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("PlantName")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.HasKey("PlantId");

                    b.ToTable("Plants");
                });

            modelBuilder.Entity("Coil.Api.Entities.RawMaterial", b =>
                {
                    b.Property<int>("RawMaterialId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("RawMaterialId"));

                    b.Property<string>("RawMaterialName")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.HasKey("RawMaterialId");

                    b.ToTable("RawMaterials");
                });

            modelBuilder.Entity("Coil.Api.Entities.Party", b =>
                {
                    b.HasOne("Coil.Api.Entities.Plant", null)
                        .WithMany("Parties")
                        .HasForeignKey("PlantId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Coil.Api.Entities.Plant", b =>
                {
                    b.Navigation("Parties");
                });
#pragma warning restore 612, 618
        }
    }
}
