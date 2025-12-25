using System;
using System.Linq;
using System.Web.Http;
using Trade;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Trade.WebAPI.Controllers
{
    /// <summary>
    /// Контроллер для авторизации пользователей
    /// </summary>
    public class AuthController : ApiController
    {
        // Секретный ключ для шифрования токена
        private const string SecretKey = "12345678123456781234567812345678";

        /// <summary>
        /// Метод входа в систему (получение токена)
        /// </summary>
        /// <param name="login">Логин пользователя</param>
        /// <param name="password">Пароль пользователя</param>
        /// <returns>JWT токен или ошибку</returns>
        [HttpGet]
        [Route("api/login")]
        public IHttpActionResult Login(string login, string password)
        {
            using (var db = new TradeEntities())
            {
                // Ищем пользователя в БД
                var currentUser = db.user.FirstOrDefault(u => u.UserLogin == login && u.UserPassword == password);

                if (currentUser == null)
                {
                    return Unauthorized(); // Ошибка 401, если не нашли пользователя
                }

                // Создаем "заявки" - информацию, которая будет зашита в токен
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, currentUser.UserLogin),
                    new Claim(ClaimTypes.Role, currentUser.role.RoleName)
                };

                // Генерируем ключ безопасности
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                // Создаем токен
                var token = new JwtSecurityToken(
                    issuer: "TradeAPI",
                    audience: "TradeUser",
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(60), // Время жизни токена
                    signingCredentials: creds
                );

                // Возвращаем токен строкой
                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    role = currentUser.role.RoleName // Возвращаем роль клиенту
                });
            }
        }
    }
}