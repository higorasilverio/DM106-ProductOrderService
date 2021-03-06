using System.ComponentModel.DataAnnotations;

namespace ProductOrderService.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O campo nome é obrigatório")]
        public string nome { get; set; }

        public string descricao { get; set; }

        public string cor { get; set; }

        [Required(ErrorMessage = "O campo modelo é obrigatório")]
        public string modelo { get; set; }

        [Required(ErrorMessage = "O campo codigo é obrigatório")]
        [StringLength(8, ErrorMessage = "O tamanho máximo do código é 8 caracteres")]
        public string codigo { get; set; }

        [Range(10, 9999, ErrorMessage = "O preço deve ser entre 10 e 9999")]
        public decimal preco { get; set; }

        [Range(0.1, 10, ErrorMessage = "O peso do produto (kg) deve estar entre 0.1 e 999")]
        public decimal peso { get; set; }

        [Range(10, 99, ErrorMessage = "A altura do produto (cm) deve estar entre 10 e 99")]
        public decimal altura { get; set; }

        [Range(10, 99, ErrorMessage = "A largura do produto (cm) deve estar entre 10 e 99")]
        public decimal largura { get; set; }

        [Range(10, 99, ErrorMessage = "O comprimento do produto (cm) deve estar entre 10 e 99")]
        public decimal comprimento { get; set; }

        [Range(10, 99, ErrorMessage = "O diametro do produto (cm) deve estar entre 10 e 99")]
        public decimal diametro { get; set; }

        [StringLength(80, ErrorMessage = "O tamanho máximo da url é 80 caracteres")]
        public string url { get; set; }
    }
}