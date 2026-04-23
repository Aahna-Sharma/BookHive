using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_Project1_Ecommerce.DataAccess.Repository;
using WebApplication_Project1_Ecommerce.DataAccess.Repository.IRepository;
using WebApplication_Project1_Ecommerce.Models;
using WebApplication_Project1_Ecommerce.Utility;

namespace WebApplication_Project1_Ecommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class coverTypeController : Controller
    {
        private readonly IUnitOfWork _unitofwork;

        public coverTypeController(IUnitOfWork unitOfWork)
        {
            _unitofwork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Upsert(int? id)
        {
            coverType covertype = new coverType();
            if (id == null) return View(covertype);
            covertype = _unitofwork.CoverTypeRepository.Get(id.GetValueOrDefault());
            if (covertype == null) return NotFound();
            return View(covertype);


        }
        
        [HttpPost]
        public IActionResult Upsert(coverType covertype)
        {
            if (covertype == null) return NotFound();

            if (!ModelState.IsValid) return View(covertype);

            if (covertype.Id == 0)
                _unitofwork.CoverTypeRepository.Add(covertype);
            else
                _unitofwork.CoverTypeRepository.Update(covertype);

            _unitofwork.Save();

            return RedirectToAction("Index");
        }

        #region APIs
        [HttpGet]
        public IActionResult GetAll()
        {
            var covertypeList = _unitofwork.CoverTypeRepository.GetAll();
            return Json(new { data = covertypeList });
        }
        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var covertypeindb = _unitofwork.CoverTypeRepository.Get(id);
            if (covertypeindb == null) return Json(new { success = false, message = "Unable to delete!!" });
            _unitofwork.CoverTypeRepository.Remove(covertypeindb);
            _unitofwork.Save();
            return Json(new { success = true, message = "Deleted!!" });
            return RedirectToAction("Index");



        }

        #endregion
    }
}
