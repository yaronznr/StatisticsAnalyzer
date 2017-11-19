using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebUI
{
    public class UserManager
    {
        public static Guid GetUserId(HttpRequest requset, HttpResponse response)
        {
            Guid userId;
            if (requset.Cookies.AllKeys.Any(c => c.StartsWith("userName")))
            {
                var userString = requset.Cookies.AllKeys.First(c => c.StartsWith("userName"));
                userId = Guid.Parse(requset["userName"]);
            }
            else
            {
                userId = Guid.NewGuid();
            }

            var cookie = new HttpCookie("userName", userId.ToString());
            cookie.Expires = DateTime.Now.AddDays(3);
            response.Cookies.Add(cookie);

            return userId;
        }
    }
}