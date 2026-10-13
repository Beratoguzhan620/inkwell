using System.ComponentModel.DataAnnotations;

namespace BlogApp.MVC.Models.BlogPost;

public class PostEditorViewModel
{
    /// <summary>Null while creating a post.</summary>
    public int? Id { get; set; }

    [Display(Name = "Kategori")]
    [Required, Range(1, int.MaxValue, ErrorMessage = "Bir kategori seçin.")]
    public int CategoryId { get; set; }

    [Display(Name = "Başlık")]
    [Required, MinLength(5), MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "İçerik")]
    [Required, MinLength(50)]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Kapak görseli")]
    public IFormFile? Cover { get; set; }

    [Display(Name = "Kapak görselini kaldır")]
    public bool RemoveCover { get; set; }

    // Display only.
    public string? CurrentCoverUrl { get; set; }
    public bool IsPublished { get; set; }
    public string? Slug { get; set; }
}
