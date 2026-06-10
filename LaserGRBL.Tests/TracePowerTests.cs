using Xunit;

namespace LaserGRBL.Tests
{
    public class TracePowerTests
    {
        [Theory]
        [InlineData(1000, 3, 30)]
        [InlineData(255, 3, 8)]
        [InlineData(100, 3, 3)]
        public void CalculateTracePower_ReturnsRoundedPercentage(
            int maximumPower,
            int percentage,
            int expected)
        {
            Assert.Equal(expected, GrblFile.CalculateTracePower(maximumPower, percentage));
        }

        [Theory]
        [InlineData(1000, -1, 0)]
        [InlineData(1000, 101, 1000)]
        public void CalculateTracePower_ClampsPercentage(
            int maximumPower,
            int percentage,
            int expected)
        {
            Assert.Equal(expected, GrblFile.CalculateTracePower(maximumPower, percentage));
        }
    }
}
