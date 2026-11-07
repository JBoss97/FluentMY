namespace FluentMY.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class IncreaseCourseLevelLength : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Courses", "Level", c => c.String(maxLength: 50));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Courses", "Level", c => c.String(maxLength: 20));
        }
    }
}
