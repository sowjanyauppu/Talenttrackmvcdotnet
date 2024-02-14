using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using TalentStreamMVC.Models;

public class JobController : Controller
{
    private readonly string _connectionString = "Server=localhost;Database=talentstreamdotnet;User=root;Password=root;";
    public IActionResult CreateJob(int? Id)
    {
        ViewBag.RecruiterId = Id ?? 0;
        return View();
    }

    [HttpPost]
    public IActionResult CreateJob(Job job, int recruiterId)
    {
        if (ModelState.IsValid)
        {
            
            bool recruiterExists = CheckRecruiterExists(recruiterId);

            if (!recruiterExists)
            {
                ModelState.AddModelError(string.Empty, "Recruiter not found");
                return View(job);
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();


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

                var jobIdQuery = "SELECT LAST_INSERT_ID()";
                int jobId;
                using (var command = new MySqlCommand(jobIdQuery, connection))
                {
                    jobId = Convert.ToInt32(command.ExecuteScalar());
                }

                TempData["NewJobId"] = jobId;
                var skills = string.Join(",", job.RequiredSkills).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(skill => skill.Trim()).ToArray();
               
                foreach (var skill in skills)
                {
                    var insertSkillsQuery = "INSERT INTO JobSkills (JobId, SkillId) " + "VALUES (@JobId, @SkillId)";

                    using (var command = new MySqlCommand(insertSkillsQuery, connection))
                    {
                        command.Parameters.AddWithValue("@JobId", jobId);
                        command.Parameters.AddWithValue("@SkillId", GetOrCreateSkillId(skill, connection));
                        command.ExecuteNonQuery();
                    }
                }
            }

            return RedirectToAction("GetJobDetails");
        }

        return View(job);
    }

    private int GetOrCreateSkillId(string skill, MySqlConnection connection)
    {
        
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
        
        var insertSkillQuery = "INSERT INTO Skills (SkillName) VALUES (@SkillName)";
        using (var command = new MySqlCommand(insertSkillQuery, connection))
        {
            command.Parameters.AddWithValue("@SkillName", skill);
            command.ExecuteNonQuery();
        }
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

            var query = "SELECT COUNT(*) FROM Recruiters WHERE Id = @RecruiterId";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@RecruiterId", recruiterId);

                int count = Convert.ToInt32(command.ExecuteScalar());
                return count > 0;
            }
        }
    }
    [HttpGet]
    [Route("Job/RecruiterJobs/{recruiterId?}")]
    public IActionResult RecruiterJobs(int recruiterId)
    {

        bool recruiterExists = CheckRecruiterExists(recruiterId);

        if (!recruiterExists)
        {
            ModelState.AddModelError(string.Empty, "Recruiter not found");
            return View("RecruiterNotFound");
        }

        List<Job> recruiterJobs = new List<Job>();

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();

            var getRecruiterJobsQuery = "SELECT * FROM Jobs WHERE RecruiterId = @RecruiterId";

            using (var command = new MySqlCommand(getRecruiterJobsQuery, connection))
            {
                command.Parameters.AddWithValue("@RecruiterId", recruiterId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {

                        Job job = new Job
                        {
                            JobId = Convert.ToInt32(reader["JobId"]),
                            JobTitle = reader["JobTitle"].ToString(),
                            MinExperience = Convert.ToInt32(reader["MinExperience"]),
                            MaxExperience = Convert.ToInt32(reader["MaxExperience"]),
                            MinSalary = Convert.ToDouble(reader["MinSalary"]),
                            MaxSalary = Convert.ToDouble(reader["MaxSalary"])
                        };
                        recruiterJobs.Add(job);
                    }
                }
            }
        }
        ViewData["JobsList"] = recruiterJobs;
        return View();
    }

    public IActionResult GetJobDetails()
    {
        if (TempData.TryGetValue("NewJobId", out var jobId))
        {
            if (int.TryParse(jobId.ToString(), out int jobIdInt))
            {
                var jobDetails = GetJobDetailsFromDatabase(jobIdInt);

                if (jobDetails != null)
                {
                    return View(jobDetails);
                }
                else
                {
                    return RedirectToAction("JobNotFound");
                }
            }
        }

        return RedirectToAction("JobNotFound");
    }

    public Job GetJobDetailsFromDatabase(int jobId)
    {
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();

            var getJobDetailsQuery = "SELECT JobId, JobTitle, MinExperience, MaxExperience, MinSalary, MaxSalary " +
                                     "FROM Jobs " +
                                     "WHERE JobId = @JobId";

            using (var command = new MySqlCommand(getJobDetailsQuery, connection))
            {
                command.Parameters.AddWithValue("@JobId", jobId);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Job
                        {
                            JobId = Convert.ToInt32(reader["JobId"]),
                            JobTitle = reader["JobTitle"].ToString(),
                            MinExperience = Convert.ToInt32(reader["MinExperience"]),
                            MaxExperience = Convert.ToInt32(reader["MaxExperience"]),
                            MinSalary = Convert.ToDouble(reader["MinSalary"]),
                            MaxSalary = Convert.ToDouble(reader["MaxSalary"])
                        };
                    }
                }
            }
        }

        return null;
    }
}
