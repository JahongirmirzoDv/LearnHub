using System.ComponentModel.DataAnnotations;

namespace LearnHub.Models;

public enum DifficultyLevel
{
    Beginner = 1,
    Intermediate = 2,
    Advanced = 3
}

public enum ResourceType
{
    [Display(Name = "Article")]
    Article = 1,

    [Display(Name = "Video")]
    Video = 2,

    [Display(Name = "PDF document")]
    Pdf = 3,

    [Display(Name = "Image")]
    Image = 4,

    [Display(Name = "External link")]
    Link = 5
}
