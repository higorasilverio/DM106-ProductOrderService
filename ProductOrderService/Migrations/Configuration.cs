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
                    codigo = "COD1", preco = 10, peso = 1, altura = 1, largura = 1,
                    comprimento = 1, diametro = 1, Url = "www.productorderservice.com/produto1"
                },
                new Product { Id = 2, nome = "produto 2", descricao = "descrição 2", cor = "verde", modelo = "modelo 2",
                    codigo = "COD2", preco = 20, peso = 2, altura = 2, largura = 2,
                    comprimento = 2, diametro = 2, Url = "www.productorderservice.com/produto2"
                },
                new Product { Id = 3, nome = "produto 3", descricao = "descrição 3", cor = "azul", modelo = "modelo 3",
                    codigo = "COD3", preco = 30, peso = 3, altura = 3, largura = 3,
                    comprimento = 3, diametro = 3, Url = "www.productorderservice.com/produto3"
                }
            );
        }
    }
}
