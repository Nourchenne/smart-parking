using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace auth.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est requis")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        [Display(Name = "Nom")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Le prix est requis")]
        [Range(0.01, 1000000, ErrorMessage = "Le prix doit être entre 0.01 et 1,000,000")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Prix")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "La quantité est requise")]
        [Range(0, 10000, ErrorMessage = "La quantité doit être entre 0 et 10,000")]
        [Display(Name = "Quantité en stock")]
        public int StockQuantity { get; set; }

        [Display(Name = "Date de création")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Date de modification")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Catégorie")]
        [StringLength(50)]
        public string? Category { get; set; }

        [Display(Name = "Actif")]
        public bool IsActive { get; set; } = true;
    }
}