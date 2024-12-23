using MyChat.Repositories;  
using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using MyChat.Controllers;  
using MyChat.Models;  
using System;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;



namespace TestProject1
{

    public class ChatControllerTests
    {
        private readonly Mock<IMessageRepository> _messageRepositoryMock;
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly ChatController _controller;
        private readonly MyChatContext _context;

        public ChatControllerTests()
        {
            // Мок репозитория сообщений
            _messageRepositoryMock = new Mock<IMessageRepository>();

            // Настройка базы данных в памяти для MyChatContext
            var options = new DbContextOptionsBuilder<MyChatContext>()
                .UseInMemoryDatabase("TestDatabase")  // Используем in-memory базу данных
                .Options;

            // Создание контекста с использованием in-memory базы данных
            _context = new MyChatContext(options);

            // Создание мока для UserManager<ApplicationUser>
            var store = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(
                store.Object, null, null, null, null, null, null, null, null);

            // Инжекция зависимостей в контроллер
            _controller = new ChatController(_messageRepositoryMock.Object, _userManagerMock.Object, _context);
        }

        [Fact]
        public async Task Create_ReturnsBadRequest_WhenMessageInscriptionIsEmpty()
        {
            // Arrange
            var message = new Message { Inscription = "" };

            // Act
            var result = await _controller.Create(message);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);

            // Преобразуем в dynamic, чтобы работать с анонимным объектом
            dynamic returnValue = badRequestResult.Value;

            // Проверяем, что поле error существует и не равно null
            Assert.NotNull(returnValue?.error);

            // Проверяем, что ошибка соответствует ожидаемому значению
            Assert.Equal("Сообщение не может быть пустым.", returnValue.error);
        }


        [Fact]
        public async Task Create_ReturnsUnauthorized_WhenUserIsNotFound()
        {
            // Arrange
            var message = new Message { Inscription = "Valid message" };

            // Мокируем, чтобы GetUserAsync возвращал null
            _userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                            .ReturnsAsync((User)null);

            // Act
            var result = await _controller.Create(message);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var returnValue = unauthorizedResult.Value as dynamic; // Преобразуем в dynamic
            Assert.Equal("Не удалось определить пользователя.", returnValue.error);
        }


        [Fact]
        public async Task Create_ReturnsJsonResult_WhenMessageIsValid()
        {
            // Arrange
            var message = new Message { Inscription = "Valid message" };

            var user = new User { Id = 123, UserName = "testUser", Avatar = "avatar.png" };

            // Мокируем, чтобы GetUserAsync возвращал пользователя
            _userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                            .ReturnsAsync(user);

            var result = await _controller.Create(message);

            var jsonResult = Assert.IsType<JsonResult>(result);
            var returnValue = jsonResult.Value as dynamic;
            Assert.Equal(user.Avatar, returnValue.avatar);
            Assert.Equal(message.DateOfDispatch.ToString("dd.MM.yyyy HH:mm:ss"), returnValue.dateOfDispatch);
            Assert.Equal(user.UserName, returnValue.userName);
            Assert.Equal(user.Id, returnValue.userid);
            Assert.Equal(message.Inscription, returnValue.inscription);
        }

        

    }

}
