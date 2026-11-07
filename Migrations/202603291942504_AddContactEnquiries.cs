namespace FluentMY.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddContactEnquiries : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ContactEnquiries",
                c => new
                    {
                        ContactEnquiryId = c.Int(nullable: false, identity: true),
                        FullName = c.String(nullable: false, maxLength: 100),
                        Email = c.String(nullable: false, maxLength: 150),
                        Subject = c.String(nullable: false, maxLength: 150),
                        Role = c.String(nullable: false, maxLength: 30),
                        Message = c.String(nullable: false, maxLength: 2000),
                        SubmittedAtUtc = c.DateTime(nullable: false),
                        IsRead = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ContactEnquiryId);
            
            AddColumn("dbo.LessonSessionAnswers", "LessonSession_LessonSessionId", c => c.Int());
            CreateIndex("dbo.LessonSessionAnswers", "LessonSession_LessonSessionId");
            AddForeignKey("dbo.LessonSessionAnswers", "LessonSession_LessonSessionId", "dbo.LessonSessions", "LessonSessionId");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.LessonSessionAnswers", "LessonSession_LessonSessionId", "dbo.LessonSessions");
            DropIndex("dbo.LessonSessionAnswers", new[] { "LessonSession_LessonSessionId" });
            DropColumn("dbo.LessonSessionAnswers", "LessonSession_LessonSessionId");
            DropTable("dbo.ContactEnquiries");
        }
    }
}
