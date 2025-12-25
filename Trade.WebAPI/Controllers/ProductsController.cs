using System;
using System.Linq;
using System.Web.Http;
using System.Data.Entity;
using Trade;

namespace Trade.WebAPI.Controllers
{
    [RoutePrefix("api/products")]
    public class ProductsController : ApiController
    {
        private TradeEntities db = new TradeEntities();

        public ProductsController()
        {
            db.Configuration.ProxyCreationEnabled = false;
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAllProducts()
        {
            //Загружаем данные из БД ВМЕСТЕ со связями
            var rawData = db.product
                .Include(p => p.productcategory1)
                .Include(p => p.productmanufacturer1)
                .Include(p => p.productsupplier1)
                .Include(p => p.unittype)
                .ToList(); // Сначала выполняем запрос к БД

            //Превращаем данные в безопсаный формат
            var safeProducts = rawData.Select(p => new
            {
                p.ProductArticleNumber,
                p.ProductName,
                p.ProductDescription,
                p.ProductPhoto,
                p.ProductCost,
                p.ProductDiscountAmount,
                p.ProductQuantityInStock,
                p.ProductCategory,
                p.ProductManufacturer,
                p.ProductSupplier,
                p.ProductUnit,

                // Заполняем вложенные объекты вручную
                productcategory1 = p.productcategory1 == null ? null : new
                {
                    CategoryName = p.productcategory1.CategoryName
                },

                productmanufacturer1 = p.productmanufacturer1 == null ? null : new
                {
                    ManufacturerName = p.productmanufacturer1.ManufacturerName
                },

                productsupplier1 = p.productsupplier1 == null ? null : new
                {
                    SupplierName = p.productsupplier1.SupplierName
                },

                unittype = p.unittype == null ? null : new
                {
                    UnitName = p.unittype.UnitName
                }
            });

            return Ok(safeProducts);
        }

        [HttpGet]
        [Route("{article}")]
        public IHttpActionResult GetProduct(string article)
        {
            // То же самое для одиночного товара
            var p = db.product
                .Include(x => x.productcategory1)
                .Include(x => x.productmanufacturer1)
                .Include(x => x.productsupplier1)
                .Include(x => x.unittype)
                .FirstOrDefault(x => x.ProductArticleNumber == article);

            if (p == null) return NotFound();

            var safeProduct = new
            {
                p.ProductArticleNumber,
                p.ProductName,
                p.ProductDescription,
                p.ProductPhoto,
                p.ProductCost,
                p.ProductDiscountAmount,
                p.ProductQuantityInStock,
                p.ProductCategory,
                p.ProductManufacturer,
                p.ProductSupplier,
                p.ProductUnit,

                productcategory1 = p.productcategory1 == null ? null : new { CategoryName = p.productcategory1.CategoryName },
                productmanufacturer1 = p.productmanufacturer1 == null ? null : new { ManufacturerName = p.productmanufacturer1.ManufacturerName },
                productsupplier1 = p.productsupplier1 == null ? null : new { SupplierName = p.productsupplier1.SupplierName },
                unittype = p.unittype == null ? null : new { UnitName = p.unittype.UnitName }
            };

            return Ok(safeProduct);
        }


        [HttpPost]
        [Route("")]
        [Authorize(Roles = "Администратор,Менеджер")]
        public IHttpActionResult AddProduct([FromBody] product productData)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (db.product.Any(p => p.ProductArticleNumber == productData.ProductArticleNumber))
                return BadRequest("Артикул занят");

            db.product.Add(productData);
            db.SaveChanges();
            return Ok("Добавлено");
        }

        [HttpPut]
        [Route("{article}")]
        [Authorize(Roles = "Администратор,Менеджер")]
        public IHttpActionResult UpdateProduct(string article, [FromBody] product productData)
        {
            var currentProduct = db.product.FirstOrDefault(p => p.ProductArticleNumber == article);
            if (currentProduct == null) return NotFound();

            currentProduct.ProductName = productData.ProductName;
            currentProduct.ProductCost = productData.ProductCost;
            currentProduct.ProductCategory = productData.ProductCategory;
            currentProduct.ProductManufacturer = productData.ProductManufacturer;
            currentProduct.ProductSupplier = productData.ProductSupplier;
            currentProduct.ProductDiscountAmount = productData.ProductDiscountAmount;
            currentProduct.ProductQuantityInStock = productData.ProductQuantityInStock;
            currentProduct.ProductDescription = productData.ProductDescription;
            currentProduct.ProductMaxDiscount = productData.ProductMaxDiscount;
            currentProduct.ProductUnit = productData.ProductUnit;

            if (!string.IsNullOrEmpty(productData.ProductPhoto))
                currentProduct.ProductPhoto = productData.ProductPhoto;

            db.SaveChanges();
            return Ok("Обновлено");
        }

        [HttpDelete]
        [Route("{article}")]
        [Authorize(Roles = "Администратор,Менеджер")]
        public IHttpActionResult DeleteProduct(string article)
        {
            var currentProduct = db.product.FirstOrDefault(p => p.ProductArticleNumber == article);
            if (currentProduct == null) return NotFound();

            if (currentProduct.orderproduct.Count > 0)
                return BadRequest("Товар есть в заказах, удаление запрещено.");

            db.product.Remove(currentProduct);
            db.SaveChanges();
            return Ok("Удалено");
        }
    }
}