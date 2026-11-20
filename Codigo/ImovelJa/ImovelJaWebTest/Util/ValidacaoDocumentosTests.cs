namespace Util.Tests;

[TestClass]
public class ValidacaoDocumentosTests
{
    [TestMethod]
    [DataRow("00000000000191")]
    [DataRow("00.000.000/0001-91")]
    [DataRow("00360305000104")]
    [DataRow("00.360.305/0001-04")]
    public void ValidarCnpj_QuandoCnpjValidoIniciadoEmZero_DeveRetornarVerdadeiro(string cnpj)
    {
        // Act
        var valido = Methods.ValidarCnpj(cnpj);

        // Assert
        Assert.IsTrue(valido);
    }

    [TestMethod]
    [DataRow("00000000000190")]
    [DataRow("00.000.000/0001-90")]
    public void ValidarCnpj_QuandoCnpjIniciadoEmZeroComDigitoVerificadorErrado_DeveRetornarFalso(string cnpj)
    {
        // Act
        var valido = Methods.ValidarCnpj(cnpj);

        // Assert
        Assert.IsFalse(valido);
    }

    [TestMethod]
    [DataRow("0000000000019A")]
    [DataRow("00.000.00A/0001-91")]
    public void ValidarCnpj_QuandoCnpjComLetra_DeveRetornarFalsoSemLancarExcecao(string cnpj)
    {
        // Act
        var valido = Methods.ValidarCnpj(cnpj);

        // Assert
        Assert.IsFalse(valido);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("00000000000000")]
    public void ValidarCnpj_QuandoCnpjVazioOuComDigitosIguais_DeveRetornarFalso(string cnpj)
    {
        // Act
        var valido = Methods.ValidarCnpj(cnpj);

        // Assert
        Assert.IsFalse(valido);
    }

    [TestMethod]
    [DataRow("00000000000191")]
    [DataRow("00.000.000/0001-91")]
    public void CnpjAttribute_QuandoCnpjValidoIniciadoEmZero_DeveSerValido(string cnpj)
    {
        // Arrange
        var atributo = new CnpjAttribute();

        // Act
        var valido = atributo.IsValid(cnpj);

        // Assert
        Assert.IsTrue(valido);
    }

    [TestMethod]
    [DataRow("0000000000019A")]
    [DataRow("00000000000190")]
    public void CnpjAttribute_QuandoCnpjInvalido_DeveSerInvalidoSemLancarExcecao(string cnpj)
    {
        // Arrange
        var atributo = new CnpjAttribute();

        // Act
        var valido = atributo.IsValid(cnpj);

        // Assert
        Assert.IsFalse(valido);
    }

    [TestMethod]
    [DataRow("01310100")]
    [DataRow("01310-100")]
    [DataRow("49500000")]
    public void CepAttribute_QuandoCepValido_DeveSerValido(string cep)
    {
        // Arrange
        var atributo = new CepAttribute();

        // Act
        var valido = atributo.IsValid(cep);

        // Assert
        Assert.IsTrue(valido);
    }

    [TestMethod]
    [DataRow("0131010")]
    [DataRow("013101000")]
    [DataRow("0131A100")]
    public void CepAttribute_QuandoCepSemOitoDigitos_DeveSerInvalido(string cep)
    {
        // Arrange
        var atributo = new CepAttribute();

        // Act
        var valido = atributo.IsValid(cep);

        // Assert
        Assert.IsFalse(valido);
    }
}
