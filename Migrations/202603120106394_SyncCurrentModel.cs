namespace FluentMY.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SyncCurrentModel : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.StudentExerciseAttempts", "ExerciseId", "dbo.Exercises");
            DropForeignKey("dbo.StudentExerciseAttempts", "StudentId", "dbo.AspNetUsers");
            DropForeignKey("dbo.StudentQuizAttempts", "LessonId", "dbo.Lessons");
            DropForeignKey("dbo.StudentQuizAttempts", "StudentId", "dbo.AspNetUsers");
            DropForeignKey("dbo.StudentExerciseAttempts", "StudentQuizAttemptId", "dbo.StudentQuizAttempts");
            DropForeignKey("dbo.Submissions", "LessonId", "dbo.Lessons");
            DropForeignKey("dbo.Submissions", "StudentId", "dbo.AspNetUsers");
            DropForeignKey("dbo.SubmissionAnswers", "ExerciseId", "dbo.Exercises");
            DropIndex("dbo.StudentExerciseAttempts", new[] { "StudentId" });
            DropIndex("dbo.StudentExerciseAttempts", new[] { "ExerciseId" });
            DropIndex("dbo.StudentExerciseAttempts", new[] { "StudentQuizAttemptId" });
            DropIndex("dbo.StudentQuizAttempts", new[] { "StudentId" });
            DropIndex("dbo.StudentQuizAttempts", new[] { "LessonId" });
            RenameColumn(table: "dbo.Feedbacks", name: "GivenByUserId", newName: "EducatorId");
            RenameIndex(table: "dbo.Feedbacks", name: "IX_GivenByUserId", newName: "IX_EducatorId");
            CreateTable(
                "dbo.Progresses",
                c => new
                    {
                        ProgressId = c.Int(nullable: false, identity: true),
                        StudentId = c.String(nullable: false, maxLength: 128),
                        CourseId = c.Int(nullable: false),
                        TotalLessons = c.Int(nullable: false),
                        CompletedLessons = c.Int(nullable: false),
                        CompletionRate = c.Int(nullable: false),
                        LastLessonId = c.Int(),
                        IsCourseCompleted = c.Boolean(nullable: false),
                        LastUpdated = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ProgressId)
                .ForeignKey("dbo.Courses", t => t.CourseId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.StudentId, cascadeDelete: true)
                .Index(t => t.StudentId)
                .Index(t => t.CourseId);
            
            AddColumn("dbo.Feedbacks", "CommentText", c => c.String(nullable: false, maxLength: 1000));
            AddForeignKey("dbo.Submissions", "LessonId", "dbo.Lessons", "LessonId");
            AddForeignKey("dbo.Submissions", "StudentId", "dbo.AspNetUsers", "Id");
            AddForeignKey("dbo.SubmissionAnswers", "ExerciseId", "dbo.Exercises", "ExerciseId");
            DropColumn("dbo.Feedbacks", "FeedbackText");
            DropTable("dbo.StudentExerciseAttempts");
            DropTable("dbo.StudentQuizAttempts");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.StudentQuizAttempts",
                c => new
                    {
                        StudentQuizAttemptId = c.Int(nullable: false, identity: true),
                        StudentId = c.String(nullable: false, maxLength: 128),
                        LessonId = c.Int(nullable: false),
                        AttemptNumber = c.Int(nullable: false),
                        TotalQuestions = c.Int(nullable: false),
                        CorrectAnswers = c.Int(nullable: false),
                        TotalScore = c.Int(nullable: false),
                        Percentage = c.Int(nullable: false),
                        SubmittedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.StudentQuizAttemptId);
            
            CreateTable(
                "dbo.StudentExerciseAttempts",
                c => new
                    {
                        StudentExerciseAttemptId = c.Int(nullable: false, identity: true),
                        StudentId = c.String(nullable: false, maxLength: 128),
                        ExerciseId = c.Int(nullable: false),
                        StudentQuizAttemptId = c.Int(nullable: false),
                        QuestionTextSnapshot = c.String(maxLength: 500),
                        SelectedAnswer = c.String(maxLength: 1),
                        CorrectAnswerSnapshot = c.String(maxLength: 1),
                        IsCorrect = c.Boolean(nullable: false),
                        Score = c.Int(nullable: false),
                        SubmittedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.StudentExerciseAttemptId);
            
            AddColumn("dbo.Feedbacks", "FeedbackText", c => c.String(nullable: false, maxLength: 1000));
            DropForeignKey("dbo.SubmissionAnswers", "ExerciseId", "dbo.Exercises");
            DropForeignKey("dbo.Submissions", "StudentId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Submissions", "LessonId", "dbo.Lessons");
            DropForeignKey("dbo.Progresses", "StudentId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Progresses", "CourseId", "dbo.Courses");
            DropIndex("dbo.Progresses", new[] { "CourseId" });
            DropIndex("dbo.Progresses", new[] { "StudentId" });
            DropColumn("dbo.Feedbacks", "CommentText");
            DropTable("dbo.Progresses");
            RenameIndex(table: "dbo.Feedbacks", name: "IX_EducatorId", newName: "IX_GivenByUserId");
            RenameColumn(table: "dbo.Feedbacks", name: "EducatorId", newName: "GivenByUserId");
            CreateIndex("dbo.StudentQuizAttempts", "LessonId");
            CreateIndex("dbo.StudentQuizAttempts", "StudentId");
            CreateIndex("dbo.StudentExerciseAttempts", "StudentQuizAttemptId");
            CreateIndex("dbo.StudentExerciseAttempts", "ExerciseId");
            CreateIndex("dbo.StudentExerciseAttempts", "StudentId");
            AddForeignKey("dbo.SubmissionAnswers", "ExerciseId", "dbo.Exercises", "ExerciseId", cascadeDelete: true);
            AddForeignKey("dbo.Submissions", "StudentId", "dbo.AspNetUsers", "Id", cascadeDelete: true);
            AddForeignKey("dbo.Submissions", "LessonId", "dbo.Lessons", "LessonId", cascadeDelete: true);
            AddForeignKey("dbo.StudentExerciseAttempts", "StudentQuizAttemptId", "dbo.StudentQuizAttempts", "StudentQuizAttemptId");
            AddForeignKey("dbo.StudentQuizAttempts", "StudentId", "dbo.AspNetUsers", "Id");
            AddForeignKey("dbo.StudentQuizAttempts", "LessonId", "dbo.Lessons", "LessonId");
            AddForeignKey("dbo.StudentExerciseAttempts", "StudentId", "dbo.AspNetUsers", "Id");
            AddForeignKey("dbo.StudentExerciseAttempts", "ExerciseId", "dbo.Exercises", "ExerciseId");
        }
    }
}
