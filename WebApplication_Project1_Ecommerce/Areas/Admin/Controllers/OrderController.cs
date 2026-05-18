using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_Project1_Ecommerce.DataAccess.Repository.IRepository;
using WebApplication_Project1_Ecommerce.Models.ViewModels;
using WebApplication_Project1_Ecommerce.Utility;

namespace WebApplication_Project1_Ecommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index(string month)
        {
            var orders = _unitOfWork.OrderHeaderRepository
                .GetAll(includeProperties: "ApplicationUser")
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            if (!string.IsNullOrWhiteSpace(month) &&
                DateTime.TryParse($"{month}-01", out var selectedMonth))
            {
                orders = orders
                    .Where(o => o.OrderDate.Year == selectedMonth.Year &&
                                o.OrderDate.Month == selectedMonth.Month)
                    .ToList();
            }

            return View(new OrderManagementVM
            {
                OrderHeaders = orders,
                SelectedMonth = month
            });
        }

        public IActionResult Details(int id)
        {
            var orderHeader = _unitOfWork.OrderHeaderRepository
                .FirstOrDefault(o => o.Id == id, includeProperties: "ApplicationUser");

            if (orderHeader == null)
            {
                return NotFound();
            }

            ViewBag.OrderDetails = _unitOfWork.OrderDetailRepository
                .GetAll(od => od.OrderHeaderId == id, includeProperties: "Product")
                .ToList();

            return View(orderHeader);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(int id)
        {
            var orderHeader = _unitOfWork.OrderHeaderRepository.Get(id);

            if (orderHeader == null)
            {
                return NotFound();
            }

            if (orderHeader.OrderStatus != SD.OrderStatusShipped &&
                orderHeader.OrderStatus != SD.OrderStatusCancelled)
            {
                orderHeader.OrderStatus = SD.OrderStatusCancelled;
                orderHeader.PaymentStatus =
                    orderHeader.PaymentStatus == SD.PaymentStatusApproved
                        ? SD.OrderStatusRefunded
                        : SD.PaymentStatusRejected;

                _unitOfWork.OrderHeaderRepository.Update(orderHeader);
                _unitOfWork.Save();
                TempData["Success"] = $"Order #{id} has been cancelled.";
            }
            else
            {
                TempData["Warning"] = "This order cannot be cancelled from its current status.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
