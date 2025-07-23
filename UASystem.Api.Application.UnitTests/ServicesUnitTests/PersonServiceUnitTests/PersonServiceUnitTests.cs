using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UASystem.Api.Application.Contracts.PersonDtos.Responses;
using UASystem.Api.Application.Enums;
using UASystem.Api.Application.ILoggingService;
using UASystem.Api.Application.IServices;
using UASystem.Api.Application.Models;
using UASystem.Api.Application.Services.PersonServices;
using UASystem.Api.Application.Services.PersonServices.Commands;
using UASystem.Api.Application.Services.PersonServices.PersonSubServices;
using UASystem.Api.Domain.Aggregate;
using UASystem.Api.Domain.DomainExceptions;
using UASystem.Api.Domain.Repositories;
using UASystem.Api.Domain.ValueObjects.PersonNameValues;

namespace UASystem.Api.Application.UnitTests.ServicesUnitTests.PersonServiceUnitTests
{
    public class PersonServiceUnitTests
    {

        [Fact]
        public async Task CreatePersonAsync_ShouldReturnSuccess_WhenPersonIsCreated()
        {
            // Arrange
            var command = new CreatePersonCommand
            {
                Title = "Mr.",
                FirstName = "John",
                MiddleName = "T.",
                LastName = "Doe",
                Suffix = "Jr.",
                CreatedBy = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid().ToString()
            };

            var name = PersonName.Create(command.FirstName,command.MiddleName, command.LastName, command.Title, command.Suffix);
            var fakePerson = Person.Create(command.CreatedBy, name);


            var mockRepository = new Mock<IPersonRepository>();
            var mockLogger = new Mock<IAppLogger<PersonService>>();
            var mockErrorHandler = new Mock<IErrorHandlingService>();
            var mockCreatePersonService = new Mock<ICreatePersonService>();

            mockCreatePersonService
                .Setup(s => s.CreatePerson(command))
                .Returns(fakePerson);

            mockRepository.Setup(r => r.CreateAsync(fakePerson, command.CreatedBy, command.CorrelationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var service = new PersonService(mockRepository.Object, mockLogger.Object, mockErrorHandler.Object, mockCreatePersonService.Object);

            // Act
            var result = await service.CreatePersonAsync(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            mockRepository.Verify(r => r.CreateAsync(fakePerson, command.CreatedBy, command.CorrelationId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreatePersonAsync_ShouldReturnFailure_WhenPersonCreationFails()
        {
            // Arrange
            var command = new CreatePersonCommand
            {
                Title = "Mr.",
                FirstName = "John",
                MiddleName = "T.",
                LastName = "Doe",
                Suffix = "Jr.",
                CreatedBy = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid().ToString()
            };
            var mockRepository = new Mock<IPersonRepository>();
            var mockLogger = new Mock<IAppLogger<PersonService>>();
            var mockErrorHandler = new Mock<IErrorHandlingService>();
            var mockCreatePersonService = new Mock<ICreatePersonService>();

            mockRepository.Setup(r => r.CreateAsync(It.IsAny<Person>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var service = new PersonService(mockRepository.Object, mockLogger.Object, mockErrorHandler.Object, mockCreatePersonService.Object);

            // Act
            var result = await service.CreatePersonAsync(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.True(result.Errors.Any(), "Expected at least one error.");
            Assert.Equal(ErrorCode.ResourceCreationFailed, result.Errors.First().Code);
        }

        [Fact]
        public async Task CreatePersonAsync_ShouldHandleOperationCanceledException()
        {
            // Arrange
            var command = new CreatePersonCommand
            {
                Title = "Mr.",
                FirstName = "John",
                MiddleName = "T.",
                LastName = "Doe",
                Suffix = "Jr.",
                CreatedBy = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid().ToString()
            };
            var token = new CancellationToken(true); // already canceled
            var mockRepository = new Mock<IPersonRepository>();
            var mockLogger = new Mock<IAppLogger<PersonService>>();
            var mockErrorHandler = new Mock<IErrorHandlingService>();
            var mockCreatePersonService = new Mock<ICreatePersonService>();

            mockErrorHandler
                .Setup(e => e.HandleCancelationToken<CreatedResponseDto>(It.IsAny<OperationCanceledException>(), command.CorrelationId))
                .Returns(OperationResult<CreatedResponseDto>.Failure(new Error(ErrorCode.OperationCancelled,
                $"The operation was Canceled: ", $"Canceled", "correlationId")));

            var service = new PersonService(mockRepository.Object, mockLogger.Object, mockErrorHandler.Object, mockCreatePersonService.Object);

            // Act
            var result = await service.CreatePersonAsync(command, token);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.True(result.Errors.Any(), "Expected at least one error.");
            Assert.Equal("The operation was Canceled: ", result.Errors.First().Message);
        }

        [Fact]
        public async Task CreatePersonAsync_ShouldHandleUnexpectedException()
        {
            // Arrange
            var command = new CreatePersonCommand
            {
                Title = "Mr.",
                FirstName = "John",
                MiddleName = "T.",
                LastName = "Doe",
                Suffix = "Jr.",
                CreatedBy = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid().ToString()
            };
            var mockRepository = new Mock<IPersonRepository>();
            var mockLogger = new Mock<IAppLogger<PersonService>>();
            var mockErrorHandler = new Mock<IErrorHandlingService>();
            var mockCreatePersonService = new Mock<ICreatePersonService>();

            mockRepository
                .Setup(r => r.CreateAsync(It.IsAny<Person>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Boom"));

            mockErrorHandler
                .Setup(e => e.HandleException<CreatedResponseDto>(It.IsAny<Exception>(), command.CorrelationId))
                .Returns(OperationResult<CreatedResponseDto>.Failure(new Error(ErrorCode.OperationCancelled,
                $"Boom", $"Canceled", "correlationId")));

            var service = new PersonService(mockRepository.Object, mockLogger.Object, mockErrorHandler.Object, mockCreatePersonService.Object);

            // Act
            var result = await service.CreatePersonAsync(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.True(result.Errors.Any(), "Expected at least one error.");
            Assert.Equal("Boom", result.Errors.First().Message);
        }

    }
}
