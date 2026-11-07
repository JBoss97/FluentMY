namespace FluentMY.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddCourseTimestamps : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Courses", "CreatedAt", c => c.DateTime(nullable: false));
            AddColumn("dbo.Courses", "UpdatedAt", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Courses", "UpdatedAt");
            DropColumn("dbo.Courses", "CreatedAt");
        }
    }
}
