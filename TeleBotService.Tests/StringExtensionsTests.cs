using TeleBotService.Extensions;

namespace TeleBotService.Tests;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("", ';', 1, "")]
    [InlineData(null, ';', 1, "")]
    [InlineData("Test", ';', 1, "Test")]
    [InlineData("Test;Part2;Part3", ';', 1, "Test")]
    [InlineData("Test;Part2;Part3", ';', 2, "Part2")]
    [InlineData("Test|Part2|Part3", '|', 3, "Part3")]
    [InlineData("Test?Par:t2?Par;t3?P4?P5?Parte6?Part7?8", '?', 6, "Parte6")]
    [InlineData("Test;Part2;Part3;P4;P5;Parte6;Part7;8", ';', 4, "P4")]
    [InlineData("Test:Part2:Part3:P4:P5:Parte6:Part7:8", ':', 7, "Part7")]
    [InlineData("Test:Part2:Part3:P4:P5:Parte6:Part7:8", ':', 8, "8")]
    [InlineData("Test?Par:t2?Par;t3?P4?P5?Parte6?Part7?8", '?', 17, "")]
    public void StringExtensions_SplitInParts_ReturnsCorrectPart(string? value, char separator, int partNumber, string expectedResult)
    {
       var result = value.GetPartAt(separator, partNumber).ToString();
       Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData(";;;", ';', "|||")]
    [InlineData(";;;", ';', "", -1, true)]
    [InlineData(":CACA", ':', "CACA", -1, true)]
    [InlineData(":CACA", ':', "|CACA", -1, false)]
    [InlineData("CACA:", ':', "CACA", -1, true)]
    [InlineData("CACA:", ':', "CACA|", -1, false)]
    [InlineData(":PEDO:", ':', "PEDO", -1, true)]
    [InlineData(":1::3::5::7", ':', "1|3|5|7", -1, true)]
    [InlineData("", ';', "")]
    [InlineData(null, ';', "")]
    [InlineData("Test", ';', "Test")]
    [InlineData("Test;Part2;Part3", ';', "Test|Part2|Part3")]
    [InlineData("Test;Part2;Part3", ';', "Test;Part2;Part3", 1)]
    [InlineData("Test;Part2;Part3", ';', "Test|Part2;Part3", 2)]
    [InlineData("Test;Part2;Part3", ';', "Test|Part2|Part3", 3)]
    [InlineData("Test_Part2_Part3", '_', "Test|Part2|Part3")]
    [InlineData("Test,Part2,Part3", ',', "Test|Part2|Part3")]
    [InlineData("Test?Par:t2?Par;t3?P4?P5?Parte6?Part7?8", '?', "Test|Par:t2|Par;t3|P4|P5|Parte6|Part7|8")]
    [InlineData("Test?Par:t2?Par;t3?P4?P5?Parte6?Part7?8", '?', "Test|Par:t2|Par;t3?P4?P5?Parte6?Part7?8", 3)]
    [InlineData("Test;Part2;Part3;P4;P5;Parte6;Part7;8", ';', "Test|Part2|Part3|P4|P5|Parte6|Part7|8")]
    [InlineData("Test:Part2:Part3:P4:P5:Parte6:Part7:8", ':', "Test|Part2|Part3|P4|P5|Parte6|Part7|8")]
    [InlineData("Test:Part2:Part3:P4:P5:Parte6:Part7:8:CACA;CACA:10:11", ':', "Test|Part2|Part3|P4|P5|Parte6|Part7|8|CACA;CACA|10|11")]
    [InlineData("Test:Part2:Part3:P4:P5:Parte6:Part7:8:CACA;CACA:10:11", ':', "Test|Part2|Part3|P4|P5|Parte6|Part7:8:CACA;CACA:10:11", 7)]
    [InlineData("Test:Part2:Part3:P4:P5:Parte6:Part7:8:CACA;CACA:10:11", ':', "Test|Part2:Part3:P4:P5:Parte6:Part7:8:CACA;CACA:10:11", 2)]
    [InlineData("Test:Part2:Part3:P4:P5:Parte6:Part7:8:CACA;CACA:10:11", ':', "Test:Part2:Part3:P4:P5:Parte6:Part7:8:CACA;CACA:10:11", 1)]
    [InlineData("Test:Part2:Part3:P4:P5:Parte6:Part7:8:CACA;CACA:10:11", ':', "Test|Part2|Part3|P4|P5|Parte6|Part7|8|CACA;CACA|10|11", 0)]
    [InlineData("Test?Par:t2?Parte3?P4?P5?Parte6?Part7?8?part9", '?', "Test|Par:t2|Parte3|P4|P5|Parte6|Part7|8|part9")]
    public void StringExtensions_SplitEnumerated_EnumeratedCorrectly(string? value, char separator, string expectedResult, int count = -1, bool removeEmpty = false)
    {
        var i = 0;
        var expected = expectedResult.Split('|');
        foreach (var part in value.SplitEnumerated(separator, count, removeEmpty))
        {
            Assert.Equal(expected[i++], part.ToString());
        }
    }
}
