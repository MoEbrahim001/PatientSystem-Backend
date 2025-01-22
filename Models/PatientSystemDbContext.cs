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
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-CLQGA5Q\\SQLEXPRESS;Database=PatientSystemDB;Trusted_Connection=True;Encrypt=False;");

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
