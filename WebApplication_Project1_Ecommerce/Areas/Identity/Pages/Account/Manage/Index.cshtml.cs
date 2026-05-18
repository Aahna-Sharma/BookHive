// FINAL CORRECT CODE FOR YOUR PROJECT
// Since you already have address fields inside ApplicationUser
// and shipping info inside OrderHeader,
// you DO NOT need UserAddress model at all.

// Use THIS Index.cshtml.cs

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication_Project1_Ecommerce.Models;
using WebApplication_Project1_Ecommerce.DataAccess.Data;

namespace WebApplication_Project1_Ecommerce.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _db;

        public IndexModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
        }

        public string Username { get; set; }

        // REAL ORDER HISTORY
        public List<OrderHeader> OrderHistory { get; set; } = new();

        // SAVED ADDRESS FROM ApplicationUser
        public ApplicationUser UserProfile { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public string PhoneNumber { get; set; }
        }

        private async Task LoadAsync(IdentityUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber
            };

            // ORDER HISTORY FROM DATABASE
            OrderHistory = await _db.OrderHeaders
                .Where(u => u.ApplicationUserId == user.Id)
                .OrderByDescending(u => u.OrderDate)
                .Take(10)
                .ToListAsync();

            // USER PROFILE + SAVED ADDRESS
            UserProfile = await _db.ApplicationUsers
                .FirstOrDefaultAsync(u => u.Id == user.Id);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound("Unable to load user.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound("Unable to load user.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            if (Input.PhoneNumber != phoneNumber)
            {
                var result = await _userManager
                    .SetPhoneNumberAsync(user, Input.PhoneNumber);

                if (!result.Succeeded)
                {
                    StatusMessage = "Error updating phone number.";
                    return RedirectToPage();
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";

            return RedirectToPage();
        }
    }
}