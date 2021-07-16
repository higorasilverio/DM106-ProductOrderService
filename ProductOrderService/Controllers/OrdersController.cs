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
                    Content = new StringContent("O pedido só pode ser visualizado pelo usuário que o criou ou por um usuário administrador"),
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

            if (!User.IsInRole("USER"))
            {
                var resp = new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("Os pedidos devem ser gerados apenas por usuários válidos e não pelo administrador"),
                    ReasonPhrase = "Unauthorized"
                };
                throw new HttpResponseException(resp);
            }

            order.username = User.Identity.Name;
            order.status = "novo";
            order.pesoTotal = 0;
            order.precoFrete = 0;
            order.precoTotal = 0;
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

            if (!order.status.Equals("novo"))
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("O status do pedido não permite o seu fechamento"),
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
                    Content = new StringContent("O pedido só pode ser apagado pelo usuário que o criou ou por um usuário administrador"),
                    ReasonPhrase = "Forbidden"
                };
                throw new HttpResponseException(resp);
            }

            string cepDestino = this.ObtemCEP();

            OrderItem[] itemArray = new OrderItem[order.OrderItems.Count];
            order.OrderItems.CopyTo(itemArray, 0);

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
                    Content = new StringContent(resultado.Servicos[0].Erro),
                    ReasonPhrase = "BadRequest"
                };
                throw new HttpResponseException(resp);
            }
        }

        // GET: api/Orders/cep
        [ResponseType(typeof(string))]
        [HttpGet]
        [Route("cep")]
        public string ObtemCEP()
        {
            string user = User.Identity.Name;
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
                    Content = new StringContent("O status do pedido não permite o seu fechamento"),
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

        private bool OrderExists(int id)
        {
            return db.Orders.Count(e => e.Id == id) > 0;
        }

        private decimal CalculaPreco(OrderItem[] itemArray)
        {
            return 100;
        }

        private decimal CalculaDiametro(OrderItem[] itemArray)
        {
            return 30;
        }

        private decimal CalculaLargura(OrderItem[] itemArray)
        {
            return 30;
        }

        private decimal CalculaAltura(OrderItem[] itemArray)
        {
            return 30;
        }

        private decimal CalculaComprimento(OrderItem[] itemArray)
        {
            return 30;
        }

        private string CalculaPeso(OrderItem[] itemArray)
        {
            decimal sum = 0;
            for (int i = 0; i < itemArray.Length; i++)
            {
                sum += itemArray[i].Product.peso;
            }
            return sum.ToString("F2");
        }
    }
}