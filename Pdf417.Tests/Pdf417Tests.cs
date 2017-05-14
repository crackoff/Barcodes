using Xunit;

namespace Pdf417.Tests
{
    public class Pdf417Tests
    {
        [Fact]
        public void CreateDataStorageTests()
        {
            var r = new Barcode(new byte[] {0, 1, 2, 3}, Settings.Default);
            Assert.Equal(8, r.RowsCount);
            Assert.Equal((1 + 5) * 17 + 1, r.ColumnsCount);

            r = new Barcode(new byte[]
            {
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16
            }, Settings.Default);
            Assert.Equal(13, r.RowsCount);
            Assert.Equal((1 + 5) * 17 + 1, r.ColumnsCount);
        }
    }
}