using System.Linq;
using Xunit;

namespace LaserGRBL.Tests
{
    public class TracePreviewParserTests
    {
        [Fact]
        public void Parse_SeparatesTravelAndLaserMoves()
        {
            string gcode = "G90\nG0 X1 Y2\nM4 S10\nG1 X3 Y4\nM5\nG0 X0 Y0";

            var segments = TracePreviewParser.Parse(gcode);

            Assert.Equal(3, segments.Count);
            Assert.False(segments[0].LaserOn);
            Assert.True(segments[1].LaserOn);
            Assert.False(segments[2].LaserOn);
        }

        [Fact]
        public void Parse_IgnoresCommandsWithoutMovement()
        {
            string gcode = "G90\nM4 S10\nM5";

            var segments = TracePreviewParser.Parse(gcode);

            Assert.Empty(segments);
        }

        [Fact]
        public void Parse_PreservesMovementOrder()
        {
            string gcode = "G90\nG0 X2 Y3\nM4 S10\nG1 X5 Y7\nG1 X8 Y9";

            var segments = TracePreviewParser.Parse(gcode);

            Assert.Equal(new[] { 2f, 5f, 8f }, segments.Select(segment => segment.End.X));
            Assert.Equal(new[] { 3f, 7f, 9f }, segments.Select(segment => segment.End.Y));
        }
    }
}
