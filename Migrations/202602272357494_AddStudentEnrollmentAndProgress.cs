namespace FluentMY.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddStudentEnrollmentAndProgress : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.StudentCourses",
                c => new
                    {
                        StudentCourseId = c.Int(nullable: false, identity: true),
                        StudentId = c.String(nullable: false, maxLength: 128),
                        CourseId = c.Int(nullable: false),
                        EnrolledAt = c.DateTime(nullable: false),
                        CurrentLessonId = c.Int(),
                    })
                .PrimaryKey(t => t.StudentCourseId)
                .ForeignKey("dbo.Courses", t => t.CourseId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.StudentId, cascadeDelete: true)
                .Index(t => t.StudentId)
                .Index(t => t.CourseId);
            
            CreateTable(
                "dbo.StudentLessonProgresses",
                c => new
                    {
                        StudentLessonProgressId = c.Int(nullable: false, identity: true),
                        StudentId = c.String(nullable: false, maxLength: 128),
                        LessonId = c.Int(nullable: false),
                        IsCompleted = c.Boolean(nullable: false),
                        CompletedAt = c.DateTime(),
                    })
                .PrimaryKey(t => t.StudentLessonProgressId)
                .ForeignKey("dbo.Lessons", t => t.LessonId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.StudentId, cascadeDelete: true)
                .Index(t => t.StudentId)
                .Index(t => t.LessonId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.StudentLessonProgresses", "StudentId", "dbo.AspNetUsers");
            DropForeignKey("dbo.StudentLessonProgresses", "LessonId", "dbo.Lessons");
            DropForeignKey("dbo.StudentCourses", "StudentId", "dbo.AspNetUsers");
            DropForeignKey("dbo.StudentCourses", "CourseId", "dbo.Courses");
            DropIndex("dbo.StudentLessonProgresses", new[] { "LessonId" });
            DropIndex("dbo.StudentLessonProgresses", new[] { "StudentId" });
            DropIndex("dbo.StudentCourses", new[] { "CourseId" });
            DropIndex("dbo.StudentCourses", new[] { "StudentId" });
            DropTable("dbo.StudentLessonProgresses");
            DropTable("dbo.StudentCourses");
        }
    }
}
