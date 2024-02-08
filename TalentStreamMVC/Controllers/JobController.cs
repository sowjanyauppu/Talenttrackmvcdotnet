using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using TalentStreamMVC.Models;

public class JobController : Controller
{
    private readonly string _connectionString = "Server=localhost;Database=talentstreamdotnet;User=root;Password=root;";

    public IActionResult CreateJob()
    {
        return View();
    }

    [HttpPost]
    public IActionResult CreateJob(Job job, int recruiterId=1)
    {
        if (ModelState.IsValid)
        {
            // Check if the recruiter with the given ID exists
            bool recruiterExists = CheckRecruiterExists(recruiterId);

            if (!recruiterExists)
            {
                ModelState.AddModelError(string.Empty, "Recruiter not found");
                return View(job);
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                // Insert job details into Jobs table
                var insertJobQuery = "INSERT INTO Jobs (RecruiterId, JobTitle, MinExperience, MaxExperience, MinSalary, MaxSalary) " +
                                    "VALUES (@RecruiterId, @JobTitle, @MinExperience, @MaxExperience, @MinSalary, @MaxSalary)";

                using (var command = new MySqlCommand(insertJobQuery, connection))
                {
                    command.Parameters.AddWithValue("@RecruiterId", recruiterId);
                    command.Parameters.AddWithValue("@JobTitle", job.JobTitle);
                    command.Parameters.AddWithValue("@MinExperience", job.MinExperience);
                    command.Parameters.AddWithValue("@MaxExperience", job.MaxExperience);
                    command.Parameters.AddWithValue("@MinSalary", job.MinSalary);
                    command.Parameters.AddWithValue("@MaxSalary", job.MaxSalary);
                    command.ExecuteNonQuery();
                }

                // Get the JobId of the newly inserted job
                var jobIdQuery = "SELECT LAST_INSERT_ID()";
                int jobId;
                using (var command = new MySqlCommand(jobIdQuery, connection))
                {
                    jobId = Convert.ToInt32(command.ExecuteScalar());
                }

                // Split the comma-separated string into a list of skills
                var skills = string.Join(",", job.RequiredSkills).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(skill => skill.Trim()).ToArray();


                // Insert skills into JobSkills table
                foreach (var skill in skills)
                {
                    var insertSkillsQuery = "INSERT INTO JobSkills (JobId, SkillId) " + "VALUES (@JobId, @SkillId)";

                    using (var command = new MySqlCommand(insertSkillsQuery, connection))
                    {
                        command.Parameters.AddWithValue("@JobId", jobId);
                        // Assuming SkillId corresponds to the SkillId in the Skills table
                        command.Parameters.AddWithValue("@SkillId", GetOrCreateSkillId(skill, connection));

                        command.ExecuteNonQuery();
                    }
                }
            }

            return RedirectToAction("JobCreationSuccess");
        }

        return View(job);
    }

    private int GetOrCreateSkillId(string skill, MySqlConnection connection)
    {
        // Check if the skill already exists in the Skills table
        var checkSkillQuery = "SELECT SkillId FROM Skills WHERE SkillName = @SkillName";
        using (var command = new MySqlCommand(checkSkillQuery, connection))
        {
            command.Parameters.AddWithValue("@SkillName", skill);

            var existingSkillId = command.ExecuteScalar();
            if (existingSkillId != null)
            {
                return Convert.ToInt32(existingSkillId);
            }
        }

        // If the skill doesn't exist, insert it and return the generated SkillId
        var insertSkillQuery = "INSERT INTO Skills (SkillName) VALUES (@SkillName)";
        using (var command = new MySqlCommand(insertSkillQuery, connection))
        {
            command.Parameters.AddWithValue("@SkillName", skill);
            command.ExecuteNonQuery();
        }

        // Get the SkillId of the newly inserted skill
        var getSkillIdQuery = "SELECT LAST_INSERT_ID()";
        using (var command = new MySqlCommand(getSkillIdQuery, connection))
        {
            return Convert.ToInt32(command.ExecuteScalar());
        }
    }

    public IActionResult JobCreationSuccess()
    {
        return View();
    }

    private bool CheckRecruiterExists(int recruiterId)
    {
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();

            var query = "SELECT COUNT(*) FROM Recruiters WHERE recruiterId = @RecruiterId";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@RecruiterId", recruiterId);

                int count = Convert.ToInt32(command.ExecuteScalar());
                return count > 0;
            }
        }
    }
}
