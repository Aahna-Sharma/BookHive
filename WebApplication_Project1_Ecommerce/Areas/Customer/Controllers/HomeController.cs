using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Diagnostics;
using System.Security.Claims;
using WebApplication_Project1_Ecommerce.DataAccess.Repository.IRepository;
using WebApplication_Project1_Ecommerce.Models;
using WebApplication_Project1_Ecommerce.Utility;

namespace WebApplication_Project1_Ecommerce.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public HomeController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if(claims 
                != null)
            {
                var count = _unitOfWork.ShoppingCartRepository.GetAll
                    (sc => sc.ApplicationUserId == claims.Value).ToList().Count;
                HttpContext.Session.SetInt32(SD.Ss_CartSessionCount, count);
            }
            var productList = _unitOfWork.ProductRepository.GetAll();
            return View(productList);
        }

        
        public IActionResult Details(int id)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (claims
                != null)
            {
                var count = _unitOfWork.ShoppingCartRepository.GetAll
                    (sc => sc.ApplicationUserId == claims.Value).ToList().Count;
                HttpContext.Session.SetInt32(SD.Ss_CartSessionCount, count);
            }
            ///****
            var productInDb = _unitOfWork.ProductRepository.FirstOrDefault(p => p.Id == id, 
                includeProperties: "Category,coverType");
            if (productInDb == null) return NotFound();
            var shoppingCart = new ShoppingCart()
            {
                Product = productInDb,
                ProductId = id,
            };

            return View(shoppingCart);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            shoppingCart.Id = 0;
            if (ModelState.IsValid)
            {
                var claimsIdentity = (ClaimsIdentity)(User.Identity);       //DETAIL OF USER
                var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                if (claims == null) return NotFound();
                shoppingCart.ApplicationUserId = claims.Value;
                //USER ID OF PERSON WHO IS LOGGED IN
                var shoppingCartInDb = _unitOfWork.ShoppingCartRepository.FirstOrDefault(sc => sc.ApplicationUserId == claims.Value && sc.ProductId == shoppingCart.ProductId);
                //INCREASING THE COUNT OF THE PRODUCT IF IS ALREADY ADDED
                //OTHERWISE WILL ADD THE PRODUCT IN THE SHOPPING CART AS IT IS
                if (shoppingCartInDb == null)
                    _unitOfWork.ShoppingCartRepository.Add(shoppingCart);
                else
                    shoppingCartInDb.Count += shoppingCart.Count;
                _unitOfWork.Save();

                return RedirectToAction(nameof(Index));

            }
            else
            {
                var productInDb = _unitOfWork.ProductRepository.FirstOrDefault(p => p.Id == shoppingCart.Id,
                includeProperties: "Category,coverType");
                if (productInDb == null) return NotFound();
                var shoppingCartEdit = new ShoppingCart()
                {
                    Product = productInDb,
                    ProductId = shoppingCart.Id,
                };

                return View(shoppingCartEdit);
            }

        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
