namespace FluentMY.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddProfileFieldsToApplicationUser : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "FullName", c => c.String(maxLength: 80));
            AddColumn("dbo.AspNetUsers", "Bio", c => c.String(maxLength: 500));
            AddColumn("dbo.AspNetUsers", "LanguageLevel", c => c.String(maxLength: 30));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "LanguageLevel");
            DropColumn("dbo.AspNetUsers", "Bio");
            DropColumn("dbo.AspNetUsers", "FullName");
        }
    }
}
