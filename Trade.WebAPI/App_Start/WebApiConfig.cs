using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors; 

namespace Trade.WebAPI
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // включения CORS для возможности получения запросов с любых сайтов или приложений
            var cors = new EnableCorsAttribute("*", "*", "*");
            config.EnableCors(cors);

            // настройка маршрутов
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // удаление XML формата, оставляем только JSON 
            config.Formatters.Remove(config.Formatters.XmlFormatter);
        }
    }
}