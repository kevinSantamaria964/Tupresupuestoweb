using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Web;


namespace Tupresupuestoweb.Repositories.Models
{
    public class EmailService
    {
        public void EnviarCorreo(string destinatario, string asunto, string cuerpoHtml)
        {
            string remitente = ConfigurationManager.AppSettings["EmailRemitente"];
            string contraseña = ConfigurationManager.AppSettings["EmailPassword"];

            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(remitente, contraseña),
                EnableSsl = true,
            };

            string rutaImagen = HttpContext.Current.Server.MapPath("~/Content/Images/logo_tupresupuesto.png");

            if (!File.Exists(rutaImagen))
                throw new FileNotFoundException("No se encontró la imagen para el correo.", rutaImagen);

            var mensaje = new MailMessage
            {
                From = new MailAddress(remitente),
                Subject = asunto,
                IsBodyHtml = true,
            };
            mensaje.To.Add(destinatario);

            LinkedResource imagen = new LinkedResource(rutaImagen, "image/png");
            imagen.ContentId = "LogoTuPresupuesto";
            imagen.TransferEncoding = TransferEncoding.Base64;

            string htmlConImagen = cuerpoHtml.Replace("{LogoCid}", "cid:LogoTuPresupuesto");

            AlternateView vistaHtml = AlternateView.CreateAlternateViewFromString(htmlConImagen, null, MediaTypeNames.Text.Html);
            vistaHtml.LinkedResources.Add(imagen);

            mensaje.AlternateViews.Add(vistaHtml);

            smtpClient.Send(mensaje);
        }
    }
}
