namespace FluentMY.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddCoursesAndLessons : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Courses",
                c => new
                    {
                        CourseId = c.Int(nullable: false, identity: true),
                        Title = c.String(nullable: false, maxLength: 120),
                        Language = c.String(nullable: false, maxLength: 40),
                        Level = c.String(maxLength: 20),
                        Description = c.String(maxLength: 500),
                        EstimatedDuration = c.String(maxLength: 20),
                    })
                .PrimaryKey(t => t.CourseId);
            
            CreateTable(
                "dbo.Lessons",
                c => new
                    {
                        LessonId = c.Int(nullable: false, identity: true),
                        Title = c.String(nullable: false, maxLength: 120),
                        DifficultyLevel = c.String(maxLength: 20),
                        Content = c.String(),
                        CourseId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.LessonId)
                .ForeignKey("dbo.Courses", t => t.CourseId, cascadeDelete: true)
                .Index(t => t.CourseId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Lessons", "CourseId", "dbo.Courses");
            DropIndex("dbo.Lessons", new[] { "CourseId" });
            DropTable("dbo.Lessons");
            DropTable("dbo.Courses");
        }
    }
}
