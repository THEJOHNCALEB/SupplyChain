using SupplyChain.Data;
using SupplyChain.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Win32;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.FileProviders;
using SharpCompress.Common;
using DnsClient;
using SupplyChain.Processor;

namespace SupplyChain.Controllers
{
    public class LoginPageController : Controller
    {
        private readonly ApplicationDbContext _appDataAccess;

        public LoginPageController(ApplicationDbContext appdb)
        {
            _appDataAccess = appdb;
        }

        [HttpGet, AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Dashboard");

            return View();

        }
      

        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> Login(LoginPageModel login)
        {
            if (string.IsNullOrWhiteSpace(login.Email))
            {
                ViewBag.message = "Email is null!";
            }

            if (string.IsNullOrWhiteSpace(login.Password))
            {
                ViewBag.message = "Password is null!";
            }

            login.Email = login.Email.ToLowerInvariant();
            var checkUser = _appDataAccess
                .QueryOne(c => c.Email == login.Email);
            if (checkUser == null)
            {
                ViewBag.ErrorMessage = "User does not exist, Please Sign Up!";
                return View();
            }

            if (!BCrypt.Net.BCrypt.EnhancedVerify(login.Password,
                    checkUser.Password))
            {
                ViewBag.ErrorMessage = "user's Password is wrong!";
                return View();
            }

            List<Claim> claims = new() {
                new Claim(ClaimTypes.Email, checkUser.Email),
                new Claim("FirstName", checkUser.FirstName),
                new Claim("Surname", checkUser.Surname),


            };
            foreach (var role in checkUser.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            ClaimsIdentity claimIdentity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal claimPrincipal = new(claimIdentity);
            await HttpContext.SignInAsync(claimPrincipal);
            return RedirectToAction("Index", "Dashboard");            
        }

        [AllowAnonymous]
        public IActionResult Register()
        {

            return View();
        }
        [AllowAnonymous]
        public IActionResult Password()
        {

            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register(LoginPageModel register, string confirmPassword, CancellationToken ctl, IFormFile files)
        {
            if (string.IsNullOrWhiteSpace(register.Password))
            {
                ViewBag.ErrorMessage = "Password cannot be empty!";
                return View();
            }

            if (string.IsNullOrWhiteSpace(register.Surname) ||
                string.IsNullOrWhiteSpace(register.FirstName) ||
                string.IsNullOrWhiteSpace(register.TelephoneNumber))
            {
                ViewBag.ErrorMessage = "Surname, Firstname, Telephone " +
                                  "number cannot be empty!";
                return View();
            }

            if (register.Password != confirmPassword)
            {
                ViewBag.ErrorMessage = "Confirm Password and Password must be the same!";
                return View();
            }

            if (string.IsNullOrWhiteSpace(register.Username))
            {
                ViewBag.ErrorMessage = "Username Cannot Be Empty!";
                return View();
            }
            if (string.IsNullOrWhiteSpace(register.UserRoleString))
            {
                ViewBag.ErrorMessage = "Username Cannot Be Empty!";
                return View();
            }

            if (!Functions.IsEmailValid(register.Email))
            {
                ViewBag.ErrorMessage = "This Email is not valid!";
                return View();
            } 
            if (files != null)
                {
            register.Email = register.Email.ToLowerInvariant();
            var checkIfUserExist = _appDataAccess
                            .QueryOne(p => p.Email == register.Email);
            if (checkIfUserExist == null)
            {
                //var paystackApiResponse = await PostToPaystackApi(register.Id, register.Email, register.FirstName, register.Surname, register.TelephoneNumber);

                //// Check if the Paystack API request was successful
                //if (paystackApiResponse.IsSuccessStatusCode)
                //{
               
                    if (files.Length > 0)
                    {
                        var fileName = Path.GetFileName(files.FileName);
                        var myUniqueFileName = Convert.ToString(Guid.NewGuid());
                        var fileExtension = Path.GetExtension(fileName);
                        var newFileName = String.Concat(myUniqueFileName, fileExtension);

                        // Constructing the full file path
                        var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "UserImages");
                        var filepath = Path.Combine(directoryPath, newFileName);

                        // Creating the directory if it doesn't exist
                        Directory.CreateDirectory(directoryPath);

                        // Saving the file
                        using (FileStream fs = System.IO.File.Create(filepath))
                        {
                            files.CopyTo(fs);
                            fs.Flush();
                        }
                        register.Id = Guid.NewGuid();
                        register.IsActive = true;
                        register.RegistrationDate = DateTime.Now;
                        register.Password = BCrypt.Net.BCrypt.EnhancedHashPassword(register.Password);
                        register.Balance = 0;
                        register.Image = newFileName.ToString();
                        register.Username = register.Username;
                        register.UserRoleString = register.UserRoleString;


                        if (register.UserRoleString == "CUSTOMER")
                        {
                            register.Roles = new List<string>
                    {
                    UsersRoles.Cart, UsersRoles.Chat, UsersRoles.Checkout, UsersRoles.Item, 
                    };
                        }
                        else if (register.UserRoleString == "RETAILER")
                        {
                            register.Roles = new List<string>
                    {
                    UsersRoles.Cart, UsersRoles.Chat, UsersRoles.Item, UsersRoles.Orders,
                    };
                        }
                        else if (register.UserRoleString == "DISTRIBUTOR")
                        {
                            register.Roles = new List<string>
                    {
                    UsersRoles.Cart, UsersRoles.Chat, UsersRoles.Item,
                    };
                        }
                        else if (register.UserRoleString == "FARMER")
                        {
                            register.Roles = new List<string>
                    {
                   UsersRoles.Chat, UsersRoles.Item, UsersRoles.Orders, UsersRoles.AddItem, UsersRoles.ViewItem,
                    };
                        }
                        else
                        {
                            register.Roles = new List<string>
                    {
                    UsersRoles.Cart, UsersRoles.Chat, UsersRoles.Checkout, UsersRoles.Item
                    };
                        }


                        _appDataAccess.Insert(register);
                        ViewBag.ErrorMessage = "User is added successfully";
                        //var newOtp = new OtpModel
                        //{
                        //    Id = Guid.NewGuid(),
                        //    Expired = DateTime.Now.AddMinutes(0),
                        //    ReceiverEmail = register.Email
                        //};

                        ////send email
                        //await SendNewEmail(newOtp, ctl);

                        var checkUser = _appDataAccess
                            .QueryOne(c => c.Email == register.Email);
                        List<Claim> claims = new() {
                new Claim(ClaimTypes.Email, checkUser.Email),
                new Claim("FirstName", checkUser.FirstName),
                new Claim("Surname", checkUser.Surname),


            };
                        foreach (var role in checkUser.Roles)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, role));
                        }

                        ClaimsIdentity claimIdentity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        ClaimsPrincipal claimPrincipal = new(claimIdentity);
                        await HttpContext.SignInAsync(claimPrincipal);
                        return RedirectToAction("Index", "Dashboard");

                        //}
                        //else
                        //{
                        //    // Handle the Paystack API error response
                        //    ViewBag.ErrorMessage = $"Failed to create Paystack account. Status code: {paystackApiResponse.StatusCode}";
                        //    return View();
                        //}
                    }
                    ViewBag.ErrorMessage = "No Image Uploaded!";
                    return View();

                }
                ViewBag.ErrorMessage = "User Already Exist";
                return View();
            }
            return View();
        }
        

        [AllowAnonymous]
        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login", "LoginPage");
        }
    }
}
