using System;
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

        private string wgOrder = "";    //Order weight
        private decimal lgOrder = 0M;   //Order length
        private decimal hgOrder = 0M;   //Order height
        private decimal wdOrder = 0M;   //Order width
        private decimal dmOrder = 0M;   //Order diameter
        private decimal pcOrder = 0M;   //Order price
        private string zipTo = "";      //Order zipcode receiver

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
                ShowErrorToUser(HttpStatusCode.NotFound,
                        string.Format("O pedido com id {0} não pôde ser localizado", id), "NotFound");

            if (!User.Identity.Name.Equals(order.username) && !User.IsInRole("ADMIN"))
                ShowErrorToUser(HttpStatusCode.Forbidden,
                        "O pedido só pode ser visualizado pelo usuário que o criou ou por um usuário administrador", 
                        "Forbidden");
            
            return Ok(order);
        }

        // GET: api/Orders/byusername?username={username}
        [HttpGet]
        [Route("byusername")]
        [ResponseType(typeof(Order))]
        public IQueryable<Order> GetAllOrderByUsername(string username)
        {
            
            if (!User.Identity.Name.Equals(username) && !User.IsInRole("ADMIN"))
                ShowErrorToUser(HttpStatusCode.Forbidden,
                        "Os pedidos só podem ser visualizados pelo usuário que os criou ou por um usuário administrador",
                        "Forbidden");
            
            return db.Orders.Where(order => order.username == username);
        }

        // POST: api/Orders
        [ResponseType(typeof(Order))]
        public IHttpActionResult PostOrder(Order order)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (order.username == null)
                ShowErrorToUser(HttpStatusCode.BadRequest,
                            "É necessário informar o usuário a.k.a. `username`", "BadRequest");

            if (!User.IsInRole("ADMIN") && !User.Identity.Name.Equals(order.username))
                ShowErrorToUser(HttpStatusCode.BadRequest,
                            "Não é possível criar um pedido para outro usuário", "BadRequest");
            
            order.status = "novo";
            order.pesoTotal = 0M;
            order.precoFrete = 0M;
            order.precoTotal = 0M;
            order.dataPedido = DateTime.Now;

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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
                ShowErrorToUser(HttpStatusCode.NotFound,
                            string.Format("O pedido com id {0} não pôde ser localizado", id), "NotFound");

            if (!User.Identity.Name.Equals(order.username) && !User.IsInRole("ADMIN"))
                ShowErrorToUser(HttpStatusCode.Forbidden,
                            "O pedido só pode ser alterado pelo usuário que o criou ou por um usuário administrador", 
                            "Forbidden");

            if (order.status.Equals("fechado"))
                ShowErrorToUser(HttpStatusCode.BadRequest,
                        "Este pedido já encontra-se fechado", "BadRequest");

            if (order.precoFrete == 0M)
                ShowErrorToUser(HttpStatusCode.BadRequest,
                        "Antes de fechar o pedido calcule seu frete `GET: api/Orders/frete?id={id}`", "BadRequest");

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
                ShowErrorToUser(HttpStatusCode.NotFound,
                                string.Format("O pedido com id {0} não pôde ser localizado", id), "NotFound");

            if (!User.Identity.Name.Equals(order.username) && !User.IsInRole("ADMIN"))
                ShowErrorToUser(HttpStatusCode.Forbidden,
                            "O pedido só pode ser apagado pelo usuário que o criou ou por um usuário administrador",
                            "Forbidden");
            
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
                ShowErrorToUser(HttpStatusCode.NotFound,
                                    string.Format("O pedido com id {0} não pôde ser localizado", id), "NotFound");

            if (!User.Identity.Name.Equals(order.username) && !User.IsInRole("ADMIN"))
                ShowErrorToUser(HttpStatusCode.Forbidden,
                            "O pedido só pode ser alterado pelo usuário que o criou ou por um usuário administrador",
                            "Forbidden");

            if (order.OrderItems.Count == 0)
                ShowErrorToUser(HttpStatusCode.BadRequest,
                        "O pedido não possui produtos associados", "BadRequest");

            zipTo = GetZip(order);
            if (zipTo == null)
                ShowErrorToUser(HttpStatusCode.BadRequest,
                        "Não foi possível obter o CEP do usuário (Erro no serviço de CRM)", "BadRequest");

            OrderItem[] itemArray = new OrderItem[order.OrderItems.Count];
            order.OrderItems.CopyTo(itemArray, 0);
            GetOrderData(itemArray);

            CalcPrecoPrazoWS correios = new CalcPrecoPrazoWS();
            cResultado resultado = correios.CalcPrecoPrazo(
                "", "", "40010", "05428-000", zipTo, wgOrder, 1, lgOrder, hgOrder, wdOrder, dmOrder, "N", pcOrder, "S");

            if (resultado.Servicos[0].Erro.Equals("0"))
            {
                order.precoFrete = decimal.Parse(resultado.Servicos[0].Valor);
                order.dataEntrega = DateTime.Now.AddDays(double.Parse(resultado.Servicos[0].PrazoEntrega));
                order.pesoTotal = Convert.ToDecimal(wgOrder);
                order.precoTotal = pcOrder;
                ClearOrderData();

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
                ClearOrderData();
                return BadRequest(resultado.Servicos[0].MsgErro);
            }
                
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }

        private string GetZip(Order order)
        {
            string user = User.IsInRole("User") ? User.Identity.Name : order.username;

            CRMRestClient crmClient = new CRMRestClient();
            Customer customer = crmClient.GetCustomerByEmail(user);

            if (customer != null)
                return customer.zip;
            else
                return null;
        }

        private void GetOrderData(OrderItem[] itemArray)
        {
            pcOrder = 0M;
            for (int i = 0; i < itemArray.Length; i++)
                pcOrder += itemArray[i].Product.preco * itemArray[i].quantidade;

            dmOrder = 0M;
            for (int i = 0; i < itemArray.Length; i++)
                dmOrder += itemArray[i].Product.diametro * itemArray[i].quantidade;

            hgOrder = 0M;
            for (int i = 0; i < itemArray.Length; i++)
                hgOrder += itemArray[i].Product.altura * itemArray[i].quantidade;

            wdOrder = 0M;
            for (int i = 0; i < itemArray.Length; i++)
                if (itemArray[i].Product.largura > wdOrder)
                    wdOrder = itemArray[i].Product.largura;

            lgOrder = 0M;
            for (int i = 0; i < itemArray.Length; i++)
                if (itemArray[i].Product.comprimento > lgOrder)
                    lgOrder = itemArray[i].Product.largura;

            decimal sum = 0M;
            wgOrder = "";
            for (int i = 0; i < itemArray.Length; i++)
                sum += itemArray[i].Product.peso * itemArray[i].quantidade;
            wgOrder = sum.ToString("F2");
        }

        private void ClearOrderData()
        {
            wgOrder = "";
            lgOrder = 0M;
            hgOrder = 0M;
            wdOrder = 0M;
            dmOrder = 0M;
            pcOrder = 0M;
            zipTo = "";
        }

        private void ShowErrorToUser(HttpStatusCode statusCode, string content, string reasonPhrase)
        {
            var resp = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content),
                ReasonPhrase = reasonPhrase
            };
            throw new HttpResponseException(resp);
        }
    }
}