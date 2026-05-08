using System;
using System.Collections.Generic;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL.DBContext;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Block> Blocks { get; set; }

    public virtual DbSet<BlockWeeklyScore> BlockWeeklyScores { get; set; }

    public virtual DbSet<DormitoryWeeklyStat> DormitoryWeeklyStats { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<EventParticipant> EventParticipants { get; set; }

    public virtual DbSet<FloorWeeklyStat> FloorWeeklyStats { get; set; }

    public virtual DbSet<Inspection> Inspections { get; set; }

    public virtual DbSet<InspectionZone> InspectionZones { get; set; }

    public virtual DbSet<RepairComment> RepairComments { get; set; }

    public virtual DbSet<RepairRequest> RepairRequests { get; set; }

    public virtual DbSet<Residence> Residences { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<StudentPoint> StudentPoints { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Строка подключения передаётся через DI из appsettings.json
        // (DAL.DependencyInjection -> AddDataAccess)
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Block>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Blocks__3214EC078A32433B");

            entity.HasIndex(e => e.BlockNumber, "UQ__Blocks__14FE551247B77DCB").IsUnique();

            entity.Property(e => e.BlockNumber).HasMaxLength(10);
        });

        modelBuilder.Entity<BlockWeeklyScore>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__BlockWee__3214EC07B2D78352");

            entity.HasIndex(e => e.BlockId, "IX_BlockWeeklyScores_BlockId");

            entity.HasIndex(e => new { e.Year, e.WeekNumber }, "IX_BlockWeeklyScores_WeekYear");

            entity.HasIndex(e => new { e.BlockId, e.WeekNumber, e.Year }, "UQ__BlockWee__B4EF87A4ADA6B02B").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Score).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.Block).WithMany(p => p.BlockWeeklyScores)
                .HasForeignKey(d => d.BlockId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BlockWeek__Block__7E37BEF6");
        });

        modelBuilder.Entity<DormitoryWeeklyStat>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Dormitor__3214EC07783661FD");

            entity.HasIndex(e => new { e.Year, e.WeekNumber }, "IX_DormitoryWeeklyStats_WeekYear");

            entity.HasIndex(e => new { e.WeekNumber, e.Year }, "UQ__Dormitor__0AD9254AA84205AC").IsUnique();

            entity.Property(e => e.AverageScore).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.CalculatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Events__3214EC0775634156");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.PointsAwarded).HasDefaultValue(0);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Organizer).WithMany(p => p.Events)
                .HasForeignKey(d => d.OrganizerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Events__Organize__534D60F1");
        });

        modelBuilder.Entity<EventParticipant>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EventPar__3214EC077C818A4F");

            entity.HasIndex(e => e.UserId, "IX_EventParticipants_UserId");
            entity.HasIndex(e => e.EventId, "IX_EventParticipants_EventId");

            entity.HasIndex(e => new { e.EventId, e.UserId }, "UQ__EventPar__A83C44D5EC688677").IsUnique();

            entity.Property(e => e.ParticipatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Event).WithMany(p => p.EventParticipants)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("FK__EventPart__Event__5812160E");

            entity.HasOne(d => d.User).WithMany(p => p.EventParticipants)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EventPart__UserI__59063A47");
        });

        modelBuilder.Entity<FloorWeeklyStat>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__FloorWee__3214EC074E922ABA");

            entity.HasIndex(e => new { e.Year, e.WeekNumber }, "IX_FloorWeeklyStats_WeekYear");

            entity.HasIndex(e => new { e.Floor, e.WeekNumber, e.Year }, "UQ__FloorWee__4F348CC2F4168D60").IsUnique();

            entity.Property(e => e.AverageScore).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.CalculatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<Inspection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Inspecti__3214EC07365A8B48");

            entity.HasIndex(e => e.BlockId, "IX_Inspections_BlockId");
            entity.HasIndex(e => e.InspectionDate, "IX_Inspections_InspectionDate");
            entity.HasIndex(e => e.InspectorId, "IX_Inspections_InspectorId");

            // Score: допустимый диапазон 0–10
            entity.ToTable(t => t.HasCheckConstraint("CK_Inspections_Score", "[Score] >= 0 AND [Score] <= 10"));

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PhotoPath).HasMaxLength(500);

            entity.HasOne(d => d.Block).WithMany(p => p.Inspections)
                .HasForeignKey(d => d.BlockId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inspectio__Block__6EF57B66");

            entity.HasOne(d => d.Inspector).WithMany(p => p.Inspections)
                .HasForeignKey(d => d.InspectorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inspectio__Inspe__71D1E811");

            entity.HasOne(d => d.Room).WithMany(p => p.Inspections)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("FK__Inspectio__RoomI__6FE99F9F");

            entity.HasOne(d => d.Zone).WithMany(p => p.Inspections)
                .HasForeignKey(d => d.ZoneId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inspectio__ZoneI__70DDC3D8");
        });

        modelBuilder.Entity<InspectionZone>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Inspecti__3214EC07C9432B7A");

            entity.HasIndex(e => e.Name, "UQ__Inspecti__737584F69C294ECF").IsUnique();

            entity.Property(e => e.DisplayName).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<RepairComment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RepairCo__3214EC070D1A3C05");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.RepairRequest).WithMany(p => p.RepairComments)
                .HasForeignKey(d => d.RepairRequestId)
                .HasConstraintName("FK__RepairCom__Repai__6754599E");

            entity.HasOne(d => d.User).WithMany(p => p.RepairComments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RepairCom__UserI__68487DD7");
        });

        modelBuilder.Entity<RepairRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RepairRe__3214EC07C27C467B");

            entity.HasIndex(e => e.AssignedToId, "IX_RepairRequests_AssignedToId");
            entity.HasIndex(e => e.Status, "IX_RepairRequests_Status");
            entity.HasIndex(e => e.RequestedById, "IX_RepairRequests_RequestedById");

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_RepairRequests_Status",
                    "[Status] IN ('Pending', 'InProgress', 'Completed', 'Cancelled')");
                t.HasCheckConstraint("CK_RepairRequests_Priority",
                    "[Priority] IN ('Low', 'Normal', 'High')");
            });

            entity.Property(e => e.CompletedAt).HasColumnType("datetime");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Priority)
                .HasMaxLength(10)
                .HasDefaultValue("Normal");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.AssignedTo).WithMany(p => p.RepairRequestAssignedTos)
                .HasForeignKey(d => d.AssignedToId)
                .HasConstraintName("FK__RepairReq__Assig__619B8048");

            entity.HasOne(d => d.Block).WithMany(p => p.RepairRequests)
                .HasForeignKey(d => d.BlockId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RepairReq__Block__5EBF139D");

            entity.HasOne(d => d.RequestedBy).WithMany(p => p.RepairRequestRequestedBies)
                .HasForeignKey(d => d.RequestedById)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RepairReq__Reque__60A75C0F");

            entity.HasOne(d => d.Room).WithMany(p => p.RepairRequests)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("FK__RepairReq__RoomI__5FB337D6");
        });

        modelBuilder.Entity<Residence>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Residenc__3214EC0775B32D31");

            entity.HasIndex(e => e.IsCurrent, "IX_Residences_IsCurrent");

            entity.HasIndex(e => e.RoomId, "IX_Residences_RoomId");

            entity.HasIndex(e => e.UserId, "IX_Residences_UserId");

            entity.Property(e => e.IsCurrent).HasDefaultValue(true);

            entity.HasOne(d => d.Block).WithMany(p => p.Residences)
                .HasForeignKey(d => d.BlockId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Residence__Block__4E88ABD4");

            entity.HasOne(d => d.Room).WithMany(p => p.Residences)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Residence__RoomI__4D94879B");

            entity.HasOne(d => d.User).WithMany(p => p.Residences)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Residence__UserI__4CA06362");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC0754F7CE32");

            entity.HasIndex(e => e.Name, "UQ__Roles__737584F6FC860500").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Rooms__3214EC07A1F7236C");

            entity.Property(e => e.Capacity).HasDefaultValue(2);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.RoomNumber).HasMaxLength(10);

            entity.HasOne(d => d.Block).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.BlockId)
                .HasConstraintName("FK__Rooms__BlockId__47DBAE45");
        });

        modelBuilder.Entity<StudentPoint>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StudentP__3214EC070FAD081C");

            entity.HasIndex(e => e.UserId, "IX_StudentPoints_UserId");
            entity.HasIndex(e => new { e.SourceType, e.SourceId }, "IX_StudentPoints_Source");

            entity.ToTable(t => t.HasCheckConstraint("CK_StudentPoints_PointsType",
                "[PointsType] IN ('Award', 'Penalty')"));

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PointsType).HasMaxLength(20);
            entity.Property(e => e.SourceType).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.StudentPoints)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__StudentPo__UserI__778AC167");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC07C26F86B3");

            entity.HasIndex(e => e.RoleId, "IX_Users_RoleId");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E46ED61669").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534D8FB1843").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__RoleId__3E52440B");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
