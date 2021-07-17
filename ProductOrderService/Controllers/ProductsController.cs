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
using ProductOrderService.Data;
using ProductOrderService.Models;

namespace ProductOrderService.Controllers
{
    [Authorize]
    public class ProductsController : ApiController
    {
        private ProductOrderServiceContext db = new ProductOrderServiceContext();

        // GET: api/Products
        public IQueryable<Product> GetProducts()
        {
            return db.Products;
        }

        // GET: api/Products/5
        [ResponseType(typeof(Product))]
        public IHttpActionResult GetProduct(int id)
        {
            Product product = db.Products.Find(id);

            if (product == null)
                ShowErrorToUser(HttpStatusCode.NotFound,
                    string.Format("O produto com id {0} não pôde ser localizado", id), "NotFound");

            return Ok(product);
        }

        // PUT: api/Products/5
        [Authorize (Roles = "ADMIN")]
        [ResponseType(typeof(void))]
        public IHttpActionResult PutProduct(int id, Product product)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != product.Id)
                ShowErrorToUser(HttpStatusCode.BadRequest,
                    string.Format("O id fornecido: {0}, é diferente do id do produto a ser alterado: {1}", id, product.Id),
                    "BadRequest");

            Product productToUpdate = db.Products.AsNoTracking().First(x => x.Id == product.Id);

            int control = 0;
            if (productToUpdate.codigo != product.codigo)
                if (CodeExists(product.codigo))
                    control = 1;           
            if (productToUpdate.modelo != product.modelo)
                if (ModelExists(product.modelo))
                    control = control == 0 ? 2 : 3;
            if (control != 0)
                InformBodyError(control, product);

            db.Entry(product).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                    ShowErrorToUser(HttpStatusCode.NotFound,
                    string.Format("O produto com id {0} não pôde ser localizado", id), "NotFound");
                else
                    throw;
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Products
        [Authorize(Roles = "ADMIN")]
        [ResponseType(typeof(Product))]
        public IHttpActionResult PostProduct(Product product)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            int control = 0;
            if (CodeExists(product.codigo))
                control = 1;
            if (ModelExists(product.modelo))
                control = control == 0 ? 2 : 3;
            if (control != 0)
                InformBodyError(control, product);

            db.Products.Add(product);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = product.Id }, product);
        }

        // DELETE: api/Products/5
        [Authorize(Roles = "ADMIN")]
        [ResponseType(typeof(Product))]
        public IHttpActionResult DeleteProduct(int id)
        {
            Product product = db.Products.Find(id);

            if (product == null)
                ShowErrorToUser(HttpStatusCode.NotFound,
                string.Format("O produto com id {0} não pôde ser localizado", id), "NotFound");

            db.Products.Remove(product);
            db.SaveChanges();

            return Ok(product);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }

        private bool ProductExists(int id)
        {
            return db.Products.Count(e => e.Id == id) > 0;
        }

        private bool CodeExists(string code)
        {
           return db.Products.Count(e => e.codigo == code) > 0;
        }

        private bool ModelExists(string model)
        {
            return db.Products.Count(e => e.modelo == model) > 0;
        }

        private void InformBodyError(int control, Product product)
        {
            switch (control)
            {
                case 1:
                    ShowErrorToUser(
                        HttpStatusCode.BadRequest,
                        string.Format("O código fornecido: {0} já está cadastrado no sistema", product.codigo),
                        "BadRequest");
                    break;
                case 2:
                    ShowErrorToUser(
                        HttpStatusCode.BadRequest,
                        string.Format("O modelo fornecido: {0} já está cadastrado no sistema", product.modelo),
                        "BadRequest");
                    break;
                case 3:
                    ShowErrorToUser(
                        HttpStatusCode.BadRequest,
                        string.Format(
                            "O modelo: {0} e o código: {1} já estão cadastrados no sistema", product.modelo, product.codigo),
                        "BadRequest");
                    break;
            }
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