namespace HackMaster.Web.Tests;

/// <summary>El azar que usa la aplicación da todos los valores del intervalo, extremos incluidos (FR-005, SC-003).</summary>
public class SystemRandomTests
{
    [Fact]
    public void A_d6_Gives_Every_Value_From_1_To_6_And_Nothing_Else__SC003()
    {
        var random = new SystemRandom();
        var counts = new int[8];

        for (var i = 0; i < 6000; i++)
        {
            var value = random.Next(1, 6);
            Assert.InRange(value, 1, 6);
            counts[value]++;
        }

        Assert.All(Enumerable.Range(1, 6), face => Assert.InRange(counts[face], 800, 1200));
    }

    [Fact]
    public void A_Range_Of_One_Value_Always_Gives_That_Value__FR005()
    {
        Assert.Equal(4, new SystemRandom().Next(4, 4));
    }
}
