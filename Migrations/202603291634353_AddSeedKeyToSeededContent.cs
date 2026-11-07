namespace FluentMY.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSeedKeyToSeededContent : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Courses", "SeedKey", c => c.String(maxLength: 100));
            AddColumn("dbo.Lessons", "SeedKey", c => c.String(maxLength: 150));
            AddColumn("dbo.Exercises", "SeedKey", c => c.String(maxLength: 200));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Exercises", "SeedKey");
            DropColumn("dbo.Lessons", "SeedKey");
            DropColumn("dbo.Courses", "SeedKey");
        }
    }
}
