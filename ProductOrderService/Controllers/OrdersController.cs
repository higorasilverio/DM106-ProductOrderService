using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
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
            
            if (User.Identity.Name != order.username && !User.IsInRole("ADMIN"))
            {
                var resp = new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("O pedido só pode ser visualizado pelo usuário que o criou ou por um usuário administrador"),
                    ReasonPhrase = "Unauthorized"
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
            
            if (User.Identity.Name != username && !User.IsInRole("ADMIN"))
            {
                var resp = new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("O pedido só pode ser visualizado pelo usuário que o criou ou por um usuário administrador"),
                    ReasonPhrase = "Unauthorized"
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
            order.dataEntrega = DateTime.Now;

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Orders.Add(order);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = order.Id }, order);
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
            
            if (User.Identity.Name != order.username && !User.IsInRole("ADMIN"))
            {
                var resp = new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("O pedido só pode ser apagado pelo usuário que o criou ou por um usuário administrador"),
                    ReasonPhrase = "Unauthorized"
                };
                throw new HttpResponseException(resp);
            }
            
            db.Orders.Remove(order);
            db.SaveChanges();

            return Ok(order);
        }

        // GET: api/Orders/frete
        [ResponseType(typeof(string))]
        [HttpGet]
        [Route("frete")]
        public IHttpActionResult CalculaFrete()
        {
            string frete;
            CalcPrecoPrazoWS correios = new CalcPrecoPrazoWS();
            cResultado resultado = correios.CalcPrecoPrazo("", "", "40010", "37540000", "37002970", "1", 1, 30, 30, 30, 30, "N", 100, "S");
            if (resultado.Servicos[0].Erro.Equals("0"))
            {
                frete = "Valor do frete: " + resultado.Servicos[0].Valor + " - Prazo de entrega: " + 
                    resultado.Servicos[0].PrazoEntrega + " dia(s)";
                return Ok(frete);
            }
            else
            {
                return BadRequest("Código do erro: " + resultado.Servicos[0].Erro + "-" + resultado.Servicos[0].MsgErro);
            }
        }

        // GET: api/Orders/cep
        [ResponseType(typeof(string))]
        [HttpGet]
        [Route("cep")]
        public IHttpActionResult ObtemCEP()
        {
            string user = User.Identity.Name;
            CRMRestClient crmClient = new CRMRestClient();
            Customer customer = crmClient.GetCustomerByEmail(user);

            if (customer != null)
            {
                return Ok(customer.zip);
            }

            else
            {
                return BadRequest("Falha ao consultar o CRM");
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
    }
}