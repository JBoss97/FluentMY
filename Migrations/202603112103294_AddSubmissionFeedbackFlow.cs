namespace FluentMY.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSubmissionFeedbackFlow : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Feedbacks",
                c => new
                    {
                        FeedbackId = c.Int(nullable: false, identity: true),
                        SubmissionId = c.Int(nullable: false),
                        GivenByUserId = c.String(maxLength: 128),
                        FeedbackType = c.String(maxLength: 20),
                        FeedbackText = c.String(nullable: false, maxLength: 1000),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.FeedbackId)
                .ForeignKey("dbo.AspNetUsers", t => t.GivenByUserId)
                .ForeignKey("dbo.Submissions", t => t.SubmissionId, cascadeDelete: false)
                .Index(t => t.SubmissionId)
                .Index(t => t.GivenByUserId);
            
            CreateTable(
                "dbo.Submissions",
                c => new
                    {
                        SubmissionId = c.Int(nullable: false, identity: true),
                        StudentId = c.String(nullable: false, maxLength: 128),
                        LessonId = c.Int(nullable: false),
                        AttemptNumber = c.Int(nullable: false),
                        TotalQuestions = c.Int(nullable: false),
                        CorrectAnswers = c.Int(nullable: false),
                        TotalScore = c.Int(nullable: false),
                        Percentage = c.Int(nullable: false),
                        IsReviewed = c.Boolean(nullable: false),
                        ReviewedAt = c.DateTime(),
                        SubmittedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.SubmissionId)
                .ForeignKey("dbo.Lessons", t => t.LessonId, cascadeDelete: false)
                .ForeignKey("dbo.AspNetUsers", t => t.StudentId, cascadeDelete: false)
                .Index(t => t.StudentId)
                .Index(t => t.LessonId);
            
            CreateTable(
                "dbo.SubmissionAnswers",
                c => new
                    {
                        SubmissionAnswerId = c.Int(nullable: false, identity: true),
                        SubmissionId = c.Int(nullable: false),
                        ExerciseId = c.Int(nullable: false),
                        QuestionTextSnapshot = c.String(maxLength: 500),
                        SelectedAnswer = c.String(maxLength: 1),
                        CorrectAnswerSnapshot = c.String(maxLength: 1),
                        IsCorrect = c.Boolean(nullable: false),
                        Score = c.Int(nullable: false),
                        SubmittedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.SubmissionAnswerId)
                .ForeignKey("dbo.Exercises", t => t.ExerciseId, cascadeDelete: false)
                .ForeignKey("dbo.Submissions", t => t.SubmissionId, cascadeDelete: false)
                .Index(t => t.SubmissionId)
                .Index(t => t.ExerciseId);
            
            AddColumn("dbo.StudentLessonProgresses", "AttemptsCount", c => c.Int(nullable: false));
            AddColumn("dbo.StudentLessonProgresses", "LastScorePercent", c => c.Int());
            AddColumn("dbo.StudentLessonProgresses", "BestScorePercent", c => c.Int());
            AddColumn("dbo.StudentLessonProgresses", "LastAttemptAt", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Submissions", "StudentId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Submissions", "LessonId", "dbo.Lessons");
            DropForeignKey("dbo.Feedbacks", "SubmissionId", "dbo.Submissions");
            DropForeignKey("dbo.SubmissionAnswers", "SubmissionId", "dbo.Submissions");
            DropForeignKey("dbo.SubmissionAnswers", "ExerciseId", "dbo.Exercises");
            DropForeignKey("dbo.Feedbacks", "GivenByUserId", "dbo.AspNetUsers");
            DropIndex("dbo.SubmissionAnswers", new[] { "ExerciseId" });
            DropIndex("dbo.SubmissionAnswers", new[] { "SubmissionId" });
            DropIndex("dbo.Submissions", new[] { "LessonId" });
            DropIndex("dbo.Submissions", new[] { "StudentId" });
            DropIndex("dbo.Feedbacks", new[] { "GivenByUserId" });
            DropIndex("dbo.Feedbacks", new[] { "SubmissionId" });
            DropColumn("dbo.StudentLessonProgresses", "LastAttemptAt");
            DropColumn("dbo.StudentLessonProgresses", "BestScorePercent");
            DropColumn("dbo.StudentLessonProgresses", "LastScorePercent");
            DropColumn("dbo.StudentLessonProgresses", "AttemptsCount");
            DropTable("dbo.SubmissionAnswers");
            DropTable("dbo.Submissions");
            DropTable("dbo.Feedbacks");
        }
    }
}
