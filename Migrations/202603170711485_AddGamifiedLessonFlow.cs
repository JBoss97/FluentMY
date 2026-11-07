namespace FluentMY.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddGamifiedLessonFlow : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.LessonSessionAnswers",
                c => new
                    {
                        LessonSessionAnswerId = c.Int(nullable: false, identity: true),
                        LessonSessionId = c.Int(nullable: false),
                        ExerciseId = c.Int(nullable: false),
                        SubmittedAnswer = c.String(),
                        SubmittedAnswerJson = c.String(),
                        IsCorrect = c.Boolean(nullable: false),
                        XPGranted = c.Int(nullable: false),
                        CreatedAtUtc = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.LessonSessionAnswerId)
                .ForeignKey("dbo.Exercises", t => t.ExerciseId)
                .ForeignKey("dbo.LessonSessions", t => t.LessonSessionId, cascadeDelete: true)
                .Index(t => t.LessonSessionId)
                .Index(t => t.ExerciseId);
            
            CreateTable(
                "dbo.LessonSessions",
                c => new
                    {
                        LessonSessionId = c.Int(nullable: false, identity: true),
                        LessonId = c.Int(nullable: false),
                        StudentId = c.String(nullable: false, maxLength: 128),
                        CurrentIndex = c.Int(nullable: false),
                        HeartsRemaining = c.Int(nullable: false),
                        CurrentStreak = c.Int(nullable: false),
                        TotalXP = c.Int(nullable: false),
                        CorrectCount = c.Int(nullable: false),
                        WrongCount = c.Int(nullable: false),
                        IsCompleted = c.Boolean(nullable: false),
                        RewardsApplied = c.Boolean(nullable: false),
                        StartedAtUtc = c.DateTime(nullable: false),
                        CompletedAtUtc = c.DateTime(),
                    })
                .PrimaryKey(t => t.LessonSessionId)
                .ForeignKey("dbo.Lessons", t => t.LessonId)
                .ForeignKey("dbo.AspNetUsers", t => t.StudentId)
                .Index(t => t.LessonId)
                .Index(t => t.StudentId);
            
            CreateTable(
                "dbo.SpeakingAttempts",
                c => new
                    {
                        SpeakingAttemptId = c.Int(nullable: false, identity: true),
                        ExerciseId = c.Int(nullable: false),
                        LessonId = c.Int(nullable: false),
                        StudentId = c.String(nullable: false, maxLength: 128),
                        AttemptNumber = c.Int(nullable: false),
                        AudioFilePath = c.String(maxLength: 255),
                        RecognizedText = c.String(),
                        ReferenceText = c.String(),
                        AccuracyScore = c.Decimal(precision: 18, scale: 2),
                        FluencyScore = c.Decimal(precision: 18, scale: 2),
                        CompletenessScore = c.Decimal(precision: 18, scale: 2),
                        PronunciationScore = c.Decimal(precision: 18, scale: 2),
                        FeedbackJson = c.String(),
                        CreatedAtUtc = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.SpeakingAttemptId)
                .ForeignKey("dbo.Exercises", t => t.ExerciseId)
                .ForeignKey("dbo.Lessons", t => t.LessonId)
                .ForeignKey("dbo.AspNetUsers", t => t.StudentId)
                .Index(t => t.ExerciseId)
                .Index(t => t.LessonId)
                .Index(t => t.StudentId);
            
            CreateTable(
                "dbo.StudentGameProfiles",
                c => new
                    {
                        StudentGameProfileId = c.Int(nullable: false, identity: true),
                        StudentId = c.String(nullable: false, maxLength: 128),
                        TotalXP = c.Int(nullable: false),
                        CurrentStreakDays = c.Int(nullable: false),
                        LongestStreakDays = c.Int(nullable: false),
                        Hearts = c.Int(nullable: false),
                        LastPracticeDateUtc = c.DateTime(),
                    })
                .PrimaryKey(t => t.StudentGameProfileId)
                .ForeignKey("dbo.AspNetUsers", t => t.StudentId)
                .Index(t => t.StudentId);
            
            AddColumn("dbo.Exercises", "PracticeArea", c => c.String(maxLength: 30));
            AddColumn("dbo.Exercises", "ExerciseType", c => c.String(maxLength: 30));
            AddColumn("dbo.Exercises", "PromptText", c => c.String());
            AddColumn("dbo.Exercises", "MatchingPairsJson", c => c.String());
            AddColumn("dbo.Exercises", "SentenceTokensJson", c => c.String());
            AddColumn("dbo.Exercises", "AudioPromptPath", c => c.String(maxLength: 255));
            AddColumn("dbo.Exercises", "ReferenceText", c => c.String());
            AddColumn("dbo.Exercises", "SyllableGuide", c => c.String());
            AddColumn("dbo.Exercises", "PronunciationHint", c => c.String());
            AddColumn("dbo.Exercises", "DisplayOrder", c => c.Int(nullable: false));
            AddColumn("dbo.Exercises", "XPReward", c => c.Int(nullable: false));
            AddColumn("dbo.Exercises", "AllowRetry", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.StudentGameProfiles", "StudentId", "dbo.AspNetUsers");
            DropForeignKey("dbo.SpeakingAttempts", "StudentId", "dbo.AspNetUsers");
            DropForeignKey("dbo.SpeakingAttempts", "LessonId", "dbo.Lessons");
            DropForeignKey("dbo.SpeakingAttempts", "ExerciseId", "dbo.Exercises");
            DropForeignKey("dbo.LessonSessionAnswers", "LessonSessionId", "dbo.LessonSessions");
            DropForeignKey("dbo.LessonSessions", "StudentId", "dbo.AspNetUsers");
            DropForeignKey("dbo.LessonSessions", "LessonId", "dbo.Lessons");
            DropForeignKey("dbo.LessonSessionAnswers", "ExerciseId", "dbo.Exercises");
            DropIndex("dbo.StudentGameProfiles", new[] { "StudentId" });
            DropIndex("dbo.SpeakingAttempts", new[] { "StudentId" });
            DropIndex("dbo.SpeakingAttempts", new[] { "LessonId" });
            DropIndex("dbo.SpeakingAttempts", new[] { "ExerciseId" });
            DropIndex("dbo.LessonSessions", new[] { "StudentId" });
            DropIndex("dbo.LessonSessions", new[] { "LessonId" });
            DropIndex("dbo.LessonSessionAnswers", new[] { "ExerciseId" });
            DropIndex("dbo.LessonSessionAnswers", new[] { "LessonSessionId" });
            DropColumn("dbo.Exercises", "AllowRetry");
            DropColumn("dbo.Exercises", "XPReward");
            DropColumn("dbo.Exercises", "DisplayOrder");
            DropColumn("dbo.Exercises", "PronunciationHint");
            DropColumn("dbo.Exercises", "SyllableGuide");
            DropColumn("dbo.Exercises", "ReferenceText");
            DropColumn("dbo.Exercises", "AudioPromptPath");
            DropColumn("dbo.Exercises", "SentenceTokensJson");
            DropColumn("dbo.Exercises", "MatchingPairsJson");
            DropColumn("dbo.Exercises", "PromptText");
            DropColumn("dbo.Exercises", "ExerciseType");
            DropColumn("dbo.Exercises", "PracticeArea");
            DropTable("dbo.StudentGameProfiles");
            DropTable("dbo.SpeakingAttempts");
            DropTable("dbo.LessonSessions");
            DropTable("dbo.LessonSessionAnswers");
        }
    }
}
