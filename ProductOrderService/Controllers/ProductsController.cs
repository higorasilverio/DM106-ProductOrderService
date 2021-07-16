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
            {
                var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(string.Format("O produto com id {0} não pôde ser localizado", id)),
                    ReasonPhrase = "NotFound"
                };
                throw new HttpResponseException(resp);
            }

            return Ok(product);
        }

        // PUT: api/Products/5
        [Authorize (Roles = "ADMIN")]
        [ResponseType(typeof(void))]
        public IHttpActionResult PutProduct(int id, Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != product.Id)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(
                        string.Format("O id fornecido: {0}, é diferente do id do produto a ser alterado: {1}", id, product.Id)),
                    ReasonPhrase = "BadRequest"
                };
                throw new HttpResponseException(resp);
            }

            Product productToUpdate = db.Products.AsNoTracking().First(x => x.Id == product.Id);

            if (productToUpdate.codigo != product.codigo)
            {
                if (CodeExists(product.codigo))
                {
                    var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(
                            string.Format("O código fornecido: {0} já está cadastrado no sistema", product.codigo)),
                        ReasonPhrase = "BadRequest"
                    };
                    throw new HttpResponseException(resp);
                }
            }

            if (productToUpdate.modelo != product.modelo)
            {
                if (ModelExists(product.modelo))
                {
                    var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(
                            string.Format("O modelo fornecido: {0} já está cadastrado no sistema", product.modelo)),
                        ReasonPhrase = "BadRequest"
                    };
                    throw new HttpResponseException(resp);
                }
            }

            db.Entry(product).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
                    {
                        Content = new StringContent(string.Format("O produto com id {0} não pôde ser localizado", id)),
                        ReasonPhrase = "NotFound"
                    };
                    throw new HttpResponseException(resp);
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Products
        [Authorize(Roles = "ADMIN")]
        [ResponseType(typeof(Product))]
        public IHttpActionResult PostProduct(Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (CodeExists(product.codigo))
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(
                            string.Format("O código fornecido: {0} já está cadastrado no sistema", product.codigo)),
                    ReasonPhrase = "BadRequest"
                };
                throw new HttpResponseException(resp);
            }

            if (ModelExists(product.modelo))
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(
                            string.Format("O modelo fornecido: {0} já está cadastrado no sistema", product.modelo)),
                    ReasonPhrase = "BadRequest"
                };
                throw new HttpResponseException(resp);
            }

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
            {
                var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(string.Format("O produto com id {0} não pôde ser localizado", id)),
                    ReasonPhrase = "NotFound"
                };
                throw new HttpResponseException(resp);
            }

            db.Products.Remove(product);
            db.SaveChanges();

            return Ok(product);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
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
    }
}