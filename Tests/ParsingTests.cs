using Benchmarks;
using Spanification;
using Xunit;

namespace Tests
{
    public class ParsingTests
    {
        [Fact]
        public void OldAndNewWayOfParsingLineReturnSameResult()
        {
            var sut = new ParsingLineOfFloats();

            sut.Count = 100;
            sut.Setup();
            
            Assert.Equal(sut.OldWay(), sut.NewWay());
        }
        
        [Fact]
        public void OldAndNewWayOfParsingFileReturnSameResult()
        {
            var sut = new ParsingUtf8File();
            
            sut.Setup();

            Assert.Equal(sut.OldWay(), sut.NewWay());
        }
    }
}