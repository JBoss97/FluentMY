namespace FluentMY.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAdminAuditLogs : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AdminAuditLogs",
                c => new
                    {
                        AdminAuditLogId = c.Int(nullable: false, identity: true),
                        AdminUserId = c.String(maxLength: 128),
                        AdminUserName = c.String(maxLength: 200),
                        ActionType = c.String(nullable: false, maxLength: 100),
                        EntityType = c.String(nullable: false, maxLength: 100),
                        EntityId = c.String(maxLength: 128),
                        EntityDisplayName = c.String(maxLength: 200),
                        Details = c.String(),
                        CreatedAtUtc = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.AdminAuditLogId);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.AdminAuditLogs");
        }
    }
}
