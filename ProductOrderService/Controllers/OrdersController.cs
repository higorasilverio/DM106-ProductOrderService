using System;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Ajax.Utilities;
using ProductOrderService.br.com.correios.ws;
using ProductOrderService.CRMClient;
using ProductOrderService.Data;
using ProductOrderService.Models;

namespace ProductOrderService.Controllers
{
    [Authorize]
    [RoutePrefix("api/Orders")]
    public class OrdersController : ApiController
    {
        private ProductOrderServiceContext db = new ProductOrderServiceContext();

        // GET: api/Orders
        [Authorize (Roles ="ADMIN")]
        public IQueryable<Order> GetOrders()
        {
            return db.Orders;
        }

        // GET: api/Orders/5
        [ResponseType(typeof(Order))]
        public IHttpActionResult GetOrder(int id)
        {
            Order order = db.Orders.Find(id);

            if (order == null)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(string.Format("O pedido com id {0} não pôde ser localizado", id)),
                    ReasonPhrase = "NotFound"
                };
                throw new HttpResponseException(resp);
            }
            
            if (!User.Identity.Name.Equals(order.username) && !User.IsInRole("ADMIN"))
            {
                var resp = new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
                    Content = new StringContent("O pedido só pode ser visualizado pelo usuário que o criou ou por um usuário administrador"),
                    ReasonPhrase = "Forbidden"
                };
                throw new HttpResponseException(resp);
            }
            
            return Ok(order);
        }

        // GET: api/Orders/byusername?username={username}
        [HttpGet]
        [Route("byusername")]
        [ResponseType(typeof(Order))]
        public IQueryable<Order> GetAllOrderByUsername(string username)

        {
            
            if (!User.Identity.Name.Equals(username) && !User.IsInRole("ADMIN"))
            {
                var resp = new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
                    Content = new StringContent(
                        "Os pedidos só podem ser visualizados pelo usuário que os criaram ou por um usuário administrador"),
                    ReasonPhrase = "Forbidden"
                };
                throw new HttpResponseException(resp);
            }
            
            return db.Orders.Where(order => order.username == username);
        }

        // POST: api/Orders
        [ResponseType(typeof(Order))]
        public IHttpActionResult PostOrder(Order order)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (User.IsInRole("ADMIN")) {
                if (order.username == null || order.username.Length == 0)
                {
                    var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent("Não foi fornecido um usuário válido para o inserção do pedido"),
                        ReasonPhrase = "BadRequest"
                    };
                    throw new HttpResponseException(resp);
                }
            } else
            {
                if (order.username != null && !order.username.Equals(User.Identity.Name))
                {
                    var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent("Não é possível criar um pedido para outro usuário"),
                        ReasonPhrase = "BadRequest"
                    };
                    throw new HttpResponseException(resp);
                }
                order.username = User.Identity.Name;
            }

            order.status = "novo";
            order.pesoTotal = 0M;
            order.precoFrete = 0M;
            order.precoTotal = 0M;
            order.dataPedido = DateTime.Now;

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Orders.Add(order);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = order.Id }, order);
        }

        // PUT: api/Orders/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutOrder(int id)
        {
            Order order = db.Orders.Find(id);

            if (order == null)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(string.Format("O pedido com id {0} não pôde ser localizado", id)),
                    ReasonPhrase = "NotFound"
                };
                throw new HttpResponseException(resp);
            }

            if (!User.Identity.Name.Equals(order.username) && !User.IsInRole("ADMIN"))
            {
                var resp = new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
                    Content = new StringContent("O pedido só pode ser alterado pelo usuário que o criou ou por um usuário administrador"),
                    ReasonPhrase = "Forbidden"
                };
                throw new HttpResponseException(resp);
            }

            if (order.status.Equals("fechado"))
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Este pedido já encontra-se fechado"),
                    ReasonPhrase = "BadRequest"
                };
                throw new HttpResponseException(resp);
            }

            if (order.precoFrete == 0M)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(
                        "O status do pedido não permite o seu fechamento. É necessário calcular o frete primeiramente"),
                    ReasonPhrase = "BadRequest"
                };
                throw new HttpResponseException(resp);
            }

            order.status = "fechado";

            db.Entry(order).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }

            return Ok(order);
        }

        // DELETE: api/Orders/5
        [ResponseType(typeof(Order))]
        public IHttpActionResult DeleteOrder(int id)
        {
            Order order = db.Orders.Find(id);
            if (order == null)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(string.Format("O pedido com id {0} não pode ser localizado", id)),
                    ReasonPhrase = "NotFound"
                };
                throw new HttpResponseException(resp);
            }
            
            if (!User.Identity.Name.Equals(order.username) && !User.IsInRole("ADMIN"))
            {
                var resp = new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
                    Content = new StringContent("O pedido só pode ser apagado pelo usuário que o criou ou por um usuário administrador"),
                    ReasonPhrase = "Forbidden"
                };
                throw new HttpResponseException(resp);
            }
            
            db.Orders.Remove(order);
            db.SaveChanges();

            return Ok(order);
        }

        // GET: api/Orders/frete?id=5
        [ResponseType(typeof(Order))]
        [HttpGet]
        [Route("frete")]
        public IHttpActionResult CalculaFrete([FromUri]int id)
        {
            Order order = db.Orders.Find(id);

            if (order == null)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(string.Format("O pedido com id {0} não pode ser localizado", id)),
                    ReasonPhrase = "NotFound"
                };
                throw new HttpResponseException(resp);
            }

            if (!User.Identity.Name.Equals(order.username) && !User.IsInRole("ADMIN"))
            {
                var resp = new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
                    Content = new StringContent("O pedido só pode ser consultado pelo usuário que o criou ou por um usuário administrador"),
                    ReasonPhrase = "Forbidden"
                };
                throw new HttpResponseException(resp);
            }

            string cepDestino = this.getCEP(order);

            OrderItem[] itemArray = new OrderItem[order.OrderItems.Count];
            order.OrderItems.CopyTo(itemArray, 0);

            if (itemArray.Length == 0)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("O pedido não possui produtos associados"),
                    ReasonPhrase = "BadRequest"
                };
                throw new HttpResponseException(resp);
            }

            string pesoPedido = CalculaPeso(itemArray);

            decimal comprimentoPedido = CalculaComprimento(itemArray);

            decimal alturaPedido = CalculaAltura(itemArray);

            decimal larguraPedido = CalculaLargura(itemArray);

            decimal diametroPedido = CalculaDiametro(itemArray);

            decimal precoPedido = CalculaPreco(itemArray);

            CalcPrecoPrazoWS correios = new CalcPrecoPrazoWS();
            cResultado resultado = correios.CalcPrecoPrazo(
                "", "", "40010", "05428-000", cepDestino, pesoPedido, 1, 
                comprimentoPedido, alturaPedido, larguraPedido, diametroPedido, "N", precoPedido, "S");
            if (resultado.Servicos[0].Erro.Equals("0"))
            {
                order.precoFrete = Decimal.Parse(resultado.Servicos[0].Valor);
                order.dataEntrega = DateTime.Now.AddDays(double.Parse(resultado.Servicos[0].PrazoEntrega));
                order.pesoTotal = Convert.ToDecimal(pesoPedido);
                order.precoTotal = precoPedido;
                
                db.Entry(order).State = EntityState.Modified;

                try
                {
                    db.SaveChanges();
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw;
                }

                return Ok(order);
            }
            else
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(resultado.Servicos[0].MsgErro),
                    ReasonPhrase = "BadRequest"
                };
                throw new HttpResponseException(resp);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private string getCEP(Order order)
        {
            string user;

            if (order == null)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Não foi fornecido o pedido"),
                    ReasonPhrase = "BadRequest"
                };
                throw new HttpResponseException(resp);
            }

            if (User.IsInRole("User"))
                user = User.Identity.Name;
            else
                user = order.username;
            CRMRestClient crmClient = new CRMRestClient();
            Customer customer = crmClient.GetCustomerByEmail(user);

            if (customer != null)
            {
                return customer.zip;
            }

            else
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Não foi possível obter o CEP do usuário"),
                    ReasonPhrase = "BadRequest"
                };
                throw new HttpResponseException(resp);
            }
        }

        private decimal CalculaPreco(OrderItem[] itemArray)
        {
            decimal sum = 0M;
            for (int i = 0; i < itemArray.Length; i++)
            {
                sum += itemArray[i].Product.preco * itemArray[i].quantidade;
            }
            return sum;
        }

        private decimal CalculaDiametro(OrderItem[] itemArray)
        {
            decimal sum = 0M;
            for (int i = 0; i < itemArray.Length; i++)
            {
                sum += itemArray[i].Product.diametro * itemArray[i].quantidade;
            }
            return sum;
        }

        private decimal CalculaAltura(OrderItem[] itemArray)
        {
            decimal sum = 0M;
            for (int i = 0; i < itemArray.Length; i++)
            {
                sum += itemArray[i].Product.altura * itemArray[i].quantidade;
            }
            return sum;
        }

        private decimal CalculaLargura(OrderItem[] itemArray)
        {
            decimal largura = 0M;
            for (int i = 0; i < itemArray.Length; i++)
            {
                if (itemArray[i].Product.largura > largura)
                    largura = itemArray[i].Product.largura;
            }
            return largura;
        }
        
        private decimal CalculaComprimento(OrderItem[] itemArray)
        {
            decimal comprimento = 0M;
            for (int i = 0; i < itemArray.Length; i++)
            {
                if (itemArray[i].Product.comprimento > comprimento)
                    comprimento = itemArray[i].Product.largura;
            }
            return comprimento;
        }

        private string CalculaPeso(OrderItem[] itemArray)
        {
            decimal sum = 0M;
            for (int i = 0; i < itemArray.Length; i++)
            {
                sum += itemArray[i].Product.peso * itemArray[i].quantidade;
            }
            return sum.ToString("F2");
        }
    }
}