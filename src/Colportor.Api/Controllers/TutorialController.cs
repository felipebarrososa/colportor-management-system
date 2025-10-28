using Microsoft.AspNetCore.Mvc;

namespace Colportor.Api.Controllers
{
    [ApiController]
    [Route("tutorial")]
    public class TutorialController : ControllerBase
    {
        /// <summary>
        /// Página principal do tutorial
        /// </summary>
        [HttpGet]
        public IActionResult GetTutorial()
        {
            return File("~/tutorial/index.html", "text/html");
        }

        /// <summary>
        /// Tutorial específico para Admin e Líderes
        /// </summary>
        [HttpGet("admin")]
        public IActionResult GetAdminTutorial()
        {
            return File("~/tutorial/admin.html", "text/html");
        }

        /// <summary>
        /// Tutorial específico para Colportores
        /// </summary>
        [HttpGet("colportor")]
        public IActionResult GetColportorTutorial()
        {
            return File("~/tutorial/colportor.html", "text/html");
        }

        /// <summary>
        /// Endpoint para verificar se o tutorial está funcionando
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new { 
                status = "Tutorial disponível",
                version = "1.0.0",
                pages = new[] { "index", "admin", "colportor" }
            });
        }
    }
}
