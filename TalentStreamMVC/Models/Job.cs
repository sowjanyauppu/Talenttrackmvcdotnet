using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace TalentStreamMVC.Models
{
    public class Job
    {
        public int JobId { get; set; }

        [Required(ErrorMessage = "Job Title is required")]
        public string? JobTitle { get; set; }

        public int MinExperience { get; set; }

        public int MaxExperience { get; set; }

        public double MinSalary { get; set; }

        public double MaxSalary { get; set; }

        [Required(ErrorMessage = "Skills are required")]
        public List<string>? RequiredSkills { get; set; }
    }
}
