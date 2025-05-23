using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Tupresupuestoweb.Filters
{
    public class CustomAuthorizeAttribute : AuthorizeAttribute
    {
        private readonly int[] _allowedRoles;

        public CustomAuthorizeAttribute(params int[] roles)
        {
            _allowedRoles = roles;
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var rolUsuario = httpContext.Session["IdRol"];
            if (rolUsuario == null)
                return false;

            int rol = Convert.ToInt32(rolUsuario);
            return _allowedRoles.Contains(rol);
        }

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            base.OnAuthorization(filterContext);

            // 🔒 Esto evita que las páginas se guarden en caché
            var response = filterContext.HttpContext.Response;
            response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            response.Cache.SetValidUntilExpires(false);
            response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
            response.Cache.SetCacheability(HttpCacheability.NoCache);
            response.Cache.SetNoStore();
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            filterContext.Result = new RedirectResult("~/Account/Login");
        }
    }
}

