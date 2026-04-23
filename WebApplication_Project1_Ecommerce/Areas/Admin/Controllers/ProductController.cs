using DocumentFormat.OpenXml.Drawing.Diagrams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApplication_Project1_Ecommerce.DataAccess.Repository;
using WebApplication_Project1_Ecommerce.DataAccess.Repository.IRepository;
using WebApplication_Project1_Ecommerce.Models;
using WebApplication_Project1_Ecommerce.Models.ViewModels;
using WebApplication_Project1_Ecommerce.Utility;

namespace WebApplication_Project1_Ecommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;


        private readonly IWebHostEnvironment webHostEnvironment;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new ProductVM
            {
                product = new Product(),
                CategoryList = _unitOfWork.CategoryRepository.GetAll().Select(cl => new SelectListItem()
                {
                    Text = cl.Name,
                    Value = cl.Id.ToString()
                }),
                coverTypeList = _unitOfWork.CoverTypeRepository.GetAll().Select(ct => new SelectListItem()
                {
                    Text = ct.Name,
                    Value = ct.Id.ToString()
                })
            };
            if (id == null)
            {
                return View(productVM);
            }

            productVM.product = _unitOfWork.ProductRepository.Get(id.GetValueOrDefault());

            if (productVM.product == null)
            {
                return NotFound();
            }

            return View(productVM);
        }

        public IActionResult Index()
        {
            return View();
        }


        [HttpPost]
        public IActionResult Upsert(ProductVM productVM)
        {
            if (ModelState.IsValid)
            {
                var webRootPath = _webHostEnvironment.WebRootPath;
                var files = HttpContext.Request.Form.Files;

                // fetch existing image url via projection to avoid tracking another Product instance
                string existingImageUrl = null;
                if (productVM.product.Id != 0)
                {
                    existingImageUrl = _unitOfWork.ProductRepository.GetAll()
                        .Where(p => p.Id == productVM.product.Id)
                        .Select(p => p.ImageUrl)
                        .FirstOrDefault();
                }

                if (files.Count() > 0)
                {
                    var fileName = Guid.NewGuid().ToString();
                    var extension = Path.GetExtension(files[0].FileName);
                    var uploads = Path.Combine(webRootPath, @"images\products");

                    // delete old file if exists
                    if (!string.IsNullOrEmpty(existingImageUrl))
                    {
                        var imagePath = Path.Combine(webRootPath, existingImageUrl.TrimStart('\\'));

                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }
                    using (var fileStream = new FileStream(Path.Combine(uploads, fileName + extension), FileMode.Create))
                    //CREATE A NEW EMPTY FILE OR WILL OVERWRITE THE FILE IF ALREADY CREATED
                    {
                        files[0].CopyTo(fileStream); //THE SELECTED FILE IS Copied to the fileStream
                    }
                    productVM.product.ImageUrl = @"\images\products\" + fileName + extension;
                    //imageurl that will be saved in db

                }
                else //image is not getting updated so the previous images is set to the url 
                {
                    if (productVM.product.Id != 0)
                    {
                        var imagesExists = _unitOfWork.ProductRepository.Get(productVM.product.Id).ImageUrl;
                        //path of image
                        productVM.product.ImageUrl = imagesExists;
                        //path of image is set to ImageUrl}

                    }
                }
                if (productVM.product.Id == 0)
                    _unitOfWork.ProductRepository.Add(productVM.product);
                else
                    _unitOfWork.ProductRepository.Update(productVM.product);
                _unitOfWork.Save();
                return RedirectToAction(nameof(Index));
            }
            else
            {
                productVM = new ProductVM
                {
                    product = new Product(),
                    CategoryList = _unitOfWork.CategoryRepository.GetAll().Select(cl => new SelectListItem()
                    {
                        Text = cl.Name,
                        Value = cl.Id.ToString()
                    }),
                    coverTypeList = _unitOfWork.CoverTypeRepository.GetAll().Select(ct => new SelectListItem()
                    {
                        Text = ct.Name,
                        Value = ct.Id.ToString()
                    })
                };
                if(productVM.product.Id != 0)
                {
                    productVM.product = _unitOfWork.ProductRepository.Get(productVM.product.Id);
                    if (productVM.product == null) return NotFound();
                }
                return View(productVM);
            }
        }


            #region APIs


            [HttpDelete]
            public IActionResult Delete(int id)
        {
            var productInDB = _unitOfWork.ProductRepository.Get(id);
            if(productInDB == null)
                return Json(new { success = false, message = "Error while deleting" });
            //image delete
            var webRootPath = _webHostEnvironment.WebRootPath;
            var imagePath = Path.Combine(webRootPath, productInDB.ImageUrl.Trim('\\'));
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
            //Deleting from db
            _unitOfWork.ProductRepository.Remove(productInDB);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Data deleted successfully" });
        }

        [HttpGet]
            public IActionResult GetAll()
            {
                var productList = _unitOfWork.ProductRepository.GetAll();
                return Json(new { data = productList });
            }
            #endregion

        }
    }
