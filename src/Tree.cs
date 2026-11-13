namespace HuffmanCodierung;

public class HuffmanTree
{
    public Node? Root { get; private set; }

    public HuffmanTree(Node? root)
    {
        Root = root;
    }

    public HuffmanTree(Dictionary<byte, int> freq)
    {
        Root = BuildTree(freq);
    }

    public static HuffmanTree FromDictionary(Dictionary<byte, string> codes)
    {
        if (codes.Count == 0)
        {
            return new HuffmanTree((Node?)null);
        }

        var root = new Node(0);
        foreach (var (symbol, code) in codes)
        {
            var current = root;
            foreach (char c in code)
            {
                if (c == '0')
                {
                    current.Left ??= new Node(0);
                    current = current.Left;
                }
                else
                {
                    current.Right ??= new Node(0);
                    current = current.Right;
                }
            }
            current.ByteVal = symbol;
        }

        return new HuffmanTree(root);
    }

    private Node? BuildTree(Dictionary<byte, int> freq)
    {
        var nodes = new List<Node>();
        foreach (var kvp in freq)
        {
            nodes.Add(new Node(kvp.Value, kvp.Key));
        }

        while (nodes.Count > 1)
        {
            nodes = nodes.OrderBy(n => n.Freq).ToList();
            var left = nodes[0];
            var right = nodes[1];
            var parent = new Node(
                freq: left.Freq + right.Freq,
                byteVal: null,
                left: left,
                right: right);
            nodes.Remove(left);
            nodes.Remove(right);
            nodes.Add(parent);
        }

        return nodes.FirstOrDefault();
    }

    public void GenerateCodes(Node? node, string currentCode, Dictionary<byte, string> codes)
    {
        if (node == null)
        {
            return;
        }

        if (node.ByteVal.HasValue)
        {
            codes[node.ByteVal.Value] = string.IsNullOrEmpty(currentCode) ? "0" : currentCode;
            return;
        }

        GenerateCodes(node.Left, currentCode + "0", codes);
        GenerateCodes(node.Right, currentCode + "1", codes);
    }

    public byte[] Decode(byte[] payload, long originalLength)
    {
        if (originalLength <= 0 || Root == null)
        {
            return Array.Empty<byte>();
        }

        byte[] result = new byte[originalLength];
        long outIndex = 0;

        if (Root.IsLeaf)
        {
            Array.Fill(result, Root.ByteVal!.Value);
            return result;
        }

        Node current = Root;
        for (int i = 0; i < payload.Length && outIndex < originalLength; i++)
        {
            byte b = payload[i];
            for (int bit = 7; bit >= 0 && outIndex < originalLength; bit--)
            {
                int bitVal = (b >> bit) & 1;
                current = (bitVal == 0) ? current.Left! : current.Right!;

                if (current.IsLeaf)
                {
                    result[outIndex++] = current.ByteVal!.Value;
                    current = Root;
                }
            }
        }

        return result;
    }

    public void Print()
    {
        PrintRecursive(Root, "", "");
    }

    private void PrintRecursive(Node? node, string indent, string edge)
    {
        if (node == null)
        {
            return;
        }

        string label = node.IsLeaf
            ? $"0x{node.ByteVal:X2} ('{(node.ByteVal >= 32 && node.ByteVal <= 126 ? (char)node.ByteVal : '.')}') ({node.Freq})"
            : $"[{node.Freq}]";

        Console.WriteLine(indent + edge + label);

        string childIndent = indent + "    ";
        PrintRecursive(node.Left, childIndent, "0: ");
        PrintRecursive(node.Right, childIndent, "1: ");
    }
}
