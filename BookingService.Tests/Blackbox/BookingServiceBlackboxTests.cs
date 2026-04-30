using BookingService.DTO;
using Moq;
using BookingService.Services;
using BookingService.Repositories;
using BookingService.Validators;
using BookingService.Messaging;

namespace BookingService.Tests.Blackbox;

public class BookingServiceBlackboxTests
{
    private readonly IBookingService _bookingService;
    private readonly Mock<IBookingReadRepository> _mockBookingReadRepository;
    private readonly Mock<IBookingWriteRepository> _mockBookingWriteRepository;
    private readonly Mock<IPricingRepository> _mockPricingRepository;
    private readonly IBookingValidator _bookingValidator;
    private readonly Mock<IBookingEventPublisher> _mockBookingEventPublisher;

    public BookingServiceBlackboxTests()
    {
        _mockBookingReadRepository = new Mock<IBookingReadRepository>();
        _mockBookingWriteRepository = new Mock<IBookingWriteRepository>();
        _mockPricingRepository = new Mock<IPricingRepository>();
        _bookingValidator = new BookingValidator();
        _mockBookingEventPublisher = new Mock<IBookingEventPublisher>();
        
        _mockPricingRepository.Setup(x => x.GetPricingAsync())
            .ReturnsAsync(new Models.Pricing
            {
                Id = Guid.NewGuid(),
                ExtraBaggageFee = 200,
                ChildDiscount = 0.5m
            });
        
        _bookingService = new Services.BookingService(
            _mockBookingReadRepository.Object, 
            _mockBookingWriteRepository.Object, 
            _mockPricingRepository.Object,
            _bookingValidator,
            _mockBookingEventPublisher.Object);
    }

    private CreateBookingRequest ValidRequest(
        int passengerCount = 1,
        DateTime? dateOfBirth = null,
        int leadPassengerCount = 1,
        bool hasExtraBaggage = false, 
        bool isOneWay = true,
        string? returnFlightId = null)
    {
        var passengers = new List<CreatePassengerRequest>();
        for (int i = 0; i < passengerCount; i++)
        {
            passengers.Add(new CreatePassengerRequest
            {
                FirstName = "TestName",
                LastName = "TestLastName",
                DateOfBirth = dateOfBirth ?? new DateTime(1993, 2, 4),
                PassportNumber = "Pass12345",
                Nationality = "Danish",
                IsLeadPassenger = i < leadPassengerCount,
                HasExtraBaggage = hasExtraBaggage
            });
        }

        return new CreateBookingRequest
        {
            FlightId = "Flight123",
            ReturnFlightId = returnFlightId,
            IsOneWay = isOneWay,
            SeatClass = Models.SeatClass.Economy,
            ContactEmail = "test@test.com",
            ContactPhone = "12345678",
            TicketPrice = 1000,
            Passengers = passengers,
        };
    }

    //Passenger count BVA
    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public async Task CreateBooking_WithInvalidPassengerCount_ThrowsArgumentException(int passengerCount)
    {
        var request = ValidRequest(passengerCount);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _bookingService.CreateBookingAsync(request, "testuser"));
    }

    //Passenger count BVA
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(8)]
    [InlineData(9)]
    public async Task CreateBooking_WithValidPassengerCount_ReturnsCorrectPassengerCount(int passengerCount)
    {
        var request = ValidRequest(passengerCount);
        var result = await _bookingService.CreateBookingAsync(request, "testuser");
        
        Assert.Equal(passengerCount, result.Passengers.Count);
    }
    
    //Lead passenger count BVA
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task CreateBooking_WithInvalidLeadPassengerCount_ThrowsArgumentException(int leadPassengerCount)
    {
        var request = ValidRequest(passengerCount: 2, leadPassengerCount: leadPassengerCount);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _bookingService.CreateBookingAsync(request, "testuser"));
    }
    
    [Fact]
    public async Task CreateBooking_WithOneLeadPassenger_ReturnsBooking()
    {
        var request = ValidRequest();
        var result = await _bookingService.CreateBookingAsync(request, "testuser");
        
        Assert.NotNull(result);
    }
    
    //Child discount BVA
    [Fact]
    public async Task CreateBooking_PassengerAged11_ReturnsDiscountedPrice()
    {
        var request = ValidRequest(dateOfBirth: new  DateTime(2015, 2, 8));
        var result =  await _bookingService.CreateBookingAsync(request, "testuser");
        
        Assert.Equal(500, result.TotalPrice);
    }

    [Fact]
    public async Task CreateBooking_PassengerAged12_ReturnsFullPrice()
    {
        var request = ValidRequest(dateOfBirth: new DateTime(2014, 2, 8));
        var result =  await _bookingService.CreateBookingAsync(request, "testuser");
        
        Assert.Equal(1000, result.TotalPrice);
    }

    [Fact]
    public async Task CreateBooking_PassengerAged13_ReturnsFullPrice()
    {
        var request = ValidRequest(dateOfBirth: new DateTime(2013, 2, 8));
        var result =  await _bookingService.CreateBookingAsync(request, "testuser");
        
        Assert.Equal(1000, result.TotalPrice);
    }
    
    //Total price decision table
    [Fact]
    public async Task CreateBooking_AdultWithNoBaggage_ReturnsFullPrice()
    {
        var request = ValidRequest();
        var result = await _bookingService.CreateBookingAsync(request, "testuser");
        
        Assert.Equal(1000, result.TotalPrice);
    }

    [Fact]
    public async Task CreateBooking_AdultWithExtraBaggage_ReturnsFullPricePlusBaggageFee()
    {
        var request = ValidRequest(hasExtraBaggage: true);
        var result = await _bookingService.CreateBookingAsync(request, "testuser");
        
        Assert.Equal(1200, result.TotalPrice);
    }

    [Fact]
    public async Task CreateBooking_ChildWithNoBaggage_ReturnsDiscountedPrice()
    {
        var request = ValidRequest(dateOfBirth: new DateTime(2018, 5, 8));
        var result = await _bookingService.CreateBookingAsync(request, "testuser");
        
        Assert.Equal(500, result.TotalPrice);
    }

    [Fact]
    public async Task CreateBooking_ChildWithExtraBaggage_ReturnsDiscountedPricePlusBaggageFee()
    {
        var request = ValidRequest(dateOfBirth: new DateTime(2018, 5, 8), hasExtraBaggage: true);
        var result = await _bookingService.CreateBookingAsync(request, "testuser");
        
        Assert.Equal(700, result.TotalPrice);
    }
    
    //IsOneWay decision table
    [Fact]
    public async Task CreateBooking_OneWayWithNoReturnFlight_ReturnsBooking()
    {
        var request = ValidRequest(isOneWay: true, returnFlightId: null);
        var result = await _bookingService.CreateBookingAsync(request, "testuser");
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task CreateBooking_OneWayWithReturnFlight_ThrowsArgumentException()
    {
        var request = ValidRequest(isOneWay: true, returnFlightId: "ReturnFlight123");
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _bookingService.CreateBookingAsync(request, "testuser"));
    }

    [Fact]
    public async Task CreateBooking_ReturnTicketWithNoReturnFlight_ThrowsArgumentException()
    {
        var request = ValidRequest(isOneWay: false, returnFlightId: null);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _bookingService.CreateBookingAsync(request, "testuser"));
    }

    [Fact]
    public async Task CreateBooking_ReturnTicketWithReturnFlight_ReturnsBooking()
    {
        var request = ValidRequest(isOneWay: false, returnFlightId: "ReturnFlight123");
        var result = await _bookingService.CreateBookingAsync(request, "testuser");
        
        Assert.NotNull(result);
    }
    
    //Passenger count EP
    [Fact]
    public async Task CreateBooking_WithNegativePassengerCount_ThrowsArgumentException()
    {
        var request = ValidRequest(passengerCount: -5);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _bookingService.CreateBookingAsync(request, "testuser"));
    }

    [Fact]
    public async Task CreateBooking_WithFivePassengers_ReturnsCorrectPassengerCount()
    {
        var request = ValidRequest(passengerCount: 5);
        var result = await _bookingService.CreateBookingAsync(request, "testuser");
        
        Assert.Equal(5, result.Passengers.Count);
    }
    
    [Fact]
    public async Task CreateBooking_WithFifteenPassengers_ThrowsArgumentException()
    {
        var request = ValidRequest(passengerCount: 15);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _bookingService.CreateBookingAsync(request, "testuser"));
    }
}