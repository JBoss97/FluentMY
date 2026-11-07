namespace FluentMY.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SyncLatestFluentMYModels : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Exercises", "OptionA", c => c.String(maxLength: 200));
            AlterColumn("dbo.Exercises", "OptionB", c => c.String(maxLength: 200));
            AlterColumn("dbo.Exercises", "OptionC", c => c.String(maxLength: 200));
            AlterColumn("dbo.Exercises", "OptionD", c => c.String(maxLength: 200));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Exercises", "OptionD", c => c.String(maxLength: 255));
            AlterColumn("dbo.Exercises", "OptionC", c => c.String(maxLength: 255));
            AlterColumn("dbo.Exercises", "OptionB", c => c.String(maxLength: 255));
            AlterColumn("dbo.Exercises", "OptionA", c => c.String(maxLength: 255));
        }
    }
}
