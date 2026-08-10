using System;
using System.IO;
using WemadeCrypt;

public static class DatDecodeProbe
{
    public static int Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("usage: DatDecodeProbe input.dat output.xml");
            return 2;
        }

        var input = File.ReadAllBytes(args[0]);
        // The published decoder only pads during encoding. These Mud3 server
        // files are 4 bytes short of an 8-byte block, so pad for decoding and
        // remove the same padding from the result afterwards.
        var padded = new byte[(input.Length + 7) / 8 * 8];
        Buffer.BlockCopy(input, 0, padded, 0, input.Length);
        var decoded = new WemadeCrypt.WemadeCrypt().DecodeBytes(padded);
        var output = new byte[input.Length];
        Buffer.BlockCopy(decoded, 0, output, 0, output.Length);
        File.WriteAllBytes(args[1], output);
        Console.WriteLine($"decoded {input.Length} bytes -> {output.Length} bytes");
        return 0;
    }
}
