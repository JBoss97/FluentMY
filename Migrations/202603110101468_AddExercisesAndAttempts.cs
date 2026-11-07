namespace FluentMY.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddExercisesAndAttempts : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Exercises",
                c => new
                    {
                        ExerciseId = c.Int(nullable: false, identity: true),
                        LessonId = c.Int(nullable: false),
                        QuestionText = c.String(nullable: false, maxLength: 500),
                        OptionA = c.String(nullable: false, maxLength: 200),
                        OptionB = c.String(nullable: false, maxLength: 200),
                        OptionC = c.String(nullable: false, maxLength: 200),
                        OptionD = c.String(nullable: false, maxLength: 200),
                        CorrectAnswer = c.String(nullable: false, maxLength: 1),
                        Marks = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ExerciseId)
                .ForeignKey("dbo.Lessons", t => t.LessonId, cascadeDelete: true)
                .Index(t => t.LessonId);
            
            CreateTable(
                "dbo.StudentExerciseAttempts",
                c => new
                    {
                        StudentExerciseAttemptId = c.Int(nullable: false, identity: true),
                        StudentId = c.String(nullable: false),
                        ExerciseId = c.Int(nullable: false),
                        SelectedAnswer = c.String(maxLength: 1),
                        IsCorrect = c.Boolean(nullable: false),
                        Score = c.Int(nullable: false),
                        SubmittedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.StudentExerciseAttemptId)
                .ForeignKey("dbo.Exercises", t => t.ExerciseId, cascadeDelete: true)
                .Index(t => t.ExerciseId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.StudentExerciseAttempts", "ExerciseId", "dbo.Exercises");
            DropForeignKey("dbo.Exercises", "LessonId", "dbo.Lessons");
            DropIndex("dbo.StudentExerciseAttempts", new[] { "ExerciseId" });
            DropIndex("dbo.Exercises", new[] { "LessonId" });
            DropTable("dbo.StudentExerciseAttempts");
            DropTable("dbo.Exercises");
        }
    }
}
