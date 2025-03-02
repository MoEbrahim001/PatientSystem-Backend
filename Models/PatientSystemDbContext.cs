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




    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.Property(e => e.Dob).HasColumnType("datetime");
            entity.Property(e => e.FaceImg).HasMaxLength(50);
            entity.Property(e => e.Mobileno).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Nationalno).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
