using System.ComponentModel.DataAnnotations;

namespace BlogApp.MVC.Models.BlogPost;

public class BlogPostCreateViewModel
{
    [Display(Name = "Kategori")]
    [Required, Range(1, int.MaxValue, ErrorMessage = "Bir kategori seçin.")]
    public int CategoryId { get; set; }

    [Display(Name = "Başlık")]
    [Required, MinLength(5), MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "İçerik")]
    [Required, MinLength(50)]
    public string Content { get; set; } = string.Empty;
}
