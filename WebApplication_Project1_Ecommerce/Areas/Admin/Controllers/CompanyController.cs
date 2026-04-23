using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata;
using WebApplication_Project1_Ecommerce.DataAccess.Repository.IRepository;
using WebApplication_Project1_Ecommerce.Models;
using WebApplication_Project1_Ecommerce.Utility;

namespace WebApplication_Project1_Ecommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CompanyController(IUnitOfWork unitofwork)
        {
            _unitOfWork = unitofwork;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Upsert(int? id)
        {
            Company company = new Company();
            if (id == null) return View(company);
            company = _unitOfWork.CompanyRepository.Get(id.GetValueOrDefault());
            if (company == null) return NotFound();
            return View(company);
        }

        [HttpPost]
        public IActionResult Upsert(Company company)
        {
            if (company == null) return BadRequest();
            if (!ModelState.IsValid) return View(company);
            if (company.Id == 0)
                _unitOfWork.CompanyRepository.Add(company);
            else
                _unitOfWork.CompanyRepository.Update(company);

            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));

        }

        #region APIs

        [HttpGet]

        public IActionResult GetAll()
        {
            return Json(new { data = _unitOfWork.CompanyRepository.GetAll() });
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var CompanyInDb = _unitOfWork.CompanyRepository.Get(id);
            if (CompanyInDb == null)
                return Json(new { success = false, message = "Unable to Delete Data!!"});
            _unitOfWork.CompanyRepository.Remove(CompanyInDb);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Data Deleted Successfully!!"});

        }


        #endregion
    }
}
