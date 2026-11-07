namespace FluentMY.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddProfileFieldsAndImage : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "ProfileImagePath", c => c.String(maxLength: 255));
        }

        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "ProfileImagePath");
        }
    }
}
