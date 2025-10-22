using Xunit;
using FormsBackend.Models.Sql; // Example namespace from your main project

namespace FormsBackend.Tests
{
    public class SampleTests
    {
        [Fact]
        public void TestAddingNumbers()
        {
            int a = 5;
            int b = 3;
            int sum = a + b;
            Assert.Equal(8, sum);
        }
    }
}