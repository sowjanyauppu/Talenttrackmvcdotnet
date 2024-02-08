using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using TalentStreamMVC.Models;

public class RecruiterController : Controller
{
    private readonly string _connectionString = "Server=localhost;Database=talentstreamdotnet;User=root;Password=root;";

    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Register(Recruiter recruiter)
    {
        if (ModelState.IsValid)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                var query = "INSERT INTO Recruiters (CompanyName, Email, MobileNumber, Password) " +
                            "VALUES (@CompanyName, @Email, @MobileNumber, @Password)";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CompanyName", recruiter.CompanyName);
                    command.Parameters.AddWithValue("@Email", recruiter.Email);
                    command.Parameters.AddWithValue("@MobileNumber", recruiter.MobileNumber);
                    command.Parameters.AddWithValue("@Password", recruiter.Password);

                    command.ExecuteNonQuery();
                }
            }

            return RedirectToAction("RegistrationSuccess");
        }

        return View(recruiter);
    }

    public IActionResult RegistrationSuccess()
    {
        return View();
    }

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(Login loginModel)
    {
      
        bool isAuthenticated = AuthenticateUser(loginModel.Email, loginModel.Password);

        if (isAuthenticated)
        {
             return RedirectToAction("Dashboard");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password");
            return View("Login", loginModel);
        }
    }

    private bool AuthenticateUser(string email, string password)
    {
       
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();

            var query = "SELECT COUNT(*) FROM Recruiters WHERE Email = @Email AND Password = @Password";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@Password", password);

                int count = Convert.ToInt32(command.ExecuteScalar());
                return count > 0;
            }
        }
    }

    public IActionResult Dashboard()
    {
         return View();
    }
}