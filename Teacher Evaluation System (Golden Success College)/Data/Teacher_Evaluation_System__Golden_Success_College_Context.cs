using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Teacher_Evaluation_System__Golden_Success_College_.Models;

namespace Teacher_Evaluation_System__Golden_Success_College_.Data
{
    public class Teacher_Evaluation_System__Golden_Success_College_Context : DbContext
    {
        public Teacher_Evaluation_System__Golden_Success_College_Context(DbContextOptions<Teacher_Evaluation_System__Golden_Success_College_Context> options)
            : base(options)
        {
        }

        public DbSet<Teacher_Evaluation_System__Golden_Success_College_.Models.Role> Role { get; set; } = default!;
        public DbSet<Teacher_Evaluation_System__Golden_Success_College_.Models.User> User { get; set; } = default!;
        public DbSet<Teacher_Evaluation_System__Golden_Success_College_.Models.Teacher> Teacher { get; set; } = default!;
        public DbSet<Teacher_Evaluation_System__Golden_Success_College_.Models.Student> Student { get; set; } = default!;
        public DbSet<Teacher_Evaluation_System__Golden_Success_College_.Models.Score> Score { get; set; } = default!;
        public DbSet<Teacher_Evaluation_System__Golden_Success_College_.Models.Criteria> Criteria { get; set; } = default!;
        public DbSet<Teacher_Evaluation_System__Golden_Success_College_.Models.Question> Question { get; set; } = default!;
        public DbSet<Teacher_Evaluation_System__Golden_Success_College_.Models.Evaluation> Evaluation { get; set; } = default!;
        public DbSet<Teacher_Evaluation_System__Golden_Success_College_.Models.Subject> Subject { get; set; } = default!;
        public DbSet<Teacher_Evaluation_System__Golden_Success_College_.Models.Section> Section { get; set; } = default!;
        public DbSet<Teacher_Evaluation_System__Golden_Success_College_.Models.Level> Level { get; set; } = default!;
        public DbSet<Teacher_Evaluation_System__Golden_Success_College_.Models.Enrollment> Enrollment { get; set; } = default!;
        public DbSet<Teacher_Evaluation_System__Golden_Success_College_.Models.ActivityLog> ActivityLog { get; set; } = default!;

        public DbSet<EvaluationPeriod> EvaluationPeriod { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================================
            // EVALUATION RELATIONSHIPS - Prevent Multiple Cascade Paths
            // ============================================
            modelBuilder.Entity<Evaluation>()
                .HasOne(e => e.Teacher)
                .WithMany(t => t.Evaluations)
                .HasForeignKey(e => e.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Evaluation>()
                .HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Evaluation>()
                .HasOne(e => e.Subject)
                .WithMany(s => s.Evaluations)
                .HasForeignKey(e => e.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // Evaluation → Scores (CASCADE DELETE)
            modelBuilder.Entity<Evaluation>()
                .HasMany(e => e.Scores)
                .WithOne(s => s.Evaluation)
                .OnDelete(DeleteBehavior.Cascade);

            // ============================================
            // TEACHER & ENROLLMENT RELATIONSHIPS
            // ============================================
            modelBuilder.Entity<Teacher>()
                .HasOne(t => t.Level)
                .WithMany()
                .HasForeignKey(t => t.LevelId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Subject)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // ============================================
            // SUBJECT RELATIONSHIPS
            // ============================================
            modelBuilder.Entity<Subject>()
               .HasOne(s => s.Level)
               .WithMany(l => l.Subjects)
               .HasForeignKey(s => s.LevelId)
               .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Subject>()
               .HasOne(s => s.Section)
               .WithMany(sec => sec.Subjects)
               .HasForeignKey(s => s.SectionId)
               .OnDelete(DeleteBehavior.Restrict);

            // ============================================
            // ACTIVITY LOG RELATIONSHIPS - NO ACTION ON DELETE
            // SQL Server doesn't allow multiple cascade paths, so we use NoAction
            // We'll manually handle ActivityLog deletion when needed
            // ============================================

            // ActivityLog → Evaluation (NO ACTION when Evaluation is deleted)
            modelBuilder.Entity<ActivityLog>()
                .HasOne(a => a.Evaluation)
                .WithMany()
                .HasForeignKey(a => a.EvaluationId)
                .OnDelete(DeleteBehavior.NoAction);

            // ActivityLog → Student (NO ACTION when Student is deleted)
            modelBuilder.Entity<ActivityLog>()
                .HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            // ActivityLog → User (NO ACTION when User is deleted)
            modelBuilder.Entity<ActivityLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // ActivityLog → Teacher (NO ACTION when Teacher is deleted)
            modelBuilder.Entity<ActivityLog>()
                .HasOne(a => a.Teacher)
                .WithMany()
                .HasForeignKey(a => a.TeacherId)
                .OnDelete(DeleteBehavior.NoAction);

            // ActivityLog → Subject (NO ACTION when Subject is deleted)
            modelBuilder.Entity<ActivityLog>()
                .HasOne(a => a.Subject)
                .WithMany()
                .HasForeignKey(a => a.SubjectId)
                .OnDelete(DeleteBehavior.NoAction);

            // ============================================
            // INDEXES FOR PERFORMANCE
            // ============================================

            // ActivityLog indexes for faster queries
            modelBuilder.Entity<ActivityLog>()
                .HasIndex(a => a.Timestamp);

            modelBuilder.Entity<ActivityLog>()
                .HasIndex(a => a.ActivityType);

            modelBuilder.Entity<ActivityLog>()
                .HasIndex(a => a.StudentId);

            modelBuilder.Entity<ActivityLog>()
                .HasIndex(a => a.UserId);

            modelBuilder.Entity<ActivityLog>()
                .HasIndex(a => a.EvaluationId);


            // Configure EvaluationPeriod
            modelBuilder.Entity<EvaluationPeriod>(entity =>
            {
                entity.HasIndex(e => e.IsCurrent);
                entity.HasIndex(e => new { e.AcademicYear, e.Semester });

                // Ensure only one current period at a time (handled in service/controller)
            });
        }
    }
}