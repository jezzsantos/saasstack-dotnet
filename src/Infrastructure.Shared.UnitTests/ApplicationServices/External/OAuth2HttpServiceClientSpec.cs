using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Shared.ApplicationServices.External;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.OAuth2;
using Infrastructure.Web.Interfaces.Clients;
using Moq;
using UnitTesting.Common;
using UnitTesting.Common.Validation;
using Xunit;

namespace Infrastructure.Shared.UnitTests.ApplicationServices.External;

[Trait("Category", "Unit")]
public class OAuth2HttpServiceClientSpec
{
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IServiceClient> _client;
    private readonly OAuth2HttpServiceClient _serviceClient;

    public OAuth2HttpServiceClientSpec()
    {
        _caller = new Mock<ICallerContext>();
        var recorder = new Mock<IRecorder>();
        _client = new Mock<IServiceClient>();
        _serviceClient = new OAuth2HttpServiceClient(recorder.Object, _client.Object, "aclientid", "aclientsecret",
            "aredirecturi");
    }

    [Fact]
    public async Task WhenExchangeCodeForTokensAsyncAndClientThrows_ThenReturnsError()
    {
        _client.Setup(c => c.PostAsync(It.IsAny<ICallerContext>(), It.IsAny<OAuth2GrantAuthorizationRequest>(),
                It.IsAny<Action<HttpRequestMessage>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("amessage"));
        var options = new OAuth2CodeTokenExchangeOptions("aservicename", "acode", scope: "ascope");

        var result = await _serviceClient.ExchangeCodeForTokensAsync(_caller.Object, options, CancellationToken.None);

        result.Should().BeError(ErrorCode.Unexpected, "amessage");
        _client.Verify(c => c.PostAsync(_caller.Object, It.Is<OAuth2GrantAuthorizationRequest>(req =>
                req.GrantType == "authorization_code"
                && req.Code == "acode"
                && req.ClientId == "aclientid"
                && req.ClientSecret == "aclientsecret"
                && req.Scope == "ascope"
                && req.RedirectUri == "aredirecturi"
            ),
            null, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenExchangeCodeForTokensAsyncAndReceivesAllTokens_ThenReturnsAllTokens()
    {
        _client.Setup(c => c.PostAsync(It.IsAny<ICallerContext>(), It.IsAny<OAuth2GrantAuthorizationRequest>(),
                It.IsAny<Action<HttpRequestMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OAuth2GrantAuthorizationResponse
            {
                AccessToken = "anaccesstoken",
                RefreshToken = "arefreshtoken",
                IdToken = "anidtoken",
                ExpiresIn = 60
            });
        var options = new OAuth2CodeTokenExchangeOptions("aservicename", "acode", scope: "ascope");

        var result = await _serviceClient.ExchangeCodeForTokensAsync(_caller.Object, options, CancellationToken.None);

        var now = DateTime.UtcNow.ToNearestSecond();
        var expiresOn = now.AddSeconds(60);
        result.Should().BeSuccess();
        result.Value.Count().Should().Be(3);
        result.Value[0].Type.Should().Be(TokenType.AccessToken);
        result.Value[0].Value.Should().Be("anaccesstoken");
        result.Value[0].ExpiresOn.Should().BeNear(expiresOn);
        result.Value[1].Type.Should().Be(TokenType.RefreshToken);
        result.Value[1].Value.Should().Be("arefreshtoken");
        result.Value[1].ExpiresOn.Should().BeNear(now.AddDays(1));
        result.Value[2].Type.Should().Be(TokenType.OtherToken);
        result.Value[2].Value.Should().Be("anidtoken");
        result.Value[2].ExpiresOn.Should().BeNear(now.AddHours(1));
        _client.Verify(c => c.PostAsync(_caller.Object, It.Is<OAuth2GrantAuthorizationRequest>(req =>
                req.GrantType == "authorization_code"
                && req.Code == "acode"
                && req.ClientId == "aclientid"
                && req.ClientSecret == "aclientsecret"
                && req.Scope == "ascope"
                && req.RedirectUri == "aredirecturi"
            ),
            null, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenExchangeCodeForTokensAsyncAndReceivesOnlyAccessToken_ThenReturnsOnlyAccessToken()
    {
        _client.Setup(c => c.PostAsync(It.IsAny<ICallerContext>(), It.IsAny<OAuth2GrantAuthorizationRequest>(),
                It.IsAny<Action<HttpRequestMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OAuth2GrantAuthorizationResponse
            {
                AccessToken = "anaccesstoken",
                RefreshToken = null,
                IdToken = null,
                ExpiresIn = 60
            });
        var options = new OAuth2CodeTokenExchangeOptions("aservicename", "acode", scope: "ascope");

        var result = await _serviceClient.ExchangeCodeForTokensAsync(_caller.Object, options, CancellationToken.None);

        var now = DateTime.UtcNow.ToNearestSecond();
        var expiresOn = now.AddSeconds(60);
        result.Should().BeSuccess();
        result.Value.Count().Should().Be(1);
        result.Value[0].Type.Should().Be(TokenType.AccessToken);
        result.Value[0].Value.Should().Be("anaccesstoken");
        result.Value[0].ExpiresOn.Should().BeNear(expiresOn);
        _client.Verify(c => c.PostAsync(_caller.Object, It.Is<OAuth2GrantAuthorizationRequest>(req =>
                req.GrantType == "authorization_code"
                && req.Code == "acode"
                && req.ClientId == "aclientid"
                && req.ClientSecret == "aclientsecret"
                && req.Scope == "ascope"
                && req.RedirectUri == "aredirecturi"
            ),
            null, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenRefreshTokenAsyncAndClientThrows_ThenReturnsError()
    {
        _client.Setup(c => c.PostAsync(It.IsAny<ICallerContext>(), It.IsAny<OAuth2GrantAuthorizationRequest>(),
                It.IsAny<Action<HttpRequestMessage>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("amessage"));
        var options = new OAuth2RefreshTokenOptions("aservicename", "arefreshtoken");

        var result = await _serviceClient.RefreshTokenAsync(_caller.Object, options, CancellationToken.None);

        result.Should().BeError(ErrorCode.Unexpected, "amessage");
        _client.Verify(c => c.PostAsync(_caller.Object, It.Is<OAuth2GrantAuthorizationRequest>(req =>
                req.GrantType == "refresh_token"
                && req.ClientId == "aclientid"
                && req.ClientSecret == "aclientsecret"
                && req.RefreshToken == "arefreshtoken"
            ),
            null, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenRefreshTokenAsyncAndReceivesAllTokens_ThenReturnsAllTokens()
    {
        _client.Setup(c => c.PostAsync(It.IsAny<ICallerContext>(), It.IsAny<OAuth2GrantAuthorizationRequest>(),
                It.IsAny<Action<HttpRequestMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OAuth2GrantAuthorizationResponse
            {
                AccessToken = "anaccesstoken",
                RefreshToken = "arefreshtoken",
                IdToken = "anidtoken",
                ExpiresIn = 60
            });
        var options = new OAuth2RefreshTokenOptions("aservicename", "arefreshtoken");

        var result = await _serviceClient.RefreshTokenAsync(_caller.Object, options, CancellationToken.None);

        var now = DateTime.UtcNow.ToNearestSecond();
        var expiresOn = now.AddSeconds(60);
        result.Should().BeSuccess();
        result.Value.Count().Should().Be(3);
        result.Value[0].Type.Should().Be(TokenType.AccessToken);
        result.Value[0].Value.Should().Be("anaccesstoken");
        result.Value[0].ExpiresOn.Should().BeNear(expiresOn);
        result.Value[1].Type.Should().Be(TokenType.RefreshToken);
        result.Value[1].Value.Should().Be("arefreshtoken");
        result.Value[1].ExpiresOn.Should().BeNear(now.AddDays(1));
        result.Value[2].Type.Should().Be(TokenType.OtherToken);
        result.Value[2].Value.Should().Be("anidtoken");
        result.Value[2].ExpiresOn.Should().BeNear(now.AddHours(1));
        _client.Verify(c => c.PostAsync(_caller.Object, It.Is<OAuth2GrantAuthorizationRequest>(req =>
                req.GrantType == "refresh_token"
                && req.ClientId == "aclientid"
                && req.ClientSecret == "aclientsecret"
                && req.RefreshToken == "arefreshtoken"
            ),
            null, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenRefreshTokenAsyncAndReceivesOnlyAccessToken_ThenReturnsOnlyAccessToken()
    {
        _client.Setup(c => c.PostAsync(It.IsAny<ICallerContext>(), It.IsAny<OAuth2GrantAuthorizationRequest>(),
                It.IsAny<Action<HttpRequestMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OAuth2GrantAuthorizationResponse
            {
                AccessToken = "anaccesstoken",
                RefreshToken = null,
                IdToken = null,
                ExpiresIn = 60
            });
        var options = new OAuth2RefreshTokenOptions("aservicename", "arefreshtoken");

        var result = await _serviceClient.RefreshTokenAsync(_caller.Object, options, CancellationToken.None);

        var now = DateTime.UtcNow.ToNearestSecond();
        var expiresOn = now.AddSeconds(60);
        result.Should().BeSuccess();
        result.Value.Count().Should().Be(1);
        result.Value[0].Type.Should().Be(TokenType.AccessToken);
        result.Value[0].Value.Should().Be("anaccesstoken");
        result.Value[0].ExpiresOn.Should().BeNear(expiresOn);
        _client.Verify(c => c.PostAsync(_caller.Object, It.Is<OAuth2GrantAuthorizationRequest>(req =>
                req.GrantType == "refresh_token"
                && req.ClientId == "aclientid"
                && req.ClientSecret == "aclientsecret"
                && req.RefreshToken == "arefreshtoken"
            ),
            null, It.IsAny<CancellationToken>()));
    }
}