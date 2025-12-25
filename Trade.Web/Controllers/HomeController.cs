using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using Trade;

namespace Trade.Web.Controllers
{
    public class HomeController : Controller
    {
        private string apiUrl = "https://localhost:44313/api/products";

        public async Task<ActionResult> Index(string searchString, int? manufacturerId, decimal? maxPrice, bool? onlyDiscount, bool? inStock, string sortOrder)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            List<product> products = new List<product>();

            // Загрузка данных
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(apiUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        products = JsonConvert.DeserializeObject<List<product>>(json);
                    }
                }
            }
            catch (Exception) { }

            // Фильтрация

            // Поиск
            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.ProductDescription != null &&
                                               p.ProductDescription.ToLower().Contains(searchString.ToLower())).ToList();
            }

            // Производитель
            if (manufacturerId != null && manufacturerId != 0)
            {
                products = products.Where(p => p.ProductManufacturer == manufacturerId).ToList();
            }

            // Цена до (с учетом скидки)
            if (maxPrice != null)
            {
                products = products.Where(p => {
                    decimal c = (decimal?)p.ProductCost ?? 0;
                    decimal d = (decimal?)p.ProductDiscountAmount ?? 0;
                    decimal final = d > 0 ? c - (c * d / 100) : c;
                    return final <= maxPrice;
                }).ToList();
            }

            // Только со скидкой
            if (onlyDiscount == true)
            {
                products = products.Where(p => ((decimal?)p.ProductDiscountAmount ?? 0) > 0).ToList();
            }

            // На складе
            if (inStock == true)
            {
                products = products.Where(p => ((int?)p.ProductQuantityInStock ?? 0) > 0).ToList();
            }

            // 3. Сортировка
            ViewBag.SortParam = sortOrder;
            ViewBag.SelectedMan = manufacturerId; // Сохраняем выбранного производителя для списка

            switch (sortOrder)
            {
                case "name_asc":
                    products = products.OrderBy(p => p.ProductName).ToList();
                    break;
                case "supplier_asc":
                    products = products.OrderBy(p => p.ProductManufacturer).ToList();
                    break;
                case "price_asc":
                    products = products.OrderBy(p => {
                        decimal c = (decimal?)p.ProductCost ?? 0;
                        decimal d = (decimal?)p.ProductDiscountAmount ?? 0;
                        return d > 0 ? c - (c * d / 100) : c;
                    }).ToList();
                    break;
                case "price_desc":
                    products = products.OrderByDescending(p => {
                        decimal c = (decimal?)p.ProductCost ?? 0;
                        decimal d = (decimal?)p.ProductDiscountAmount ?? 0;
                        return d > 0 ? c - (c * d / 100) : c;
                    }).ToList();
                    break;
                default:
                    products = products.OrderBy(p => p.ProductName).ToList();
                    break;
            }

            return View(products);
        }
    }
}