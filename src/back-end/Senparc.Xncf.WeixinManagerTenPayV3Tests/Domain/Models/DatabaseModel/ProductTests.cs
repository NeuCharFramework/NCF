using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.WeixinManagerTenPayV3.Domain.Models.DatabaseModel;
using Senparc.Xncf.WeixinManagerTenPayV3.Models.DatabaseModel.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManagerTenPayV3.Domain.Models.DatabaseModel.Tests
{
    [TestClass()]
    public class ProductTests
    {
        [TestMethod()]
        public void ProductTest()
        {
            var productDto = new ProductDto()
            {
                Name = "测试商品",
                Price = 100,
                Store = 100,
                Description = "测试商品",
                ImageUrl = "http://www.senparc.com",
                IsOpen = true,
                OriginalPrice = 200,
                Note = "备注"
            };
            var product = new Product(productDto);
            Assert.AreEqual(product.Name, productDto.Name);
            Assert.AreEqual(product.Price, productDto.Price);
            Assert.AreEqual(product.Store, productDto.Store);
            Assert.AreEqual(product.Description, productDto.Description);
            Assert.AreEqual(product.ImageUrl, productDto.ImageUrl);
            Assert.AreEqual(product.IsOpen, productDto.IsOpen);
            Assert.AreEqual(product.OriginalPrice, productDto.OriginalPrice);
            Assert.AreEqual(product.Note, productDto.Note);
        }
    }
}