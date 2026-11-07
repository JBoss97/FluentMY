namespace FluentMY.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddEducatorIdToCourse : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Courses", "EducatorId", c => c.String(maxLength: 128));
            AlterColumn("dbo.Courses", "Language", c => c.String(nullable: false, maxLength: 50));
            AlterColumn("dbo.Courses", "EstimatedDuration", c => c.String(maxLength: 50));
            CreateIndex("dbo.Courses", "EducatorId");
            AddForeignKey("dbo.Courses", "EducatorId", "dbo.AspNetUsers", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Courses", "EducatorId", "dbo.AspNetUsers");
            DropIndex("dbo.Courses", new[] { "EducatorId" });
            AlterColumn("dbo.Courses", "EstimatedDuration", c => c.String(maxLength: 20));
            AlterColumn("dbo.Courses", "Language", c => c.String(nullable: false, maxLength: 40));
            DropColumn("dbo.Courses", "EducatorId");
        }
    }
}
