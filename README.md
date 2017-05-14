# Barcodes
Library for .NET Core for PDF417 barcode creation

# Examples
```c#
var barcode = new Barcode("Hello, world!", Settings.Default);
barcode.Canvas.SaveBmp(args[0]);
```

```c#
var barcode = new Barcode(new byte[]
{
    0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
    0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48
}, Settings.Default);
barcode.Canvas.SaveBmp(args[0]);
```
