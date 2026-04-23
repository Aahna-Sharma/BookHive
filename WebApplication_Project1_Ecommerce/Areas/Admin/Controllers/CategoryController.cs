using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_Project1_Ecommerce.DataAccess.Repository.IRepository;
using WebApplication_Project1_Ecommerce.Models;
using WebApplication_Project1_Ecommerce.Utility;

namespace WebApplication_Project1_Ecommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin +","+ SD.Role_Employee)]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Upsert(int? id)
        {
            Category category = new Category();
            //CREATE
            if (id == null) return View(category);
            //EDIT
            category = _unitOfWork.CategoryRepository.Get(id.GetValueOrDefault());
            if(category  == null) return NotFound();
            return  View(category);


        }

        [HttpPost]
        public IActionResult Upsert(Category category)
        {
            if (category == null) return NotFound();

            if (!ModelState.IsValid) return View(category);

            if (category.Id == 0)
                _unitOfWork.CategoryRepository.Add(category);
            else
                _unitOfWork.CategoryRepository.Update(category);

            _unitOfWork.Save();

            return RedirectToAction("Index");
        }

        #region APIs
        [HttpGet]
        public IActionResult GetAll() {
            var categoryList = _unitOfWork.CategoryRepository.GetAll();
            return Json(new { data = categoryList});
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var categoryInDb = _unitOfWork.CategoryRepository.Get(id);
            if(categoryInDb == null) return Json(new {success = false, message = "Unable to delete!!"});
            _unitOfWork.CategoryRepository.Remove(categoryInDb);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Data deleted successfully!!" });
            return RedirectToAction(nameof(Index));

        }


        #endregion
    }
}
