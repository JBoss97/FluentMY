namespace FluentMY.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SyncDatabaseSchema : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.StudentCourses", "IsCompleted", c => c.Boolean(nullable: false));
            AddColumn("dbo.StudentCourses", "CompletedAt", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.StudentCourses", "CompletedAt");
            DropColumn("dbo.StudentCourses", "IsCompleted");
        }
    }
}
