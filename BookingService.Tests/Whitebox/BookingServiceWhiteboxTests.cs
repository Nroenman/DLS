using BookingService.DTO;
using Moq;
using BookingService.Services;
using BookingService.Repositories;
using BookingService.Validators;

namespace BookingService.Tests.Whitebox;

public class BookingServiceWhiteboxTests
{
    private readonly IBookingService _bookingService;
    private readonly Mock<IBookingReadRepository> _mockBookingReadRepository;
    private readonly Mock<IBookingWriteRepository> _mockBookingWriteRepository;
    private readonly Mock<IPricingRepository> _mockPricingRepository;
    private readonly IBookingValidator _bookingValidator;

    public BookingServiceWhiteboxTests()
    {
        _mockBookingReadRepository = new Mock<IBookingReadRepository>();
        _mockBookingWriteRepository = new Mock<IBookingWriteRepository>();
        _mockPricingRepository = new Mock<IPricingRepository>();
        _bookingValidator = new BookingValidator();

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
            _bookingValidator);
    }

    private CreateBookingRequest ValidRequest()
    {
        return new CreateBookingRequest
        {
            FlightId = "Flight123",
            ReturnFlightId = null,
            IsOneWay = true,
            SeatClass = Models.SeatClass.Economy,
            ContactEmail = "test@test.com",
            ContactPhone = "12345678",
            TicketPrice = 1000,
            Passengers = ValidPassengerList()
        };
    }

    private List<CreatePassengerRequest> ValidPassengerList()
    {
        return new List<CreatePassengerRequest>
        {
            new CreatePassengerRequest
            {
                FirstName = "TestName",
                LastName = "TestLastName",
                DateOfBirth = new DateTime(1993, 2, 4),
                PassportNumber = "Pass12345",
                Nationality = "Danish",
                IsLeadPassenger = true,
                HasExtraBaggage = false
            }
        };
    }

    //Passenger count
    [Fact]
    public void ValidatePassengerCount_WithNullPassengers_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            _bookingValidator.ValidatePassengerCount(null));

        Assert.Equal("Number of passengers must be greater than 0", exception.Message);
    }

    [Fact]
    public async Task ValidatePassengerCount_WithValidPassengers_ReturnsResponse()
    {
        var request = ValidRequest();
        var response = await _bookingService.CreateBookingAsync(request, "testuser");

        Assert.Equal(request.Passengers.Count, response.Passengers.Count);
    }

    //Passenger details
    [Fact]
    public void ValidatePassengerDetails_WithEmptyFirstName_ThrowsArgumentException()
    {
        var passengers = ValidPassengerList();
        passengers[0].FirstName = "";
        var exception = Assert.Throws<ArgumentException>(() =>
            _bookingValidator.ValidatePassengerDetails(passengers));

        Assert.Equal("First name is required", exception.Message);
    }

    [Fact]
    public void ValidatePassengerDetails_WithEmptyLastName_ThrowsArgumentException()
    {
        var passengers = ValidPassengerList();
        passengers[0].LastName = "";
        var exception = Assert.Throws<ArgumentException>(() =>
            _bookingValidator.ValidatePassengerDetails(passengers));

        Assert.Equal("Last name is required", exception.Message);
    }

    [Fact]
    public void ValidatePassengerDetails_WithEmptyDateOfBirth_ThrowsArgumentException()
    {
        var passengers = ValidPassengerList();
        passengers[0].DateOfBirth = default;
        var exception = Assert.Throws<ArgumentException>(() =>
            _bookingValidator.ValidatePassengerDetails(passengers));

        Assert.Equal("Date of birth is required", exception.Message);
    }

    [Fact]
    public void ValidatePassengerDetails_WithFutureDateOfBirth_ThrowsArgumentException()
    {
        var passengers = ValidPassengerList();
        passengers[0].DateOfBirth = new DateTime(2028, 2, 4);
        var exception = Assert.Throws<ArgumentException>(() =>
            _bookingValidator.ValidatePassengerDetails(passengers));

        Assert.Equal("Date of birth must be in the past", exception.Message);
    }

    [Fact]
    public void ValidatePassengerDetails_WithEmptyPassportNumber_ThrowsArgumentException()
    {
        var passengers = ValidPassengerList();
        passengers[0].PassportNumber = "";
        var exception = Assert.Throws<ArgumentException>(() =>
            _bookingValidator.ValidatePassengerDetails(passengers));

        Assert.Equal("Passport number is required", exception.Message);
    }

    [Fact]
    public void ValidatePassengerDetails_WithEmptyNationality_ThrowsArgumentException()
    {
        var passengers = ValidPassengerList();
        passengers[0].Nationality = "";
        var exception = Assert.Throws<ArgumentException>(() =>
            _bookingValidator.ValidatePassengerDetails(passengers));

        Assert.Equal("Nationality is required", exception.Message);
    }

    [Fact]
    public async Task ValidatePassengerDetails_WithValidDetails_ReturnsResponse()
    {
        var request = ValidRequest();
        var response = await _bookingService.CreateBookingAsync(request, "testuser");

        Assert.Equal(request.Passengers[0].FirstName, response.Passengers[0].FirstName);
    }

    //Flight info
    [Fact]
    public void ValidateFlightInfo_WithEmptyFlightId_ThrowsArgumentException()
    {
        var request = ValidRequest();
        request.FlightId = "";
        var exception = Assert.Throws<ArgumentException>(() =>
            _bookingValidator.ValidateFlightInfo(request));

        Assert.Equal("Flight ID is required", exception.Message);
    }

    [Fact]
    public async Task ValidateFlightInfo_WithValidFlightId_ReturnsResponse()
    {
        var request = ValidRequest();
        var response = await _bookingService.CreateBookingAsync(request, "testuser");

        Assert.Equal(request.FlightId, response.FlightId);
    }

    //Booking details
    [Fact]
    public void ValidateBookingDetails_WithEmptySeatClass_ThrowsArgumentException()
    {
        var request = ValidRequest();
        request.SeatClass = null;
        var exception = Assert.Throws<ArgumentException>(() =>
            _bookingValidator.ValidateBookingDetails(request));

        Assert.Equal("Seat class is required", exception.Message);
    }

    [Fact]
    public void ValidateBookingDetails_WithZeroTicketPrice_ThrowsArgumentException()
    {
        var request = ValidRequest();
        request.TicketPrice = 0;
        var exception = Assert.Throws<ArgumentException>(() =>
            _bookingValidator.ValidateBookingDetails(request));

        Assert.Equal("Ticket price must be greater than 0", exception.Message);
    }

    [Fact]
    public void ValidateBookingDetails_WithEmptyContactEmail_ThrowsArgumentException()
    {
        var request = ValidRequest();
        request.ContactEmail = "";
        var exception = Assert.Throws<ArgumentException>(() =>
            _bookingValidator.ValidateBookingDetails(request));

        Assert.Equal("Contact email is required", exception.Message);
    }
    
    [Fact]
    public void ValidateBookingDetails_WithInvalidEmailFormat_ThrowsArgumentException()
    {
        var request = ValidRequest();
        request.ContactEmail = "notvalid@email";
        var exception = Assert.Throws<ArgumentException>(() =>
            _bookingValidator.ValidateBookingDetails(request));

        Assert.Equal("Contact email format is invalid", exception.Message);
    }

    [Fact]
    public void ValidateBookingDetails_WithEmptyContactPhone_ThrowsArgumentException()
    {
        var request = ValidRequest();
        request.ContactPhone = "";
        var exception = Assert.Throws<ArgumentException>(() =>
            _bookingValidator.ValidateBookingDetails(request));

        Assert.Equal("Contact phone number is required", exception.Message);
    }

    [Fact]
    public async Task ValidateBookingDetails_WithValidBookingDetails_ReturnsResponse()
    {
        var request = ValidRequest();
        var response = await _bookingService.CreateBookingAsync(request, "testuser");

        Assert.Equal(request.SeatClass, response.SeatClass);
    }

    //Get by userid
    [Fact]
    public void ValidateGetBookingsByUserId_WithEmptyUserId_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            _bookingValidator.ValidateGetBookingsByUserId(""));

        Assert.Equal("User ID is required", exception.Message);
    }
    
    //Update booking status
    [Fact]
    public void ValidateUpdateBookingStatus_WithInvalidStatus_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            _bookingValidator.ValidateUpdateBookingStatus((Models.BookingStatus)999));

        Assert.Equal("Invalid booking status", exception.Message);
    }
    
    //Birthday not happened 
    [Fact]
    public async Task CreateBookingAsync_WithPassengerBirthdayNotYetOccurredThisYear_ReturnsChildDiscount()
    {
        var request = ValidRequest();
        request.Passengers[0].DateOfBirth = new  DateTime(2015, 12, 24);
        var response = await _bookingService.CreateBookingAsync(request, "testuser");
        
        Assert.Equal(500, response.TotalPrice);
    }
    
    //Cancel booking
    [Fact]
    public void ValidateCancelBooking_WithNoBooking_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            _bookingValidator.ValidateCancelBooking(null, "testuser"));

        Assert.Equal("Booking not found", exception.Message);
    }
    
    [Fact]
    public void ValidateCancelBooking_WithWrongUserId_ThrowsArgumentException()
    {
        var booking = new Models.Booking
        {
            Id = Guid.NewGuid(),
            UserId = "correctuser",
            Status = Models.BookingStatus.Confirmed
        };

        var exception = Assert.Throws<ArgumentException>(() =>
            _bookingValidator.ValidateCancelBooking(booking, "wronguser"));

        Assert.Equal("Not authorized", exception.Message);
    }
    
    [Fact]
    public void ValidateCancelBooking_WithAlreadyCancelledBooking_ThrowsArgumentException()
    {
        var booking = new Models.Booking
        {
            Id = Guid.NewGuid(),
            UserId = "testuser",
            Status = Models.BookingStatus.Cancelled
        };

        var exception = Assert.Throws<ArgumentException>(() =>
            _bookingValidator.ValidateCancelBooking(booking, "testuser"));

        Assert.Equal("Booking is already cancelled", exception.Message);
    }
    
    [Fact]
    public void ValidateCancelBooking_WithValidBooking_DoesNotThrow()
    {
        var booking = new Models.Booking
        {
            Id = Guid.NewGuid(),
            UserId = "testuser",
            Status = Models.BookingStatus.Confirmed
        };

        var exception = Record.Exception(() =>
            _bookingValidator.ValidateCancelBooking(booking, "testuser"));

        Assert.Null(exception);
    }
    
    //Pricing
    [Fact]
    public async Task CreateBooking_WhenPricingIsNull_ThrowsInvalidOperationException()
    {
        _mockPricingRepository.Setup(x => x.GetPricingAsync())
            .ReturnsAsync((Models.Pricing)null);
        var request = ValidRequest();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _bookingService.CreateBookingAsync(request, "testuser"));
    }
}