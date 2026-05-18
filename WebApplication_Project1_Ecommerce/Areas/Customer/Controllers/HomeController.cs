using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            RefreshCartSessionCount();

            var productList = _unitOfWork.ProductRepository.GetAll().ToList();

            var soldCounts = _unitOfWork.OrderDetailRepository
                .GetAll(includeProperties: "OrderHeader")
                .Where(od => od.OrderHeader != null &&
                    od.OrderHeader.PaymentStatus == SD.PaymentStatusApproved &&
                    od.OrderHeader.OrderStatus != SD.OrderStatusCancelled &&
                    od.OrderHeader.OrderStatus != SD.OrderStatusRefunded)
                .GroupBy(od => od.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(od => od.Count));

            ViewBag.SoldCounts = soldCounts;
            ViewBag.PopularProductIds = soldCounts
                .OrderByDescending(sc => sc.Value)
                .Take(4)
                .Select(sc => sc.Key)
                .ToHashSet();

            return View(productList);
        }

        
        public IActionResult Details(int id)
        {
            RefreshCartSessionCount();

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
                var productInDb = _unitOfWork.ProductRepository.FirstOrDefault(p => p.Id == shoppingCart.ProductId,
                includeProperties: "Category,coverType");
                if (productInDb == null) return NotFound();
                var shoppingCartEdit = new ShoppingCart()
                {
                    Product = productInDb,
                    ProductId = shoppingCart.ProductId,
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

        private void RefreshCartSessionCount()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var claims = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier);

            if (claims == null)
            {
                return;
            }

            var count = _unitOfWork.ShoppingCartRepository
                .GetAll(sc => sc.ApplicationUserId == claims.Value)
                .Count();

            HttpContext.Session.SetInt32(SD.Ss_CartSessionCount, count);
        }
    }
}
