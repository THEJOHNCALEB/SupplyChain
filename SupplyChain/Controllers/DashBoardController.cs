using SupplyChain.Data;
using SupplyChain.Models;
using SupplyChain.Processor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Win32;
using System.Text;
using Amazon.Runtime;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System;
using static MongoDB.Driver.WriteConcern;
using System.Numerics;
using System.Transactions;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace SupplyChain.Controllers
{

  
    public class DashBoardController : Controller
    {

        private readonly ApplicationDbContext _userDataAccess;
        private readonly ItemsDbContext _itemDataAccess;
        private readonly CartDbContext _cartDataAccess;
        private readonly OrderDbContext _orderDataAccess;
        //private readonly IHttpClientFactory _httpClientFactory; _httpClientFactory = httpClientFactory;, IHttpClientFactory httpClientFactory
        public DashBoardController(ApplicationDbContext userdb, ItemsDbContext itemdb, CartDbContext cartdb, OrderDbContext orderdb)
        {
            _userDataAccess = userdb;
            _itemDataAccess = itemdb;
            _cartDataAccess = cartdb;
            _orderDataAccess = orderdb;
        }

        [Authorize]
        public IActionResult Index()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "LoginPage");

            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            var user = _userDataAccess.QueryOne(c => c.Email == email);

            return View(user);
        }

        [Authorize(Roles = UsersRoles.Item)]
        public IActionResult Item()
        {
            return View();
        } 
        
        [HttpPost]
        public string AddToCart(CartModel drug, Guid Identification)
        {
            try
            {
                // Retrieve the user's email from claims
                var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

                // Check if the user exists in the database
                var checkIfUserExist = _userDataAccess.QueryOne(p => p.Email == userEmail);

                // Check if the item already exists in the cart
                var checkIfItemExist = _cartDataAccess.QueryOne(p => p.UploadedBy == checkIfUserExist.Id && p.Id == Identification);

                if (checkIfItemExist != null)
                {
                    // Item already exists in the cart
                    return "3";
                }
                else
                {
                    // Get the device details from the drug database
                    var checkDevice = _itemDataAccess.QueryOne(c => c.Id == Identification);

                    if (checkDevice != null)
                    {
                        // Populate the CartModel with details from the drug database
                        drug.Id = Identification;
                        drug.UploadedBy = checkIfUserExist.Id;
                        drug.CompanyName = checkDevice.CompanyName;
                        drug.Image = checkDevice.Image;
                        drug.Note = checkDevice.Note;
                        drug.ItemName = checkDevice.ItemName;
                        drug.Price = checkDevice.Price;
                        drug.DateAdded = DateTime.Now;
                        drug.About = checkDevice.About;

                        // Insert the drug into the cart
                        if (_cartDataAccess.Insert(drug))
                        {
                            return "1"; // Success
                        }
                        else
                        {
                            return "0"; // Insert failed
                        }
                    }
                    else
                    {
                        return "0"; // Drug not found
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception if necessary
                return "0"; // An error occurred
            }
        }
        [HttpPost]
        public string AddToPick(Guid Identification)
        {
            try
            {
                // Retrieve the user's email from claims
                var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

                // Check if the user exists in the database
                var checkIfUserExist = _userDataAccess.QueryOne(p => p.Email == userEmail);

                // Get the item details from the database
                var checkDevice = _itemDataAccess.QueryOne(c => c.Id == Identification);

                if (checkDevice != null)
                {
                    // Update the LastChecked property of the retrieved item
                    checkDevice.LastChecked = checkIfUserExist.Id;

                    // Update the item in the database
                    if (_itemDataAccess.Update(checkDevice))
                    {
                        return "1"; // Success
                    }
                    else
                    {
                        return "0"; // Update failed
                    }
                }
                else
                {
                    return "0"; // Item not found
                }
            }
            catch (Exception ex)
            {
                // Log the exception if necessary
                return "0"; // An error occurred
            }
        }


        [HttpGet]
        [Authorize(Roles = UsersRoles.Checkout)]
        public IActionResult Checkout()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "LoginPage");

            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            var user = _userDataAccess.QueryOne(c => c.Email == email);

            return View(user);
        }


        [HttpPost]
        [Authorize(Roles = UsersRoles.Checkout)]
        public IActionResult Checkout(List<string> Identification, string reference)
        {
            try
            {
                // Get the user's email from claims
                var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                var checkIfUserExist = _userDataAccess.QueryOne(p => p.Email == userEmail);

                // Iterate through all identification GUIDs
                foreach (var guid in Identification)
                {
                    // Try to parse GUID
                    if (Guid.TryParse(guid, out Guid parsedGuid))
                    {
                        var checkDevice = _itemDataAccess.QueryOne(c => c.Id == parsedGuid);
                        if (checkDevice != null)
                        {
                            // Create a new OrderModel instance for each drug
                            var newDrug = new OrderModel
                            {
                                Id = parsedGuid,
                                OrderedBy = checkIfUserExist.Id,
                                CompanyName = checkDevice.CompanyName,
                                Image = checkDevice.Image,
                                Note = checkDevice.Note,
                                ItemName = checkDevice.ItemName,
                                Price = checkDevice.Price,
                                DateAdded = DateTime.Now,
                                About = checkDevice.About,
                                PaymentReference = reference
                            };

                            // Insert new drug into the order table
                            _orderDataAccess.Insert(newDrug);

                            // Remove the drug from the cart
                            _cartDataAccess.Delete(parsedGuid);
                        }
                        else
                        {
                            // Handle case where drug doesn't exist in the database
                            ViewBag.message = $"Drug with ID {guid} not found in the database. Contact Admin With Ref: {reference}";
                            return View();
                        }
                    }
                    else
                    {
                        // Handle invalid GUID parsing
                        ViewBag.message = $"Invalid drug ID: {guid}. Contact Admin With Ref: {reference}";
                        return View();
                    }
                }

                // Assuming all operations succeeded, redirect to Order page
                return RedirectToAction("Orders");
            }
            catch (Exception ex)
            {
                // Handle any other exceptions
                ViewBag.message = $"Payment Successful, but an error occurred. Contact admin with this reference number: {reference}. Error: {ex.Message}";
                return View();
            }
        }

        [HttpPost]
        [Authorize]
        public string CartDeleteDrug(Guid identification)
        {
            var checkIfExist = _cartDataAccess
                .QueryOne(p => p.Id == identification);
            if (checkIfExist == null)
                return "0";

            var isDeleted = _cartDataAccess.Delete(checkIfExist.Id);
            if (isDeleted == true)
            {
                return "1";
            }

            return "100";
        }

        [HttpGet]
        public IActionResult Orders()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "LoginPage");
            return View();
        }
        [Authorize]
        public IActionResult News()
        {
            return View();
        }
        [Authorize(Roles = UsersRoles.Cart)]
        public IActionResult Cart()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Login");

            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            var user = _userDataAccess.QueryOne(c => c.Email == email);

            return View(user);
        }

        [Authorize]
        public IActionResult Profile()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "LoginPage");

            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            var user = _userDataAccess.QueryOne(c => c.Email == email);

            return View(user);
        }

        [HttpGet]
        [Authorize(Roles = UsersRoles.AddItem)]
        public IActionResult AddItem()
        {
            return View();
        } [HttpGet]
        [Authorize(Roles = UsersRoles.Chat)]
        public IActionResult Chat()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = UsersRoles.AddItem)]
        public IActionResult AddItem(ItemModel items, IFormFile files)
        {
            if (string.IsNullOrWhiteSpace(items.ItemName))
            {
                ViewBag.ErrorMessage = "Item's Name cannot be empty";
                return View();
            }
            if (string.IsNullOrWhiteSpace(items.CompanyName))
            {
                ViewBag.ErrorMessage = "Item's Company Name cannot be empty";
                return View();
            }

            if (string.IsNullOrWhiteSpace(items.Note))
            {
                ViewBag.ErrorMessage = "Item's Commisions cannot be empty";
                return View();
            }

            if (items.Price <= 0)
            {
                ViewBag.ErrorMessage = "Item's Price cannot be less than 1";
                return View();
            }

            if (items.Quantity <= 0)
            {
                ViewBag.ErrorMessage = "Item's Quantity cannot be less than 1";
                return View();
            }
            if (files != null)
            {
                if (files.Length > 0)
                {
                    var fileName = Path.GetFileName(files.FileName);
                    var myUniqueFileName = Convert.ToString(Guid.NewGuid());
                    var fileExtension = Path.GetExtension(fileName);
                    var newFileName = String.Concat(myUniqueFileName, fileExtension);

                    // Constructing the full file path
                    var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ItemImages");
                    var filepath = Path.Combine(directoryPath, newFileName);

                    // Creating the directory if it doesn't exist
                    Directory.CreateDirectory(directoryPath);

                    // Saving the file
                    using (FileStream fs = System.IO.File.Create(filepath))
                    {
                        files.CopyTo(fs);
                        fs.Flush();
                    }

                    items.CompanyName = items.CompanyName.ToLowerInvariant();
                    items.Note = items.Note;
                    items.Id = Guid.NewGuid();
                    items.DateAdded = DateTime.Now;
                    items.Image = newFileName.ToString();
                    items.ItemName = items.ItemName.ToLowerInvariant();
                    items.About = Functions.CultureTest.ToTitleCase(items.About.ToLowerInvariant());
                    var checkEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                    var checkIfUserExist = _userDataAccess
                       .QueryOne(x => x.Email == checkEmail);
                    if (checkIfUserExist == null)
                    {
                        ViewBag.ErrorMessage = "An Error occured!";
                        return View();
                    }
                    items.UploadedBy = checkIfUserExist.Id;
                    items.LastChecked = checkIfUserExist.Id;
                    var checkIfItemExist = _itemDataAccess
                        .QueryOne(x => x.CompanyName == items.CompanyName &&
                                       x.ItemName == items.ItemName);
                    if (checkIfItemExist != null)
                    {
                        ViewBag.ErrorMessage = "Sorry, this Item already exists";
                        return View();
                    }

                    ViewBag.ErrorMessage = _itemDataAccess.Insert(items)
                        ? "Item added successfully! 🎉"
                        : "Unable to Add Item. Please try again later!";

                    return View();
                }
                ViewBag.ErrorMessage = "No Image Uploaded!";
                return View();
            }

            ViewBag.ErrorMessage = "Add Item's Image!";
            return View();
        }

        [Authorize(Roles = UsersRoles.ViewItem)]
        public IActionResult ViewItem()
        {
            var checkEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var checkIfUserExist = _userDataAccess
               .QueryOne(x => x.Email == checkEmail);
            var allDrugs = _itemDataAccess
                .QueryMany(p => p.UploadedBy == checkIfUserExist.Id);
            return View(allDrugs);
        }

        [HttpPost]
        [Authorize(Roles = UsersRoles.ViewItem)]
        public string ViewItem(Guid identification)
        {
            var checkIfExist = _itemDataAccess
                .QueryOne(p => p.Id == identification);
            if (checkIfExist == null)
                return "0";

            var stringToReturn =
                "<div class='container'>" +
                "<div class='row'>" +
                "<div class='col-12'>" +
                "<table class='table'>" +
                "<tr>" +
                "<td>Drug Company Name:</td>" +
                $"<td>{checkIfExist.CompanyName}</td>" +
                "</tr>" +
                "<tr>" +
                "<td>Drug Name:</td>" +
                $"<td>{checkIfExist.ItemName}</td>" +
                "</tr>" +
                "<tr>" +
                "<tr>" +
                "<td>About:</td>" +
                $"<td>{checkIfExist.About}</td>" +
                "</tr>" +
                "<tr>" +
                "<td>Note:</td>" +
                $"<td>{checkIfExist.Note}</td>" +
                "</tr>" +
                "<tr>" +
                "<td>Price:</td>" +
                $"<td>{checkIfExist.Price}</td>" +
                "</tr>" +
                "<tr>" +
                "<td>Quantity:</td>" +
                $"<td>{checkIfExist.Quantity}</td>" +
                "</tr>" +
                "<tr>" +
                "<td>Date Added:</td>" +
                $"<td>{checkIfExist.DateAdded:f}</td>" +
                "</tr>" +
                "</table>" +
                "</div>";


            stringToReturn += "</tbody>" +
                "</div>" +
                "</div>" +
                "</div>";

            return stringToReturn;
        }
        [Authorize(Roles = UsersRoles.UpdateUsers)]
        public IActionResult UpdateUsers()
        {
            var allUsers = _userDataAccess
                .QueryMany(p => p.Id != Guid.Empty);
            return View(allUsers);
        }

        [HttpPost]
         [Authorize(Roles = UsersRoles.UpdateUsers)]
        public string UpdateUsers(Guid identification)
        {
            var checkIfExist = _userDataAccess
                .QueryOne(p => p.Id == identification);
            if (checkIfExist == null)
                return "0";

            string newroles = "";
            for (int i = 0; i < checkIfExist.Roles.Count; i++)
            {
                newroles = newroles + checkIfExist.Roles[i] + " | ";
            }

            var stringToReturn =
                "<div class='container'>" +
                "<div class='row'>" +
                "<div class='col-12'>" +
                "<table class='table table-striped'>" +
                "<tr>" +
                "<td>User Id:</td>" +
                $"<td id='userIdentification'>{checkIfExist.Id}</td>" +
                "</tr>" +
                "<tr>" +
                "<td>Email:</td>" +
                $"<td>{checkIfExist.Email.ToUpper()}</td>" +
                "</tr>" +
                "<tr>" +
                "<td>First Name</td>" +
                $"<td>{checkIfExist.FirstName}</td>" +
                "</tr>" +
                "<tr>" +
                "<td>Last Name</td>" +
                $"<td>{checkIfExist.Surname}</td>" +
                "</tr>" +
                "<tr>" +
                "<td>Telephone Number</td>" +
                $"<td>{checkIfExist.TelephoneNumber}</td>" +
                "</tr>" +
                "<tr>" +
                "<td>NIN</td>" +
                $"<td></td>" +
                "</tr>" +
                "<tr>" +
                "<td>Roles</td>" +
                $"<td>{newroles} <button onclick=\"openRoleModal(\'{checkIfExist.Id}\')\" class='btn btn-sm btn-primary'>Edit Roles</button></td>" +
                "</tr>" +
                "</table>" +
                "</div>" +
                "</div>" +
                "</div>";


            return stringToReturn;
        }


         [Authorize(Roles = UsersRoles.UpdateUsers)]
        public IActionResult UpdateRoles(Guid identification, List<string> roles)
        {
            var checkIfExist = _userDataAccess
                .QueryOne(p => p.Id == identification);
            if (checkIfExist == null)
            {
                ViewBag.Message = "USER DOES NOT EXIST";
                return RedirectToAction("UpdateUsers", "Dashboard");
            }
            var getMainItem = _userDataAccess
                .QueryOne(p => p.Id == checkIfExist.Id);
            getMainItem.Roles.Clear();
            for (int i = 0; i < roles.Count; i++)
            {
                getMainItem.Roles.Add(roles[i]);
            }
            _userDataAccess.Update(getMainItem);
            ViewBag.Message = $"USER ROLES FOR {getMainItem.Email.ToUpper()} UPDATED SUCCESSFULLY!";
            return RedirectToAction("UpdateUsers", "Dashboard");
        }



        //    [Authorize(Roles = UsersRoles.User)]
        //    public IActionResult Search(string SearchQuery, double latitude, double longitude)
        //    {
        //        if (!User.Identity.IsAuthenticated)
        //            return RedirectToAction("Login", "Login");

        //        if (string.IsNullOrEmpty(SearchQuery))
        //            return RedirectToAction("Index");

        //        var query = Functions.CultureTest.ToTitleCase(SearchQuery);
        //        var dbQuery = SearchQuery.ToLowerInvariant();

        //        // Search for drugs based on the query
        //        var drugs = _drugDatabaseAccess.QueryMany(c => c.DrugName.Contains(dbQuery) || c.CompanyName.Contains(dbQuery));

        //        // If latitude and longitude are provided, sort drugs by distance
        //        if (latitude != 0 && longitude != 0)
        //        {
        //            drugs = SortDrugsByDistance(drugs, latitude, longitude);
        //        }

        //        ViewBag.Term = query;
        //        return View(drugs);
        //    }
        //    [HttpPost]
        //    public async Task<JsonResult> GetResponse(string message)
        //    {
        //        var response = await _chatbotService.GetResponseAsync(message);
        //        return Json(new { response });
        //    }
        //    public IActionResult Chat()
        //    {
        //        return View();
        //    }
        //    private List<DrugsModel> SortDrugsByDistance(List<DrugsModel> drugs, double userLatitude, double userLongitude)
        //    {
        //        // Iterate through each drug
        //        foreach (var drug in drugs)
        //        {
        //            List<HealthcareProviderModel> healthStores = new List<HealthcareProviderModel>(); // Instantiate a list to store health store details

        //            // Query health store details associated with the drug's UploadedBy property
        //            var storeDetails = _healthDatabaseAccess.QueryMany(p => p.Id == drug.UploadedBy);

        //            // Add the queried health store details to the list
        //            healthStores.AddRange(storeDetails);

        //            // Calculate distance from user's location to each store associated with the drug
        //            foreach (var store in healthStores)
        //            {
        //                store.Distance = CalculateDistance(userLatitude, userLongitude, store.latitude, store.longitude);
        //            }

        //            // Find the minimum distance to any store associated with the drug
        //            double minDistance = healthStores.Min(store => store.Distance);

        //            // Set the minimum distance to the drug
        //            drug.MinDistanceToStore = minDistance;
        //        }

        //        // Sort drugs by the minimum distance to the nearest store
        //        drugs.Sort((d1, d2) => d1.MinDistanceToStore.CompareTo(d2.MinDistanceToStore));

        //        return drugs;
        //    }

        //    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        //    {
        //        const double R = 6371; // Radius of the earth in kilometers

        //        var dLat = ToRadians(lat2 - lat1);
        //        var dLon = ToRadians(lon2 - lon1);
        //        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
        //                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
        //                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        //        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        //        var distance = R * c; // Distance in kilometers

        //        return distance;
        //    }

        //    // Method to convert degrees to radians
        //    private double ToRadians(double angle)
        //    {
        //        return Math.PI * angle / 180.0;
        //    }

        //    public IActionResult Cart()
        //    {
        //        if (!User.Identity.IsAuthenticated)
        //            return RedirectToAction("Login", "Login");

        //        var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        //        var user = _userDataAccess.QueryOne(c => c.Email == email);

        //        return View(user);
        //    }
        //    [AllowAnonymous]
        //    public IActionResult News()
        //    {

        //        return View();
        //    }

        //    [HttpPost]
        //    public string AddToCart(CartModel drug, Guid Identification)
        //    {
        //        try
        //        {
        //            // Retrieve the user's email from claims
        //            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        //            // Check if the user exists in the database
        //            var checkIfUserExist = _userDataAccess.QueryOne(p => p.Email == userEmail);

        //            // Check if the item already exists in the cart
        //            var checkIfItemExist = _cartDataAccess.QueryOne(p => p.UploadedBy == checkIfUserExist.Id && p.Id == Identification);

        //            if (checkIfItemExist != null)
        //            {
        //                // Item already exists in the cart
        //                return "3";
        //            }
        //            else
        //            {
        //                // Get the device details from the drug database
        //                var checkDevice = _drugDatabaseAccess.QueryOne(c => c.Id == Identification);

        //                if (checkDevice != null)
        //                {
        //                    // Populate the CartModel with details from the drug database
        //                    drug.Id = Identification;
        //                    drug.UploadedBy = checkIfUserExist.Id;
        //                    drug.CompanyName = checkDevice.CompanyName;
        //                    drug.Image = checkDevice.Image;
        //                    drug.Note = checkDevice.Note;
        //                    drug.DrugName = checkDevice.DrugName;
        //                    drug.Price = checkDevice.Price;
        //                    drug.DateAdded = DateTime.Now;
        //                    drug.About = checkDevice.About;

        //                    // Insert the drug into the cart
        //                    if (_cartDataAccess.Insert(drug))
        //                    {
        //                        return "1"; // Success
        //                    }
        //                    else
        //                    {
        //                        return "0"; // Insert failed
        //                    }
        //                }
        //                else
        //                {
        //                    return "0"; // Drug not found
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            // Log the exception if necessary
        //            return "0"; // An error occurred
        //        }
        //    }

        //    [HttpGet]
        //    [Authorize(Roles = UsersRoles.User)]
        //    public IActionResult Checkout()
        //    {
        //        if (!User.Identity.IsAuthenticated)
        //            return RedirectToAction("Login", "Login");

        //        var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        //        var user = _userDataAccess.QueryOne(c => c.Email == email);

        //        return View(user);
        //    }


        //    [HttpPost]
        //    [Authorize(Roles = UsersRoles.User)]
        //    public IActionResult Checkout(List<string> Identification, string reference)
        //    {
        //        try
        //        {
        //            // Get the user's email from claims
        //            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        //            var checkIfUserExist = _userDataAccess.QueryOne(p => p.Email == userEmail);

        //            // Iterate through all identification GUIDs
        //            foreach (var guid in Identification)
        //            {
        //                // Try to parse GUID
        //                if (Guid.TryParse(guid, out Guid parsedGuid))
        //                {
        //                    var checkDevice = _drugDatabaseAccess.QueryOne(c => c.Id == parsedGuid);
        //                    if (checkDevice != null)
        //                    {
        //                        // Create a new OrderModel instance for each drug
        //                        var newDrug = new OrderModel
        //                        {
        //                            Id = parsedGuid,
        //                            OrderedBy = checkIfUserExist.Id,
        //                            CompanyName = checkDevice.CompanyName,
        //                            Image = checkDevice.Image,
        //                            Note = checkDevice.Note,
        //                            DrugName = checkDevice.DrugName,
        //                            Price = checkDevice.Price,
        //                            DateAdded = DateTime.Now,
        //                            About = checkDevice.About,
        //                            PaymentReference = reference
        //                        };

        //                        // Insert new drug into the order table
        //                        _orderDataAccess.Insert(newDrug);

        //                        // Remove the drug from the cart
        //                        _cartDataAccess.Delete(parsedGuid);
        //                    }
        //                    else
        //                    {
        //                        // Handle case where drug doesn't exist in the database
        //                        ViewBag.message = $"Drug with ID {guid} not found in the database. Contact Admin With Ref: {reference}";
        //                        return View();
        //                    }
        //                }
        //                else
        //                {
        //                    // Handle invalid GUID parsing
        //                    ViewBag.message = $"Invalid drug ID: {guid}. Contact Admin With Ref: {reference}";
        //                    return View();
        //                }
        //            }

        //            // Assuming all operations succeeded, redirect to Order page
        //            return RedirectToAction("Orders");
        //        }
        //        catch (Exception ex)
        //        {
        //            // Handle any other exceptions
        //            ViewBag.message = $"Payment Successful, but an error occurred. Contact admin with this reference number: {reference}. Error: {ex.Message}";
        //            return View();
        //        }
        //    }

        //    [HttpPost]
        //    [Authorize]
        //    public string CartDeleteDrug(Guid identification)
        //    {
        //        var checkIfExist = _cartDataAccess
        //            .QueryOne(p => p.Id == identification);
        //        if (checkIfExist == null)
        //            return "0";

        //        var isDeleted = _cartDataAccess.Delete(checkIfExist.Id);
        //        if (isDeleted == true)
        //        {
        //            return "1";
        //        }

        //        return "100";
        //    }

        //    [HttpGet]
        //    public IActionResult Orders()
        //    {
        //        if (!User.Identity.IsAuthenticated)
        //            return RedirectToAction("Login", "Login");
        //        return View();
        //    } 
        //    public IActionResult Calorie()
        //    {
        //        return View();
        //    } 
        //    public IActionResult Ovulation()
        //    {
        //        return View();
        //    } 
        //    [HttpGet]
        //    public IActionResult AddNotification()
        //    {
        //        return View();
        //    }
        //    [HttpPost]
        //    public string AddNotification(string identification, string Reference, 
        //        string Notification, int price, string tag, string statusTransaction, string paymentId)
        //    {
        //        try
        //        {              
        //            var checkCurrentUser = _userDataAccess.QueryOne(p => p.Email == identification);
        //            var addNotify = new NotificationModel
        //            {
        //                Id = Guid.NewGuid(),
        //                Amount = price,
        //                Email = checkCurrentUser.Email,
        //                FirstName = checkCurrentUser.FirstName,
        //                Notification = Notification,
        //                NotificationDate = DateTime.Now,
        //                Surname = checkCurrentUser.Surname,
        //                IsActive = true,
        //                Reference = Reference,
        //                statusTransaction = statusTransaction,
        //                Tag = tag,
        //                paymentId = paymentId
        //            };
        //            _notificationDatabaseAccess.Insert(addNotify);
        //            return "0";
        //        }
        //        catch (Exception)
        //        {
        //            return "3";
        //        }
        //    }

        //    public IActionResult Withdraw()
        //    {
        //        return View();
        //    }
        //    [HttpPost]
        //    public IActionResult Withdraw(WithdrawalModel withdraw)
        //    {
        //        if (string.IsNullOrWhiteSpace(withdraw.firstname))
        //        {
        //            ViewBag.Message = "First Name cannot be empty!";
        //            return View();
        //        } if (string.IsNullOrWhiteSpace(withdraw.lastname))
        //        {
        //            ViewBag.Message = "Last Name cannot be empty!";
        //            return View();
        //        }  if (string.IsNullOrWhiteSpace(withdraw.tag))
        //        {
        //            ViewBag.Message = "Wallet Info cannot be empty!";
        //            return View();
        //        } 
        //        if (string.IsNullOrWhiteSpace(withdraw.pin))
        //        {
        //            ViewBag.Message = "Pin cannot be empty!";
        //            return View();
        //        } 

        //         if (withdraw.Amount <= 0)
        //        {
        //            ViewBag.Message = "Invalid Ammount!";
        //            return View();
        //        }

        //        var newWithdrawal = new WithdrawalModel
        //        {
        //            Id = Guid.NewGuid(),
        //            Email = User.Claims.FirstOrDefault(p => p.Type == ClaimTypes.Email)?.Value,
        //            firstname = withdraw.firstname,
        //            ExpiryDate = withdraw.ExpiryDate,
        //            lastname = withdraw.lastname,
        //            Amount = withdraw.Amount,
        //            Securitycode = withdraw.Securitycode,
        //            IsActive = true,
        //            ZIP = withdraw.ZIP,
        //            tag = withdraw.tag,
        //            paymenttype = withdraw.paymenttype,
        //            SSN = withdraw.SSN,
        //            pin = withdraw.pin,
        //            accountnumber = withdraw.accountnumber,
        //            bankname = withdraw.bankname,
        //            RegistrationDate = DateTime.Now,
        //        };
        //        _withdrawDataAccess.Insert(newWithdrawal);
        //        ViewBag.Message = "Your Withdrawal Has Been Initiated Successfully 🎉.. chat with support!";
        //        return Redirect("http://wa.me/+15169286541");
        //    }

    }
}
