namespace FluentMY.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddEducatorIdToLesson : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Lessons", "EducatorId", c => c.String(nullable: false, maxLength: 128));
            CreateIndex("dbo.Lessons", "EducatorId");
            AddForeignKey("dbo.Lessons", "EducatorId", "dbo.AspNetUsers", "Id", cascadeDelete: false);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Lessons", "EducatorId", "dbo.AspNetUsers");
            DropIndex("dbo.Lessons", new[] { "EducatorId" });
            DropColumn("dbo.Lessons", "EducatorId");
        }
    }
}
