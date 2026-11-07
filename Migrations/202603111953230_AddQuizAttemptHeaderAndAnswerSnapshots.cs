namespace FluentMY.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddQuizAttemptHeaderAndAnswerSnapshots : DbMigration
    {
        public override void Up()
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
                .PrimaryKey(t => t.StudentQuizAttemptId)
                .ForeignKey("dbo.Lessons", t => t.LessonId, cascadeDelete: false)
                .ForeignKey("dbo.AspNetUsers", t => t.StudentId, cascadeDelete: false)
                .Index(t => t.StudentId)
                .Index(t => t.LessonId);
            
            AddColumn("dbo.StudentExerciseAttempts", "StudentQuizAttemptId", c => c.Int(nullable: false));
            AddColumn("dbo.StudentExerciseAttempts", "QuestionTextSnapshot", c => c.String(maxLength: 500));
            AddColumn("dbo.StudentExerciseAttempts", "CorrectAnswerSnapshot", c => c.String(maxLength: 1));
            CreateIndex("dbo.StudentExerciseAttempts", "StudentQuizAttemptId");
            AddForeignKey("dbo.StudentExerciseAttempts", "StudentQuizAttemptId", "dbo.StudentQuizAttempts", "StudentQuizAttemptId", cascadeDelete: false);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.StudentQuizAttempts", "StudentId", "dbo.AspNetUsers");
            DropForeignKey("dbo.StudentQuizAttempts", "LessonId", "dbo.Lessons");
            DropForeignKey("dbo.StudentExerciseAttempts", "StudentQuizAttemptId", "dbo.StudentQuizAttempts");
            DropIndex("dbo.StudentQuizAttempts", new[] { "LessonId" });
            DropIndex("dbo.StudentQuizAttempts", new[] { "StudentId" });
            DropIndex("dbo.StudentExerciseAttempts", new[] { "StudentQuizAttemptId" });
            DropColumn("dbo.StudentExerciseAttempts", "CorrectAnswerSnapshot");
            DropColumn("dbo.StudentExerciseAttempts", "QuestionTextSnapshot");
            DropColumn("dbo.StudentExerciseAttempts", "StudentQuizAttemptId");
            DropTable("dbo.StudentQuizAttempts");
        }
    }
}
