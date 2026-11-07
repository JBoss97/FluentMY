namespace FluentMY.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateExerciseForGamifiedTypes : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Exercises", "QuestionText", c => c.String(maxLength: 500));
            AlterColumn("dbo.Exercises", "OptionA", c => c.String(maxLength: 255));
            AlterColumn("dbo.Exercises", "OptionB", c => c.String(maxLength: 255));
            AlterColumn("dbo.Exercises", "OptionC", c => c.String(maxLength: 255));
            AlterColumn("dbo.Exercises", "OptionD", c => c.String(maxLength: 255));
            AlterColumn("dbo.Exercises", "CorrectAnswer", c => c.String(maxLength: 500));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Exercises", "CorrectAnswer", c => c.String(nullable: false, maxLength: 1));
            AlterColumn("dbo.Exercises", "OptionD", c => c.String(nullable: false, maxLength: 200));
            AlterColumn("dbo.Exercises", "OptionC", c => c.String(nullable: false, maxLength: 200));
            AlterColumn("dbo.Exercises", "OptionB", c => c.String(nullable: false, maxLength: 200));
            AlterColumn("dbo.Exercises", "OptionA", c => c.String(nullable: false, maxLength: 200));
            AlterColumn("dbo.Exercises", "QuestionText", c => c.String(nullable: false, maxLength: 500));
        }
    }
}
