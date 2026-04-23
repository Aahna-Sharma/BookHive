using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.V2;
using System.Security.Claims;
using WebApplication_Project1_Ecommerce.DataAccess.Repository.IRepository;
using WebApplication_Project1_Ecommerce.Models;
using WebApplication_Project1_Ecommerce.Models.ViewModels;
using WebApplication_Project1_Ecommerce.Utility;

namespace WebApplication_Project1_Ecommerce.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (claims == null) //IF USER IS NOT LOGGED IN
            {
                ShoppingCartVM = new ShoppingCartVM()
                {
                    ListCart = new List<ShoppingCart>()     //CREATES EMPTY LIST
                };
                return View(ShoppingCartVM);
            }

            var count = _unitOfWork.ShoppingCartRepository.GetAll(sc => sc.ApplicationUserId == claims.Value).ToList().Count;
            HttpContext.Session.SetInt32(SD.Ss_CartSessionCount, count);

            ShoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _unitOfWork.ShoppingCartRepository.GetAll(sc => sc.ApplicationUserId == claims.Value, includeProperties: "Product"),
                OrderHeader = new OrderHeader(),
                //GET DATA OF USER AND PRODUCT FROM SHOPPING CART TABLE
            };

            ShoppingCartVM.OrderHeader.OrderTotal = 0;
            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUserRepository.FirstOrDefault(au => au.Id == claims.Value);
            foreach (var list in ShoppingCartVM.ListCart)
            {
                list.Price = SD.GetPriceBasedOnQuantity(list.Count, list.Product.Price, list.Product.Price50, list.Product.Price100);
                ShoppingCartVM.OrderHeader.OrderTotal += (list.Price * list.Count);
                //if (list.Product.Description.Length >150)
                //{
                // list.Product.Description = list.Product.Description.Substring(0, 149);
                //}
            }
            return View(ShoppingCartVM);
        }

        public IActionResult summary(List<int> selectedItems)
        {

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            // ❌ If nothing selected → go back
            if (selectedItems == null || !selectedItems.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            ShoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _unitOfWork.ShoppingCartRepository
                    .GetAll(sc => sc.ApplicationUserId == claims.Value, includeProperties: "Product")
                    .Where(sc => selectedItems.Contains(sc.Id)),

                OrderHeader = new OrderHeader()
            };

            ShoppingCartVM.OrderHeader.ApplicationUser =
                _unitOfWork.ApplicationUserRepository.FirstOrDefault(au => au.Id == claims.Value);

            ShoppingCartVM.OrderHeader.OrderTotal = 0;

            foreach (var list in ShoppingCartVM.ListCart)
            {
                list.Price = SD.GetPriceBasedOnQuantity(
                    list.Count,
                    list.Product.Price,
                    list.Product.Price50,
                    list.Product.Price100
                );

                ShoppingCartVM.OrderHeader.OrderTotal += (list.Price * list.Count);
            }

            return View(ShoppingCartVM);
        }
        public IActionResult Plus(int id)
        {
            var cart = _unitOfWork.ShoppingCartRepository.Get(id);

            cart.Count += 1;

            _unitOfWork.ShoppingCartRepository.Update(cart);
            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }
        public IActionResult Minus(int id)
        {
            var cart = _unitOfWork.ShoppingCartRepository.Get(id);

            if (cart.Count > 1)
            {
                cart.Count -= 1;
                _unitOfWork.ShoppingCartRepository.Update(cart);
            }
            else
            {
                _unitOfWork.ShoppingCartRepository.Remove(cart);
            }

            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult delete(int id)
        {
            var cart = _unitOfWork.ShoppingCartRepository.Get(id);
            _unitOfWork.ShoppingCartRepository.Remove(cart);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("summary")]
        public IActionResult SummaryPost(List<int> selectedItems, string stripeToken)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (claims == null) return NotFound();

            if (selectedItems == null || !selectedItems.Any())
            {
                return RedirectToAction(nameof(Index));
            }
            if (ShoppingCartVM.OrderHeader == null)
            {
                ShoppingCartVM.OrderHeader = new OrderHeader();
            }
            ShoppingCartVM.ListCart = _unitOfWork.ShoppingCartRepository
                .GetAll(sc => sc.ApplicationUserId == claims.Value, includeProperties: "Product")
                .Where(sc => selectedItems.Contains(sc.Id))
                .ToList();
            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUserRepository.FirstOrDefault(au => au.Id == claims.Value);
            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
            ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;
            ShoppingCartVM.OrderHeader.OrderStatus = SD.OrderStatusPending;
            ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
            ShoppingCartVM.OrderHeader.ApplicationUserId = claims.Value;

            _unitOfWork.OrderHeaderRepository.Add(ShoppingCartVM.OrderHeader);
            _unitOfWork.Save();
            ShoppingCartVM.OrderHeader.OrderTotal = 0;
            foreach (var list in ShoppingCartVM.ListCart)
            {
                list.Price = SD.GetPriceBasedOnQuantity(
                    list.Count,
                    list.Product.Price,
                    list.Product.Price50,
                    list.Product.Price100
                );

                ShoppingCartVM.OrderHeader.OrderTotal += (list.Price * list.Count);

                OrderDetail orderDetail = new OrderDetail()
                {
                    OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                    ProductId = list.ProductId,
                    Price = list.Price,
                    Count = list.Count,
                };

                _unitOfWork.OrderDetailRepository.Add(orderDetail);
            }

            // 🔥 Save ALL order details together
            _unitOfWork.Save();
            foreach (var list in ShoppingCartVM.ListCart)
            {
                _unitOfWork.ShoppingCartRepository.Remove(list);
            }
            _unitOfWork.Save();
            //SESSION COUNT
            var count = _unitOfWork.ShoppingCartRepository
     .GetAll(sc => sc.ApplicationUserId == claims.Value)
     .Count();

            HttpContext.Session.SetInt32(SD.Ss_CartSessionCount, count);

            if (stripeToken == null)
            {
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayPending;
                ShoppingCartVM.OrderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
                ShoppingCartVM.OrderHeader.OrderStatus = SD.OrderStatusApproved;

            }
            else
            {
                var options = new ChargeCreateOptions()
                {
                    Amount = Convert.ToInt32(ShoppingCartVM.OrderHeader.OrderTotal * 100),
                    Currency = "usd",
                    Description = "OrderId: " + ShoppingCartVM.OrderHeader.Id.ToString(),
                    Source = stripeToken,

                };
                var service = new ChargeService();
                Charge charge = service.Create(options);
                if (charge.BalanceTransactionId == null)
                {
                    ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
                }
                else
                {
                    ShoppingCartVM.OrderHeader.TransactionId = charge.BalanceTransactionId;
                }
                if (charge.Status.ToLower() == "succeeded")
                {
                    ShoppingCartVM.OrderHeader.OrderStatus = SD.OrderStatusApproved;
                    ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusApproved;
                    ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
                }
                _unitOfWork.Save();
            }

            return RedirectToAction("OrderConfirmation", "Cart", new { id = ShoppingCartVM.OrderHeader.Id });

        }

        public IActionResult OrderConfirmation(int id)
        {
            return View();
        }
    }
}

