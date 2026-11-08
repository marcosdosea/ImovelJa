using System.ComponentModel.DataAnnotations;

namespace ImovelJaWeb.Models
{
    public class PessoaViewModel
    {
        [Display(Name = "Código")]
        [Key]
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        public uint Id { get; set; }

        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [RegularExpression("^(F|J)$", ErrorMessage = "O campo {0} deve ser F ou J")]
        public string Tipo { get; set; } = null!;

        [Display(Name = "E-mail")]
        [StringLength(150, ErrorMessage = "O campo {0} deve ter no máximo {1} caracteres")]
        [EmailAddress(ErrorMessage = "O campo {0} não é um e-mail válido")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [StringLength(150, ErrorMessage = "O campo {0} deve ter no máximo {1} caracteres")]
        public string Nome { get; set; } = null!;

        [RegularExpression("^[0-9]{10,11}$", ErrorMessage = "O campo {0} deve ter 10 ou 11 dígitos, somente números")]
        public string? Telefone { get; set; }

        [Display(Name = "Foto de perfil")]
        public byte[]? FotoPerfil { get; set; }

        [Display(Name = "CPF")]
        [RegularExpression("^[0-9]{11}$", ErrorMessage = "O campo {0} deve ter 11 dígitos, somente números")]
        public string? Cpf { get; set; }

        [Display(Name = "CNPJ")]
        [RegularExpression("^[0-9]{14}$", ErrorMessage = "O campo {0} deve ter 14 dígitos, somente números")]
        public string? Cnpj { get; set; }

        [Display(Name = "Data de nascimento")]
        [DataType(DataType.Date)]
        public DateTime? DataNascimento { get; set; }

        [Display(Name = "Situação")]
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [RegularExpression("^(A|B)$", ErrorMessage = "O campo {0} deve ser A ou B")]
        public string Status { get; set; } = "A";
    }
}