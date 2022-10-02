using Senparc.Ncf.Core.Models;
using Senparc.Xncf.WeixinManagerTenPayV3.Models.DatabaseModel.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManagerTenPayV3.Domain.Models.DatabaseModel
{
    [Table(name: Register.DATABASE_PREFIX + nameof(Product))]
    [Serializable]
    public class Product : EntityBase<int>
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; private set; }
        [Required]
        public decimal Price { get; private set; }
        [Required]
        public int Store { get; private set; }
        [Required]
        public string Description { get; private set; }
        [Required]
        public string ImageUrl { get; private set; }
        [Required]
        public bool IsOpen { get; private set; }

        public decimal? OriginalPrice { get; private set; }
        public string Note { get; private set; }

        Product() { }

        public Product(ProductDto productDto)
        {
            Name = productDto.Name;
            Price = productDto.Price;
            Store = productDto.Store;
            Description = productDto.Description;
            ImageUrl = productDto.ImageUrl;
            IsOpen = productDto.IsOpen;
            OriginalPrice = productDto.OriginalPrice;
            Note = productDto.Note;
        }
    }
}
