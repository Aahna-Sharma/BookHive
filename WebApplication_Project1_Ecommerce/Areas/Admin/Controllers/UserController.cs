using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_Project1_Ecommerce.DataAccess.Data;
using WebApplication_Project1_Ecommerce.DataAccess.Repository.IRepository;
using WebApplication_Project1_Ecommerce.Models;
using WebApplication_Project1_Ecommerce.Utility;

namespace WebApplication_Project1_Ecommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly IUnitOfWork _unitOfwork;
        private readonly ApplicationDbContext _context;
        public UserController(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _context=context;
            _unitOfwork=unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }

        #region APIs

        [HttpGet]
        public IActionResult GetAll()
        {
            var userList = _context.ApplicationUsers.ToList();
            //aspnetusers(gives user data)
            var roleList = _context.Roles.ToList(); //name of user...aspnetroles
            var userRole = _context.UserRoles.ToList(); //roles assigned to user...aspnetuserroles

            foreach(var user in userList)
            {
                var roleId = userRole.FirstOrDefault(u => u.UserId == user.Id).RoleId;
                //FETCHES THE ROLE ID CORRESPONDING TO THE USERID
                user.Role = roleList.FirstOrDefault(r => r.Id == roleId).Name;
                //FETCHES THE NAME OF ROLE CORRESSPONDING TO ROLE ID FROM ROLE LIST
                if (user.CompanyId == null)
                {
                    user.Company = new Company()
                    {
                        Name = " "
                        //Inserting empty string for company name
                        //When company id Is null So it does not show error 
                    };

                }
                if (user.CompanyId != null)
                {
                    user.Company = new Company() { 
                     Name=_unitOfwork.CompanyRepository.Get(Convert.ToInt32(user.CompanyId)).Name
                    };

                }
            }
           
            var adminUser = userList.FirstOrDefault(u => u.Role == SD.Role_Admin);
            userList.Remove(adminUser);
            return Json(new {data=userList});
        }

        [HttpPost]
        public IActionResult LockUnlock([FromBody]string id)
        {
            bool isLocked = false;
            var userInDb = _unitOfwork.ApplicationUserRepository.FirstOrDefault(u => u.Id == id);
            if(userInDb == null)
            {
                return Json(new {success =false, message = "Something went wrong"});
            }
            if (userInDb != null && userInDb.LockoutEnd > DateTime.Now)
            {
                userInDb.LockoutEnd = DateTime.Now;
                isLocked = false;
            }
            else
            {
                userInDb.LockoutEnd = DateTime.Now.AddYears(100);
                isLocked = true;
            }
            _context.SaveChanges();
            return Json(new { success = true, message = isLocked == true ? "User successfully Locked" : "User successfully Unlocked" });
        }

        #endregion
    }
}
