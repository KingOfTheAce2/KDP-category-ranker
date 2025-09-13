using FluentAssertions;
using KDP_CATEGORY_RANKERCore.Enums;
using KDP_CATEGORY_RANKERCore.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KDP_CATEGORY_RANKERTests.Unit;

public class SalesEstimationServiceTests
{
    private readonly Mock<ILogger<SalesEstimationService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly SalesEstimationService _service;

    public SalesEstimationServiceTests()
    {
        _loggerMock = new Mock<ILogger<SalesEstimationService>>();
        _configurationMock = new Mock<IConfiguration>();
        
        // Setup default configuration values
        _configurationMock.Setup(x => x.GetValue<double>("SalesEstimation:Kindle:CoefficientA", 5500.0))
                         .Returns(5500.0);
        _configurationMock.Setup(x => x.GetValue<double>("SalesEstimation:Kindle:CoefficientB", -0.83))
                         .Returns(-0.83);
        _configurationMock.Setup(x => x.GetValue<double>("SalesEstimation:Print:CoefficientA", 2600.0))
                         .Returns(2600.0);
        _configurationMock.Setup(x => x.GetValue<double>("SalesEstimation:Print:CoefficientB", -0.75))
                         .Returns(-0.75);

        _service = new SalesEstimationService(_configurationMock.Object, _loggerMock.Object);
    }

    [Theory]
    [InlineData(100, BookFormat.Kindle, 213)]  // Expected: 5500 * 100^-0.83 ≈ 213
    [InlineData(1000, BookFormat.Kindle, 42)] // Expected: 5500 * 1000^-0.83 ≈ 42
    [InlineData(100, BookFormat.Paperback, 104)] // Expected: 2600 * 100^-0.75 ≈ 104
    [InlineData(1000, BookFormat.Paperback, 25)] // Expected: 2600 * 1000^-0.75 ≈ 25
    public void EstimateDailySales_ValidBSR_ReturnsCorrectEstimate(int bsr, BookFormat format, int expectedSales)
    {
        // Act
        var result = _service.EstimateDailySales(bsr, format);

        // Assert
        result.Should().BeCloseTo(expectedSales, 10); // Allow 10% tolerance
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public void EstimateDailySales_ZeroBSR_ReturnsZero()
    {
        // Act
        var result = _service.EstimateDailySales(0, BookFormat.Kindle);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void EstimateDailySales_NegativeBSR_ReturnsZero()
    {
        // Act
        var result = _service.EstimateDailySales(-100, BookFormat.Kindle);

        // Assert
        result.Should().Be(0);
    }

    [Theory]
    [InlineData(10, 9.99, 0.6, 1798.20)]  // 10 * 9.99 * 30 * 0.6
    [InlineData(50, 19.99, 0.5, 14992.50)] // 50 * 19.99 * 30 * 0.5
    public void EstimateMonthlyEarnings_ValidInput_ReturnsCorrectEstimate(int dailySales, decimal price, double revenueFactor, decimal expected)
    {
        // Act
        var result = _service.EstimateMonthlyEarnings(dailySales, price, revenueFactor);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void UpdateCoefficients_ValidInput_UpdatesCoefficients()
    {
        // Arrange
        var newA = 6000.0;
        var newB = -0.90;

        // Act
        _service.UpdateCoefficients(BookFormat.Kindle, newA, newB);
        var (coefficientA, coefficientB) = _service.GetCoefficients(BookFormat.Kindle);

        // Assert
        coefficientA.Should().Be(newA);
        coefficientB.Should().Be(newB);
    }
}