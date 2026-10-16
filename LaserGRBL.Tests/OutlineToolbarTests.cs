using Xunit;

namespace LaserGRBL.Tests
{
    // Settings are shared by the application, so this class runs as one collection.
    [Collection("Outline settings")]
    public class OutlineToolbarTests
    {
        [Fact]
        public void SavedDecimalSpeedIsUsedIndependentlyOfJogSpeed()
        {
            Settings.SetObject("Outline Trace Speed", 1234.5m);
            Assert.Equal(1234.5m, MainForm.GetOutlineTraceSpeed(500));
            Assert.Equal(1234.5m, MainForm.GetOutlineTraceSpeed(9000));
        }
        [Fact]
        public void LegacyIntegerSpeedIsRetained()
        {
            Settings.SetObject("Outline Trace Speed", 2500);
            Assert.Equal(2500m, MainForm.GetOutlineTraceSpeed(500));
        }
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(100001)]
        public void InvalidStoredSpeedIsNotSilentlyReplacedWithJogSpeed(int invalid)
        {
            Settings.SetObject("Outline Trace Speed", (decimal)invalid);
            Assert.Equal((decimal)invalid, MainForm.GetOutlineTraceSpeed(500));
        }
        [Fact]
        public void InvalidStoredTypeFailsClosed()
        {
            Settings.SetObject("Outline Trace Speed", "fast");
            Assert.Equal(decimal.MinValue, MainForm.GetOutlineTraceSpeed(500));
        }
    }
}
