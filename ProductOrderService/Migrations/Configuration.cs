namespace ProductOrderService.Migrations
{
    using ProductOrderService.Models;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<ProductOrderService.Data.ProductOrderServiceContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(ProductOrderService.Data.ProductOrderServiceContext context)
        {
            //  This method will be called after migrating to the latest version.
            context.Products.AddOrUpdate(
                p => p.Id,
                new Product { Id = 1, nome = "produto 1", descricao = "descrição 1", cor = "vermelho", modelo = "modelo 1",
                    codigo = "COD1", preco = 10, peso = 1, altura = 15, largura = 15,
                    comprimento = 15, diametro = 15, url = "www.productorderservice.com/produto1"
                },
                new Product { Id = 2, nome = "produto 2", descricao = "descrição 2", cor = "verde", modelo = "modelo 2",
                    codigo = "COD2", preco = 20, peso = 2, altura = 20, largura = 20,
                    comprimento = 20, diametro = 20, url = "www.productorderservice.com/produto2"
                },
                new Product { Id = 3, nome = "produto 3", descricao = "descrição 3", cor = "azul", modelo = "modelo 3",
                    codigo = "COD3", preco = 30, peso = 3, altura = 30, largura = 30,
                    comprimento = 30, diametro = 30, url = "www.productorderservice.com/produto3"
                }
            );
        }
    }
}
