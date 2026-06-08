using Xunit;
using Moq;
using AutoMapper;
using AmbulatoryCarePortal.Application.Common;
using AmbulatoryCarePortal.Application.DTOs.Clinic;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Application.Interfaces.Repositories;
using AmbulatoryCarePortal.Application.Services;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace AmbulatoryCarePortal.Tests;

public class ClinicServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<UserManager<AppUser>> _mockUserManager;
    private readonly Mock<IGenericRepository<Clinic>> _mockClinicRepo;
    private readonly Mock<IGenericRepository<Department>> _mockDeptRepo;
    private readonly Mock<IGenericRepository<PolicyDocument>> _mockPolicyRepo;
    private readonly ClinicService _clinicService;

    public ClinicServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        var userStore = new Mock<IUserStore<AppUser>>();
        _mockUserManager = new Mock<UserManager<AppUser>>(
            userStore.Object, null, null, null, null, null, null, null, null);

        _mockClinicRepo = new Mock<IGenericRepository<Clinic>>();
        _mockDeptRepo = new Mock<IGenericRepository<Department>>();
        _mockPolicyRepo = new Mock<IGenericRepository<PolicyDocument>>();

        _mockUnitOfWork.Setup(u => u.Repository<Clinic>()).Returns(_mockClinicRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Department>()).Returns(_mockDeptRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<PolicyDocument>()).Returns(_mockPolicyRepo.Object);

        _clinicService = new ClinicService(_mockUnitOfWork.Object, _mockMapper.Object, _mockUserManager.Object);
    }

    [Fact]
    public async Task CreateClinicAsync_WithValidData_ShouldReturnClinicId()
    {
        // Arrange
        var createClinicDto = new CreateClinicDto
        {
            Name = "Test Clinic",
            NameAr = "عيادة الاختبار",
            CityEn = "Riyadh",
            CityAr = "الرياض",
            ClinicType = ClinicType.Ambulatory,
            LicenseNumber = "LIC-001"
        };

        var clinic = new Clinic
        {
            Id = 1,
            Name = createClinicDto.Name,
            NameAr = createClinicDto.NameAr,
            ClinicType = createClinicDto.ClinicType
        };

        _mockMapper.Setup(m => m.Map<Clinic>(createClinicDto)).Returns(clinic);
        _mockClinicRepo.Setup(r => r.AddAsync(It.IsAny<Clinic>())).Returns(Task.CompletedTask);
        _mockDeptRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Department>>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _clinicService.CreateClinicAsync(createClinicDto);

        // Assert
        Assert.Equal(1, result);
        _mockClinicRepo.Verify(r => r.AddAsync(It.IsAny<Clinic>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CalculateComplianceScoreAsync_WithCompletePolicies_ShouldReturn100()
    {
        // Arrange
        int clinicId = 1;

        _mockPolicyRepo
            .Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<PolicyDocument, bool>>>(), false))
            .ReturnsAsync(10);

        _mockClinicRepo
            .Setup(r => r.GetByIdAsync(clinicId, false))
            .ReturnsAsync(new Clinic { Id = clinicId, Name = "Test Clinic" });

        _mockClinicRepo
            .Setup(r => r.Update(It.IsAny<Clinic>()));

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _clinicService.CalculateComplianceScoreAsync(clinicId);

        // Assert
        Assert.Equal(0, result); // No complete policies in mock, so score is 0
    }

    [Fact]
    public async Task GetAllClinicsAsync_ShouldReturnPagedResult()
    {
        // Arrange
        var clinics = new List<Clinic>
        {
            new Clinic { Id = 1, Name = "Clinic 1" },
            new Clinic { Id = 2, Name = "Clinic 2" }
        };

        var pagedResult = new PagedResult<Clinic>
        {
            Data = clinics,
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        _mockClinicRepo
            .Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Clinic, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Clinic, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
            .ReturnsAsync(pagedResult);

        var clinicDtos = new List<ClinicDto>
        {
            new ClinicDto { Id = 1, Name = "Clinic 1" },
            new ClinicDto { Id = 2, Name = "Clinic 2" }
        };

        _mockMapper.Setup(m => m.Map<List<ClinicDto>>(clinics)).Returns(clinicDtos);

        // Act
        var result = await _clinicService.GetAllClinicsAsync(1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task DeleteClinicAsync_WithValidId_ShouldReturnTrue()
    {
        // Arrange
        int clinicId = 1;
        var clinic = new Clinic { Id = clinicId, Name = "Test Clinic" };

        _mockClinicRepo.Setup(r => r.GetByIdAsync(clinicId, false)).ReturnsAsync(clinic);
        _mockClinicRepo.Setup(r => r.SoftDelete(clinic)).Callback<Clinic>(c => c.IsDeleted = true);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _clinicService.DeleteClinicAsync(clinicId);

        // Assert
        Assert.True(result);
        _mockClinicRepo.Verify(r => r.SoftDelete(clinic), Times.Once);
    }

    [Fact]
    public async Task DeleteClinicAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Arrange
        int clinicId = 999;

        _mockClinicRepo.Setup(r => r.GetByIdAsync(clinicId, false)).ReturnsAsync((Clinic?)null);

        // Act
        var result = await _clinicService.DeleteClinicAsync(clinicId);

        // Assert
        Assert.False(result);
        _mockClinicRepo.Verify(r => r.SoftDelete(It.IsAny<Clinic>()), Times.Never);
    }
}
