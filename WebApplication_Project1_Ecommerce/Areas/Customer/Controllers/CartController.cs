using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Stripe;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using WebApplication_Project1_Ecommerce.DataAccess.Repository.IRepository;
using WebApplication_Project1_Ecommerce.Models;
using WebApplication_Project1_Ecommerce.Models.ViewModels;
using WebApplication_Project1_Ecommerce.Utility;
using Stripe.Checkout;

namespace WebApplication_Project1_Ecommerce.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;

        public CartController(
            IUnitOfWork unitOfWork,
            IEmailSender emailSender,
            UserManager<IdentityUser> userManager,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _userManager = userManager;
            _configuration = configuration;
        }

        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (claims == null)
            {
                ShoppingCartVM = new ShoppingCartVM()
                {
                    ListCart = new List<ShoppingCart>(),
                    OrderHeader = new OrderHeader()
                };
                return View(ShoppingCartVM);
            }

            var cartItems = _unitOfWork.ShoppingCartRepository
                .GetAll(sc => sc.ApplicationUserId == claims.Value, includeProperties: "Product")
                .ToList();

            HttpContext.Session.SetInt32(SD.Ss_CartSessionCount, cartItems.Count);

            ShoppingCartVM = new ShoppingCartVM()
            {
                ListCart = cartItems,
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
                    list.Product.Price100);

                ShoppingCartVM.OrderHeader.OrderTotal += list.Price * list.Count;
            }

            if (ShoppingCartVM.OrderHeader.ApplicationUser?.EmailConfirmed == true)
            {
                ViewBag.EmailMessage = "";
                ViewBag.EmailCSS = "text-success";
            }
            else
            {
                ViewBag.EmailMessage = "Email must be confirmed before checkout.";
                ViewBag.EmailCSS = "text-danger";
            }

            return View(ShoppingCartVM);
        }

        [HttpPost]
        [ActionName("Index")]
        public async Task<IActionResult> IndexPost()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            if (user.EmailConfirmed)
            {
                return RedirectToAction(nameof(Index));
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                null,
                new { area = "Identity", userId = user.Id, code },
                Request.Scheme);

            await _emailSender.SendEmailAsync(
                user.Email,
                "Confirm your email",
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

            TempData["Success"] = "Verification email sent.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult summary(List<int> selectedItems)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (claims == null) return NotFound();

            if (selectedItems == null || !selectedItems.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            ShoppingCartVM = BuildSelectedCart(claims.Value, selectedItems);
            PopulateOrderFromUser(ShoppingCartVM.OrderHeader, claims.Value);
            PopulateSavedAddresses(claims.Value);

            return View(ShoppingCartVM);
        }

        public IActionResult Plus(int id)
        {
            var cart = _unitOfWork.ShoppingCartRepository.Get(id);
            if (cart == null) return NotFound();

            cart.Count += 1;
            _unitOfWork.ShoppingCartRepository.Update(cart);
            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int id)
        {
            var cart = _unitOfWork.ShoppingCartRepository.Get(id);
            if (cart == null) return NotFound();

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
            if (cart == null) return NotFound();

            _unitOfWork.ShoppingCartRepository.Remove(cart);
            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        [ValidateAntiForgeryToken]
        [ActionName("summary")]
        [HttpPost]
        public IActionResult SummaryPost(List<int> selectedItems)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claims == null)
                return NotFound();

            if (selectedItems == null || !selectedItems.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            var postedOrderHeader = ShoppingCartVM?.OrderHeader;

            // Build selected cart
            ShoppingCartVM = BuildSelectedCart(claims.Value, selectedItems);

            // Fill user details
            PopulateOrderFromUser(ShoppingCartVM.OrderHeader, claims.Value);

            // Apply posted address details
            ApplyPostedOrderDetails(
                ShoppingCartVM.OrderHeader,
                postedOrderHeader
            );

            // Order initial setup
            ShoppingCartVM.OrderHeader.OrderStatus = SD.OrderStatusPending;
            ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
            ShoppingCartVM.OrderHeader.ApplicationUserId = claims.Value;

            // Save Order Header
            _unitOfWork.OrderHeaderRepository.Add(
                ShoppingCartVM.OrderHeader
            );
            _unitOfWork.Save();

            // Save Order Details
            foreach (var list in ShoppingCartVM.ListCart)
            {
                OrderDetail orderDetail = new OrderDetail()
                {
                    OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                    ProductId = list.ProductId,
                    Price = list.Price,
                    Count = list.Count
                };

                _unitOfWork.OrderDetailRepository.Add(orderDetail);
            }

            _unitOfWork.Save();

            var domain = $"{Request.Scheme}://{Request.Host.Value}/";

            var options = new Stripe.Checkout.SessionCreateOptions
            {
                SuccessUrl = domain +
                             $"Customer/Cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",

                CancelUrl = domain +
                            "Customer/Cart/Summary",

                Mode = "payment",

                LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
        {
            new Stripe.Checkout.SessionLineItemOptions
            {
                PriceData =
                    new Stripe.Checkout.SessionLineItemPriceDataOptions
                    {
                        UnitAmount =
                            (long)(ShoppingCartVM.OrderHeader.OrderTotal * 100),

                        Currency = "usd",

                        ProductData =
                            new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "BookHive Order"
                            }
                    },

                Quantity = 1
            }
        }
            };

            var service = new Stripe.Checkout.SessionService();
            Stripe.Checkout.Session session = service.Create(options);

            var stripeService = new Stripe.Checkout.SessionService();
            var stripeSession = stripeService.Create(options);

            Response.Headers.Add("Location", stripeSession.Url);

            return new StatusCodeResult(303);

            Response.Headers.Add("Location", session.Url);

            return new StatusCodeResult(303);

            // Redirect to Stripe Hosted Checkout Page
            Response.Headers.Add("Location", session.Url);

            return new StatusCodeResult(303);
        }

        public IActionResult OrderConfirmation(int id)
        {
            var orderHeader = _unitOfWork.OrderHeaderRepository
                .FirstOrDefault(u => u.Id == id, includeProperties: "ApplicationUser");

            if (orderHeader == null)
            {
                return NotFound();
            }

            var orderDetails = _unitOfWork.OrderDetailRepository
                .GetAll(u => u.OrderHeaderId == id, includeProperties: "Product")
                .ToList();

            var cartItems = orderDetails.Select(detail => new ShoppingCart
            {
                Product = detail.Product,
                Count = detail.Count,
                Price = detail.Price
            }).ToList();

            // Prevent duplicate SMS/call if page refreshed
            if (orderHeader.PaymentStatus != SD.PaymentStatusApproved)
            {
                orderHeader.PaymentStatus = SD.PaymentStatusApproved;
                orderHeader.OrderStatus = SD.OrderStatusApproved;

                _unitOfWork.OrderHeaderRepository.Update(orderHeader);
                _unitOfWork.Save();

                SendOrderNotifications(orderHeader);
                SendInvoiceEmail(orderHeader, cartItems);
            }

            return View(id);
        }

        [AllowAnonymous]
        public IActionResult OrderVoiceMessage(int id)
        {
            var message =
                $"Your BookHive order number {id} has been confirmed and payment was successful. Thank you for shopping with us.";

            return Content(
                $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><Response><Say voice=\"alice\">{HtmlEncoder.Default.Encode(message)}</Say></Response>",
                "text/xml");
        }

        private ShoppingCartVM BuildSelectedCart(string userId, List<int> selectedItems)
        {
            var shoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _unitOfWork.ShoppingCartRepository
                    .GetAll(sc => sc.ApplicationUserId == userId, includeProperties: "Product")
                    .Where(sc => selectedItems.Contains(sc.Id))
                    .ToList(),
                OrderHeader = new OrderHeader()
            };

            shoppingCartVM.OrderHeader.OrderTotal = 0;

            foreach (var list in shoppingCartVM.ListCart)
            {
                list.Price = SD.GetPriceBasedOnQuantity(
                    list.Count,
                    list.Product.Price,
                    list.Product.Price50,
                    list.Product.Price100);

                shoppingCartVM.OrderHeader.OrderTotal += list.Price * list.Count;
            }

            return shoppingCartVM;
        }

        private void PopulateOrderFromUser(OrderHeader orderHeader, string userId)
        {
            orderHeader.ApplicationUser =
                _unitOfWork.ApplicationUserRepository.FirstOrDefault(au => au.Id == userId);

            orderHeader.Name = orderHeader.ApplicationUser.Name;
            orderHeader.PhoneNumber = orderHeader.ApplicationUser.PhoneNumber;
            orderHeader.StreetAddress = orderHeader.ApplicationUser.StreetAddress;
            orderHeader.City = orderHeader.ApplicationUser.City;
            orderHeader.State = orderHeader.ApplicationUser.State;
            orderHeader.PostalCode = orderHeader.ApplicationUser.PostalCode;
        }

        private void PopulateSavedAddresses(string userId)
        {
            var addresses = new List<string>();
            var user = _unitOfWork.ApplicationUserRepository.FirstOrDefault(au => au.Id == userId);

            AddAddress(addresses, user?.StreetAddress, user?.City, user?.State, user?.PostalCode);

            var previousOrders = _unitOfWork.OrderHeaderRepository
                .GetAll(o => o.ApplicationUserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Take(8);

            foreach (var order in previousOrders)
            {
                AddAddress(addresses, order.StreetAddress, order.City, order.State, order.PostalCode);
            }

            ViewBag.SavedAddresses = addresses.Distinct().ToList();
        }

        private void AddAddress(List<string> addresses, string street, string city, string state, string postalCode)
        {
            if (string.IsNullOrWhiteSpace(street))
            {
                return;
            }

            addresses.Add($"{street}|{city}|{state}|{postalCode}");
        }

        private void ApplyPostedOrderDetails(OrderHeader orderHeader, OrderHeader postedOrderHeader)
        {
            if (postedOrderHeader == null)
            {
                return;
            }

            orderHeader.Name = postedOrderHeader.Name;
            orderHeader.PhoneNumber = postedOrderHeader.PhoneNumber;
            orderHeader.StreetAddress = postedOrderHeader.StreetAddress;
            orderHeader.City = postedOrderHeader.City;
            orderHeader.State = postedOrderHeader.State;
            orderHeader.PostalCode = postedOrderHeader.PostalCode;
        }

        private void SendOrderNotifications(OrderHeader orderHeader)
        {
            var accountSid = _configuration["Twilio:AccountSid"];
            var authToken = _configuration["Twilio:AuthToken"];
            var twilioPhoneNumber = _configuration["Twilio:PhoneNumber"];
            var customerPhoneNumber = NormalizePhoneNumber(orderHeader.PhoneNumber);

            if (string.IsNullOrWhiteSpace(accountSid) ||
                string.IsNullOrWhiteSpace(authToken) ||
                string.IsNullOrWhiteSpace(twilioPhoneNumber) ||
                string.IsNullOrWhiteSpace(customerPhoneNumber))
            {
                TempData["Warning"] = "Order placed, but the customer phone number must include a valid country code, for example +91797348XXXX.";
                return;
            }


            try
            {
                TwilioClient.Init(accountSid, authToken);

                var sms = MessageResource.Create(
                   body: $"Hello {orderHeader.Name}, your BookHive Order #{orderHeader.Id} is confirmed and payment was successful. Thank you for shopping with us!",
                   from: new PhoneNumber(twilioPhoneNumber),
                   to: new PhoneNumber(customerPhoneNumber));

                var voiceMessage =
     $"Hello {orderHeader.Name}. Your order number {orderHeader.Id} has been confirmed successfully. Your payment was received and your books will be delivered soon. Thank you for choosing BookHive.";
                var twiml =
                    $"<Response><Say voice=\"Polly.Aditi\">{HtmlEncoder.Default.Encode(voiceMessage)}</Say></Response>";

                var call = CallResource.Create(
                    to: new PhoneNumber(customerPhoneNumber),
                    from: new PhoneNumber(twilioPhoneNumber),
                    twiml: new Twiml(twiml));

                TempData["Success"] = $"Twilio SMS and call were queued. SMS: {sms.Sid}, Call: {call.Sid}";
            }
            catch (Exception ex)
            {
                TempData["Warning"] = $"Order placed, but Twilio failed: {ex.Message}";
            }
        }

        private void SendInvoiceEmail(OrderHeader orderHeader, IEnumerable<ShoppingCart> cartItems)
        {
            var email = orderHeader.ApplicationUser?.Email;

            if (string.IsNullOrWhiteSpace(email))
            {
                return;
            }

            var rows = new StringBuilder();

            foreach (var item in cartItems)
            {
                rows.Append($@"
                    <tr>
                        <td style='padding:10px;border-bottom:1px solid #e5e7eb;'>{HtmlEncoder.Default.Encode(item.Product.Title)}</td>
                        <td style='padding:10px;border-bottom:1px solid #e5e7eb;text-align:center;'>{item.Count}</td>
                        <td style='padding:10px;border-bottom:1px solid #e5e7eb;text-align:right;'>${item.Price:0.00}</td>
                        <td style='padding:10px;border-bottom:1px solid #e5e7eb;text-align:right;'>${item.Price * item.Count:0.00}</td>
                    </tr>");
            }

            var invoice = $@"
                <div style='font-family:Segoe UI,Arial,sans-serif;background:#f6f7fb;padding:24px;color:#172033;'>
                    <div style='max-width:720px;margin:auto;background:#ffffff;border-radius:12px;overflow:hidden;border:1px solid #e5e7eb;'>
                        <div style='background:#111827;color:#ffffff;padding:24px;'>
                            <h1 style='margin:0;'>BookHive Invoice</h1>
                            <p style='margin:6px 0 0;'>Order #{orderHeader.Id} confirmed on {orderHeader.OrderDate:dd MMM yyyy}</p>
                        </div>
                        <div style='padding:24px;'>
                            <p><strong>Customer:</strong> {HtmlEncoder.Default.Encode(orderHeader.Name)}</p>
                            <p><strong>Ship to:</strong> {HtmlEncoder.Default.Encode(orderHeader.StreetAddress)}, {HtmlEncoder.Default.Encode(orderHeader.City)}, {HtmlEncoder.Default.Encode(orderHeader.State)} - {HtmlEncoder.Default.Encode(orderHeader.PostalCode)}</p>
                            <p><strong>Payment:</strong> {orderHeader.PaymentStatus}</p>
                            <table style='width:100%;border-collapse:collapse;margin-top:20px;'>
                                <thead>
                                    <tr style='background:#f3f4f6;'>
                                        <th style='padding:10px;text-align:left;'>Book</th>
                                        <th style='padding:10px;text-align:center;'>Qty</th>
                                        <th style='padding:10px;text-align:right;'>Price</th>
                                        <th style='padding:10px;text-align:right;'>Total</th>
                                    </tr>
                                </thead>
                                <tbody>{rows}</tbody>
                            </table>
                            <h2 style='text-align:right;margin-top:20px;'>Grand Total: ${orderHeader.OrderTotal:0.00}</h2>
                        </div>
                    </div>
                </div>";

            _emailSender.SendEmailAsync(email, $"Invoice for Order #{orderHeader.Id}", invoice);
        }

        private string NormalizePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return null;
            }

            var trimmedPhoneNumber = phoneNumber.Trim();

            if (trimmedPhoneNumber.StartsWith("+"))
            {
                return "+" + new string(trimmedPhoneNumber.Skip(1).Where(char.IsDigit).ToArray());
            }

            var digitsOnly = new string(trimmedPhoneNumber.Where(char.IsDigit).ToArray());

            if (digitsOnly.Length == 10)
            {
                return $"+91{digitsOnly}";
            }

            if (digitsOnly.Length > 10)
            {
                return $"+{digitsOnly}";
            }

            return null;
        }
    }
}
