using System.Net;
using IntegrationTests.Infrastructure;
using Schichtpilot.Models.Enums;
using Xunit;

namespace IntegrationTests.Controllers;

public class UserControllerTests : IntegrationTestBase
{
    public UserControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateUser_BindsBodyAndRoutesToService()
    {
        var payload = new
        {
            Email = "new.user@example.com",
            AddressDto = new
            {
                Street = "Main Street 1",
                City = "Berlin",
                PostalCode = 1011
            },
            FirstName = "Maria",
            LastName = "Muster",
            Birthdate = new DateTime(1995, 5, 12),
            Password = "StrongPass1!"
        };

        var response = await Client.PostAsync("/api/User", JsonContent(payload));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(UserService.LastCreatedUserDto);
        Assert.Equal(payload.Email, UserService.LastCreatedUserDto?.Email);
        Assert.Equal(payload.FirstName, UserService.LastCreatedUserDto?.FirstName);
        Assert.Equal(payload.LastName, UserService.LastCreatedUserDto?.LastName);
        Assert.NotNull(UserService.LastCreatedUserDto?.AddressDto);
        Assert.Equal(payload.AddressDto.Street, UserService.LastCreatedUserDto?.AddressDto.Street);
        Assert.Equal(payload.AddressDto.City, UserService.LastCreatedUserDto?.AddressDto.City);
        Assert.Equal(payload.AddressDto.PostalCode, UserService.LastCreatedUserDto?.AddressDto.PostalCode);
        Assert.Equal(payload.Password, UserService.LastCreatedPassword);
    }

    [Fact]
    public async Task GetUserData_BindsRouteId()
    {
        var response = await Client.GetAsync("/api/User/42");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(42, UserService.LastGetUserDataUserId);
    }

    [Fact]
    public async Task GetUsers_BindsQueryParameters()
    {
        var response = await Client.GetAsync(
            "/api/User/all?Page=2&PageSize=10&SortProperty=FirstName&Ascending=true&AccountStatus=Ok&Searchstring=anna&JobFilters=Chef&JobFilters=Waiter");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.NotNull(UserService.LastGetUsersPagination);
        Assert.Equal(2, UserService.LastGetUsersPagination?.Page);
        Assert.Equal(10, UserService.LastGetUsersPagination?.PageSize);

        Assert.NotNull(UserService.LastGetUsersSorting);
        Assert.Equal(UserSortEnum.FirstName, UserService.LastGetUsersSorting?.SortProperty);
        Assert.True(UserService.LastGetUsersSorting?.Ascending);

        Assert.NotNull(UserService.LastGetUsersFilter);
        Assert.Equal(AccountStatusEnum.Ok, UserService.LastGetUsersFilter?.AccountStatus);
        Assert.Equal("anna", UserService.LastGetUsersFilter?.Searchstring);
        Assert.NotNull(UserService.LastGetUsersFilter?.JobFilters);
        Assert.Contains("Chef", UserService.LastGetUsersFilter?.JobFilters ?? Array.Empty<string>());
        Assert.Contains("Waiter", UserService.LastGetUsersFilter?.JobFilters ?? Array.Empty<string>());
    }
}
