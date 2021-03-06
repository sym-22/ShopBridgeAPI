using DataAccess.Classes;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace DataAccess.Tests
{
    [TestFixture]
    public class ItemsSqlDataAccessTests
    {
        private  Mock<IDbContextFactory<ItemContext>> _mockDbContextFactory;
        private  Mock<ItemContext> _mockItemContext;
        private  IConfiguration _mockInMemoryConfiguration;


        [SetUp]
        public void Setup()
        {
            _mockDbContextFactory = new Mock<IDbContextFactory<ItemContext>>();

            var options = new DbContextOptionsBuilder<ItemContext>().Options;
            _mockItemContext = new Mock<ItemContext>(options);


            var globalConfig = new Dictionary<string, string>();
            globalConfig.Add("ItemsPerPage", "5");

            _mockInMemoryConfiguration = new ConfigurationBuilder().AddInMemoryCollection(globalConfig).Build();
        }

        [Test]
        public void AddItemTest_Exception()
        {
            var mockItemSet = new Mock<DbSet<Item>>();
            var mockCategoriesSet = GetDefaultCategoriesDbSet();

            _mockItemContext.Setup(x => x.Items).Returns(mockItemSet.Object);

            _mockDbContextFactory.Setup(x => x.CreateDbContext()).Throws(new Exception(message: "Test Exception"));

            ItemsSqlDataAccess itemsSqlDataAccess = new ItemsSqlDataAccess(_mockDbContextFactory.Object, _mockInMemoryConfiguration);

            var exception = Assert.ThrowsAsync<Exception>(async () =>
            {
                Item addedItem = await itemsSqlDataAccess.AddItemAsync(It.IsAny<Item>());
            });

            Assert.AreEqual("Test Exception", exception.Message);
        }

        [Test]
        public async Task EditItemTest_Success()
        {
            var mockItemSet = GetDefaultItemsDbSet();

            _mockDbContextFactory.Setup(x => x.CreateDbContext()).Returns(_mockItemContext.Object);
            _mockItemContext.Setup(x => x.Items).Returns(mockItemSet.Object);

            Item itemToUpdate = new Item { ItemId = 1, ItemName = "test item1", ItemDescription = "desc 1", ItemPrice = 60 };
            ItemsSqlDataAccess itemsSqlDataAccess = new ItemsSqlDataAccess(_mockDbContextFactory.Object, _mockInMemoryConfiguration);
            Item updatedItem = await itemsSqlDataAccess.EditItemAsync(itemToUpdate);

            mockItemSet.Verify(m => m.Update(It.IsAny<Item>()), Times.Once());
            _mockItemContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());

            Assert.AreEqual(itemToUpdate.ItemName, updatedItem.ItemName);
            Assert.AreEqual(itemToUpdate.ItemDescription, updatedItem.ItemDescription);
            Assert.AreEqual(itemToUpdate.ItemPrice, updatedItem.ItemPrice);

        }

        [Test]
        public void EditItemTest_Exception()
        {
            var mockItemSet = new Mock<DbSet<Item>>();

            _mockItemContext.Setup(x => x.Items).Returns(mockItemSet.Object);

            _mockDbContextFactory.Setup(x => x.CreateDbContext()).Returns(_mockItemContext.Object);

            _mockItemContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                                         .ThrowsAsync(new Exception(message: "Test Exception"));

            ItemsSqlDataAccess itemsSqlDataAccess = new ItemsSqlDataAccess(_mockDbContextFactory.Object, _mockInMemoryConfiguration);

            var exception = Assert.ThrowsAsync<Exception>(async () =>
            {
                Item addedItem = await itemsSqlDataAccess.EditItemAsync(It.IsAny<Item>());

            });

            Assert.AreEqual("Test Exception", exception.Message);
        }

        [Test]
        public void GetAllItemsTest_Exception()
        {
            var mockItemSet = new Mock<DbSet<Item>>();

            _mockItemContext.Setup(x => x.Items).Throws(new Exception(message: "Test Exception"));

            _mockDbContextFactory.Setup(x => x.CreateDbContext()).Returns(_mockItemContext.Object);

            ItemsSqlDataAccess itemsSqlDataAccess = new ItemsSqlDataAccess(_mockDbContextFactory.Object, _mockInMemoryConfiguration);

            var exception = Assert.ThrowsAsync<Exception>(async () =>
            {
                List<Item> items = await itemsSqlDataAccess.GetAllItemsAsync(It.IsAny<int>());

            });

            Assert.AreEqual("Test Exception", exception.Message);
        }

        [Test]
        public async Task DeleteItemTest_SingleExists_Success()
        {

            Mock<DbSet<Item>> mockItemSet = GetDefaultItemsDbSet();


            _mockItemContext.Setup(x => x.Items).Returns(mockItemSet.Object);
            _mockDbContextFactory.Setup(x => x.CreateDbContext()).Returns(_mockItemContext.Object);


            ItemsSqlDataAccess itemsSqlDataAccess = new ItemsSqlDataAccess(_mockDbContextFactory.Object, _mockInMemoryConfiguration);
            await itemsSqlDataAccess.DeleteItemAsync(1);

            Item item1 = new Item { ItemId = 1, ItemName = "test item1", ItemDescription = "desc 1", ItemPrice = 55 };
            mockItemSet.Setup(m => m.Find(1)).Returns(item1);

            mockItemSet.Verify(m => m.Find(It.IsAny<int>()), Times.Once());
            mockItemSet.Verify(m => m.Remove(It.IsAny<Item>()), Times.Once());
            _mockItemContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());

        }

        [Test]
        public async Task DeleteItemTest_MultipleExists_Success()
        {

            Mock<DbSet<Item>> mockItemSet = GetDefaultItemsDbSet();

            Item item1 = new Item { ItemId = 1, ItemName = "test item1", ItemDescription = "desc 1", ItemPrice = 55, AvailableQuantity = 3 };
            mockItemSet.Setup(m => m.Find(1)).Returns(item1);

            _mockItemContext.Setup(x => x.Items).Returns(mockItemSet.Object);
            _mockDbContextFactory.Setup(x => x.CreateDbContext()).Returns(_mockItemContext.Object);

            ItemsSqlDataAccess itemsSqlDataAccess = new ItemsSqlDataAccess(_mockDbContextFactory.Object, _mockInMemoryConfiguration);
            await itemsSqlDataAccess.DeleteItemAsync(1);

            mockItemSet.Verify(m => m.Find(It.IsAny<int>()), Times.Once());
            mockItemSet.Verify(m => m.Remove(It.IsAny<Item>()), Times.Never());
            _mockItemContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());

        }

        [Test]
        public void DeleteItemTest_Exception()
        {
            var mockItemSet = new Mock<DbSet<Item>>();

            _mockDbContextFactory.Setup(x => x.CreateDbContext()).Throws(new Exception(message: "Test Exception"));

            ItemsSqlDataAccess itemsSqlDataAccess = new ItemsSqlDataAccess(_mockDbContextFactory.Object, _mockInMemoryConfiguration);

            var exception = Assert.ThrowsAsync<Exception>(async () =>
            {
               await itemsSqlDataAccess.DeleteItemAsync(It.IsAny<int>());

            });

            Assert.AreEqual("Test Exception", exception.Message);
        }


        private Mock<DbSet<Item>> GetDefaultItemsDbSet()
        {
            Item item1 = new Item { ItemId = 1, ItemName = "test item1", ItemDescription = "desc 1", ItemPrice = 55 , AvailableQuantity =3 };
            Item item2 = new Item { ItemId = 2, ItemName = "test item2", ItemDescription = "desc 2", ItemPrice = 48 };
            Item item3 = new Item { ItemId = 3, ItemName = "test item3", ItemDescription = "desc 3", ItemPrice = 47 };

            var itemList = new List<Item> { item1, item2, item3 }.AsQueryable();

            var mockItemSet = new Mock<DbSet<Item>>();
            mockItemSet.As<IQueryable<Item>>().Setup(m => m.Provider).Returns(itemList.Provider);
            mockItemSet.As<IQueryable<Item>>().Setup(m => m.Expression).Returns(itemList.Expression);
            mockItemSet.As<IQueryable<Item>>().Setup(m => m.ElementType).Returns(itemList.ElementType);
            mockItemSet.As<IQueryable<Item>>().Setup(m => m.GetEnumerator()).Returns(itemList.GetEnumerator());

            return mockItemSet;
        }

        private Mock<DbSet<Category>> GetDefaultCategoriesDbSet()
        {
            Category apparels = new Category { CategoryId = 1, CategoryName = "apparels" };
            Category accessories = new Category { CategoryId = 2, CategoryName = "accessories" };
            Category others = new Category { CategoryId = 3, CategoryName = "others" };

            var categoryList = new List<Category> {apparels, accessories, others}.AsQueryable();

            var mockCategorySet = new Mock<DbSet<Category>>();
            mockCategorySet.As<IQueryable<Category>>().Setup(m => m.Provider).Returns(categoryList.Provider);
            mockCategorySet.As<IQueryable<Category>>().Setup(m => m.Expression).Returns(categoryList.Expression);
            mockCategorySet.As<IQueryable<Category>>().Setup(m => m.ElementType).Returns(categoryList.ElementType);
            mockCategorySet.As<IQueryable<Category>>().Setup(m => m.GetEnumerator()).Returns(categoryList.GetEnumerator());

            return mockCategorySet;
        }
    }
}