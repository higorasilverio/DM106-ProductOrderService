namespace ProductOrderService.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddProducts : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Products",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        nome = c.String(nullable: false),
                        descricao = c.String(),
                        cor = c.String(),
                        modelo = c.String(nullable: false),
                        codigo = c.String(nullable: false, maxLength: 8),
                        preco = c.Decimal(nullable: false, precision: 18, scale: 2),
                        peso = c.Decimal(nullable: false, precision: 18, scale: 2),
                        altura = c.Decimal(nullable: false, precision: 18, scale: 2),
                        largura = c.Decimal(nullable: false, precision: 18, scale: 2),
                        comprimento = c.Decimal(nullable: false, precision: 18, scale: 2),
                        diametro = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Url = c.String(maxLength: 80),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Products");
        }
    }
}
