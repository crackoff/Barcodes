using Pdf417;

namespace Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            //var barcode = new Barcode("Hello, world!\r\n[w0QweRT7$]qwertyuiop", Settings.Default);
            //var barcode = new Barcode("A", Settings.Default);
            //var barcode = new Barcode("1234567890qwertyuiopasdfghjklzxcvbnm,./234567890-!@#$%^&*()_asdfghjkp[sderftgyui2415dsf62gys123a24s65dr6ftgyuia3sw4edrftyua3s4d5fr6tg4sd57f6gtde4576rtfz15297s61f629s1f2s96267b&^TB^VB&^TGB&N^T^Tb76tbt9b^TB&6bt76tbdt12bt6tvb976tvb967b76V76RVR697V76RV976RV976rv976v976rv76rbv97rb6bvr76RGNB7T7F6BB87T68GTG768G78HYG678HYUVTRDE5ftd567yg879", Settings.Default);

            //var barcode = new Barcode(new byte[] {65}, Settings.Default);
//            var barcode = new Barcode(new byte[]
//            {
//                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
//                0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48
//            }, Settings.Default);

            //var barcode = new Barcode(new byte[] {97, 108, 99, 111, 111, 108}, Settings.Default);
            //var barcode = new Barcode(new byte[] {97, 108, 99, 111, 111, 108, 105, 113, 117, 101}, Settings.Default);

            var barcode = new Barcode("6273917032349234", Settings.Default);

            barcode.Canvas.SaveBmp(args[0]);
        }
    }
}