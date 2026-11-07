namespace FluentMY.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddFullProfileFeatures : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "JoinedAt", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "JoinedAt");
        }
    }
}
