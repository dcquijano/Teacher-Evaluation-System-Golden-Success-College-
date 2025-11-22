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
        public Teacher_Evaluation_System__Golden_Success_College_Context (DbContextOptions<Teacher_Evaluation_System__Golden_Success_College_Context> options)
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 🔵 FIX MULTIPLE CASCADE PATH ERROR
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

            // Fix cascade issue: Teacher → Level
            modelBuilder.Entity<Teacher>()
                .HasOne(t => t.Level)
                .WithMany()
                .HasForeignKey(t => t.LevelId)
                .OnDelete(DeleteBehavior.Restrict); // or DeleteBehavior.NoAction

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


           // Existing relationships
            modelBuilder.Entity<Evaluation>()
                .HasMany(e => e.Scores)
                .WithOne(s => s.Evaluation)
                .OnDelete(DeleteBehavior.Cascade);

            // Add indexes for ActivityLog for better performance
            modelBuilder.Entity<ActivityLog>()
                .HasIndex(a => a.Timestamp);

            modelBuilder.Entity<ActivityLog>()
                .HasIndex(a => a.ActivityType);

            modelBuilder.Entity<ActivityLog>()
                .HasIndex(a => a.StudentId);

            modelBuilder.Entity<ActivityLog>()
                .HasIndex(a => a.UserId);

        }


    }
}
