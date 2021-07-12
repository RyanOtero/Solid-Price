﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Solidworks_Cutlist_Generator.Model;

namespace Solidworks_Cutlist_Generator.Migrations
{
    [DbContext(typeof(CutListGeneratorContext))]
    partial class CutlistGeneratorContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 64)
                .HasAnnotation("ProductVersion", "5.0.7");

            modelBuilder.Entity("Solidworks_Cutlist_Generator.Model.StockItem", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<decimal>("CostPerFoot")
                        .HasColumnType("decimal(18, 2)");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<int>("MatType")
                        .HasColumnType("int");

                    b.Property<int>("ProfType")
                        .HasColumnType("int");

                    b.Property<string>("Series")
                        .HasColumnType("text");

                    b.Property<int>("StockLength")
                        .HasColumnType("int");

                    b.Property<int?>("VendorID")
                        .HasColumnType("int");

                    b.HasKey("ID");

                    b.HasIndex("VendorID");

                    b.ToTable("StockItems");
                });

            modelBuilder.Entity("Solidworks_Cutlist_Generator.Model.Vendor", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("ContactName")
                        .HasColumnType("text");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("text");

                    b.Property<string>("VendorName")
                        .HasColumnType("text");

                    b.HasKey("ID");

                    b.ToTable("Vendors");
                });

            modelBuilder.Entity("Solidworks_Cutlist_Generator.Model.StockItem", b =>
                {
                    b.HasOne("Solidworks_Cutlist_Generator.Model.Vendor", "Vendor")
                        .WithMany()
                        .HasForeignKey("VendorID");

                    b.Navigation("Vendor");
                });
#pragma warning restore 612, 618
        }
    }
}
