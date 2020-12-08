using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

public class STLScaler
{

    public enum Method
    {
        None,
        SqRt,
    }

    private byte[] header;
    private UInt32 triCount;
    private Triangle[] triangles;
    private string filePath;

    public STLScaler(string filePath, Method m, int iterations, bool trimBase)
    {

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"File not found: {filePath}");
            Environment.Exit(1);
        }

        this.filePath = filePath;
        ReadContents();

        if (trimBase) TrimBase();

        Action scale_method;

        switch (m)
        {
            case Method.SqRt:
                scale_method = ScaleSqRt;
                break;
            default:
                Console.WriteLine("Skipping Scale Op");
                scale_method = () => { };
                break;
        }

        for (int i = 0; i < iterations; i++)
        {
            Console.WriteLine($"Running scale iteration {i + 1}");
            scale_method();
        }

        Write();
    }

    private void ReadContents()
    {
        using (BinaryReader reader = new BinaryReader(File.Open(this.filePath, FileMode.Open)))
        {
            header = reader.ReadBytes(80);
            triCount = reader.ReadUInt32();
            Console.WriteLine("Triangles: " + triCount);

            triangles = new Triangle[triCount];
            for (int i = 0; i < triCount; i++)
            {
                triangles[i] = new Triangle(reader.ReadBytes(50));
            }
        }
    }

    private Single FindMinZ()
    {
        Single minZ = Single.MaxValue;

        foreach (Triangle t in triangles)
        {
            minZ = MathF.Min(minZ, t.V1.Z);
            minZ = MathF.Min(minZ, t.V2.Z);
            minZ = MathF.Min(minZ, t.V3.Z);
        }
        return minZ;
    }

    private void TrimBase()
    {
        Console.WriteLine("Trimming Base layer");
        Single minZ = FindMinZ();
        List<Triangle> keep = new List<Triangle>();
        foreach (Triangle t in triangles)
        {
            if (t.V1.Z == minZ || t.V2.Z == minZ || t.V3.Z == minZ)
            {
                continue;
            }
            keep.Add(t);
        }

        Console.WriteLine("Realigning bottom to zero");
        Single zeroOffset = 0 - FindMinZ();

        foreach (Triangle t in triangles)
        {
            t.V1.Z += zeroOffset;
            t.V2.Z += zeroOffset;
            t.V3.Z += zeroOffset;
        }


        triangles = keep.ToArray();



        triCount = (UInt32)triangles.Length;
    }

    private void ScaleSqRt()
    {
        foreach (Triangle t in triangles)
        {
            t.V1.Z = MathF.Sqrt(t.V1.Z);
            t.V2.Z = MathF.Sqrt(t.V2.Z);
            t.V3.Z = MathF.Sqrt(t.V3.Z);
        }
    }

    private void Write()
    {
        string outPath = Path.GetDirectoryName(filePath) + "\\" + Path.GetFileNameWithoutExtension(filePath) + "_scaled" + Path.GetExtension(filePath);
        Console.WriteLine("Writing to: " + outPath);

        using (BinaryWriter writer = new BinaryWriter(File.Open(outPath, FileMode.CreateNew)))
        {
            writer.Write(header);
            writer.Write(triCount);
            foreach (Triangle t in triangles)
            {
                writer.Write(t.Dump());
            }
        }

    }

    private class Triangle
    {
        public Vector3 Normal;
        public Vector3 V1;
        public Vector3 V2;
        public Vector3 V3;
        public UInt16 AttributeByteCount;

        public Triangle(byte[] bytes)
        {
            if (bytes.Length != 50)
            {
                throw new System.Exception("Invalid number of bytes for triangle: " + bytes.Length);
            }

            int offset = 0;
            Single[] reals = new Single[12];
            for (int i = 0; i < reals.Length; i++)
            {
                reals[i] = BitConverter.ToSingle(bytes, offset);
                offset += 4;
            }

            Normal = new Vector3(reals[0], reals[1], reals[2]);
            V1 = new Vector3(reals[3], reals[4], reals[5]);
            V2 = new Vector3(reals[6], reals[7], reals[8]);
            V3 = new Vector3(reals[9], reals[10], reals[11]);
            AttributeByteCount = BitConverter.ToUInt16(bytes, offset);
        }

        public byte[] Dump()
        {
            List<byte> dump = new List<byte>();

            dump.AddRange(DumpVector3(Normal));
            dump.AddRange(DumpVector3(V1));
            dump.AddRange(DumpVector3(V2));
            dump.AddRange(DumpVector3(V3));
            dump.AddRange(BitConverter.GetBytes(AttributeByteCount));
            return dump.ToArray();
        }

        private byte[] DumpVector3(Vector3 v)
        {
            List<byte> r = new List<byte>();
            r.AddRange(BitConverter.GetBytes(v.X));
            r.AddRange(BitConverter.GetBytes(v.Y));
            r.AddRange(BitConverter.GetBytes(v.Z));
            return r.ToArray();
        }
    }
}