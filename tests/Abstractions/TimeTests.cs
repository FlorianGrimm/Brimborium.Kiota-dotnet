using System;
using Xunit;

namespace Microsoft.Kiota.Abstractions.Tests
{
    public class TimeTests
    {
        [Fact]
        public void TestTimeEquality()
        {
            var time1 = new TimeOnly(10, 30, 0);
            var time2 = new TimeOnly(10, 30, 0);
            var time3 = new TimeOnly(12, 0, 0);

            Assert.Equal(time1, time2);
            Assert.NotEqual(time1, time3);
        }

        [Fact]
        public void TestTimeToString()
        {
            var time = new TimeOnly(15, 45, 30);
            var expectedString = "15:45:30";

            Assert.Equal(expectedString, time.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
