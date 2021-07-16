namespace ProductOrderService.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class nullableDeliveryDate : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Orders", "dataEntrega", c => c.DateTime());
            AlterColumn("dbo.Orders", "status", c => c.String(maxLength: 10));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Orders", "status", c => c.String());
            AlterColumn("dbo.Orders", "dataEntrega", c => c.DateTime(nullable: false));
        }
    }
}
