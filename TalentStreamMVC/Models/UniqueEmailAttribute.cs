using System;
using System.ComponentModel.DataAnnotations;
using MySql.Data.MySqlClient;

namespace TalentStreamMVC.Models
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class UniqueEmailAttribute : ValidationAttribute
    {
        private readonly string _connectionString= ConnectionStrings.TalentStream;

        public UniqueEmailAttribute(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value != null)
            {
                string? email = value?.ToString();

                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    var query = "SELECT COUNT(*) FROM Recruiters WHERE Email = @Email";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Email", email);

                        int count = Convert.ToInt32(command.ExecuteScalar());

                        if (count > 0)
                        {
                            return new ValidationResult("Email is already in use");
                        }
                    }
                }
            }

            return ValidationResult.Success;
        }
    }
}