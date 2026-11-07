namespace FluentMY.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLessonAndExerciseTimestamps : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Lessons", "CreatedAt", c => c.DateTime(nullable: false));
            AddColumn("dbo.Lessons", "UpdatedAt", c => c.DateTime());
            AddColumn("dbo.Exercises", "CreatedAt", c => c.DateTime(nullable: false));
            AddColumn("dbo.Exercises", "UpdatedAt", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Exercises", "UpdatedAt");
            DropColumn("dbo.Exercises", "CreatedAt");
            DropColumn("dbo.Lessons", "UpdatedAt");
            DropColumn("dbo.Lessons", "CreatedAt");
        }
    }
}
