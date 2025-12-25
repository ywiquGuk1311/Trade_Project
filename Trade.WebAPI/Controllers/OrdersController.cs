using System;
using System.Linq;
using System.Web.Http;
using Trade; 

namespace Trade.WebAPI.Controllers
{
    /// <summary>
    /// Контроллер для работы с заказами
    /// </summary>
    [RoutePrefix("api/orders")]
    public class OrdersController : ApiController
    {
        private TradeEntities db = new TradeEntities();

        /// <summary>
        /// 1. Получение списка заказов конкретного пользователя.
        /// Доступно для авторизованных пользователей.
        /// </summary>
        /// <param name="login">Логин пользователя</param>
        [HttpGet]
        [Route("user/{login}")]
        [Authorize] // Требуется любой токен
        public IHttpActionResult GetOrdersByUser(string login)
        {
            // Проверка безопасности:
            // Пользователь может смотреть ТОЛЬКО свои заказы.
            // Исключение: Администраторы и Менеджеры могут смотреть чьи угодно заказы.
            var currentLogin = User.Identity.Name; // Логин из токена
            if (currentLogin != login && !User.IsInRole("Администратор") && !User.IsInRole("Менеджер"))
            {
                return BadRequest("У вас нет прав на просмотр чужих заказов.");
            }

            // Ищем заказы в таблице 'order', где логин пользователя совпадает.
            var orders = db.order
                .Where(o => o.user.UserLogin == login)
                .ToList() // Сначала загружаем данные, чтобы Select сработал без ошибок
                .Select(o => new
                {
                    o.OrderID,
                    o.OrderStatus,
                    o.OrderDate,
                    o.OrderDeliveryDate,
                    o.OrderPickupPoint,
                    // Формируем состав заказа
                    Products = o.orderproduct.Select(op => new
                    {
                        op.product.ProductName, // Название товара
                        op.product.ProductArticleNumber, // Артикул
                        Count = op.ProductAmount // Количество в заказе
                    })
                });

            return Ok(orders);
        }

        /// <summary>
        /// 2. Изменение статуса заказа и даты доставки.
        /// Доступно ТОЛЬКО для ролей "Администратор" и "Менеджер".
        /// </summary>
        /// <param name="id">Номер заказа (OrderID)</param>
        /// <param name="orderData">Данные для обновления</param>
        [HttpPut]
        [Route("{id}/status")]
        [Authorize(Roles = "Администратор,Менеджер")]
        public IHttpActionResult UpdateOrderStatus(int id, [FromBody] order orderData)
        {
            // Ищем заказ в базе по ID
            var currentOrder = db.order.FirstOrDefault(o => o.OrderID == id);

            if (currentOrder == null)
            {
                return NotFound();
            }

            // 1. Меняем статус заказа
            // Мы берем статус, который прислал менеджер, и записываем в базу.
            currentOrder.OrderStatus = orderData.OrderStatus;

            // 2. Меняем дату доставки (если она указана)
            if (orderData.OrderDeliveryDate != null)
            {
                currentOrder.OrderDeliveryDate = orderData.OrderDeliveryDate;
            }

            // Сохраняем изменения в базе данных
            db.SaveChanges();

            return Ok("Статус и дата доставки обновлены");
        }
    }
}