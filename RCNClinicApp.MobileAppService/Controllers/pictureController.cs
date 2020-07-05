using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace RCNClinicApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class pictureController : ControllerBase
    {
        private IRepositoryAsync<tblPicture> repository;
        private readonly IHostingEnvironment _hostingEnvironment;

        public pictureController(IRepositoryAsync<tblPicture> repository, IHostingEnvironment hostingEnvironment)
        {
            this.repository = repository;
            _hostingEnvironment = hostingEnvironment;
        }


        //  Task<ActionResult<IEnumerable<tblPicture>>>
        [HttpGet("{IdReception}/{date}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IEnumerable<tblPicture>> GetAll(long IdReception, string date)
        {
            long idVisit = MYHelper.getIdVisitbyIdReception(IdReception, date);
            var result= await repository.GetAll(c => c.IdVisit == idVisit);
            result.ToList().ForEach(c => { c.Filepath = MYHelper.getbase64(Path.Combine(_hostingEnvironment.WebRootPath, "FileUpload",c.Filepath)); });
            return result;

        }

        [HttpGet("{IdReception}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetAll(long IdReception)
        {
           
            List<string> lst = new List<string>();
            ContextDb db = new ContextDb();
            var reception = db.tblReceptions.FirstOrDefault(c => c.Id == IdReception);
            string path = string.Empty;
            string base64 = string.Empty;
            if (!string.IsNullOrEmpty(reception.PenSatisfaction))
            {
                path = Path.Combine(_hostingEnvironment.WebRootPath, "FileUpload", reception.PenSatisfaction);
                // byte[] b = System.IO.File.ReadAllBytes(path);
                base64 = MYHelper.getbase64(path);
                lst.Add(base64);
            }
            var xx = db.tblVisits.Where(c => c.IdReception == IdReception).Select(c => c.Id).ToArray();

            foreach (var pic in db.tblPictures.Where(c => xx.Contains(c.IdVisit)).OrderBy(c => c.IdVisit))
            {
                path = Path.Combine(_hostingEnvironment.WebRootPath, "FileUpload",  pic.Filepath); 
               // byte[] b = System.IO.File.ReadAllBytes(path);
                base64=MYHelper.getbase64(path);
                lst.Add(base64);
            }
            return Ok( lst);

        }



        [HttpPut("{id}")]
        //[HttpPut]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<bool>> Put(long id, tblPicture model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            try
            {
                await repository.Update(model);
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                return false;
            }
        }


        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<JsonR>> Post([FromForm]tblPicture model)
        {

            long idVisit = MYHelper.getIdVisitbyIdReception(model.IdReception, model.date);
            if (idVisit < 1)
                return new JsonR { Title = "error", Message = "چنین ویزیتی وجود ندارد" };
            model.IdVisit = idVisit;


            try
            {
                string guidefile = string.Empty;
                if (model.Image.Length > 0)
                {
                    IFormFile files = model.Image;
                    if (string.IsNullOrEmpty(Path.GetExtension(files.FileName)))
                        guidefile = Guid.NewGuid() + ".jpg";
                    else
                        guidefile = Guid.NewGuid() + Path.GetExtension(files.FileName);
                    var filePath = Path.Combine(_hostingEnvironment.WebRootPath, "FileUpload", guidefile);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await files.CopyToAsync(fileStream);
                        fileStream.Flush();
                        model.Filepath = guidefile;
                    }
                }

                await repository.Add(model);
                return new JsonR { Title = "success", Message = guidefile };

            }
            catch (Exception err)
            {
                return new JsonR { Title = "error", Message = err.Message };

            }

        }

        [HttpDelete("{id}")]

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<JsonR>> Delete(long id)
        {
            var tblPicture = await repository.Get(id);
            if (tblPicture == null)
            {
                return new JsonR { Title = "error", Message = "چنین فایلی وجود ندارد" };
            }

            try
            {
                await repository.Delete(id);
                return new JsonR { Title = "success", Message = "عملیات با موفقیت انجام گردید" };
            }
            catch (Exception)
            {
                return new JsonR { Title = "error", Message = "خطا در عملیات!مجددا سعی کنید" };
            }
        }



    }

}