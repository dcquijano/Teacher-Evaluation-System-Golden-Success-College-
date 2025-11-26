using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Teacher_Evaluation_System__Golden_Success_College_.Helper;
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


            // ============================================
            // SEED ROLES
            // ============================================

            modelBuilder.Entity<Role>().HasData(
                  new Role { RoleId = 1, Name = "Student" },
                  new Role { RoleId = 2, Name = "Admin" },
                  new Role { RoleId = 3, Name = "Super Admin" }
              );

            // ============================================
            // SEED DEFAULT USERS
            // ============================================

            // Hash passwords for default accounts
            string superAdminPassword = PasswordHelper.HashPassword("superadmin"); // you can change default password
            string adminPassword = PasswordHelper.HashPassword("admin");           // you can change default password

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    FullName = "Default Super Admin",
                    Email = "ninojusay1@gmail.com",
                    Password = superAdminPassword,
                    RoleId = 3
                },
                new User
                {
                    UserId = 2,
                    FullName = "Default Admin",
                    Email = "admin@example.com",
                    Password = adminPassword,
                    RoleId = 2
                }
            );

            // ============================================
            // SEED DEFAULT CRITERIA
            // ============================================
            modelBuilder.Entity<Criteria>().HasData(
                new Criteria
                {
                    CriteriaId = 1,
                    Name = "A. TEACHER ACTIONS (Teaching Practices)"
                },
                new Criteria
                {
                    CriteriaId = 2,
                    Name = "B. TEACHER-STUDENT INTERACTION"
                }
            );


            // ============================================
            // SEED QUESTIONS
            // ============================================
            modelBuilder.Entity<Question>().HasData(
                // Criteria 1: TEACHER ACTIONS
                new Question { QuestionId = 1, CriteriaId = 1, Description = "My teacher clearly explains the lesson and learning goals." },
                new Question { QuestionId = 2, CriteriaId = 1, Description = "My teacher uses examples, activities, and materials that make the lesson easier to understand." },
                new Question { QuestionId = 3, CriteriaId = 1, Description = "My teacher checks if students understand the lesson (through questions, activities, or assessments)." },
                new Question { QuestionId = 4, CriteriaId = 1, Description = "My teacher gives feedback, advice, or help when students find it difficult to learn." },
                new Question { QuestionId = 5, CriteriaId = 1, Description = "My teacher manages the class well (time, rules, order) so learning is not disturbed." },
                new Question { QuestionId = 6, CriteriaId = 1, Description = "My teacher asks questions that make students think more deeply or critically." },

                // Criteria 2: TEACHER-STUDENT INTERACTION
                new Question { QuestionId = 7, CriteriaId = 2, Description = "My teacher encourages us to be active and engaged in learning tasks." },
                new Question { QuestionId = 8, CriteriaId = 2, Description = "My teacher guides us in using different learning materials and technology to achieve our goals." },
                new Question { QuestionId = 9, CriteriaId = 2, Description = "My teacher motivates us to share ideas, reflections, or solutions to real-life challenges." },
                new Question { QuestionId = 10, CriteriaId = 2, Description = "My teacher promotes collaboration and meaningful interactions among students." },
                new Question { QuestionId = 11, CriteriaId = 2, Description = "My teacher helps us explain and understand how our work relates to learning goals." },
                new Question { QuestionId = 12, CriteriaId = 2, Description = "My teacher encourages us to ask questions to clarify or deepen our understanding." },
                new Question { QuestionId = 13, CriteriaId = 2, Description = "My teacher helps us connect our lessons to daily life and real-world situations." },
                new Question { QuestionId = 14, CriteriaId = 2, Description = "My teacher integrates 21st century skills (communication, collaboration, critical thinking, creativity) into lessons." },
                new Question { QuestionId = 15, CriteriaId = 2, Description = "My teacher connects our learning with the school’s PVMGO (Philosophy, Vision, Mission, Goals, and Objectives)." }
            );
        }
    }
}