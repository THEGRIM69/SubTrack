using SubTrack.Core.Contracts;
using SubTrack.Core.Results;

namespace SubTrack.Tests.Core;

/// <summary>
/// F0-15 — Contrato de resultados: el éxito lleva valor, el fallo lleva códigos.
/// </summary>
public sealed class ResultTests
{
    [Fact]
    public void Ok_ExponeElValor()
    {
        var result = Result<int>.Ok(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Fallo_NoPermiteLeerElValor()
    {
        var result = Result<int>.Fail(ErrorCodes.CobroYaPagado, "Este cobro ya fue registrado como pagado.");

        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
        Assert.Equal(ErrorCodes.CobroYaPagado, result.Errors[0].Code);
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void Fallo_ConservaTodosLosErroresDeValidacion()
    {
        var result = Result.Fail(
        [
            new DomainError(ErrorCodes.NombreRequerido, "Debe escribir un nombre.", "Name"),
            new DomainError(ErrorCodes.ImporteInvalido, "El importe debe ser mayor que cero.", "AmountCents")
        ]);

        Assert.True(result.IsFailure);
        Assert.Equal(2, result.Errors.Count);
        Assert.Equal("Name", result.Errors[0].Field);
    }

    [Fact]
    public void SinValor_OkEsExitoso()
    {
        var result = Result.Ok();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void PagedResult_CalculaPaginasYContinuacion()
    {
        var page = new PagedResult<int>([1, 2, 3], Page: 1, PageSize: 50, TotalCount: 120);

        Assert.Equal(3, page.TotalPages);
        Assert.True(page.HasNextPage);

        var last = new PagedResult<int>([1], Page: 3, PageSize: 50, TotalCount: 120);

        Assert.False(last.HasNextPage);
    }
}
