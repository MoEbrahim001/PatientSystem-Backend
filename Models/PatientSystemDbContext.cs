using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PatientSystem.Models;

public partial class PatientSystemDbContext : DbContext
{
    public PatientSystemDbContext()
    {
    }

    public PatientSystemDbContext(DbContextOptions<PatientSystemDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Patient> Patients { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  => optionsBuilder.UseSqlServer("Server=DESKTOP-CLQGA5Q\\SQLEXPRESS;Initial Catalog=PatientSystemDB;Trusted_Connection=true;TrustServerCertificate=true;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //modelBuilder.Entity<Patient>(entity =>
        //{
        //    entity.Property(e => e.Id).ValueGeneratedNever();
        //    entity.Property(e => e.Mobileno).HasMaxLength(50);
        //    entity.Property(e => e.Name).HasMaxLength(100);
        //    entity.Property(e => e.Nationalno).HasMaxLength(50);
        //});

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
